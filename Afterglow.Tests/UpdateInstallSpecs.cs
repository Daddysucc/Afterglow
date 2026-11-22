using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Afterglow.Core;
using Afterglow.Localization;
using Afterglow.Services;
using Afterglow.Utils;
using AwesomeAssertions;
using Xunit;

namespace Afterglow.Tests;

public class UpdateInstallSpecs : IDisposable
{
    private static readonly Version CurrentVersion = new(3, 1, 0, 0);

    private const string Digest =
        "sha256:9F86D081884C7D659A2FEAA0C55AD015A3BF4F1B2B0B822CD15D6C15B0F00A08";

    private readonly string _directoryPath = Path.Combine(
        Path.GetTempPath(),
        "Afterglow.Tests." + Guid.NewGuid().ToString("N")
    );

    public UpdateInstallSpecs() => Directory.CreateDirectory(_directoryPath);

    private static Release? GetNewerRelease(string releaseJson)
    {
        using var document = JsonDocument.Parse(releaseJson);
        return UpdateCheck.TryGetNewerRelease(document.RootElement, CurrentVersion);
    }

    private static string ReadRepositoryFile(params string[] path)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (
            directory is not null
            && !File.Exists(Path.Combine(directory.FullName, "Afterglow.slnx"))
        )
            directory = directory.Parent;

