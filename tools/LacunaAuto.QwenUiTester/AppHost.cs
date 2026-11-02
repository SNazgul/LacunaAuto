using System.Diagnostics;

namespace LacunaAuto.QwenUiTester;

internal sealed class AppHost : IAsyncDisposable
{
    private enum ReadinessMode
    {
        SuccessStatusCode,
        AnyHttpResponse,
    }

    private readonly Process? _process;

    private AppHost(Process? process, bool reusedExistingServer)
    {
        _process = process;
        ReusedExistingServer = reusedExistingServer;
    }

    public bool ReusedExistingServer { get; }

    public static Task<AppHost> EnsureWebRunningAsync(
        string repositoryRoot,
        string webBaseUrl,
        bool reuseRunningApp,
        string apiBaseUrl)
    {
        var projectPath = Path.Combine(
            repositoryRoot,
            "src",
            "LacunaAuto.Web",
            "LacunaAuto.Web",
            "LacunaAuto.Web.csproj");

        return EnsureRunningAsync(
            repositoryRoot,
            webBaseUrl,
            reuseRunningApp,
            projectPath,
            "LacunaAuto.Web",
            "--reuse-running-app",
            ReadinessMode.SuccessStatusCode,
            apiBaseUrl);
    }

    public static Task<AppHost> EnsureApiRunningAsync(
        string repositoryRoot,
        string apiBaseUrl,
        bool reuseRunningApi)
    {
        var projectPath = Path.Combine(
            repositoryRoot,
            "src",
            "LacunaAuto.Api",
            "LacunaAuto.Api.csproj");

        return EnsureRunningAsync(
            repositoryRoot,
            apiBaseUrl,
            reuseRunningApi,
            projectPath,
            "LacunaAuto.Api",
            "--reuse-running-api",
            ReadinessMode.AnyHttpResponse);
    }

    private static async Task<AppHost> EnsureRunningAsync(
        string repositoryRoot,
        string appBaseUrl,
        bool reuseRunningApp,
        string projectPath,
        string processName,
        string reuseFlagName,
        ReadinessMode readinessMode,
        string? apiBaseUrl = null)
    {
        if (await IsReadyAsync(appBaseUrl, readinessMode))
        {
            if (!reuseRunningApp)
            {
                throw new InvalidOperationException(
                    $"{processName} is already responding at {appBaseUrl}. " +
                    $"Stop it before running Qwen UI Tester, or pass {reuseFlagName} only after confirming it serves the exact revision under review.");
            }

            return new AppHost(process: null, reusedExistingServer: true);
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = ProcessRunner.ResolveExecutable("dotnet"),
            WorkingDirectory = repositoryRoot,
            UseShellExecute = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            CreateNoWindow = false,
        };

        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(projectPath);
        startInfo.ArgumentList.Add("--no-launch-profile");

        startInfo.Environment["ASPNETCORE_URLS"] = appBaseUrl.TrimEnd('/');
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        if (apiBaseUrl is not null)
        {
            startInfo.Environment["ListingsApi__BaseUrl"] = apiBaseUrl;
        }

        var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Failed to start {processName}.");

        var deadline = DateTimeOffset.UtcNow.AddSeconds(90);

        while (DateTimeOffset.UtcNow < deadline)
        {
            if (process.HasExited)
            {
                throw new InvalidOperationException(
                    $"{processName} exited before becoming ready. Exit code: {process.ExitCode}.");
            }

            if (await IsReadyAsync(appBaseUrl, readinessMode))
                return new AppHost(process, reusedExistingServer: false);

            await Task.Delay(500);
        }

        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
            // Process already exited.
        }

        throw new TimeoutException($"{processName} did not become ready at {appBaseUrl} within 90 seconds.");
    }

    public ValueTask DisposeAsync()
    {
        if (_process is null || _process.HasExited)
            return ValueTask.CompletedTask;

        try
        {
            _process.Kill(entireProcessTree: true);
            _process.WaitForExit(10_000);
        }
        catch (InvalidOperationException)
        {
            // Process already exited.
        }

        _process.Dispose();
        return ValueTask.CompletedTask;
    }

    private static async Task<bool> IsReadyAsync(string appBaseUrl, ReadinessMode readinessMode)
    {
        using var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(2),
        };

        try
        {
            using var response = await client.GetAsync(appBaseUrl);
            return readinessMode == ReadinessMode.SuccessStatusCode
                ? response.IsSuccessStatusCode
                : true;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException)
        {
            return false;
        }
    }
}
