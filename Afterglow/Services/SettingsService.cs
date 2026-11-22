using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Text.Json.Serialization;
using System.Threading;
using Afterglow.Core;
using Afterglow.Framework;
using Afterglow.Localization;
using Afterglow.Models;
using Afterglow.PlatformInterop;
using Cogwheel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Win32;

namespace Afterglow.Services;

[ObservableObject]
public partial class SettingsService : SettingsBase
{
    private readonly string _filePath;

    public SettingsService()
        : this(StartOptions.Current.SettingsPath) { }

    // Allows loading settings from a specific file (e.g. in tests)
    internal SettingsService(string filePath)
        : base(filePath, SettingsSerializerContext.Default) => _filePath = filePath;

    private readonly RegistrySwitch<int> _extendedGammaRangeSwitch = new(
        RegistryHive.LocalMachine,
        @"Software\Microsoft\Windows NT\CurrentVersion\ICM",
        "GdiICMGammaRange",
        256
    );

    // What the startup entry runs
    internal static string AutoStartCommand { get; } =
        $"\"{Program.ExecutableFilePath}\" {StartOptions.IsInitiallyHiddenArgument}";

    private readonly RegistrySwitch<string> _autoStartSwitch = new(
        RegistryHive.CurrentUser,
        @"Software\Microsoft\Windows\CurrentVersion\Run",
        Program.Name,
        AutoStartCommand,
        // Paths aren't case-sensitive, and the installer may write the entry with a differently
        // cased path (e.g. the folder name as the user typed it)
        StringComparer.OrdinalIgnoreCase
    );

    [ObservableProperty]
    public partial bool IsFirstTimeExperienceEnabled { get; set; } = true;

    // The limited gamma range is only pointed out once: it can also be unlocked in the settings
    // later, and the installer unlocks it anyway
    [ObservableProperty]
    public partial bool IsGammaRangePromptEnabled { get; set; } = true;

    [ObservableProperty]
    [JsonIgnore] // comes from registry
    public partial bool IsExtendedGammaRangeUnlocked { get; set; }

    // General

    public double MinimumTemperature => 500;

    public double MaximumTemperature => 20_000;

    public double MinimumBrightness => 0.1;

    public double MaximumBrightness => 1;

    [ObservableProperty]
    public partial ColorConfiguration DayConfiguration { get; set; } = new(6600, 1);

    [ObservableProperty]
    public partial ColorConfiguration NightConfiguration { get; set; } = new(3900, 0.85);

    [ObservableProperty]
    public partial bool IsEveningConfigurationEnabled { get; set; } = true;

    [ObservableProperty]
    public partial ColorConfiguration EveningConfiguration { get; set; } = new(4500, 0.95);

    [ObservableProperty]
    public partial TimeOnly NightStartTime { get; set; } = new(22, 00);

    // Always kept within 0-3 hours, whether it's set from the UI or loaded from the file
    public TimeSpan ConfigurationTransitionDuration
    {
        get;
        set => SetProperty(ref field, Cycle.ClampTransitionDuration(value));
    } = TimeSpan.FromMinutes(40);

    [ObservableProperty]
    public partial double ConfigurationTransitionOffset { get; set; }

    [ObservableProperty]
    public partial TimeSpan ConfigurationSmoothingMaxDuration { get; set; } =
        TimeSpan.FromSeconds(5);

    // Location

    [ObservableProperty]
    public partial bool IsManualSunriseSunsetEnabled { get; set; } = true;

    [ObservableProperty]
    [JsonPropertyName("ManualSunriseTime")]
    public partial TimeOnly ManualSunrise { get; set; } = new(07, 20);

    [ObservableProperty]
    [JsonPropertyName("ManualSunsetTime")]
    public partial TimeOnly ManualSunset { get; set; } = new(16, 30);

    [ObservableProperty]
    public partial GeoLocation? Location { get; set; }

    // Advanced

    [ObservableProperty]
    public partial Language Language { get; set; }

    [ObservableProperty]
    [JsonIgnore] // comes from registry
    public partial bool IsAutoStartEnabled { get; set; }

    // Hide to the system tray on launch, even when not started automatically with Windows
    [ObservableProperty]
    public partial bool IsStartMinimizedEnabled { get; set; }

    // Look for newer releases on GitHub now and then. Stored under the name the original
    // LightBulb used for its updates, so that turning them off there carries over.
    [ObservableProperty]
    [JsonPropertyName("IsAutoUpdateEnabled")]
    public partial bool IsUpdateCheckEnabled { get; set; } = true;

