using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Afterglow.Core.Utils;

namespace Afterglow.Core;

/// <summary>
/// Finds and downloads the file that updates this copy of the app, as published with each release
/// (see the release workflow): the installer for installed copies, the single executable for
/// portable ones, each for a specific processor architecture.
/// </summary>
public static class UpdatePackage
{
    // e.g. "win-x64", or null for architectures that aren't released
    public static string? TryGetRuntimeId(Architecture architecture) =>
        architecture switch
        {
            Architecture.X64 => "win-x64",
            Architecture.Arm64 => "win-arm64",
            Architecture.X86 => "win-x86",
            _ => null,
        };

    // e.g. "Afterglow-Installer.win-x64.exe" or "Afterglow.win-x64.exe"
    public static string? TryGetAssetName(
        string appName,
        bool isInstaller,
        Architecture architecture
    ) =>
        TryGetRuntimeId(architecture) is { } runtimeId
            ? isInstaller
                ? $"{appName}-Installer.{runtimeId}.exe"
                : $"{appName}.{runtimeId}.exe"
            : null;

    public static ReleaseAsset? TryFindAsset(Release release, string assetName) =>
        release.Assets.FirstOrDefault(a =>
            string.Equals(a.Name, assetName, StringComparison.OrdinalIgnoreCase)
        );

    /// <summary>
    /// Whether the file can be downloaded and verified: it comes from GitHub over a secure
    /// connection, and its digest is known (GitHub calculates one for every uploaded file).
    /// </summary>
    public static bool CanDownload(ReleaseAsset asset) =>
        asset.DownloadUrl.Scheme == Uri.UriSchemeHttps
        && string.Equals(asset.DownloadUrl.Host, "github.com", StringComparison.OrdinalIgnoreCase)
        && asset.Sha256 is not null
        && asset.Size > 0;

    /// <summary>
    /// Checks a downloaded file against the size and digest that GitHub reported for it.
    /// </summary>
    public static async Task<bool> IsIntactAsync(
        string filePath,
        ReleaseAsset asset,
        CancellationToken cancellationToken = default
    )
    {
        if (asset.Sha256 is null)
            return false;

        await using var stream = File.OpenRead(filePath);
        if (stream.Length != asset.Size)
            return false;

        var hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
        return string.Equals(
            Convert.ToHexStringLower(hash),
            asset.Sha256,
            StringComparison.OrdinalIgnoreCase
        );
    }

    /// <summary>
    /// Downloads the file to the given path, and makes sure that it's exactly the file that was
    /// released. Reports progress as a fraction. Throws if it can't be downloaded or verified, in
    /// which case nothing is left at the given path.
    /// </summary>
    public static async Task DownloadAsync(
        ReleaseAsset asset,
        string filePath,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default
    )
    {
        if (!CanDownload(asset))
            throw new InvalidOperationException($"The file '{asset.Name}' can't be verified.");

        try
        {
            using (
                var response = await Http
                    .Client.GetAsync(
                        asset.DownloadUrl,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken
                    )
                    .ConfigureAwait(false)
            )
            {
                response.EnsureSuccessStatusCode();

                await using var source = await response
                    .Content.ReadAsStreamAsync(cancellationToken)
                    .ConfigureAwait(false);
                await using var destination = new FileStream(
                    filePath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None
                );

                var buffer = new byte[81920];
                var totalRead = 0L;

                while (true)
                {
                    var read = await source
                        .ReadAsync(buffer, cancellationToken)
                        .ConfigureAwait(false);
                    if (read == 0)
                        break;

                    totalRead += read;

                    // Never more than what was released
                    if (totalRead > asset.Size)
                        throw new InvalidDataException($"The file '{asset.Name}' is too large.");

                    await destination
                        .WriteAsync(buffer.AsMemory(0, read), cancellationToken)
                        .ConfigureAwait(false);
                    progress?.Report((double)totalRead / asset.Size);
                }
            }

            if (!await IsIntactAsync(filePath, asset, cancellationToken).ConfigureAwait(false))
            {
                throw new InvalidDataException(
                    $"The file '{asset.Name}' doesn't match the released one."
                );
            }
        }
        catch
        {
            TryDelete(filePath);
            throw;
        }
    }

    private static void TryDelete(string filePath)
    {
        try
        {
            File.Delete(filePath);
        }
        catch
        {
            // Best effort
        }
    }
}
