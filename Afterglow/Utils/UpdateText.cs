using System;
using System.Globalization;
using Afterglow.Localization;
using Afterglow.Services;

namespace Afterglow.Utils;

// What the update buttons (on the dashboard and in the settings) say at each stage of updating
internal static class UpdateText
{
    public static bool IsBusy(UpdateInstallState state) =>
        state is UpdateInstallState.Downloading or UpdateInstallState.Installing;

    // e.g. "Downloading update… 42%", or null when not updating
    public static string? TryGetProgress(
        LocalizationManager localization,
        UpdateInstallState state,
        double downloadProgress
    ) =>
        state switch
        {
            UpdateInstallState.Downloading => string.Format(
                localization.UpdateDownloadingText,
                Math.Clamp(downloadProgress, 0, 1).ToString("P0", CultureInfo.CurrentCulture)
            ),
            UpdateInstallState.Installing => localization.UpdateInstallingText,
            _ => null,
        };

    public static string GetButton(
        LocalizationManager localization,
        UpdateInstallState state,
        double downloadProgress
    ) =>
        TryGetProgress(localization, state, downloadProgress)
        ?? (
            state == UpdateInstallState.Failed
                ? localization.UpdateFailedButton
                : localization.UpdateAvailableButton
        );

    // What clicking does: update, or (if that failed) open the download page
    public static string GetAction(
        LocalizationManager localization,
        UpdateInstallState state,
        Version version
    ) =>
        state == UpdateInstallState.Failed
            ? string.Format(localization.UpdateFailedTooltip, Program.DisplayName)
            : string.Format(
                localization.UpdateAvailableTooltip,
                Program.DisplayName,
                version.ToString(3)
            );
}