    // How often to look for newer releases, when that's turned on. Weekly by default: releases
    // don't come out more often, and a fix for a new release has time to come out before most
    // copies even see that release.
    [ObservableProperty]
    public partial UpdateCheckFrequency UpdateCheckFrequency { get; set; } =
        UpdateCheckFrequency.Weekly;

    // When the last check for updates succeeded, and the newer version it found (if any). They're
    // remembered between launches, so that checks follow the chosen frequency rather than happening
    // on every launch, and so that an update that was found is offered again after a restart.
    [ObservableProperty]
    public partial DateTimeOffset? LastUpdateCheckTime { get; set; }

    [ObservableProperty]
    public partial string? AvailableUpdateVersion { get; set; }

    [ObservableProperty]
    public partial bool IsDefaultToDayConfigurationEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsConfigurationSmoothingEnabled { get; set; } = true;

    [ObservableProperty]
    public partial bool IsPauseWhenFullScreenEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsPauseWhenScreenLockedEnabled { get; set; }

    // When pausing for fullscreen applications, only pause the display that runs it
    [ObservableProperty]
    public partial bool IsPerMonitorFullscreenPauseEnabled { get; set; } = true;

    // Pause on all displays while the computer is being streamed (Sunshine) or accessed remotely (RDP)
    // Opt-in, so that Afterglow never pauses unexpectedly
    [ObservableProperty]
    public partial bool IsStreamingAutoPauseEnabled { get; set; }

    // Local TCP ports of streaming hosts (e.g. Sunshine's RTSP port), separated by commas.
    // Remote Desktop is detected regardless of these.
    [ObservableProperty]
    public partial string StreamingPorts { get; set; } = StreamingPortList.Default;

    // Displays

    // Stored as a list (rather than a dictionary keyed by device path) to keep the file readable
    [ObservableProperty]
    public partial IReadOnlyList<MonitorProfile>? MonitorProfiles { get; set; }

    // Whether display temperature offsets scale perceptually (in mireds) across the cycle, so that
    // dissimilar panels stay matched at night, rather than being fixed Kelvin values.
    // Offsets are stored in Kelvin either way, so switching never changes the saved values.
    [ObservableProperty]
    public partial bool IsPerceptualMonitorOffsetEnabled { get; set; } = true;

    [JsonIgnore]
    public MonitorOffsetScaling MonitorOffsetScaling =>
        IsPerceptualMonitorOffsetEnabled
            ? MonitorOffsetScaling.Perceptual
            : MonitorOffsetScaling.Fixed;

    [ObservableProperty]
    public partial bool IsGammaPollingEnabled { get; set; }

    [ObservableProperty]
    public partial TimeSpan GammaPollingInterval { get; set; } = TimeSpan.FromSeconds(1);

    // Application whitelist

