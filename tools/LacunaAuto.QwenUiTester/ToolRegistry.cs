using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LacunaAuto.QwenUiTester;

internal sealed class ToolRegistry
{
    private const int MaxToolOutputCharacters = 80_000;

    private readonly string _repositoryRoot;
    private readonly string _baseRef;
    private readonly string _headRef;
    private readonly string _appBaseUrl;
    private readonly string _e2eDirectory;
    private readonly string _fullRegressionReportPath;

    public ToolRegistry(
        string repositoryRoot,
        string baseRef,
        string headRef,
        string appBaseUrl,
        string fullRegressionReportPath)
    {
        _repositoryRoot = repositoryRoot;
        _baseRef = baseRef;
        _headRef = headRef;
        _appBaseUrl = appBaseUrl;
        _e2eDirectory = Path.Combine(repositoryRoot, "e2e");
        _fullRegressionReportPath = fullRegressionReportPath;
    }

    public JsonArray GetDefinitions()
    {
        return
        [
            Function(
                "read_file",
                "Read a UTF-8 text file inside the LacunaAuto repository. Use targeted paths only.",
                new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["path"] = new JsonObject
                        {
                            ["type"] = "string",
                            ["description"] = "Repository-relative text file path.",
                        },
                    },
                    ["required"] = new JsonArray("path"),
                    ["additionalProperties"] = false,
                }),
            Function(
                "search_text",
                "Search tracked repository files for an exact text fragment using git grep.",
                new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["query"] = new JsonObject
                        {
                            ["type"] = "string",
                            ["description"] = "Exact text fragment to search for.",
                        },
                        ["path"] = new JsonObject
                        {
                            ["type"] = "string",
                            ["description"] = "Optional repository-relative path scope.",
                        },
                    },
                    ["required"] = new JsonArray("query"),
                    ["additionalProperties"] = false,
                }),
            Function(
                "git_diff",
                "Read the locked review diff between the configured base and head refs. Optionally limit it to one repository-relative path.",
                new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["path"] = new JsonObject
                        {
                            ["type"] = "string",
                            ["description"] = "Optional repository-relative path to limit the diff.",
                        },
                    },
                    ["additionalProperties"] = false,
                }),
            Function(
                "run_playwright",
                "Run an existing focused Playwright spec from e2e/specs against the app under review. This cannot create or modify tests.",
                new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["spec"] = new JsonObject
                        {
                            ["type"] = "string",
                            ["description"] = "Path relative to e2e, required to start with specs/ and end in .ts.",
                        },
                        ["grep"] = new JsonObject
                        {
                            ["type"] = "string",
                            ["description"] = "Optional Playwright --grep pattern.",
                        },
                    },
                    ["required"] = new JsonArray("spec"),
                    ["additionalProperties"] = false,
                }),
            Function(
                "read_e2e_report",
                "Read the preserved JSON report from the initial full Playwright regression run.",
                new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject(),
                    ["additionalProperties"] = false,
                }),
            Function(
                "http_get",
                "GET one route from the LacunaAuto app under review and return status plus a bounded response body.",
                new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["route"] = new JsonObject
                        {
                            ["type"] = "string",
                            ["description"] = "Relative application route, for example /listings or /manifest.webmanifest.",
                        },
                    },
                    ["required"] = new JsonArray("route"),
                    ["additionalProperties"] = false,
                }),
        ];
    }

    public async Task<string> ExecuteAsync(string name, string arguments)
    {
        try
        {
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(arguments) ? "{}" : arguments);
            var root = document.RootElement;

            return name switch
            {
                "read_file" => await ReadFileAsync(GetRequiredString(root, "path")),
                "search_text" => await SearchTextAsync(
                    GetRequiredString(root, "query"),
                    GetOptionalString(root, "path")),
                "git_diff" => await GitDiffAsync(GetOptionalString(root, "path")),
                "run_playwright" => await RunPlaywrightAsync(
                    GetRequiredString(root, "spec"),
                    GetOptionalString(root, "grep")),
                "read_e2e_report" => await ReadE2eReportAsync(),
                "http_get" => await HttpGetAsync(GetRequiredString(root, "route")),
                _ => $"TOOL_ERROR: Unknown tool '{name}'.",
            };
        }
        catch (Exception exception)
        {
            return $"TOOL_ERROR: {exception.GetType().Name}: {exception.Message}";
        }
    }

    private async Task<string> ReadFileAsync(string relativePath)
    {
        var fullPath = ResolveRepositoryPath(relativePath);

        if (!File.Exists(fullPath))
            return $"FILE_NOT_FOUND: {relativePath}";

        if (!IsSupportedTextFile(fullPath))
            return $"UNSUPPORTED_FILE_TYPE: {relativePath}";

        var text = await File.ReadAllTextAsync(fullPath);
        return Limit(text, MaxToolOutputCharacters);
    }

    private async Task<string> SearchTextAsync(string query, string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Search query cannot be empty.");

        var arguments = new List<string>
        {
            "grep",
            "-n",
            "-F",
            "-e",
            query,
            "--",
        };

        if (!string.IsNullOrWhiteSpace(relativePath))
        {
            ResolveRepositoryPath(relativePath);
            arguments.Add(NormalizeGitPath(relativePath));
        }

        var result = await ProcessRunner.RunAsync(
            "git",
            arguments,
            _repositoryRoot,
            TimeSpan.FromSeconds(30));

        if (result.ExitCode == 1 && string.IsNullOrWhiteSpace(result.CombinedOutput))
            return "NO_MATCHES";

        return Limit(FormatProcessResult(result), MaxToolOutputCharacters);
    }

    private async Task<string> GitDiffAsync(string? relativePath)
    {
        var arguments = new List<string>
        {
            "diff",
            "--no-ext-diff",
            "--unified=30",
            $"{_baseRef}...{_headRef}",
            "--",
        };

        if (!string.IsNullOrWhiteSpace(relativePath))
        {
            ResolveRepositoryPath(relativePath);
            arguments.Add(NormalizeGitPath(relativePath));
        }

        var result = await ProcessRunner.RunAsync(
            "git",
            arguments,
            _repositoryRoot,
            TimeSpan.FromSeconds(60));

        return Limit(FormatProcessResult(result), MaxToolOutputCharacters);
    }

    private async Task<string> RunPlaywrightAsync(string spec, string? grep)
    {
        var normalizedSpec = NormalizeGitPath(spec);

        if (!normalizedSpec.StartsWith("specs/", StringComparison.Ordinal) ||
            normalizedSpec.Contains("..", StringComparison.Ordinal) ||
            !normalizedSpec.EndsWith(".ts", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("spec must be an existing TypeScript file under e2e/specs/.");
        }

        var fullSpecPath = Path.Combine(_e2eDirectory, normalizedSpec.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(fullSpecPath))
            throw new FileNotFoundException("Playwright spec was not found.", normalizedSpec);

        var arguments = new List<string>
        {
            "test",
            "--",
            normalizedSpec,
        };

        if (!string.IsNullOrWhiteSpace(grep))
        {
            arguments.Add("--grep");
            arguments.Add(grep);
        }

        var result = await ProcessRunner.RunAsync(
            "npm",
            arguments,
            _e2eDirectory,
            TimeSpan.FromMinutes(3),
            new Dictionary<string, string?>
            {
                ["E2E_BASE_URL"] = _appBaseUrl,
            });

        PreserveLatestFocusedReport();

        return Limit(FormatProcessResult(result), MaxToolOutputCharacters);
    }

    private async Task<string> ReadE2eReportAsync()
    {
        if (!File.Exists(_fullRegressionReportPath))
            return "FULL_REGRESSION_REPORT_NOT_FOUND";

        var content = await File.ReadAllTextAsync(_fullRegressionReportPath);
        return Limit(content, MaxToolOutputCharacters);
    }

    private async Task<string> HttpGetAsync(string route)
    {
        if (Uri.TryCreate(route, UriKind.Absolute, out _))
            throw new ArgumentException("route must be relative to the configured application origin.");

        var baseUri = new Uri(_appBaseUrl, UriKind.Absolute);
        var targetUri = new Uri(baseUri, route.TrimStart('/'));

        using var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(20),
        };

        using var response = await client.GetAsync(targetUri);
        var body = await response.Content.ReadAsStringAsync();

        var builder = new StringBuilder();
        builder.AppendLine($"Status: {(int)response.StatusCode} {response.ReasonPhrase}");
        builder.AppendLine($"Content-Type: {response.Content.Headers.ContentType}");
        builder.AppendLine();
        builder.Append(Limit(body, 30_000));

        return builder.ToString();
    }

    private string ResolveRepositoryPath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            throw new ArgumentException("Path cannot be empty.");

        var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(_repositoryRoot, normalized));
        var rootWithSeparator = Path.GetFullPath(_repositoryRoot)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
            Path.DirectorySeparatorChar;

        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        if (!fullPath.StartsWith(rootWithSeparator, comparison))
            throw new InvalidOperationException("Path escapes the repository root.");

        var relative = Path.GetRelativePath(_repositoryRoot, fullPath);
        if (relative.Equals(".git", comparison) ||
            relative.StartsWith($".git{Path.DirectorySeparatorChar}", comparison))
        {
            throw new InvalidOperationException("Reading .git internals is not allowed.");
        }

        return fullPath;
    }

    private static bool IsSupportedTextFile(string path)
    {
        var extension = Path.GetExtension(path);

        return extension.ToLowerInvariant() switch
        {
            ".cs" or ".razor" or ".css" or ".json" or ".md" or ".ts" or ".tsx" or
            ".js" or ".mjs" or ".yml" or ".yaml" or ".csproj" or ".props" or
            ".targets" or ".sln" or ".html" or ".xml" or ".txt" => true,
            _ => false,
        };
    }

    private void PreserveLatestFocusedReport()
    {
        var sourcePath = Path.Combine(_e2eDirectory, "e2e-report.json");
        if (!File.Exists(sourcePath))
            return;

        var artifactDirectory = Path.Combine(_e2eDirectory, "e2e-artifacts");
        Directory.CreateDirectory(artifactDirectory);

        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
        var targetPath = Path.Combine(artifactDirectory, $"qwen-focused-{timestamp}.json");
        File.Copy(sourcePath, targetPath, overwrite: true);
    }

    private static string FormatProcessResult(ProcessResult result)
    {
        return $"""
ExitCode: {result.ExitCode}
TimedOut: {result.TimedOut}

{result.CombinedOutput}
""";
    }

    private static string GetRequiredString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(property.GetString()))
        {
            throw new ArgumentException($"Tool argument '{propertyName}' is required.");
        }

        return property.GetString()!;
    }

    private static string? GetOptionalString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property) ||
            property.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (property.ValueKind != JsonValueKind.String)
            throw new ArgumentException($"Tool argument '{propertyName}' must be a string.");

        return property.GetString();
    }

    private static string NormalizeGitPath(string path)
    {
        return path.Replace('\\', '/').TrimStart('/');
    }

    private static string Limit(string value, int maxCharacters)
    {
        if (value.Length <= maxCharacters)
            return value;

        return value[..maxCharacters] +
            $"\n\n[TRUNCATED by runner after {maxCharacters} characters]";
    }

    private static JsonObject Function(string name, string description, JsonObject parameters)
    {
        return new JsonObject
        {
            ["type"] = "function",
            ["function"] = new JsonObject
            {
                ["name"] = name,
                ["description"] = description,
                ["parameters"] = parameters,
            },
        };
    }
}