        return File.ReadAllText(Path.Combine([directory!.FullName, .. path]));
    }

    // ---- Release files ----

    [Fact]
    public void I_get_the_files_of_a_newer_release_with_their_digests()
    {
        // Act
        var release = GetNewerRelease(
            $$"""
            {
              "tag_name": "v3.2.0",
              "assets": [
                {
                  "name": "Afterglow-Installer.win-x64.exe",
                  "size": 1234,
                  "digest": "{{Digest}}",
                  "browser_download_url": "https://github.com/Daddysucc/Afterglow/releases/download/v3.2.0/Afterglow-Installer.win-x64.exe"
                },
                {
                  "name": "Afterglow.win-x64.exe",
                  "size": 5678,
                  "browser_download_url": "https://github.com/Daddysucc/Afterglow/releases/download/v3.2.0/Afterglow.win-x64.exe"
                },
                { "name": "No link", "size": 1 },
                { "name": "No size", "browser_download_url": "https://github.com/a/b" },
                "not a file"
              ]
            }
            """
        );

        // Assert
        release.Should().NotBeNull();
        release!.Version.Should().Be(new Version(3, 2, 0));
        release.Assets.Should().HaveCount(2);

        var installer = release.Assets[0];
        installer.Name.Should().Be("Afterglow-Installer.win-x64.exe");
        installer.Size.Should().Be(1234);
        installer.Sha256.Should().Be(Digest["sha256:".Length..].ToLowerInvariant());
        UpdatePackage.CanDownload(installer).Should().BeTrue();

        // Without a digest, the file can't be verified, so it's never installed automatically
        release.Assets[1].Sha256.Should().BeNull();
        UpdatePackage.CanDownload(release.Assets[1]).Should().BeFalse();
    }

    [Fact]
    public void I_do_not_get_offered_files_of_a_release_that_is_not_newer()
    {
        // Act & assert
        GetNewerRelease("""{ "tag_name": "v3.1.0", "assets": [] }""").Should().BeNull();
        GetNewerRelease("""{ "tag_name": "v3.2.0", "prerelease": true }""").Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("sha512:9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08")]
    [InlineData("sha256:9f86d0")]
    [InlineData("sha256:zz86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08")]
    public void I_do_not_get_a_file_verified_against_a_digest_that_is_not_sha256(string? digest)
    {
        // Act & assert
        ReleaseAsset.TryParseSha256Digest(digest).Should().BeNull();
    }

    [Theory]
    [InlineData("http://github.com/Daddysucc/Afterglow/releases/download/v3.2.0/a.exe")]
    [InlineData("https://example.com/Daddysucc/Afterglow/releases/download/v3.2.0/a.exe")]
    [InlineData("https://github.com.example.com/a.exe")]
    public void I_do_not_get_files_downloaded_from_anywhere_but_github_over_https(string url)
    {
        // Arrange
        var asset = new ReleaseAsset("a.exe", new Uri(url), 10, new string('a', 64));

        // Act & assert
        UpdatePackage.CanDownload(asset).Should().BeFalse();
    }

    [Fact]
    public async Task I_get_a_downloaded_file_accepted_only_if_it_is_exactly_the_released_one()
    {
        // Arrange
        var content = Encoding.UTF8.GetBytes("test");
        var digest = Convert.ToHexStringLower(SHA256.HashData(content));
        var filePath = Path.Combine(_directoryPath, "update.exe");
        await File.WriteAllBytesAsync(filePath, content);

        var url = new Uri("https://github.com/Daddysucc/Afterglow/releases/download/v3.2.0/a.exe");

        // Act & assert
        (await UpdatePackage.IsIntactAsync(filePath, new ReleaseAsset("a.exe", url, 4, digest)))
            .Should()
            .BeTrue();

        // Different size
        (await UpdatePackage.IsIntactAsync(filePath, new ReleaseAsset("a.exe", url, 5, digest)))
            .Should()
            .BeFalse();

        // Different content
        (
            await UpdatePackage.IsIntactAsync(
                filePath,
                new ReleaseAsset("a.exe", url, 4, new string('0', 64))
            )
        )
            .Should()
            .BeFalse();
    }

    [Fact]
    public async Task I_do_not_get_a_file_downloaded_if_it_cannot_be_verified()
    {
        // Arrange
        var asset = new ReleaseAsset(
            "a.exe",
            new Uri("https://example.com/a.exe"),
            10,
            new string('a', 64)
        );
        var filePath = Path.Combine(_directoryPath, "update.exe");

        // Act
        var act = () => UpdatePackage.DownloadAsync(asset, filePath);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        File.Exists(filePath).Should().BeFalse();
    }

    // ---- Matching the published files ----

    [Theory]
    [InlineData(Architecture.X64, "win-x64")]
    [InlineData(Architecture.Arm64, "win-arm64")]
    [InlineData(Architecture.X86, "win-x86")]
    public void I_get_the_update_built_for_my_processor(Architecture architecture, string runtimeId)
    {
        // Arrange
        var workflow = ReadRepositoryFile(".github", "workflows", "build.yml");

        // Act
        var installerName = UpdatePackage.TryGetAssetName("Afterglow", true, architecture);
        var portableName = UpdatePackage.TryGetAssetName("Afterglow", false, architecture);

        // Assert
        installerName.Should().Be($"Afterglow-Installer.{runtimeId}.exe");
        portableName.Should().Be($"Afterglow.{runtimeId}.exe");

        // Every architecture is released, under the names the updater looks for
        workflow.Should().Contain(runtimeId);
        workflow.Should().Contain("packages/Afterglow-Installer.${{ matrix.rid }}.exe");
        workflow.Should().Contain("packages/Afterglow.${{ matrix.rid }}.exe");
    }

    [Fact]
    public void I_get_my_update_installed_by_an_installer_that_understands_the_app()
    {
        // Arrange
        var script = ReadRepositoryFile("Installer", "Installer.iss");

        // Act
        var arguments = UpdateHandoff.GetInstallerArguments(1234, @"C:\Temp Dir\ready");

        // Assert
        arguments.Should().Contain("/SILENT").And.Contain("/SUPPRESSMSGBOXES");
        arguments.Should().Contain("/UPDATE=1").And.Contain("/UPDATEPID=1234");
        arguments.Should().Contain("\"/UPDATEREADYFILE=C:\\Temp Dir\\ready\"");

        // The installer reads the same parameters
        script.Should().Contain("{param:UPDATE|0}");
        script.Should().Contain("{param:UPDATEPID|0}");
        script.Should().Contain("{param:UPDATEREADYFILE|}");

        // ...and starts the new version afterwards, or the current one if it didn't install
        script.Should().Contain("Flags: nowait postinstall; Check: IsUpdating");
        script.Should().Contain("procedure DeinitializeSetup()");
    }

    // ---- Restarting ----

    [Fact]
    public void I_get_the_new_version_started_once_the_previous_one_has_closed()
    {
        // Act
        var options = StartOptions.Parse([StartOptions.PreviousVersionArgument, "4321"]);

        // Assert
        options.PreviousVersionProcessId.Should().Be(4321);
        StartOptions.Parse([]).PreviousVersionProcessId.Should().BeNull();
        StartOptions
            .Parse([StartOptions.PreviousVersionArgument])
            .PreviousVersionProcessId.Should()
            .BeNull();
        StartOptions
            .Parse([StartOptions.PreviousVersionArgument, "x"])
            .PreviousVersionProcessId.Should()
            .BeNull();
    }

    [Fact]
    public void I_get_the_previous_executable_kept_next_to_the_new_one_until_it_has_closed()
    {
        // Act & assert
        UpdateHandoff
            .GetNewExecutablePath(@"D:\Tools\Afterglow.exe")
            .Should()
            .Be(@"D:\Tools\Afterglow.exe.new");
        UpdateHandoff
            .GetPreviousExecutablePath(@"D:\Tools\Afterglow.exe")
            .Should()
            .Be(@"D:\Tools\Afterglow.exe.old");
    }

    // ---- Progress ----

    [Fact]
    public void I_can_see_how_far_the_update_has_come()
    {
        // Arrange
        using var manager = new LocalizationManager(
            new SettingsService(Path.Combine(_directoryPath, "Settings.json"))
        );
        var version = new Version(3, 2, 0);

        // Act & assert
        UpdateText
            .GetButton(manager, UpdateInstallState.None, 0)
            .Should()
            .Be(manager.UpdateAvailableButton);
        UpdateText.GetButton(manager, UpdateInstallState.Downloading, 0.42).Should().Contain("42");
        UpdateText
            .GetButton(manager, UpdateInstallState.Installing, 1)
            .Should()
            .Be(manager.UpdateInstallingText);
        UpdateText
            .GetButton(manager, UpdateInstallState.Failed, 0)
            .Should()
            .Be(manager.UpdateFailedButton);

        UpdateText.GetAction(manager, UpdateInstallState.None, version).Should().Contain("3.2.0");
        UpdateText
            .GetAction(manager, UpdateInstallState.Failed, version)
            .Should()
            .Contain(Program.DisplayName);

        // Clicking does nothing while it's in progress
        UpdateText.IsBusy(UpdateInstallState.Downloading).Should().BeTrue();
        UpdateText.IsBusy(UpdateInstallState.Installing).Should().BeTrue();
        UpdateText.IsBusy(UpdateInstallState.None).Should().BeFalse();
        UpdateText.IsBusy(UpdateInstallState.Failed).Should().BeFalse();
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_directoryPath, true);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
