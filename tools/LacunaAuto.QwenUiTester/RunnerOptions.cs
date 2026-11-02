namespace LacunaAuto.QwenUiTester;

internal sealed record RunnerOptions(
    string BaseRef,
    string HeadRef,
    string Scope,
    string Acceptance,
    string WebBaseUrl,
    string ApiBaseUrl,
    bool ApiRequired,
    bool BackendRelevant,
    string PostgresBootstrapStatus,
    string ChatCompletionsUrl,
    string Model,
    string? ApiKey,
    string ReasoningEffort,
    int MaxTokens,
    int MaxToolTurns,
    bool SkipRegression,
    bool AllowDirty,
    bool ReuseRunningApp,
    bool ReuseRunningApi,
    bool StrataSmoke,
    bool RegressionOnly)
{
    private const string DefaultWebBaseUrl = "http://127.0.0.1:5132/";
    private const string DefaultApiBaseUrl = "http://127.0.0.1:5133/";
    private const string DefaultModel = "qwen3.8-flash-next-coder-iq1_m";
    private const string DefaultReasoningEffort = "high";

    public static RunnerOptions Parse(string[] args)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var flags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];
            if (!argument.StartsWith("--", StringComparison.Ordinal))
                throw new ArgumentException($"Unexpected argument: {argument}");

            var name = argument[2..];
            if (name is "skip-regression" or "allow-dirty" or "reuse-running-app" or
                "reuse-running-api" or "strata-smoke" or "regression-only" or
                "api-required" or "api-optional" or "backend-relevant")
            {
                flags.Add(name);
                continue;
            }

            if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
                throw new ArgumentException($"Missing value for --{name}.");

            values[name] = args[++index];
        }

        var maxTokens = ParsePositiveInt(
            Get(values, "max-tokens", Environment.GetEnvironmentVariable("QWEN_UI_MAX_TOKENS") ?? "48000"),
            "max-tokens");

        var maxToolTurns = ParsePositiveInt(
            Get(values, "max-tool-turns", Environment.GetEnvironmentVariable("QWEN_UI_MAX_TOOL_TURNS") ?? "16"),
            "max-tool-turns");

        var chatCompletionsUrl = GetRequired(
            values,
            "strata-url",
            Environment.GetEnvironmentVariable("STRATA_CHAT_COMPLETIONS_URL"));

        if (!Uri.TryCreate(chatCompletionsUrl, UriKind.Absolute, out var endpointUri) ||
            endpointUri.Scheme is not ("http" or "https"))
        {
            throw new ArgumentException(
                "--strata-url / STRATA_CHAT_COMPLETIONS_URL must be an absolute HTTP(S) URL to the full chat-completions endpoint.");
        }

        var reasoningEffort = Get(
            values,
            "reasoning-effort",
            Environment.GetEnvironmentVariable("STRATA_REASONING_EFFORT") ?? DefaultReasoningEffort);

        if (reasoningEffort is not ("none" or "low" or "medium" or "high"))
        {
            throw new ArgumentException(
                "--reasoning-effort must be one of: none, low, medium, high.");
        }

        var apiRequired = flags.Contains("api-required")
            ? true
            : flags.Contains("api-optional")
                ? false
                : ParseBoolean(
                    Get(values, "api-required", Environment.GetEnvironmentVariable("QWEN_API_REQUIRED") ?? "true"),
                    "api-required");

        var backendRelevant = flags.Contains("backend-relevant")
            ? true
            : ParseBoolean(
                Get(values, "backend-relevant", Environment.GetEnvironmentVariable("QWEN_BACKEND_RELEVANT") ?? "false"),
                "backend-relevant");

        return new RunnerOptions(
            BaseRef: Get(values, "base", "main"),
            HeadRef: Get(values, "head", "HEAD"),
            Scope: Get(values, "scope", "Review the current UI implementation changes."),
            Acceptance: Get(values, "acceptance", "Run the relevant regression coverage, inspect the changed UI behavior, and report only evidence-backed conclusions."),
            WebBaseUrl: EnsureTrailingSlash(Get(
                values,
                "web-base-url",
                Get(values, "base-url", Environment.GetEnvironmentVariable("E2E_BASE_URL") ?? DefaultWebBaseUrl))),
            ApiBaseUrl: EnsureTrailingSlash(Get(values, "api-base-url", Environment.GetEnvironmentVariable("QWEN_API_BASE_URL") ?? DefaultApiBaseUrl)),
            ApiRequired: apiRequired,
            BackendRelevant: backendRelevant,
            PostgresBootstrapStatus: Get(
                values,
                "postgres-bootstrap-status",
                Environment.GetEnvironmentVariable("QWEN_POSTGRES_BOOTSTRAP_STATUS") ?? "unknown"),
            ChatCompletionsUrl: chatCompletionsUrl,
            Model: Get(values, "model", Environment.GetEnvironmentVariable("STRATA_MODEL") ?? DefaultModel),
            ApiKey: Environment.GetEnvironmentVariable("STRATA_API_KEY"),
            ReasoningEffort: reasoningEffort,
            MaxTokens: maxTokens,
            MaxToolTurns: maxToolTurns,
            SkipRegression: flags.Contains("skip-regression"),
            AllowDirty: flags.Contains("allow-dirty"),
            ReuseRunningApp: flags.Contains("reuse-running-app"),
            ReuseRunningApi: flags.Contains("reuse-running-api"),
            StrataSmoke: flags.Contains("strata-smoke"),
            RegressionOnly: flags.Contains("regression-only"));
    }

    private static string Get(
        IReadOnlyDictionary<string, string> values,
        string name,
        string fallback)
    {
        return values.TryGetValue(name, out var value) ? value : fallback;
    }

    private static string GetRequired(
        IReadOnlyDictionary<string, string> values,
        string name,
        string? fallback)
    {
        if (values.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value))
            return value;

        if (!string.IsNullOrWhiteSpace(fallback))
            return fallback;

        throw new ArgumentException(
            $"Missing --{name}. Supply the full STRATA chat-completions endpoint via --{name} or STRATA_CHAT_COMPLETIONS_URL.");
    }

    private static int ParsePositiveInt(string value, string name)
    {
        if (!int.TryParse(value, out var parsed) || parsed <= 0)
            throw new ArgumentException($"--{name} must be a positive integer.");

        return parsed;
    }

    private static bool ParseBoolean(string value, string name)
    {
        if (bool.TryParse(value, out var parsed))
            return parsed;

        return value switch
        {
            "1" => true,
            "0" => false,
            _ => throw new ArgumentException($"--{name} must be true/false."),
        };
    }

    private static string EnsureTrailingSlash(string value)
    {
        return value.EndsWith("/", StringComparison.Ordinal) ? value : value + "/";
    }
}
