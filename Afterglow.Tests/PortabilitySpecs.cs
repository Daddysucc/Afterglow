using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using Afterglow.Core;
using Afterglow.Services;
using Afterglow.ViewModels.Components;
using AwesomeAssertions;
using PowerKit.Json.Extensions;
using Xunit;

namespace Afterglow.Tests;

// Things that differ from one PC to another: regional formats, sleep, and how releases are packaged
public class PortabilitySpecs : IDisposable
{
    private readonly string _directoryPath = Path.Combine(
        Path.GetTempPath(),
        "Afterglow.Tests." + Guid.NewGuid().ToString("N")
    );

    public PortabilitySpecs() => Directory.CreateDirectory(_directoryPath);

    // ---- Regional formats ----

    // Locales that write decimals with a comma (e.g. "0,8"), and group digits with a dot or a space
    public static TheoryData<string> CommaDecimalCultures { get; } = new("de-DE", "fr-FR", "uk-UA");

    // Runs with the given regional format, like on a PC set up for that region
    private static void InCulture(string cultureName, Action action)
    {
        var previousCulture = CultureInfo.CurrentCulture;
        var previousUICulture = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo(
                cultureName
            );

            action();
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUICulture;
        }
    }

    [Theory]
    [MemberData(nameof(CommaDecimalCultures))]
    public void I_can_load_settings_with_fractional_values_on_a_pc_that_uses_comma_decimals(
        string cultureName
    ) =>
        InCulture(
            cultureName,
            () =>
            {
                // Arrange
                // Settings files always use dots, whatever the regional format
                var filePath = Path.Combine(_directoryPath, "Settings.json");
                File.WriteAllText(
                    filePath,
                    """
                    {
                      "NightConfiguration": { "Temperature": 3400, "Brightness": 0.8 },
                      "ConfigurationTransitionOffset": 0.25,
                      "Location": { "Latitude": 50.45, "Longitude": -30.52 }
                    }
                    """
                );

                // Act
                // Only loaded, never saved (saving also writes to the registry)
                var settings = new SettingsService(filePath);
                var isLoaded = settings.Load();

                // Assert
                isLoaded.Should().BeTrue();
                settings.NightConfiguration.Should().Be(new ColorConfiguration(3400, 0.8));
                settings.ConfigurationTransitionOffset.Should().Be(0.25);
                settings.Location.Should().Be(new GeoLocation(50.45, -30.52));
            }
        );

    [Theory]
    [MemberData(nameof(CommaDecimalCultures))]
    public void I_can_enter_coordinates_on_a_pc_that_uses_comma_decimals(string cultureName) =>
        InCulture(
            cultureName,
            () =>
            {
                // Act
                var location = GeoLocation.TryParse("50.45, -30.52");

                // Assert
                location.Should().Be(new GeoLocation(50.45, -30.52));

                // Shown the same way it's entered, so that it can be parsed back
                location.ToString().Should().Be("50.45, -30.52");
                GeoLocation.TryParse(location.ToString()).Should().Be(location);
            }
        );

    [Theory]
    [MemberData(nameof(CommaDecimalCultures))]
    public void I_can_get_a_searched_location_on_a_pc_that_uses_comma_decimals(
        string cultureName
    ) =>
        InCulture(
            cultureName,
            () =>
            {
                // Arrange
                // The place search service returns coordinates as strings, with dots
                using var document = JsonDocument.Parse(
                    """{ "lat": "48.8566", "lon": "-2.3522" }"""
                );

                // Act
                var latitude = document.RootElement.GetProperty("lat").GetDoubleCoerced();
                var longitude = document.RootElement.GetProperty("lon").GetDoubleCoerced();

                // Assert
                latitude.Should().Be(48.8566);
                longitude.Should().Be(-2.3522);
            }
        );

    // ---- Temporary pause ----

    [Fact]
    public void I_observe_a_temporary_pause_lasting_until_its_end()
    {
        // Arrange
        var start = new DateTimeOffset(2026, 11, 14, 22, 00, 00, TimeSpan.Zero);
        var end = start + TimeSpan.FromMinutes(15);

        // Act & assert
        DashboardViewModel.IsTemporaryPauseOver(end, start).Should().BeFalse();
        DashboardViewModel
            .IsTemporaryPauseOver(end, end - TimeSpan.FromSeconds(1))
            .Should()
            .BeFalse();
        DashboardViewModel.IsTemporaryPauseOver(end, end).Should().BeTrue();
        DashboardViewModel.IsTemporaryPauseOver(null, end).Should().BeFalse();
    }

    [Fact]
    public void I_observe_a_pause_until_sunrise_ending_when_waking_up_after_sunrise()
    {
        // Arrange
        // Paused until sunrise at night, then the laptop was shut until the next morning
        var sunrise = new DateTimeOffset(2026, 11, 15, 07, 20, 00, TimeSpan.Zero);
        var wake = sunrise + TimeSpan.FromMinutes(40);

        // Act & assert
        // The time asleep counts, so the pause is over as soon as the computer wakes up
        DashboardViewModel.IsTemporaryPauseOver(sunrise, wake).Should().BeTrue();
    }

    // ---- Packaging ----

    private static string RepositoryDirPath { get; } = FindRepositoryDirPath();

    private static string FindRepositoryDirPath()
    {
        for (
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            directory is not null;
            directory = directory.Parent
        )
        {
            if (File.Exists(Path.Combine(directory.FullName, "Afterglow.slnx")))
                return directory.FullName;
        }

        throw new DirectoryNotFoundException("Repository not found.");
    }

    private static string ReadRepositoryFile(string relativePath) =>
        File.ReadAllText(Path.Combine(RepositoryDirPath, relativePath));

    [Fact]
    public void I_can_build_the_installer_from_files_that_exist_in_the_repository()
    {
        // Arrange
        var installerDirPath = Path.Combine(RepositoryDirPath, "Installer");
        var script = ReadRepositoryFile(Path.Combine("Installer", "Installer.iss"));

        // Act
        // Files the script includes or uses directly, relative to its own folder (the published
        // app itself is only copied into "Source" by the release workflow)
        var referencedPaths = new[] { "Source:", "SetupIconFile=" }
            .SelectMany(prefix =>
                script
                    .Split('\n')
                    .Select(l => l.Trim())
                    .Where(l => l.StartsWith(prefix, StringComparison.Ordinal))
                    .Select(l => l[prefix.Length..].Trim().Split(';')[0].Trim().Trim('"'))
            )
            .Where(p => !p.StartsWith(@"Source\", StringComparison.Ordinal))
            .ToArray();

        // Assert
        referencedPaths.Should().Contain([".installed", @"..\License.txt", @"..\favicon.ico"]);
        referencedPaths
            .Should()
            .AllSatisfy(p =>
                File.Exists(
                        Path.Combine(installerDirPath, p.Replace('\\', Path.DirectorySeparatorChar))
                    )
                    .Should()
                    .BeTrue(p)
            );
    }

    [Fact]
    public void I_can_only_install_each_release_where_its_app_can_run()
    {
        // Arrange
        var script = ReadRepositoryFile(Path.Combine("Installer", "Installer.iss"));
        var workflow = ReadRepositoryFile(Path.Combine(".github", "workflows", "build.yml"));

        // Assert
        // The workflow tells the installer which architecture it packages, and the installer
        // restricts itself to it (installing 64-bit apps to "Program Files")
        workflow.Should().Contain("INSTALLER_APP_ARCHITECTURE");
        script.Should().Contain("GetEnv(\"INSTALLER_APP_ARCHITECTURE\")");
        script.Should().Contain("ArchitecturesInstallIn64BitMode=x64compatible");
        script.Should().Contain("ArchitecturesInstallIn64BitMode=arm64");

        // Windows reads the gamma range setting from the 64-bit registry on 64-bit systems
        script.Should().Contain("Root: HKLM64;");
    }

    [Fact]
    public void I_can_install_over_lightbulb_or_an_older_afterglow_without_losing_anything()
    {
        // Arrange
        var script = ReadRepositoryFile(Path.Combine("Installer", "Installer.iss"));

        // Assert
        // The same AppId as LightBulb, so that Windows sees this as a newer version of it
        script.Should().Contain("#define AppId \"892F745F-A497-42ED-B503-8D74936D0BEB\"");
        script.Should().Contain("AppId={{{#AppId}}");
        script.Should().Contain(@"\Uninstall\{{#AppId}}_is1");

        // The names that the app itself uses
        script.Should().Contain($"#define AppName \"{Program.Name}\"");
        script.Should().Contain($"#define AppLegacyName \"{Program.LegacyName}\"");
        script
            .Should()
            .Contain($"StartHiddenArgument = '{StartOptions.IsInitiallyHiddenArgument}'");
        script.Should().Contain("AppMutex={#AppName}_Identity,{#AppLegacyName}_Identity");

        // The previous uninstaller deletes the settings, so they're copied first, and nothing is
        // uninstalled if they can't be
        var prepareToInstall = script[script.IndexOf("function PrepareToInstall")..];
        prepareToInstall
            .IndexOf("CopyLegacySettings()")
            .Should()
            .BePositive()
            .And.BeLessThan(prepareToInstall.IndexOf("Exec("));
    }

    [Fact]
    public void I_get_a_portable_copy_that_updates_everything_by_replacing_its_executable()
    {
        // Arrange
        var workflow = ReadRepositoryFile(Path.Combine(".github", "workflows", "build.yml"));
        var project = ReadRepositoryFile(Path.Combine("Afterglow", "Afterglow.csproj"));

        // Assert
        // Published as a single file, with the native libraries bundled into it too
        workflow.Should().Contain("-p:PublishSingleFile=true");
        project.Should().Contain("<IncludeNativeLibrariesForSelfExtract");

        // The release fails if anything ends up next to the executable, where updates would miss it
        workflow.Should().Contain("Check that the portable app is a single file");
        workflow.Should().Contain("$files.Count -ne 1 -or $files[0].Name -ne 'Afterglow.exe'");
    }

    [Fact]
    public void I_do_not_get_files_left_behind_by_an_update_that_no_longer_needs_them()
    {
        // Arrange
        var workflow = ReadRepositoryFile(Path.Combine(".github", "workflows", "build.yml"));

        // Line endings depend on how the repository was checked out
        var script = ReadRepositoryFile(Path.Combine("Installer", "Installer.iss"))
            .ReplaceLineEndings("\n");

        // Assert
        // The workflow lists the app's files, and the installer installs that list with them
        script.Should().Contain("#define AppFilesListName \".files\"");
        workflow.Should().Contain("Set-Content -Path (Join-Path $source '.files') -Encoding ascii");
        workflow
            .IndexOf("'.files'")
            .Should()
            .BeGreaterThan(workflow.IndexOf("Copy-Item -Path Afterglow/bin/publish/"))
            .And.BeLessThan(workflow.IndexOf("& $compiler.FullName Installer/Installer.iss"));

        // The previous list is read before the new files are copied, and files are only removed
        // once the new version is in place
        var curStepChanged = script[script.IndexOf("procedure CurStepChanged")..];
        curStepChanged.Should().Contain("if CurStep = ssInstall then\n    LoadPreviousAppFiles();");
        curStepChanged
            .IndexOf("RemoveObsoleteAppFiles();")
            .Should()
            .BeGreaterThan(curStepChanged.IndexOf("if CurStep = ssPostInstall then"));

        // Nothing outside the installation folder can be removed
        script.Should().Contain("(Pos('..', Result) > 0) or (Pos(':', Result) > 0)");
    }

    [Fact]
    public void I_get_releases_signed_with_every_file_of_afterglow_s_own_included()
    {
        // Arrange
        var workflow = ReadRepositoryFile(Path.Combine(".github", "workflows", "build.yml"));
        var props = ReadRepositoryFile("Directory.Build.props");
        var installerScript = ReadRepositoryFile(Path.Combine("Installer", "Installer.iss"));

        var app = System.Xml.Linq.XDocument.Parse(
            ReadRepositoryFile(Path.Combine(".signpath", "app.xml"))
        );
        var installer = System.Xml.Linq.XDocument.Parse(
            ReadRepositoryFile(Path.Combine(".signpath", "installer.xml"))
        );

        static string[] GetSignedPaths(System.Xml.Linq.XDocument configuration) =>
            configuration
                .Descendants()
                .Where(e => e.Name.LocalName == "pe-file")
                .Select(e =>
                    string.Join(
                        "/",
                        e.AncestorsAndSelf()
                            .Where(a => a.Name.LocalName is "pe-file" or "directory")
                            .Reverse()
                            .Select(a => (string?)a.Attribute("path"))
                    )
                )
                .ToArray();

        // Assert
        // The app's own files (and the portable executable) for every architecture, and nothing
        // else: the libraries it comes with aren't its own
        string[] runtimeIds = ["win-x64", "win-arm64", "win-x86"];
        GetSignedPaths(app)
            .Should()
            .BeEquivalentTo(
                runtimeIds.SelectMany(rid =>
                    new[]
                    {
                        "Afterglow.exe",
                        "Afterglow.dll",
                        "Afterglow.Core.dll",
                        "Afterglow.PlatformInterop.dll",
                        "portable/Afterglow.exe",
                    }.Select(file => $"{rid}/{file}")
                )
            );
        GetSignedPaths(installer)
            .Should()
            .BeEquivalentTo(runtimeIds.Select(rid => $"Afterglow-Installer.{rid}.exe"));

        // Every signed file must say that it's Afterglow, in the version being released
        app.Descendants()
            .Concat(installer.Descendants())
            .Where(e => e.Name.LocalName == "pe-file")
            .Should()
            .AllSatisfy(e =>
            {
                ((string?)e.Attribute("product-name")).Should().Be("Afterglow");
                ((string?)e.Attribute("product-version")).Should().Be("${version}");
            });
        props.Should().Contain("<Product>Afterglow</Product>");
        props
            .Should()
            .Contain(
                "<IncludeSourceRevisionInInformationalVersion>false</IncludeSourceRevisionInInformationalVersion>"
            );
        installerScript.Should().Contain("VersionInfoProductName={#AppName}");
        installerScript.Should().Contain("VersionInfoProductTextVersion={#AppVersion}");

        // The workflow uses these configurations, and checks that the files it got back are signed
        workflow.Should().Contain("artifact-configuration-slug: app");
        workflow.Should().Contain("artifact-configuration-slug: installer");
        workflow.Should().Contain("version: ${{ toJSON(needs.version.outputs.value) }}");
        workflow.Should().Contain("Get-AuthenticodeSignature");
    }

    [Fact]
    public void I_can_package_releases_for_every_architecture_with_the_files_the_workflow_expects()
    {
        // Arrange
        var workflow = ReadRepositoryFile(Path.Combine(".github", "workflows", "build.yml"));
        var script = ReadRepositoryFile(Path.Combine("Installer", "Installer.iss"));
        var project = ReadRepositoryFile(Path.Combine("Afterglow", "Afterglow.csproj"));

        // Assert
        workflow.Should().Contain("win-x64").And.Contain("win-arm64").And.Contain("win-x86");

        // The packaged files are named after what the build produces
        script.Should().Contain("OutputBaseFilename=Afterglow-Installer");
        workflow.Should().Contain("Installer/bin/Afterglow-Installer.exe");
        workflow.Should().Contain("Afterglow/bin/portable/Afterglow.exe");

        // The compiler is found wherever it's installed, rather than at a version-specific path
        workflow.Should().NotContain(@"Inno Setup 6\ISCC.exe");

        // The app's icon exists where the project expects it
        project.Should().Contain("<ApplicationIcon>../favicon.ico</ApplicationIcon>");
        File.Exists(Path.Combine(RepositoryDirPath, "favicon.ico")).Should().BeTrue();
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
