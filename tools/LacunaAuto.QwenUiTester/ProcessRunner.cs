using System.Diagnostics;
using System.Text;

namespace LacunaAuto.QwenUiTester;

internal sealed record ProcessResult(
    int ExitCode,
    string StandardOutput,
    string StandardError,
    bool TimedOut)
{
    public string CombinedOutput
    {
        get
        {
            var builder = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(StandardOutput))
                builder.AppendLine(StandardOutput.TrimEnd());

            if (!string.IsNullOrWhiteSpace(StandardError))
            {
                if (builder.Length > 0)
                    builder.AppendLine();

                builder.AppendLine(StandardError.TrimEnd());
            }

            return builder.ToString();
        }
    }
}

internal static class ProcessRunner
{
    public static async Task<ProcessResult> RunAsync(
        string fileName,
        IEnumerable<string> arguments,
        string workingDirectory,
        TimeSpan timeout,
        IReadOnlyDictionary<string, string?>? environment = null)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = ResolveExecutable(fileName),
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);

        if (environment is not null)
        {
            foreach (var pair in environment)
            {
                if (pair.Value is null)
                    startInfo.Environment.Remove(pair.Key);
                else
                    startInfo.Environment[pair.Key] = pair.Value;
            }
        }

        using var process = new Process
        {
            StartInfo = startInfo,
        };

        if (!process.Start())
            throw new InvalidOperationException($"Failed to start process '{fileName}'.");

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        using var timeoutSource = new CancellationTokenSource(timeout);
        var timedOut = false;

        try
        {
            await process.WaitForExitAsync(timeoutSource.Token);
        }
        catch (OperationCanceledException)
        {
            timedOut = true;

            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
                // The process exited between the timeout and Kill().
            }

            await process.WaitForExitAsync();
        }

        var standardOutput = await outputTask;
        var standardError = await errorTask;

        return new ProcessResult(
            ExitCode: timedOut ? -1 : process.ExitCode,
            StandardOutput: standardOutput,
            StandardError: standardError,
            TimedOut: timedOut);
    }

    public static string ResolveLocalNodeTool(string packageRoot, string toolName)
    {
        var fileName = OperatingSystem.IsWindows()
            ? $"{toolName}.cmd"
            : toolName;

        var fullPath = Path.Combine(
            packageRoot,
            "node_modules",
            ".bin",
            fileName);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException(
                $"Local Node tool '{toolName}' was not found at '{fullPath}'. " +
                $"Install the repository's E2E dependencies first.",
                fullPath);
        }

        return fullPath;
    }

    public static string ResolveExecutable(string fileName)
    {
        if (!OperatingSystem.IsWindows())
            return fileName;

        return fileName switch
        {
            "npm" => "npm.cmd",
            "npx" => "npx.cmd",
            _ => fileName,
        };
    }
}
