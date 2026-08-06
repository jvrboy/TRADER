using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WinRunner.Core.Processes;

/// <summary>Result of running an external process.</summary>
public sealed class ProcessResult
{
    public int ExitCode { get; set; }
    public string StandardOutput { get; set; } = string.Empty;
    public string StandardError { get; set; } = string.Empty;
    public bool TimedOut { get; set; }
    public string? CommandLine { get; set; }

    public string CombinedOutput =>
        string.IsNullOrWhiteSpace(StandardError)
            ? StandardOutput
            : StandardOutput.TrimEnd() + Environment.NewLine + StandardError;
}

/// <summary>
/// Runs external processes (wine, wineserver, etc.) safely with output
/// capture, timeouts, and environment injection.
/// </summary>
public sealed class ProcessRunner
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Runs a process, capturing stdout and stderr. Does not throw on a non-zero exit code.
    /// </summary>
    public async Task<ProcessResult> RunAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        string? workingDirectory = null,
        IReadOnlyDictionary<string, string>? environment = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            WorkingDirectory = workingDirectory ?? string.Empty,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        foreach (var arg in arguments)
        {
            psi.ArgumentList.Add(arg);
        }

        if (environment != null)
        {
            foreach (var kv in environment)
            {
                psi.Environment[kv.Key] = kv.Value;
            }
        }

        // Ensure wine uses a non-interactive, headless-friendly mode where possible.
        psi.Environment["WINEDEBUG"] = psi.Environment.TryGetValue("WINEDEBUG", out var wd) ? wd : "-all";

        var commandLine = BuildCommandLine(fileName, arguments);

        using var process = new Process { StartInfo = psi };
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null) stdout.AppendLine(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null) stderr.AppendLine(e.Data);
        };

        try
        {
            if (!process.Start())
            {
                return new ProcessResult { ExitCode = -1, CommandLine = commandLine, StandardError = "Failed to start process." };
            }
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            return new ProcessResult { ExitCode = -1, CommandLine = commandLine, StandardError = ex.Message };
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var effectiveTimeout = timeout ?? DefaultTimeout;
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(effectiveTimeout);

        bool timedOut = false;
        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            timedOut = true;
            TryKillTree(process);
            await process.WaitForExitAsync(CancellationToken.None);
        }

        return new ProcessResult
        {
            ExitCode = timedOut ? -2 : process.ExitCode,
            StandardOutput = stdout.ToString(),
            StandardError = stderr.ToString(),
            TimedOut = timedOut,
            CommandLine = commandLine,
        };
    }

    /// <summary>
    /// Launches a process without waiting for it to exit (fire-and-forget for GUI apps).
    /// Used when starting a Windows application under wine.
    /// </summary>
    public bool LaunchDetached(
        string fileName,
        IReadOnlyList<string> arguments,
        string? workingDirectory = null,
        IReadOnlyDictionary<string, string>? environment = null)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            WorkingDirectory = workingDirectory ?? string.Empty,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var arg in arguments) psi.ArgumentList.Add(arg);
        if (environment != null)
        {
            foreach (var kv in environment) psi.Environment[kv.Key] = kv.Value;
        }
        psi.Environment["WINEDEBUG"] = "-all";

        try
        {
            using var p = Process.Start(psi);
            return p != null;
        }
        catch
        {
            return false;
        }
    }

    private static void TryKillTree(Process process)
    {
        try
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
        }
        catch
        {
            // best-effort
        }
    }

    private static string BuildCommandLine(string fileName, IReadOnlyList<string> args)
    {
        var sb = new StringBuilder();
        sb.Append('"').Append(fileName).Append('"');
        foreach (var a in args)
        {
            sb.Append(' ').Append('"').Append(a.Replace("\"", "\\\"")).Append('"');
        }
        return sb.ToString();
    }
}
