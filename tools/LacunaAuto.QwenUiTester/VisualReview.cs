using System.Text.Json;
using System.Text.Json.Nodes;

namespace LacunaAuto.QwenUiTester;

internal sealed record ToolExecutionResult(string Text, VisualCapture? Capture = null);

internal sealed record VisualCapture(string Route, string Viewport, string File, byte[] Png)
{
    public string Label => $"Screenshot {Route}, {Viewport}";
}

internal sealed class VisualReview
{
    private const int MaxProbesPerTool = 9;
    private const int MaxPngBytes = 1024 * 1024;
    private const int MaxTotalPngBytes = 4 * MaxPngBytes;
    private const int MaxSelectorLength = 256;
    private const int MaxSelectors = 8;
    private const string ClientPrefix = "src/LacunaAuto.Web/LacunaAuto.Web.Client/";

    private static readonly string[] _viewports = ["mobile", "tablet", "desktop"];
    private static readonly Dictionary<string, string> _readySelectors = new(StringComparer.Ordinal)
    {
        ["/"] = ".home-page",
        ["/listings"] = ".listings-page",
        ["/listings/1"] = ".details-shell",
    };

    private readonly string _e2eDirectory;
    private readonly string _webBaseUrl;
    private readonly string _runDirectory = Guid.NewGuid().ToString("N");
    private readonly Dictionary<(string Route, string Viewport), string> _screenshots = [];
    private readonly HashSet<(string Route, string Viewport)> _delivered = [];
    private readonly HashSet<(string Route, string Viewport)> _layouts = [];
    private int _captureAttempts;
    private int _layoutAttempts;
    private int _pngBytes;

    public VisualReview(string e2eDirectory, string webBaseUrl, IEnumerable<string> changedPaths)
    {
        _e2eDirectory = e2eDirectory;
        _webBaseUrl = webBaseUrl;
        RelevantRoutes = changedPaths.SelectMany(RoutesForPath).Distinct(StringComparer.Ordinal).Order().ToArray();
    }

    public string[] RelevantRoutes { get; }

    public string? CoveredRoute => RelevantRoutes.FirstOrDefault(route =>
        _viewports.All(viewport => _layouts.Contains((route, viewport)) && _delivered.Contains((route, viewport))));

    public object ScreenshotEvidence => _screenshots.Select(pair => new
    {
        route = pair.Key.Route,
        viewport = pair.Key.Viewport,
        file = pair.Value,
        delivered = _delivered.Contains(pair.Key),
    }).ToArray();

    public object LayoutEvidence => _layouts.Select(pair => new
    {
        route = pair.Route,
        viewport = pair.Viewport,
    }).ToArray();

    public int ScreenshotCount => _screenshots.Count;

    public string? MissingEvidenceReason() => CoveredRoute is not null
        ? null
        : RelevantRoutes.Length == 0
            ? "No supported visual route was mapped from the locked diff."
            : "No relevant route has both layout and delivered PNG evidence at all three viewports.";

    public IEnumerable<string> EvidenceLines() =>
        _screenshots.Select(pair =>
            $"- Screenshot {pair.Key.Route} / {pair.Key.Viewport}: {pair.Value}" +
            (_delivered.Contains(pair.Key) ? " (delivered)" : " (delivery unverified)"))
        .Concat(_layouts.Select(pair => $"- Layout inspection {pair.Route} / {pair.Viewport}"));

    public void MarkDelivered(VisualCapture capture) => _delivered.Add((capture.Route, capture.Viewport));

    public static JsonObject ToolParameters(bool screenshot)
    {
        var properties = new JsonObject
        {
            ["route"] = new JsonObject
            {
                ["type"] = "string",
                ["enum"] = new JsonArray(_readySelectors.Keys.Select(route => (JsonNode?)JsonValue.Create(route)).ToArray()),
            },
            ["viewport"] = new JsonObject
            {
                ["type"] = "string",
                ["enum"] = new JsonArray("mobile", "tablet", "desktop"),
            },
            ["ready_selector"] = new JsonObject { ["type"] = "string", ["maxLength"] = MaxSelectorLength },
        };

        if (!screenshot)
        {
            properties["selectors"] = new JsonObject
            {
                ["type"] = "array",
                ["maxItems"] = MaxSelectors,
                ["items"] = new JsonObject { ["type"] = "string", ["maxLength"] = MaxSelectorLength },
            };
        }

        return new JsonObject
        {
            ["type"] = "object",
            ["properties"] = properties,
            ["required"] = new JsonArray("route", "viewport"),
            ["additionalProperties"] = false,
        };
    }

