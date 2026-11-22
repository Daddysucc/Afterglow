using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Afterglow.Utils;

/// <summary>
/// Hands over to a downloaded update: the installer (for installed copies), or the new executable
/// (for portable ones, replacing this one in place). This app keeps running until the next version
/// is sure to start, so that it can carry on as before if it doesn't.
/// </summary>
internal static class UpdateHandoff
{
    // Where the installer is downloaded to (and removed from, once the new version has started)
    public static string InstallerDirPath { get; } =
        Path.Combine(Path.GetTempPath(), $"{Program.Name}-Update");

    // Written by the installer once it's running with administrator rights (see Installer.iss)
    private static string InstallerReadyFilePath => Path.Combine(InstallerDirPath, "ready");

    // How long the installer may take to report that it's running, after its own process has
    // exited (in case it restarted itself to get administrator rights)
    private static readonly TimeSpan InstallerReadyGracePeriod = TimeSpan.FromSeconds(5);

    // How long the administrator prompt may stay open before the update is given up
    private static readonly TimeSpan InstallerReadyTimeout = TimeSpan.FromMinutes(30);

    // Files next to the executable that's being replaced: the new version while it's downloaded,
    // and the previous version (which can be renamed, but not deleted, while it's running)
    public static string GetNewExecutablePath(string executablePath) => executablePath + ".new";

    public static string GetPreviousExecutablePath(string executablePath) =>
        executablePath + ".old";

    // The executable this process was started from, if it's this app's own (rather than, for
    // example, the dotnet host while debugging)
    public static string? TryGetRunningExecutablePath() =>
        Environment.ProcessPath is { } path
        && path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(
            Path.GetFileNameWithoutExtension(path),
            "dotnet",
            StringComparison.OrdinalIgnoreCase
        )
            ? path
            : null;

    // Installs without asking anything (only showing its progress), waits for this process to
    // exit, and starts the new version afterwards
    public static string GetInstallerArguments(int processId, string readyFilePath) =>
        "/SILENT /SUPPRESSMSGBOXES /NORESTART /SP- /UPDATE=1 "
        + $"/UPDATEPID={processId} \"/UPDATEREADYFILE={readyFilePath}\"";

    /// <summary>
    /// Starts the installer, and waits until it's running with administrator rights. Returns
    /// false if it isn't (e.g. the administrator prompt was declined), in which case this app
    /// carries on. Otherwise, the installer is now waiting for this app to close.
    /// </summary>
    public static async Task<bool> StartInstallerAsync(string installerPath)
    {
        TryDelete(InstallerReadyFilePath);

        // The installer doesn't run while this app is (and it would wait in vain for this app to
        // close if it saw it as already running)
        Program.ReleaseInstance();

        try
        {
            using var installer = Process.Start(
                new ProcessStartInfo(
                    installerPath,
                    GetInstallerArguments(Environment.ProcessId, InstallerReadyFilePath)
                )
                {
                    // Lets the installer show the administrator prompt itself, so that it can
                    // later start the new version without administrator rights
                    UseShellExecute = true,
                    WorkingDirectory = InstallerDirPath,
                }
            );

            if (installer is null || !await WaitForInstallerAsync(installer))
            {
                Program.TryReacquireInstance();
                return false;
            }

            return true;
        }
        catch
        {
            Program.TryReacquireInstance();
            throw;
        }
    }

    private static async Task<bool> WaitForInstallerAsync(Process installer)
    {
        var deadline = DateTimeOffset.Now + InstallerReadyTimeout;
        DateTimeOffset? exitedAt = null;

        while (DateTimeOffset.Now < deadline)
        {
            if (File.Exists(InstallerReadyFilePath))
                return true;

            if (installer.HasExited)
            {
                exitedAt ??= DateTimeOffset.Now;

                if (DateTimeOffset.Now - exitedAt > InstallerReadyGracePeriod)
                    return false;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250));
        }

        return false;
    }

    /// <summary>
    /// Puts the downloaded executable in place of the running one (which is kept next to it until
    /// the new version has started), and starts it. If anything fails, the running executable is
    /// put back, and this app carries on.
    /// </summary>
    public static void ReplaceExecutable(string executablePath, string newExecutablePath)
    {
        var previousExecutablePath = GetPreviousExecutablePath(executablePath);

        TryDelete(previousExecutablePath);
        File.Move(executablePath, previousExecutablePath);

        try
        {
            File.Move(newExecutablePath, executablePath);
        }
        catch
        {
            File.Move(previousExecutablePath, executablePath);
            throw;
        }

        // The new version can't start while this app holds on to being the only instance
        Program.ReleaseInstance();

        try
        {
            using var _ = Process.Start(
                new ProcessStartInfo(executablePath)
                {
                    ArgumentList =
                    {
                        StartOptions.PreviousVersionArgument,
                        Environment.ProcessId.ToString(),
                    },
                    UseShellExecute = false,
                    WorkingDirectory = Path.GetDirectoryName(executablePath) ?? "",
                }
            );
        }
        catch
        {
            // Put the running version back
            File.Move(executablePath, newExecutablePath, true);
            File.Move(previousExecutablePath, executablePath);

            Program.TryReacquireInstance();
            throw;
        }
    }

    /// <summary>
    /// Removes what's left over from updating to this version: the previous executable, and the
    /// downloaded installer. Never throws.
    /// </summary>
    public static void CleanUp()
    {
        if (TryGetRunningExecutablePath() is { } executablePath)
        {
            TryDelete(GetPreviousExecutablePath(executablePath));
            TryDelete(GetNewExecutablePath(executablePath));
        }

        try
        {
            if (Directory.Exists(InstallerDirPath))
                Directory.Delete(InstallerDirPath, true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to remove the downloaded update: {ex.Message}");
        }
    }

    private static void TryDelete(string filePath)
    {
        try
        {
            File.Delete(filePath);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to delete '{filePath}': {ex.Message}");
        }
    }
}
