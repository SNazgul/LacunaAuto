using System.Diagnostics;

namespace LacunaAuto.QwenUiTester;

internal sealed class AppHost : IAsyncDisposable
{
    private readonly Process? _process;

    private AppHost(Process? process, bool reusedExistingServer)
    {
        _process = process;
        ReusedExistingServer = reusedExistingServer;
    }

    public bool ReusedExistingServer { get; }

    public static async Task<AppHost> EnsureRunningAsync(
        string repositoryRoot,
        string appBaseUrl,
        bool reuseRunningApp)
    {
        if (await IsReadyAsync(appBaseUrl))
        {
            if (!reuseRunningApp)
            {
                throw new InvalidOperationException(
                    $"An application is already responding at {appBaseUrl}. " +
                    "Stop it before running Qwen UI Tester, or pass --reuse-running-app only after confirming it serves the exact revision under review.");
            }

            return new AppHost(process: null, reusedExistingServer: true);
        }

        var projectPath = Path.Combine(
            repositoryRoot,
            "src",
            "LacunaAuto.Web",
            "LacunaAuto.Web",
            "LacunaAuto.Web.csproj");

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

        var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start LacunaAuto.Web.");

        var deadline = DateTimeOffset.UtcNow.AddSeconds(90);

        while (DateTimeOffset.UtcNow < deadline)
        {
            if (process.HasExited)
            {
                throw new InvalidOperationException(
                    $"LacunaAuto.Web exited before becoming ready. Exit code: {process.ExitCode}.");
            }

            if (await IsReadyAsync(appBaseUrl))
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

        throw new TimeoutException($"LacunaAuto.Web did not become ready at {appBaseUrl} within 90 seconds.");
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

    private static async Task<bool> IsReadyAsync(string appBaseUrl)
    {
        using var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(2),
        };

        try
        {
            using var response = await client.GetAsync(appBaseUrl);
            return response.IsSuccessStatusCode;
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