    public async Task<ToolExecutionResult> ProbeAsync(bool screenshot, JsonElement arguments)
    {
        foreach (var argument in arguments.EnumerateObject())
        {
            if (argument.Name is not ("route" or "viewport" or "ready_selector") &&
                (screenshot || argument.Name != "selectors"))
            {
                throw new ArgumentException("Unknown browser probe argument.");
            }
        }

        var route = arguments.GetProperty("route").GetString();
        if (route is null || !_readySelectors.TryGetValue(route, out var requiredSelector))
            throw new ArgumentException("Choose a supported Web route: /, /listings, or /listings/1.");

        var viewport = arguments.GetProperty("viewport").GetString();
        if (viewport is null || !_viewports.Contains(viewport))
            throw new ArgumentException("Choose a viewport preset: mobile, tablet, or desktop.");

        var readySelector = arguments.TryGetProperty("ready_selector", out var ready) ? Selector(ready) : null;
        var selectors = Array.Empty<string>();
        if (arguments.TryGetProperty("selectors", out var selectorArray))
        {
            if (selectorArray.GetArrayLength() > MaxSelectors)
                throw new ArgumentException($"Select at most {MaxSelectors} CSS selectors.");

            selectors = selectorArray.EnumerateArray().Select(Selector).ToArray();
        }

        var key = (route, viewport);
        if (screenshot && _screenshots.ContainsKey(key))
            throw new ArgumentException("This route and viewport already have a screenshot.");

        if (screenshot ? _captureAttempts >= MaxProbesPerTool : _layoutAttempts >= MaxProbesPerTool)
            throw new InvalidOperationException("The browser probe budget for this tool is exhausted.");

        if (screenshot && _pngBytes >= MaxTotalPngBytes)
            throw new InvalidOperationException("The screenshot byte budget is exhausted.");

        string? file = null;
        if (screenshot)
        {
            _captureAttempts++;
            file = $"screenshots/{_runDirectory}/{_captureAttempts:D2}-{viewport}.png";
        }
        else
        {
            _layoutAttempts++;
        }

        var probeArguments = JsonSerializer.Serialize(new
        {
            mode = screenshot ? "screenshot" : "layout",
            route,
            viewport,
            requiredSelector,
            readySelector,
            selectors,
            file,
            byteLimit = Math.Min(MaxPngBytes, MaxTotalPngBytes - _pngBytes),
        });
        var result = await ProcessRunner.RunAsync(
            "node",
            ["tools/qwen-browser-probe.mjs", probeArguments],
            _e2eDirectory,
            TimeSpan.FromSeconds(45),
            new Dictionary<string, string?>
            {
                ["E2E_BASE_URL"] = _webBaseUrl,
                ["STRATA_API_KEY"] = null,
            });

        if (result.TimedOut)
            throw new InvalidOperationException("Browser probe timed out.");

        if (result.ExitCode != 0)
        {
            var error = result.StandardError.Trim();
            throw new InvalidOperationException($"Browser probe failed: {error[..Math.Min(error.Length, 600)]}");
        }

        if (file is null)
        {
            _layouts.Add(key);
            return new ToolExecutionResult(result.StandardOutput.Trim());
        }

        var fullPath = Path.Combine(_e2eDirectory, "qwen-reviews", file);
        var bytes = new FileInfo(fullPath).Length;
        if (bytes <= 8 || bytes > Math.Min(MaxPngBytes, MaxTotalPngBytes - _pngBytes))
            throw new InvalidOperationException("The captured file does not meet the screenshot byte limit.");

        var png = await File.ReadAllBytesAsync(fullPath);
        if (!png.AsSpan().StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
            throw new InvalidOperationException("The captured file is not a PNG.");

        _pngBytes += png.Length;
        _screenshots.Add(key, file);
        return new ToolExecutionResult(result.StandardOutput.Trim(), new VisualCapture(route, viewport, file, png));
    }

    private static string Selector(JsonElement value)
    {
        var selector = value.GetString();
        if (string.IsNullOrWhiteSpace(selector) || selector.Length > MaxSelectorLength || selector.Any(char.IsControl))
            throw new ArgumentException($"A CSS selector must be between 1 and {MaxSelectorLength} characters.");

        return selector;
    }

    private static IEnumerable<string> RoutesForPath(string path)
    {
        if (path.StartsWith("src/LacunaAuto.Web/LacunaAuto.Web/Components/Pages/", StringComparison.Ordinal))
            return [];

        if (path.StartsWith("src/LacunaAuto.UI.Shared/Components/VehicleListingCard.", StringComparison.Ordinal) ||
            path.StartsWith("src/LacunaAuto.UI.Shared/Components/SkeletonCard.", StringComparison.Ordinal))
        {
            return ["/", "/listings"];
        }

        if (path.StartsWith("src/LacunaAuto.UI.Shared/", StringComparison.Ordinal) ||
            path.StartsWith("src/LacunaAuto.Web/LacunaAuto.Web/", StringComparison.Ordinal))
        {
            return _readySelectors.Keys;
        }

        var prefix = path.StartsWith(ClientPrefix, StringComparison.Ordinal) ? ClientPrefix : "src/LacunaAuto.Web.Client/";
        if (!path.StartsWith(prefix, StringComparison.Ordinal))
            return [];

        var relativePath = path[prefix.Length..];
        return relativePath switch
        {
            "Pages/Home.razor" or "Pages/Home.razor.css" or "Pages/Home.razor.cs" => ["/"],
            "Pages/Listings.razor" or "Pages/Listings.razor.css" or "Pages/Listings.razor.cs" => ["/listings"],
            "Pages/ListingDetails.razor" or "Pages/ListingDetails.razor.css" or "Pages/ListingDetails.razor.cs" => ["/listings/1"],
            _ when relativePath.StartsWith("Pages/", StringComparison.Ordinal) ||
                relativePath.StartsWith("Components/Pages/", StringComparison.Ordinal) => [],
            _ => _readySelectors.Keys,
        };
    }
}