    [ObservableProperty]
    public partial bool IsApplicationWhitelistEnabled { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<ExternalApplication>? WhitelistedApplications { get; set; }

    // Words that pause when they're part of the foreground window's title (ignoring case)
    [ObservableProperty]
    public partial IReadOnlyList<string>? WhitelistedWindowTitles { get; set; }

    // Each kind of whitelist match can be turned off on its own, without losing its list
    [ObservableProperty]
    public partial bool IsWhitelistedApplicationPauseEnabled { get; set; } = true;

    [ObservableProperty]
    public partial bool IsWhitelistedWindowTitlePauseEnabled { get; set; } = true;

    // Only pause the displays that show whitelisted windows, rather than all of them
    [ObservableProperty]
    public partial bool IsPerMonitorWhitelistPauseEnabled { get; set; } = true;

    // HotKeys

    [ObservableProperty]
    public partial HotKey ToggleHotKey { get; set; }

    [ObservableProperty]
    [JsonPropertyName("FocusWindowHotKey")]
    public partial HotKey ToggleWindowHotKey { get; set; }

    [ObservableProperty]
    public partial HotKey IncreaseTemperatureOffsetHotKey { get; set; }

    [ObservableProperty]
    public partial HotKey DecreaseTemperatureOffsetHotKey { get; set; }

    [ObservableProperty]
    public partial HotKey IncreaseBrightnessOffsetHotKey { get; set; }

    [ObservableProperty]
    public partial HotKey DecreaseBrightnessOffsetHotKey { get; set; }

    [ObservableProperty]
    public partial HotKey ResetConfigurationOffsetHotKey { get; set; }

    [ObservableProperty]
    public partial HotKey ToggleGrayscaleHotKey { get; set; }

    // Grayscale

    [ObservableProperty]
    public partial GrayscaleSchedule GrayscaleSchedule { get; set; } = GrayscaleSchedule.Off;

    // When disabled, grayscale uses a neutral white point instead of the color temperature
    [ObservableProperty]
    public partial bool IsGrayscaleTemperatureShiftEnabled { get; set; } = true;

    // Grayscale state chosen manually, which applies until the schedule next changes it
    // (null when grayscale simply follows the schedule)
    [ObservableProperty]
    public partial bool? GrayscaleOverride { get; set; }

    public override void Reset()
    {
        var isGammaRangePromptEnabled = IsGammaRangePromptEnabled;

        base.Reset();

        // Don't reset the first-time experience, or bring back prompts that were already shown
        IsFirstTimeExperienceEnabled = false;
        IsGammaRangePromptEnabled = isGammaRangePromptEnabled;

        // Trigger UI updates
        OnPropertyChanged(string.Empty);
    }

    public override void Save()
    {
        // Disallow auto-start in debug mode to make things simpler
#if DEBUG
        IsAutoStartEnabled = false;
#endif

        TrySaveToFile();

        // Update values in the registry. Each one separately, so that failing to update one (e.g.
        // when privilege elevation for the gamma range is declined) doesn't skip the other.
        // Failures can happen if the user doesn't have the necessary permissions to update the
        // corresponding registry keys, and privilege elevation has failed or isn't possible.
        // Saving happens at any time (including during shutdown), so it must never throw.
        // https://github.com/Tyrrrz/LightBulb/issues/335
        TryUpdateRegistry(() => _extendedGammaRangeSwitch.IsSet = IsExtendedGammaRangeUnlocked);
        TryUpdateRegistry(() => _autoStartSwitch.IsSet = IsAutoStartEnabled);

        // Trigger UI updates
        OnPropertyChanged(string.Empty);
    }

    private static void TryUpdateRegistry(Action update)
    {
        try
        {
            update();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to update registry: {ex}");
        }
    }

    // Errors that mean the file can't be accessed right now, as opposed to it having invalid contents
    private static bool IsFileAccessError(Exception ex) =>
        ex
            is UnauthorizedAccessException
                or SecurityException
                or (IOException and not (FileNotFoundException or DirectoryNotFoundException));

    // Set when the settings file exists but couldn't be read, so that it doesn't get overwritten
    private bool _isFileUnreadable;

    internal static int FileAccessAttempts => 5;

    internal static TimeSpan FileAccessRetryDelay { get; } = TimeSpan.FromMilliseconds(100);

    // Writes the settings file only, without touching the registry. Returns false if it couldn't be
    // written (e.g. it's read-only or locked), which must not crash the app, as saving happens
    // from the dashboard and the settings page at any time.
    internal bool TrySaveToFile()
    {
        if (_isFileUnreadable)
        {
            Debug.WriteLine("Not saving settings, because the existing file could not be read.");
            return false;
        }

        try
        {
            base.Save();
            return true;
        }
        // Any file system error, including a missing or invalid directory
        catch (Exception ex)
            when (ex is IOException or UnauthorizedAccessException or SecurityException)
        {
            Debug.WriteLine($"Failed to save settings: {ex}");
            return false;
        }
    }

    // The file may be briefly locked by another process (e.g. a cloud sync client or an antivirus
    // scan right after signing in), so reading is retried a few times before giving up
    private bool LoadFileWithRetries()
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return base.Load();
            }
            catch (Exception ex) when (IsFileAccessError(ex) && attempt < FileAccessAttempts)
            {
                Debug.WriteLine($"Settings file is not accessible (attempt {attempt}): {ex}");
                Thread.Sleep(FileAccessRetryDelay);
            }
        }
    }

    public override bool Load()
    {
        bool wasLoaded;
        _isFileUnreadable = false;

        try
        {
            wasLoaded = LoadFileWithRetries();
        }
        catch (Exception ex) when (IsFileAccessError(ex))
        {
            // The file exists but can't be read. It's not corrupt, so it's neither backed up nor
            // replaced: run with defaults for now, and leave the file alone for the next launch.
            Debug.WriteLine($"Failed to read settings, using defaults for this session: {ex}");

            Reset();
            _isFileUnreadable = true;

            wasLoaded = false;
        }
        catch (Exception ex)
        {
            // The file is corrupted (e.g. malformed JSON or invalid values). Rather than crashing
            // on startup, keep a copy of it for reference, and start over with clean defaults for
            // everything that can't be read, keeping everything that still can.
            Debug.WriteLine($"Failed to load settings, recovering what can be read: {ex}");

            BackUpCorruptFile();
            Reset();

            wasLoaded = TryRecoverReadableSettings();
        }

        // Settings files may come from older versions, may have been edited by hand, or may contain
        // values that newer versions no longer accept, so bring everything within valid ranges
        SanitizeLoadedValues();

        // The evening is an intermediate stage, so keep it between the day and night values.
        // This matters for settings files saved before the evening stage existed, where the
        // default evening configuration may not fit the user's own day and night configurations.
        EveningConfiguration = new ColorConfiguration(
            ClampBetween(
                EveningConfiguration.Temperature,
                NightConfiguration.Temperature,
                DayConfiguration.Temperature
            ),
            ClampBetween(
                EveningConfiguration.Brightness,
                NightConfiguration.Brightness,
                DayConfiguration.Brightness
            )
        );

        // Get values from the registry
        IsExtendedGammaRangeUnlocked = _extendedGammaRangeSwitch.IsSet;
        IsAutoStartEnabled = _autoStartSwitch.IsSet;

        // Trigger UI updates
        OnPropertyChanged(string.Empty);

        return wasLoaded;
    }

    public ColorConfiguration GetStageConfiguration(CycleStage stage) =>
        stage switch
        {
            CycleStage.Day => DayConfiguration,
            CycleStage.Evening => EveningConfiguration,
            _ => NightConfiguration,
        };

    // Day, evening, and night values are kept in order (day >= evening >= night),
    // so changing one stage may push the others along with it.
    private void SetStageValues(
        CycleStage stage,
        double value,
        Func<ColorConfiguration, double> getValue,
        Func<ColorConfiguration, double, ColorConfiguration> withValue
    )
    {
        var day = getValue(DayConfiguration);
        var evening = getValue(EveningConfiguration);
        var night = getValue(NightConfiguration);

        switch (stage)
        {
            case CycleStage.Day:
                day = value;
                evening = Math.Min(evening, day);
                night = Math.Min(night, evening);
                break;
            case CycleStage.Evening:
                evening = value;
                day = Math.Max(day, evening);
                night = Math.Min(night, evening);
                break;
            default:
                night = value;
                evening = Math.Max(evening, night);
                day = Math.Max(day, evening);
                break;
        }

        DayConfiguration = withValue(DayConfiguration, day);
        EveningConfiguration = withValue(EveningConfiguration, evening);
        NightConfiguration = withValue(NightConfiguration, night);
    }

    public void SetStageTemperature(CycleStage stage, double temperature) =>
        SetStageValues(
            stage,
            Math.Clamp(temperature, MinimumTemperature, MaximumTemperature),
            c => c.Temperature,
            (c, v) => c with { Temperature = v }
        );

    public void SetStageBrightness(CycleStage stage, double brightness) =>
        SetStageValues(
            stage,
            Math.Clamp(brightness, MinimumBrightness, MaximumBrightness),
            c => c.Brightness,
            (c, v) => c with { Brightness = v }
        );

    // Called for every display on every update tick, so it avoids allocations
    public MonitorProfile? TryGetMonitorProfile(string devicePath)
    {
        if (MonitorProfiles is not { } profiles)
            return null;

        foreach (var profile in profiles)
        {
            if (string.Equals(profile.DevicePath, devicePath, StringComparison.OrdinalIgnoreCase))
                return profile;
        }

        return null;
    }

    /// <summary>
    /// Re-attaches saved profiles to connected displays whose device paths have changed since
    /// (e.g. after a graphics driver update), so that their preferences aren't lost.
    /// </summary>
    /// <returns>Whether any profile was updated.</returns>
    public bool ReconcileMonitorProfiles(IReadOnlyList<string> connectedDevicePaths)
    {
        if (MonitorProfiles is not { Count: > 0 } profiles)
            return false;

        var rekeyed = MonitorProfileMatcher.TryRekey(connectedDevicePaths, profiles);
        if (rekeyed is null)
            return false;

        MonitorProfiles = rekeyed;
        return true;
    }

    // Creates the profile if the display doesn't have one yet
    public void UpdateMonitorProfile(
        string devicePath,
        string name,
        Func<MonitorProfile, MonitorProfile> update
    )
    {
        var existing =
            TryGetMonitorProfile(devicePath)
            ?? new MonitorProfile { DevicePath = devicePath, LastKnownName = name };

        var updated = update(existing) with { LastKnownName = name };
        if (updated == existing)
            return;

        MonitorProfiles =
        [
            .. (MonitorProfiles ?? []).Where(p =>
                !string.Equals(p.DevicePath, devicePath, StringComparison.OrdinalIgnoreCase)
            ),
            updated,
        ];
    }

    public void ForgetMonitorProfile(string devicePath) =>
        MonitorProfiles = [
            .. (MonitorProfiles ?? []).Where(p =>
                !string.Equals(p.DevicePath, devicePath, StringComparison.OrdinalIgnoreCase)
            ),
        ];

    private void SanitizeLoadedValues()
    {
        static TimeSpan ClampTime(TimeSpan value, TimeSpan min, TimeSpan max) =>
            value < min ? min
            : value > max ? max
            : value;

        DayConfiguration = DayConfiguration.Clamp(
            MinimumTemperature,
            MaximumTemperature,
            MinimumBrightness,
            MaximumBrightness
        );

        EveningConfiguration = EveningConfiguration.Clamp(
            MinimumTemperature,
            MaximumTemperature,
            MinimumBrightness,
            MaximumBrightness
        );

        NightConfiguration = NightConfiguration.Clamp(
            MinimumTemperature,
            MaximumTemperature,
            MinimumBrightness,
            MaximumBrightness
        );

        // Also enforced by the property itself
        ConfigurationTransitionDuration = Cycle.ClampTransitionDuration(
            ConfigurationTransitionDuration
        );

        ConfigurationTransitionOffset = double.IsFinite(ConfigurationTransitionOffset)
            ? Math.Clamp(ConfigurationTransitionOffset, 0, 1)
            : 0;

        ConfigurationSmoothingMaxDuration = ClampTime(
            ConfigurationSmoothingMaxDuration,
            TimeSpan.Zero,
            TimeSpan.FromMinutes(1)
        );

        // A very short interval would force gamma updates on every tick and waste CPU
        GammaPollingInterval = ClampTime(
            GammaPollingInterval,
            TimeSpan.FromSeconds(0.5),
            TimeSpan.FromSeconds(10)
        );

        StreamingPorts = StreamingPortList.Normalize(StreamingPorts);

        // Blank entries (which could otherwise match every window), padding, and duplicates
        if (WhitelistedWindowTitles is not null)
            WhitelistedWindowTitles = WindowTitleKeywords.Normalize(WhitelistedWindowTitles);

        // Coordinates that don't exist on Earth (e.g. edited by hand) are treated as not set
        if (Location is { IsValid: false })
            Location = null;

        // Numeric values that don't correspond to any schedule
        if (!Enum.IsDefined(GrayscaleSchedule))
            GrayscaleSchedule = GrayscaleSchedule.Off;

        if (!Enum.IsDefined(UpdateCheckFrequency))
            UpdateCheckFrequency = UpdateCheckFrequency.Weekly;

        // With grayscale always on, turning it off manually only lasts for the session
        if (GrayscaleSchedule == GrayscaleSchedule.On)
            GrayscaleOverride = null;

        // Drop entries without a device path (which can't be matched to any display) and
        // duplicates (keeping the last one), and keep offsets within the supported range
        if (MonitorProfiles is not null)
        {
            MonitorProfiles = MonitorProfiles
                .Where(p => p is not null && !string.IsNullOrWhiteSpace(p.DevicePath))
                .GroupBy(p => p.DevicePath, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.Last())
                .Select(p =>
                    p with
                    {
                        LastKnownName = p.LastKnownName ?? "",
                        TemperatureOffset = MonitorProfile.NormalizeTemperatureOffset(
                            p.TemperatureOffset
                        ),
                        BrightnessOffset = double.IsFinite(p.BrightnessOffset)
                            ? MonitorProfile.NormalizeBrightnessOffset(p.BrightnessOffset)
                            : 0,
                    }
                )
                .ToArray();
        }
    }

    // Copies (rather than moves) the corrupt file, so that if Afterglow is closed before the
    // settings are saved again, the next launch doesn't mistake it for a fresh install
    private void BackUpCorruptFile()
    {
        try
        {
            var filePath = _filePath;
            if (File.Exists(filePath))
                File.Copy(filePath, filePath + ".bak", true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to back up the corrupt settings file: {ex}");
        }
    }

    // Unlike Math.Clamp, this doesn't throw if the bounds are in the wrong order
    private static double ClampBetween(double value, double bound1, double bound2) =>
        Math.Clamp(value, Math.Min(bound1, bound2), Math.Max(bound1, bound2));
}

public partial class SettingsService
{
    [JsonSerializable(typeof(SettingsService))]
    [JsonSourceGenerationOptions(UseStringEnumConverter = true)]
    private partial class SettingsSerializerContext : JsonSerializerContext;
}
