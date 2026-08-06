using WinRunner.Core.Processes;

namespace WinRunner.Core.Installer;

/// <summary>The outcome of an install operation.</summary>
public sealed class InstallResult
{
    public bool Success { get; private set; }
    public int ExitCode { get; private set; }
    public string? ErrorMessage { get; private set; }
    public ProcessResult? ProcessResult { get; private set; }

    /// <summary>The id of the newly registered app (set on success).</summary>
    public string? AppId { get; private set; }

    public static InstallResult Succeeded(ProcessResult result, string appId) =>
        new() { Success = true, ExitCode = result.ExitCode, ProcessResult = result, AppId = appId };

    public static InstallResult Failed(int exitCode, string error) =>
        new() { Success = false, ExitCode = exitCode, ErrorMessage = error };
}
