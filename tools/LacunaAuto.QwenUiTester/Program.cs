using System.Text;
using System.Text.Json;

namespace LacunaAuto.QwenUiTester;

internal static class Program
{
    private const string FullRegressionReportFileName = "qwen-full-regression-report.json";
    private const string FocusedBackendTestProjectPath = @"test\LacunaAuto.Api.Test\LacunaAuto.Api.Test.csproj";
    private const string FocusedBackendTestCommand = $"dotnet test {FocusedBackendTestProjectPath}";

    private sealed record RuntimeTopologyEvidence(
        bool ApiRuntimeAvailable,
        string ApiRuntimeStatus,
        bool WebRuntimeAvailable,
        string WebRuntimeStatus,
        bool TopologyReady);

    private sealed record BackendTestEvidence(
        bool Executed,
        ProcessResult Result,
        string Note);

    public static async Task<int> Main(string[] args)
    {
        AppHost? apiHost = null;
        AppHost? webHost = null;

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
            var artifactDirectory = Path.Combine(e2eDirectory, "qwen-reviews");
            Directory.CreateDirectory(artifactDirectory);

            var fullRegressionReportPath = Path.Combine(
                artifactDirectory,
                FullRegressionReportFileName);

            Console.WriteLine($"Repository : {repositoryRoot}");
            Console.WriteLine($"Base       : {options.BaseRef} ({baseSha})");
            Console.WriteLine($"Head       : {options.HeadRef} ({currentHead})");
            Console.WriteLine($"Web URL    : {options.WebBaseUrl}");
            Console.WriteLine($"API URL    : {options.ApiBaseUrl}");
            Console.WriteLine($"API req    : {options.ApiRequired}");
            Console.WriteLine($"Backend    : {options.BackendRelevant}");
            Console.WriteLine($"Postgres   : {options.PostgresBootstrapStatus}");
            Console.WriteLine($"STRATA     : {options.ChatCompletionsUrl}");
            Console.WriteLine($"Auth       : {(string.IsNullOrWhiteSpace(options.ApiKey) ? "none" : "Authorization: ******")}");
            Console.WriteLine($"Reasoning  : {options.ReasoningEffort}");
            Console.WriteLine($"Model      : {options.Model}");
            Console.WriteLine();

            var apiRuntime = await EnsureApiRuntimeAsync(options, repositoryRoot);
            apiHost = apiRuntime.Host;

            (AppHost? Host, bool Available, string Status) webRuntime;
            if (apiRuntime.Available || !options.ApiRequired)
            {
                webRuntime = await EnsureWebRuntimeAsync(options, repositoryRoot);
                webHost = webRuntime.Host;
            }
            else
            {
                webRuntime = (
                    Host: null,
                    Available: false,
                    Status: "Skipped because required API runtime was unavailable.");
            }

            var topologyEvidence = new RuntimeTopologyEvidence(
                ApiRuntimeAvailable: apiRuntime.Available,
                ApiRuntimeStatus: apiRuntime.Status,
                WebRuntimeAvailable: webRuntime.Available,
                WebRuntimeStatus: webRuntime.Status,
                TopologyReady: webRuntime.Available && (apiRuntime.Available || !options.ApiRequired));

            Console.WriteLine($"API runtime: {topologyEvidence.ApiRuntimeStatus}");
            Console.WriteLine($"Web runtime: {topologyEvidence.WebRuntimeStatus}");
            Console.WriteLine($"Topology   : {(topologyEvidence.TopologyReady ? "ready" : "unavailable")}");
            Console.WriteLine();

            var regression = topologyEvidence.TopologyReady
                ? options.SkipRegression
                    ? new ProcessResult(
                        ExitCode: 0,
                        StandardOutput: "Regression run skipped by --skip-regression.",
                        StandardError: string.Empty,
                        TimedOut: false)
                    : await RunFullRegressionAsync(e2eDirectory, options.WebBaseUrl)
                : new ProcessResult(
                    ExitCode: -1,
                    StandardOutput: "Regression run skipped because required runtime topology was unavailable.",
                    StandardError: $"API runtime: {topologyEvidence.ApiRuntimeStatus}\nWeb runtime: {topologyEvidence.WebRuntimeStatus}",
                    TimedOut: false);

            if (topologyEvidence.TopologyReady && !options.SkipRegression)
                PreserveFullRegressionReport(e2eDirectory, fullRegressionReportPath);

            Console.WriteLine($"Regression : exit {regression.ExitCode}, timeout={regression.TimedOut}");

            var backendTestEvidence = await CollectBackendTestEvidenceAsync(
                repositoryRoot,
                options.BackendRelevant);

            Console.WriteLine($"Backend test executed: {backendTestEvidence.Executed}");
            if (backendTestEvidence.Executed)
            {
                Console.WriteLine($"Backend test exit    : {backendTestEvidence.Result.ExitCode}, timeout={backendTestEvidence.Result.TimedOut}");
            }
            else
            {
                Console.WriteLine($"Backend test status  : {backendTestEvidence.Note}");
            }

            if (options.RegressionOnly)
            {
                Console.WriteLine();
                Console.WriteLine("Regression output:");
                Console.WriteLine("----------------------------------------");
                Console.WriteLine(Limit(regression.CombinedOutput, 40_000).TrimEnd());
                Console.WriteLine("----------------------------------------");

                if (options.ApiRequired && !topologyEvidence.TopologyReady)
                    return 3;

                return regression.ExitCode == 0 && !regression.TimedOut ? 0 : 2;
            }

            if (regression.ExitCode != 0 || regression.TimedOut)
            {
                Console.WriteLine();
                Console.WriteLine("Regression failure output:");
                Console.WriteLine("----------------------------------------");
                Console.WriteLine(Limit(regression.CombinedOutput, 40_000).TrimEnd());
                Console.WriteLine("----------------------------------------");
                Console.WriteLine();
            }

            if (backendTestEvidence.Executed &&
                (backendTestEvidence.Result.ExitCode != 0 || backendTestEvidence.Result.TimedOut))
            {
                Console.WriteLine();
                Console.WriteLine("Focused backend test output:");
                Console.WriteLine("----------------------------------------");
                Console.WriteLine(Limit(backendTestEvidence.Result.CombinedOutput, 40_000).TrimEnd());
                Console.WriteLine("----------------------------------------");
                Console.WriteLine();
            }

            var tools = new ToolRegistry(
                repositoryRoot,
                options.BaseRef,
                options.HeadRef,
                options.WebBaseUrl,
                options.ApiBaseUrl,
                topologyEvidence.ApiRuntimeAvailable,
                fullRegressionReportPath);

            var systemPrompt = BuildSystemPrompt(repositoryRoot);
            var userPrompt = BuildUserPrompt(
                options,
                baseSha,
                currentHead,
                workingTreeDirty,
                diffStat,
                topologyEvidence,
                backendTestEvidence,
                regression);

            string modelReview;
            if (options.ApiRequired && !topologyEvidence.TopologyReady)
            {
                modelReview = BuildTopologyBlockedReview(
                    topologyEvidence,
                    backendTestEvidence,
                    regression,
                    options.SkipRegression);
            }
            else
            {
                using var agent = new StrataAgent(options, tools);
                modelReview = await agent.RunAsync(systemPrompt, userPrompt);
            }

            var modelStatus = ParseModelStatus(modelReview);
            var finalStatus = ResolveFinalStatus(
                modelStatus,
                regression,
                options.SkipRegression,
                topologyEvidence,
                backendTestEvidence,
                options.ApiRequired);

            var reviewPath = Path.Combine(artifactDirectory, "qwen-ui-review.md");
            var metadataPath = Path.Combine(artifactDirectory, "qwen-ui-review.json");

            var wrappedReview = BuildWrappedReview(
                options,
                baseSha,
                currentHead,
                workingTreeDirty,
                topologyEvidence,
                backendTestEvidence,
                regression,
                finalStatus,
                modelReview);

            await File.WriteAllTextAsync(
                reviewPath,
                wrappedReview,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
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
                        webBaseUrl = options.WebBaseUrl,
                        apiBaseUrl = options.ApiBaseUrl,
                        apiRequired = options.ApiRequired,
                        apiRuntimeAvailable = topologyEvidence.ApiRuntimeAvailable,
                        apiRuntimeStatus = topologyEvidence.ApiRuntimeStatus,
                        webRuntimeAvailable = topologyEvidence.WebRuntimeAvailable,
                        webRuntimeStatus = topologyEvidence.WebRuntimeStatus,
                        topologyReady = topologyEvidence.TopologyReady,
                        postgresBootstrapStatus = options.PostgresBootstrapStatus,
                        regressionExitCode = regression.ExitCode,
                        regressionTimedOut = regression.TimedOut,
                        regressionSkipped = options.SkipRegression,
                        backendRelevant = options.BackendRelevant,
                        focusedBackendTestsExecuted = backendTestEvidence.Executed,
                        focusedBackendTestsExitCode = backendTestEvidence.Executed ? backendTestEvidence.Result.ExitCode : (int?)null,
                        focusedBackendTestsTimedOut = backendTestEvidence.Executed ? backendTestEvidence.Result.TimedOut : (bool?)null,
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
        finally
        {
            if (webHost is not null)
                await webHost.DisposeAsync();

            if (apiHost is not null)
                await apiHost.DisposeAsync();
        }
    }

    private static async Task<(AppHost? Host, bool Available, string Status)> EnsureApiRuntimeAsync(
        RunnerOptions options,
        string repositoryRoot)
    {
        try
        {
            var host = await AppHost.EnsureApiRunningAsync(
                repositoryRoot,
                options.ApiBaseUrl,
                options.ReuseRunningApi);

            var status = host.ReusedExistingServer
                ? "Available (reused explicitly authorized running API server)."
                : "Available (runner started LacunaAuto.Api).";

            return (host, true, status);
        }
        catch (Exception exception)
        {
            return (null, false, $"{exception.GetType().Name}: {exception.Message}");
        }
    }

    private static async Task<(AppHost? Host, bool Available, string Status)> EnsureWebRuntimeAsync(
        RunnerOptions options,
        string repositoryRoot)
    {
        try
        {
            var host = await AppHost.EnsureWebRunningAsync(
                repositoryRoot,
                options.WebBaseUrl,
                options.ReuseRunningApp);

            var status = host.ReusedExistingServer
                ? "Available (reused explicitly authorized running Web server)."
                : "Available (runner started LacunaAuto.Web).";

            return (host, true, status);
        }
        catch (Exception exception)
        {
            return (null, false, $"{exception.GetType().Name}: {exception.Message}");
        }
    }

    private static async Task<BackendTestEvidence> CollectBackendTestEvidenceAsync(
        string repositoryRoot,
        bool backendRelevant)
    {
        if (!backendRelevant)
        {
            return new BackendTestEvidence(
                Executed: false,
                Result: new ProcessResult(
                    ExitCode: 0,
                    StandardOutput: "Focused backend tests were skipped because backend-relevant diff classification is false.",
                    StandardError: string.Empty,
                    TimedOut: false),
                Note: "Skipped because backend-relevant diff classification is false.");
        }

        Console.WriteLine("Running focused backend test project...");
        var result = await ProcessRunner.RunAsync(
            "dotnet",
            ["test", FocusedBackendTestProjectPath],
            repositoryRoot,
            TimeSpan.FromMinutes(6));

        return new BackendTestEvidence(
            Executed: true,
            Result: result,
            Note: $"Executed: {FocusedBackendTestCommand}");
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
        Console.WriteLine("Auth       : Authorization: ******");
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
        string webBaseUrl)
    {
        Console.WriteLine("Running full Playwright regression suite...");

        var playwright = ProcessRunner.ResolveLocalNodeTool(
            e2eDirectory,
            "playwright");

        return await ProcessRunner.RunAsync(
            playwright,
            ["test"],
            e2eDirectory,
            TimeSpan.FromMinutes(8),
            new Dictionary<string, string?>
            {
                ["E2E_BASE_URL"] = webBaseUrl,
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
- The HTTP tool requires an explicit target (`web` or `api`); never treat `/api/*` on `web` as API evidence.
- If required runtime topology is unavailable, classify as BLOCKED and clearly list what was not verified.

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
        RuntimeTopologyEvidence topologyEvidence,
        BackendTestEvidence backendTestEvidence,
        ProcessResult regression)
    {
        var regressionOutput = Limit(regression.CombinedOutput, 20_000);
        var backendOutput = backendTestEvidence.Executed
            ? Limit(backendTestEvidence.Result.CombinedOutput, 12_000)
            : backendTestEvidence.Note;

        return $"""
Perform an independent UI automation review of the exact revision below.

Base ref: {options.BaseRef}
Base SHA: {baseSha}
Head ref: {options.HeadRef}
Tested SHA: {testedSha}
Working tree dirty: {workingTreeDirty}

Web origin: {options.WebBaseUrl}
Web runtime available: {topologyEvidence.WebRuntimeAvailable}
Web runtime status: {topologyEvidence.WebRuntimeStatus}

API origin: {options.ApiBaseUrl}
API runtime required: {options.ApiRequired}
API runtime available: {topologyEvidence.ApiRuntimeAvailable}
API runtime status: {topologyEvidence.ApiRuntimeStatus}
PostgreSQL bootstrap status: {options.PostgresBootstrapStatus}

Authoritative topology ready: {topologyEvidence.TopologyReady}
Backend-relevant diff classification: {options.BackendRelevant}

Requested scope:
{options.Scope}

Acceptance criteria:
{options.Acceptance}

Changed-file summary:
{diffStat}

The runner already attempted runtime setup and regression/test evidence collection before contacting you.

Regression exit code: {regression.ExitCode}
Regression timed out: {regression.TimedOut}
Regression was skipped by flag: {options.SkipRegression}

Focused backend test command: {FocusedBackendTestCommand}
Focused backend tests executed: {backendTestEvidence.Executed}
Focused backend tests exit code: {(backendTestEvidence.Executed ? backendTestEvidence.Result.ExitCode : null)}
Focused backend tests timed out: {(backendTestEvidence.Executed ? backendTestEvidence.Result.TimedOut : null)}

Regression command output:
<REGRESSION_OUTPUT>
{regressionOutput}
</REGRESSION_OUTPUT>

Focused backend test output:
<FOCUSED_BACKEND_TEST_OUTPUT>
{backendOutput}
</FOCUSED_BACKEND_TEST_OUTPUT>

Use the available tools to inspect the locked diff, relevant source/specs, preserved regression report, explicit web/api HTTP behavior, and focused existing Playwright specs when useful.

Do not broaden the task into unrelated implementation work.
Do not treat the existing regression suite as sufficient proof for new behavior if the diff introduces behavior the suite does not cover.
Because this runner version cannot author new adaptive probes, explicitly list any behavior that still requires a new test or human visual verification.
Do not infer API behavior from web-target responses; use explicit target-aware tool calls.
If required runtime topology is unavailable for material claims, classify that gap as BLOCKED/not verified rather than product FAIL from wrong-process probing.

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

    private static string BuildTopologyBlockedReview(
        RuntimeTopologyEvidence topologyEvidence,
        BackendTestEvidence backendTestEvidence,
        ProcessResult regression,
        bool regressionSkipped)
    {
        return $"""
Result: BLOCKED

## Regression
- Required runtime topology was unavailable, so authoritative regression verification could not be completed.
- Regression exit code: `{regression.ExitCode}`
- Regression timed out: `{regression.TimedOut}`
- Regression skipped by flag: `{regressionSkipped}`

## Verified
- API runtime required: `True`
- API runtime available: `{topologyEvidence.ApiRuntimeAvailable}`
- API runtime status: `{topologyEvidence.ApiRuntimeStatus}`
- Web runtime available: `{topologyEvidence.WebRuntimeAvailable}`
- Web runtime status: `{topologyEvidence.WebRuntimeStatus}`
- Focused backend tests executed: `{backendTestEvidence.Executed}`
- Focused backend tests exit code: `{(backendTestEvidence.Executed ? backendTestEvidence.Result.ExitCode : null)}`
- Focused backend tests timed out: `{(backendTestEvidence.Executed ? backendTestEvidence.Result.TimedOut : null)}`

## Findings
- The required PostgreSQL/API/Web topology was not fully available, so the review cannot make authoritative runtime conclusions for this SHA.

## Not verified
- Runtime behavior requiring the required API+Web topology.
- Any behavior that depends on a successful authoritative full Playwright regression run on this topology.

## Recommendation
Restore the required topology and rerun the review on the same tested SHA.
""";
    }

    private static string BuildWrappedReview(
        RunnerOptions options,
        string baseSha,
        string testedSha,
        bool workingTreeDirty,
        RuntimeTopologyEvidence topologyEvidence,
        BackendTestEvidence backendTestEvidence,
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
- Web origin: `{options.WebBaseUrl}`
- API origin: `{options.ApiBaseUrl}`
- API runtime required: `{options.ApiRequired}`
- API runtime available: `{topologyEvidence.ApiRuntimeAvailable}`
- API runtime status: `{topologyEvidence.ApiRuntimeStatus}`
- PostgreSQL bootstrap status: `{options.PostgresBootstrapStatus}`
- Web runtime available: `{topologyEvidence.WebRuntimeAvailable}`
- Web runtime status: `{topologyEvidence.WebRuntimeStatus}`
- Topology ready: `{topologyEvidence.TopologyReady}`
- Regression exit code: `{regression.ExitCode}`
- Regression timed out: `{regression.TimedOut}`
- Regression skipped: `{options.SkipRegression}`
- Backend-relevant diff classification: `{options.BackendRelevant}`
- Focused backend tests executed: `{backendTestEvidence.Executed}`
- Focused backend tests exit code: `{(backendTestEvidence.Executed ? backendTestEvidence.Result.ExitCode : null)}`
- Focused backend tests timed out: `{(backendTestEvidence.Executed ? backendTestEvidence.Result.TimedOut : null)}`
- Working tree dirty: `{workingTreeDirty}`
- Generated UTC: `{DateTimeOffset.UtcNow:O}`

## Model review

{modelReview.Trim()}
""";
    }

    private static string ParseModelStatus(string modelReview)
    {
        var firstNonEmptyLine = modelReview
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .FirstOrDefault(line => line.Length > 0);

        return firstNonEmptyLine switch
        {
            "Result: PASS" => "PASS",
            "Result: FAIL" => "FAIL",
            "Result: BLOCKED" => "BLOCKED",
            _ => "BLOCKED",
        };
    }

    private static string ResolveFinalStatus(
        string modelStatus,
        ProcessResult regression,
        bool regressionSkipped,
        RuntimeTopologyEvidence topologyEvidence,
        BackendTestEvidence backendTestEvidence,
        bool apiRequired)
    {
        if (apiRequired && !topologyEvidence.TopologyReady)
            return "BLOCKED";

        if (backendTestEvidence.Executed &&
            (backendTestEvidence.Result.ExitCode != 0 || backendTestEvidence.Result.TimedOut) &&
            string.Equals(modelStatus, "PASS", StringComparison.Ordinal))
        {
            return "BLOCKED";
        }

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
