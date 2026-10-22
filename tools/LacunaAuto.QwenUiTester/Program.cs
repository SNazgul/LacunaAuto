using System.Text.Json;
using System.Text.RegularExpressions;

namespace LacunaAuto.QwenUiTester;

internal static class Program
{
    private const string FullRegressionReportFileName = "qwen-full-regression-report.json";

    public static async Task<int> Main(string[] args)
    {
        try
        {
            var options = RunnerOptions.Parse(args);

            if (options.StrataSmoke)
                return await RunStrataSmokeAsync(options);

            var repositoryRoot = FindRepositoryRoot(Environment.CurrentDirectory);

            var currentHead = await GitAsync(repositoryRoot, "rev-parse", "HEAD");
            var requestedHead = await GitAsync(repositoryRoot, "rev-parse", options.HeadRef);

            if (!string.Equals(currentHead, requestedHead, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"The checked-out revision ({currentHead}) does not match --head {options.HeadRef} ({requestedHead}). " +
                    "Checkout the exact revision under review before running the tester.");
            }

            var status = await GitAsync(repositoryRoot, "status", "--porcelain");
            var workingTreeDirty = !string.IsNullOrWhiteSpace(status);

            if (workingTreeDirty && !options.AllowDirty)
            {
                throw new InvalidOperationException(
                    "The working tree is not clean. Commit/stash changes before testing, " +
                    "or use --allow-dirty only for an explicitly non-authoritative local probe.");
            }

            var baseSha = await GitAsync(repositoryRoot, "rev-parse", options.BaseRef);
            var diffStat = await GitAsync(
                repositoryRoot,
                "diff",
                "--stat",
                $"{options.BaseRef}...{options.HeadRef}");

            var e2eDirectory = Path.Combine(repositoryRoot, "e2e");
            var artifactDirectory = Path.Combine(e2eDirectory, "e2e-artifacts");
            Directory.CreateDirectory(artifactDirectory);

            var fullRegressionReportPath = Path.Combine(
                artifactDirectory,
                FullRegressionReportFileName);

            Console.WriteLine($"Repository : {repositoryRoot}");
            Console.WriteLine($"Base       : {options.BaseRef} ({baseSha})");
            Console.WriteLine($"Head       : {options.HeadRef} ({currentHead})");
            Console.WriteLine($"App URL    : {options.AppBaseUrl}");
            Console.WriteLine($"STRATA     : {options.ChatCompletionsUrl}");
            Console.WriteLine($"Auth       : {(string.IsNullOrWhiteSpace(options.ApiKey) ? "none" : "Authorization: Bearer <configured>")}");
            Console.WriteLine($"Reasoning  : {options.ReasoningEffort}");
            Console.WriteLine($"Model      : {options.Model}");
            Console.WriteLine();

            await using var appHost = await AppHost.EnsureRunningAsync(
                repositoryRoot,
                options.AppBaseUrl,
                options.ReuseRunningApp);

            Console.WriteLine(
                appHost.ReusedExistingServer
                    ? "Application: reusing explicitly authorized running server."
                    : "Application: runner started LacunaAuto.Web.");

            var regression = options.SkipRegression
                ? new ProcessResult(
                    ExitCode: 0,
                    StandardOutput: "Regression run skipped by --skip-regression.",
                    StandardError: string.Empty,
                    TimedOut: false)
                : await RunFullRegressionAsync(e2eDirectory, options.AppBaseUrl);

            if (!options.SkipRegression)
                PreserveFullRegressionReport(e2eDirectory, fullRegressionReportPath);

            Console.WriteLine($"Regression : exit {regression.ExitCode}, timeout={regression.TimedOut}");

            var tools = new ToolRegistry(
                repositoryRoot,
                options.BaseRef,
                options.HeadRef,
                options.AppBaseUrl,
                fullRegressionReportPath);

            var systemPrompt = BuildSystemPrompt(repositoryRoot);
            var userPrompt = BuildUserPrompt(
                options,
                baseSha,
                currentHead,
                workingTreeDirty,
                diffStat,
                regression);

            using var agent = new StrataAgent(options, tools);
            var modelReview = await agent.RunAsync(systemPrompt, userPrompt);

            var modelStatus = ParseModelStatus(modelReview);
            var finalStatus = ResolveFinalStatus(
                modelStatus,
                regression,
                options.SkipRegression);

            var reviewPath = Path.Combine(artifactDirectory, "qwen-ui-review.md");
            var metadataPath = Path.Combine(artifactDirectory, "qwen-ui-review.json");

            var wrappedReview = BuildWrappedReview(
                options,
                baseSha,
                currentHead,
                workingTreeDirty,
                regression,
                finalStatus,
                modelReview);

            await File.WriteAllTextAsync(reviewPath, wrappedReview);
            await File.WriteAllTextAsync(
                metadataPath,
                JsonSerializer.Serialize(
                    new
                    {
                        result = finalStatus,
                        testedSha = currentHead,
                        baseRef = options.BaseRef,
                        baseSha,
                        headRef = options.HeadRef,
                        model = options.Model,
                        chatCompletionsUrl = options.ChatCompletionsUrl,
                        reasoningEffort = options.ReasoningEffort,
                        appBaseUrl = options.AppBaseUrl,
                        regressionExitCode = regression.ExitCode,
                        regressionTimedOut = regression.TimedOut,
                        regressionSkipped = options.SkipRegression,
                        workingTreeDirty,
                        generatedAtUtc = DateTimeOffset.UtcNow,
                        reviewPath,
                    },
                    new JsonSerializerOptions
                    {
                        WriteIndented = true,
                    }));

            Console.WriteLine();
            Console.WriteLine($"Result     : {finalStatus}");
            Console.WriteLine($"Review     : {reviewPath}");
            Console.WriteLine($"Metadata   : {metadataPath}");

            return finalStatus switch
            {
                "PASS" => 0,
                "FAIL" => 2,
                _ => 3,
            };
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine($"Qwen UI Tester failed: {exception.Message}");
            return 10;
        }
    }

