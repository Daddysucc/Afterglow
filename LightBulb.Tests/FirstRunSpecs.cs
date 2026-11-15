using System;
using System.IO;
using System.Threading;
using AwesomeAssertions;
using LightBulb.PlatformInterop;
using LightBulb.Services;
using LightBulb.Utils;
using Xunit;

namespace LightBulb.Tests;

public class FirstRunSpecs : IDisposable
{
    private readonly string _directoryPath = Path.Combine(
        Path.GetTempPath(),
        "LightBulb.Tests." + Guid.NewGuid().ToString("N")
    );

    // Unique, so that these never interfere with a running copy of the app
    private readonly string _instanceName = "LightBulb.Tests." + Guid.NewGuid().ToString("N");

    public FirstRunSpecs() => Directory.CreateDirectory(_directoryPath);

    // ---- Single instance ----

    [Fact]
    public void I_cannot_start_a_second_copy_while_one_is_running()
    {
        // Arrange
        using var first = SingleInstance.TryAcquire(_instanceName);

        // Act
        using var second = SingleInstance.TryAcquire(_instanceName);

        // Assert
        first.Should().NotBeNull();
        second.Should().BeNull();
    }

    [Fact]
    public void I_can_bring_the_running_copy_to_the_front_by_starting_the_app_again()
    {
        // Arrange
        using var instance = SingleInstance.TryAcquire(_instanceName);
        using var activated = new ManualResetEventSlim();
        instance!.ListenForActivation(activated.Set);

        // Act
        var isActivated = SingleInstance.TryActivateRunningInstance(_instanceName);

        // Assert
        isActivated.Should().BeTrue();
        activated.Wait(TimeSpan.FromSeconds(10)).Should().BeTrue();
    }

    [Fact]
    public void I_do_not_lose_a_request_to_show_the_app_while_it_is_still_starting()
    {
        // Arrange
        using var instance = SingleInstance.TryAcquire(_instanceName);
        using var activated = new ManualResetEventSlim();

        // Act
        // Requested before the running copy has started listening
        SingleInstance.TryActivateRunningInstance(_instanceName).Should().BeTrue();
        instance!.ListenForActivation(activated.Set);

        // Assert
        activated.Wait(TimeSpan.FromSeconds(10)).Should().BeTrue();
    }

    [Fact]
    public void I_can_start_the_app_again_once_the_running_copy_has_exited()
    {
        // Arrange
        SingleInstance.TryAcquire(_instanceName)!.Dispose();

        // Act
        var isActivated = SingleInstance.TryActivateRunningInstance(_instanceName);
        using var instance = SingleInstance.TryAcquire(_instanceName);

        // Assert
        isActivated.Should().BeFalse();
        instance.Should().NotBeNull();
    }

    // ---- Temporary locations ----

    private const string Downloads = @"C:\Users\Someone\Downloads";
    private const string Temp = @"C:\Users\Someone\AppData\Local\Temp";

    [Theory]
    [InlineData(@"C:\Users\Someone\Downloads")]
    [InlineData(@"C:\Users\Someone\Downloads\")]
    [InlineData(@"C:\Users\Someone\Downloads\Afterglow")]
    [InlineData(@"c:\users\someone\downloads\afterglow")]
    [InlineData(@"C:\Users\Someone\AppData\Local\Temp\Temp1_Afterglow.zip")]
    public void I_see_downloads_and_temporary_folders_treated_as_temporary(string dirPath)
    {
        // Act & assert
        AppLocation.IsTemporary(dirPath, [Downloads, Temp]).Should().BeTrue();
    }

    [Theory]
    [InlineData(@"C:\Program Files (x86)\LightBulb")]
    [InlineData(@"C:\Users\Someone\Tools\Afterglow")]
    [InlineData(@"C:\Users\Someone\Desktop")]
    // Only shares the beginning of the name
    [InlineData(@"C:\Users\Someone\DownloadsArchive\Afterglow")]
    [InlineData(@"C:\Users\Someone\AppData\Local\TempFiles")]
    public void I_see_other_folders_treated_as_permanent(string dirPath)
    {
        // Act & assert
        AppLocation.IsTemporary(dirPath, [Downloads, Temp]).Should().BeFalse();
    }

    [Fact]
    public void I_see_a_folder_treated_as_permanent_when_the_downloads_folder_is_unknown()
    {
        // Act & assert
        AppLocation
            .IsTemporary(@"C:\Users\Someone\Downloads", [null, "", "  "])
            .Should()
            .BeFalse();
    }

    [Fact]
    public void I_can_find_the_downloads_folder()
    {
        // Act
        var downloadsDirPath = KnownFolders.TryGetDownloadsDirPath();

        // Assert
        downloadsDirPath.Should().NotBeNullOrWhiteSpace();
        Path.IsPathFullyQualified(downloadsDirPath!).Should().BeTrue();
    }

    // ---- Gamma range prompt ----

    private SettingsService LoadSettings(string json)
    {
        var filePath = Path.Combine(_directoryPath, "Settings.json");
        File.WriteAllText(filePath, json);

        var settings = new SettingsService(filePath);
        settings.Load();

        return settings;
    }

    [Fact]
    public void I_get_asked_about_the_gamma_range_once_with_new_or_older_settings()
    {
        // Act
        var settings = LoadSettings("""{ "IsFirstTimeExperienceEnabled": false }""");

        // Assert
        settings.IsGammaRangePromptEnabled.Should().BeTrue();
    }

    [Fact]
    public void I_do_not_get_asked_about_the_gamma_range_again_after_resetting_settings()
    {
        // Arrange
        var settings = LoadSettings("""{ "IsGammaRangePromptEnabled": false }""");

        // Act
        settings.Reset();

        // Assert
        settings.IsGammaRangePromptEnabled.Should().BeFalse();
        settings.IsFirstTimeExperienceEnabled.Should().BeFalse();
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_directoryPath, true);
        }
        catch
        {
            // Ignore
        }
    }
}