    private static async Task<int> RunStrataSmokeAsync(RunnerOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new InvalidOperationException(
                "STRATA_API_KEY is not configured. Set it in the process environment before running --strata-smoke.");
        }

        Console.WriteLine("STRATA smoke test");
        Console.WriteLine($"Endpoint   : {options.ChatCompletionsUrl}");
        Console.WriteLine($"Model      : {options.Model}");
        Console.WriteLine($"Reasoning  : {options.ReasoningEffort}");
        Console.WriteLine("Auth       : Authorization: Bearer <configured>");
        Console.WriteLine();

        using var agent = new StrataAgent(options);
        var response = await agent.RunSmokeAsync();

        Console.WriteLine("Transport  : OK");
        Console.WriteLine("Auth       : OK");
        Console.WriteLine("Tool call  : OK");
        Console.WriteLine("Tool result: OK");
        Console.WriteLine($"Final      : {response}");

        return 0;
    }

    private static async Task<ProcessResult> RunFullRegressionAsync(
        string e2eDirectory,
        string appBaseUrl)
    {
        Console.WriteLine("Running full Playwright regression suite...");

        return await ProcessRunner.RunAsync(
            "npm",
            ["test"],
            e2eDirectory,
            TimeSpan.FromMinutes(8),
            new Dictionary<string, string?>
            {
                ["E2E_BASE_URL"] = appBaseUrl,
            });
    }

    private static void PreserveFullRegressionReport(
        string e2eDirectory,
        string fullRegressionReportPath)
    {
        var sourcePath = Path.Combine(e2eDirectory, "e2e-report.json");

        if (!File.Exists(sourcePath))
            return;

        File.Copy(sourcePath, fullRegressionReportPath, overwrite: true);
    }

    private static string BuildSystemPrompt(string repositoryRoot)
    {
        var agents = ReadRequiredFile(repositoryRoot, "AGENTS.md");
        var uiAutomationSkill = ReadRequiredFile(
            repositoryRoot,
            ".github/skills/strata-qwen-coder-ui-automation/SKILL.md");
        var verificationSkill = ReadRequiredFile(
            repositoryRoot,
            ".github/skills/strata-qwen-coder-verification/SKILL.md");

        return $"""
You are the independent local UI automation tester for LacunaAuto.

Active model identity: Strata Qwen Coder.
Execution context: LacunaAuto local Qwen UI Tester runner/API.

This identity and execution context are supplied explicitly by the runner, so the two Strata Qwen Coder skills below are active.

You are a tester/reviewer, not an implementation agent.
- Do not modify application source, tests, project files, documentation, Git state, or configuration.
- Use only the provided read-only/verification tools.
- Existing Playwright tests may be executed, but you cannot create or edit tests in this runner version.
- Never claim visual inspection merely because a screenshot or DOM exists.
- Distinguish automated browser/layout evidence from human visual acceptance.
- A failed regression run prevents PASS.
- Report product defects, test defects, environment blockers, and anything not verified separately.
- Keep conclusions tied to the exact tested SHA.

Repository rules:

<AGENTS>
{agents}
</AGENTS>

Active UI automation skill:

<UI_AUTOMATION_SKILL>
{uiAutomationSkill}
</UI_AUTOMATION_SKILL>

Active verification skill:

<VERIFICATION_SKILL>
{verificationSkill}
</VERIFICATION_SKILL>
""";
    }

    private static string BuildUserPrompt(
        RunnerOptions options,
        string baseSha,
        string testedSha,
        bool workingTreeDirty,
        string diffStat,
        ProcessResult regression)
    {
        var regressionOutput = Limit(regression.CombinedOutput, 20_000);

        return $"""
Perform an independent UI automation review of the exact revision below.

Base ref: {options.BaseRef}
Base SHA: {baseSha}
Head ref: {options.HeadRef}
Tested SHA: {testedSha}
Working tree dirty: {workingTreeDirty}
Application origin: {options.AppBaseUrl}

Requested scope:
{options.Scope}

Acceptance criteria:
{options.Acceptance}

Changed-file summary:
{diffStat}

The runner already executed the normal Playwright regression suite before contacting you.

Regression exit code: {regression.ExitCode}
Regression timed out: {regression.TimedOut}
Regression was skipped: {options.SkipRegression}

Regression command output:
<REGRESSION_OUTPUT>
{regressionOutput}
</REGRESSION_OUTPUT>

Use the available tools to inspect the locked diff, relevant source/specs, the preserved full regression JSON report, HTTP behavior, and focused existing Playwright specs when useful.

Do not broaden the task into unrelated implementation work.
Do not treat the existing regression suite as sufficient proof for new behavior if the diff introduces behavior the suite does not cover.
Because this runner version cannot author new adaptive probes, explicitly list any behavior that still requires a new test or human visual verification.

Your final response MUST be Markdown and its first non-empty line MUST be exactly one of:
Result: PASS
Result: FAIL
Result: BLOCKED

Then include these sections:
## Regression
## Verified
## Findings
## Not verified
## Recommendation

For each material finding include trigger, expected behavior, observed behavior, classification, and evidence.
A PASS means no material defect was found within the verified scope and the normal regression run passed.
""";
    }

    private static string BuildWrappedReview(
        RunnerOptions options,
        string baseSha,
        string testedSha,
        bool workingTreeDirty,
        ProcessResult regression,
        string finalStatus,
        string modelReview)
    {
        return $"""
# Strata Qwen UI Review

- Result: **{finalStatus}**
- Tested SHA: `{testedSha}`
- Base: `{options.BaseRef}` (`{baseSha}`)
- Model: `{options.Model}`
- Reasoning effort: `{options.ReasoningEffort}`
- STRATA endpoint: `{options.ChatCompletionsUrl}`
- Application origin: `{options.AppBaseUrl}`
- Regression exit code: `{regression.ExitCode}`
- Regression timed out: `{regression.TimedOut}`
- Regression skipped: `{options.SkipRegression}`
- Working tree dirty: `{workingTreeDirty}`
- Generated UTC: `{DateTimeOffset.UtcNow:O}`

## Model review

{modelReview.Trim()}
""";
    }

    private static string ParseModelStatus(string modelReview)
    {
        var match = Regex.Match(
            modelReview,
            @"(?im)^Result:\s*(PASS|FAIL|BLOCKED)\s*$");

        return match.Success
            ? match.Groups[1].Value.ToUpperInvariant()
            : "BLOCKED";
    }

    private static string ResolveFinalStatus(
        string modelStatus,
        ProcessResult regression,
        bool regressionSkipped)
    {
        if (!regressionSkipped &&
            (regression.ExitCode != 0 || regression.TimedOut) &&
            string.Equals(modelStatus, "PASS", StringComparison.Ordinal))
        {
            return "BLOCKED";
        }

        return modelStatus;
    }

    private static async Task<string> GitAsync(
        string repositoryRoot,
        params string[] arguments)
    {
        var result = await ProcessRunner.RunAsync(
            "git",
            arguments,
            repositoryRoot,
            TimeSpan.FromSeconds(60));

        if (result.ExitCode != 0 || result.TimedOut)
        {
            throw new InvalidOperationException(
                $"git {string.Join(' ', arguments)} failed.\n{result.CombinedOutput}");
        }

        return result.StandardOutput.Trim();
    }

    private static string FindRepositoryRoot(string startPath)
    {
        var directory = new DirectoryInfo(Path.GetFullPath(startPath));

        while (directory is not null)
        {
            var gitPath = Path.Combine(directory.FullName, ".git");
            var agentsPath = Path.Combine(directory.FullName, "AGENTS.md");

            if ((Directory.Exists(gitPath) || File.Exists(gitPath)) &&
                File.Exists(agentsPath))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate the LacunaAuto repository root from the current directory.");
    }

    private static string ReadRequiredFile(
        string repositoryRoot,
        string relativePath)
    {
        var fullPath = Path.Combine(
            repositoryRoot,
            relativePath.Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Required runner context file is missing: {relativePath}");

        return File.ReadAllText(fullPath);
    }

    private static string Limit(string value, int maxCharacters)
    {
        if (value.Length <= maxCharacters)
            return value;

        return value[..maxCharacters] +
            $"\n\n[TRUNCATED by runner after {maxCharacters} characters]";
    }
}
