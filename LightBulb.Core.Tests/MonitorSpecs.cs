using System.Linq;
using AwesomeAssertions;
using Xunit;

namespace LightBulb.Core.Tests;

public class MonitorSpecs
{
    private static readonly ColorConfiguration ActiveConfiguration = new(4500, 0.95);
    private static readonly ColorConfiguration PausedConfiguration = ColorConfiguration.Default;

    // Same bounds as the app's settings
    private static MonitorColorContext CreateContext(
        ColorConfiguration? activeConfiguration = null,
        bool isGloballyPaused = false
    ) =>
        new(
            activeConfiguration ?? ActiveConfiguration,
            PausedConfiguration,
            isGloballyPaused,
            MinimumTemperature: 500,
            MaximumTemperature: 20_000,
            MinimumBrightness: 0.1,
            MaximumBrightness: 1
        );

    private static MonitorProfile CreateProfile(
        int temperatureOffset = 0,
        double brightnessOffset = 0,
        bool isEnabled = true
    ) =>
        new()
        {
            DevicePath = @"\\?\DISPLAY#TEST#1",
            LastKnownName = "Test display",
            IsEnabled = isEnabled,
            TemperatureOffset = temperatureOffset,
            BrightnessOffset = brightnessOffset,
        };

    private static AutoPauseSettings AllEnabledSettings { get; } =
        new(
            IsStreamingAutoPauseEnabled: true,
            IsPauseWhenScreenLockedEnabled: true,
            IsPauseWhenFullScreenEnabled: true,
            IsPerMonitorFullScreenPauseEnabled: true,
            IsApplicationWhitelistEnabled: true
        );

    [Fact]
    public void I_observe_the_cycle_configuration_on_a_display_without_a_profile()
    {
        // Act
        var configuration = MonitorColorResolver.Resolve(CreateContext(), null);

        // Assert
        configuration.Should().Be(ActiveConfiguration);
    }

    [Fact]
    public void I_observe_the_offsets_of_a_display_applied_on_top_of_the_cycle_configuration()
    {
        // Act
        var configuration = MonitorColorResolver.Resolve(
            CreateContext(),
            CreateProfile(-250, -0.05)
        );

        // Assert
        configuration.Temperature.Should().Be(4250);
        configuration.Brightness.Should().BeApproximately(0.90, 1e-9);
    }

    [Theory]
    [InlineData(19_800, 1000, 20_000)] // above the maximum temperature
    [InlineData(800, -1000, 500)] // below the minimum temperature
    public void I_observe_the_temperature_of_a_display_clamped_within_the_configured_bounds(
        double cycleTemperature,
        int temperatureOffset,
        double expectedTemperature
    )
    {
        // Act
        var configuration = MonitorColorResolver.Resolve(
            CreateContext(new ColorConfiguration(cycleTemperature, 0.5)),
            CreateProfile(temperatureOffset)
        );

        // Assert
        configuration.Temperature.Should().Be(expectedTemperature);
    }

    [Theory]
    [InlineData(0.95, 0.25, 1.0)] // above 100%
    [InlineData(0.2, -0.25, 0.1)] // below 10%
    public void I_observe_the_brightness_of_a_display_clamped_between_10_and_100_percent(
        double cycleBrightness,
        double brightnessOffset,
        double expectedBrightness
    )
    {
        // Act
        var configuration = MonitorColorResolver.Resolve(
            CreateContext(new ColorConfiguration(4500, cycleBrightness)),
            CreateProfile(brightnessOffset: brightnessOffset)
        );

        // Assert
        configuration.Brightness.Should().BeApproximately(expectedBrightness, 1e-9);
    }

    [Fact]
    public void I_observe_out_of_range_display_offsets_limited_to_the_supported_range()
    {
        // Act
        var configuration = MonitorColorResolver.Resolve(
            CreateContext(new ColorConfiguration(4500, 0.5)),
            CreateProfile(5000, 0.9)
        );

        // Assert
        configuration.Temperature.Should().Be(4500 + MonitorProfile.MaxTemperatureOffset);
        configuration
            .Brightness.Should()
            .BeApproximately(0.5 + MonitorProfile.MaxBrightnessOffset, 1e-9);
    }

    [Theory]
    [InlineData(137, 150)]
    [InlineData(-1234, -1000)]
    [InlineData(24, 0)]
    public void I_observe_display_temperature_offsets_snapped_to_50_kelvin_steps(
        double offset,
        int expectedOffset
    )
    {
        // Act & assert
        MonitorProfile.NormalizeTemperatureOffset(offset).Should().Be(expectedOffset);
    }

    [Fact]
    public void I_observe_the_primary_display_follow_the_cycle_exactly_ignoring_its_saved_offsets()
    {
        // Arrange
        var profile = CreateProfile(-500, -0.2);

        // Act
        var configuration = MonitorColorResolver.Resolve(CreateContext(), profile, isPrimary: true);

        // Assert
        configuration.Should().Be(ActiveConfiguration);

        // The saved offsets are kept, in case a different display becomes the primary one later
        profile.TemperatureOffset.Should().Be(-500);
        profile.BrightnessOffset.Should().Be(-0.2);
    }

    [Fact]
    public void I_observe_non_primary_displays_apply_their_saved_offsets_while_the_primary_display_does_not()
    {
        // Arrange
        var context = CreateContext();
        var primaryProfile = CreateProfile(300, 0.1);
        var secondaryProfile = CreateProfile(-250, -0.05);

        // Act
        var primary = MonitorColorResolver.Resolve(context, primaryProfile, isPrimary: true);
        var secondary = MonitorColorResolver.Resolve(context, secondaryProfile, isPrimary: false);

        // Assert
        primary.Should().Be(ActiveConfiguration);
        secondary.Temperature.Should().Be(4250);
        secondary.Brightness.Should().BeApproximately(0.90, 1e-9);
    }

    [Fact]
    public void I_observe_a_former_primary_display_apply_its_saved_offsets_once_it_is_no_longer_primary()
    {
        // Arrange
        var profile = CreateProfile(-250, -0.05);

        // Act
        var whilePrimary = MonitorColorResolver.Resolve(CreateContext(), profile, isPrimary: true);
        var afterwards = MonitorColorResolver.Resolve(CreateContext(), profile, isPrimary: false);

        // Assert
        whilePrimary.Should().Be(ActiveConfiguration);
        afterwards.Temperature.Should().Be(4250);
    }

    [Fact]
    public void I_observe_the_primary_display_still_being_excludable_and_pausable()
    {
        // Act
        var excluded = MonitorColorResolver.Resolve(
            CreateContext(),
            CreateProfile(isEnabled: false),
            isPrimary: true
        );

        var paused = MonitorColorResolver.Resolve(
            CreateContext(isGloballyPaused: true),
            CreateProfile(-250),
            isPrimary: true
        );

        // Assert
        excluded.Should().Be(MonitorColorResolver.NeutralConfiguration);
        paused.Should().Be(PausedConfiguration);
    }

    [Fact]
    public void I_observe_an_excluded_display_stay_neutral_regardless_of_the_cycle_or_pauses()
    {
        // Arrange
        var profile = CreateProfile(-500, -0.2, isEnabled: false);

        // Act
        var active = MonitorColorResolver.Resolve(CreateContext(), profile);
        var globallyPaused = MonitorColorResolver.Resolve(
            CreateContext(isGloballyPaused: true),
            profile
        );
        var pausedAlone = MonitorColorResolver.Resolve(CreateContext(), profile, isPaused: true);

        // Assert
        active.Should().Be(new ColorConfiguration(6600, 1));
        globallyPaused.Should().Be(new ColorConfiguration(6600, 1));
        pausedAlone.Should().Be(new ColorConfiguration(6600, 1));
    }

    [Fact]
    public void I_observe_only_the_display_running_a_fullscreen_application_being_paused()
    {
        // Arrange
        var state = AutoPause.Evaluate(
            new AutoPauseConditions(false, false, @"\\.\DISPLAY2", false),
            AllEnabledSettings
        );

        var monitors = new[] { @"\\.\DISPLAY1", @"\\.\DISPLAY2", @"\\.\DISPLAY3" };
        var context = CreateContext(isGloballyPaused: state.IsGloballyPaused);

        // Act
        var configurations = monitors
            .Select(m =>
                MonitorColorResolver.Resolve(context, null, isPaused: m == state.PausedMonitor)
            )
            .ToArray();

        // Assert
        state.Reason.Should().Be(PauseReason.None);
        configurations[0].Should().Be(ActiveConfiguration);
        configurations[1].Should().Be(PausedConfiguration);
        configurations[2].Should().Be(ActiveConfiguration);
    }

    [Fact]
    public void I_observe_all_displays_being_paused_by_a_fullscreen_application_when_not_paused_per_display()
    {
        // Act
        var state = AutoPause.Evaluate(
            new AutoPauseConditions(false, false, @"\\.\DISPLAY2", false),
            AllEnabledSettings with
            {
                IsPerMonitorFullScreenPauseEnabled = false,
            }
        );

        // Assert
        state.Should().Be(new AutoPauseState(PauseReason.FullScreen, null));
    }

    [Fact]
    public void I_observe_remote_streaming_taking_precedence_over_all_other_pause_reasons()
    {
        // Act
        var state = AutoPause.Evaluate(
            new AutoPauseConditions(true, true, @"\\.\DISPLAY2", true),
            AllEnabledSettings
        );

        // Assert
        state.Should().Be(new AutoPauseState(PauseReason.RemoteStreaming, null));
        state.IsGloballyPaused.Should().BeTrue();
    }

    [Fact]
    public void I_observe_remote_streaming_ignored_when_streaming_auto_pause_is_disabled()
    {
        // Act
        var state = AutoPause.Evaluate(
            new AutoPauseConditions(true, false, null, false),
            AllEnabledSettings with
            {
                IsStreamingAutoPauseEnabled = false,
            }
        );

        // Assert
        state.Should().Be(AutoPauseState.None);
    }

    [Fact]
    public void I_observe_a_whitelisted_application_pausing_all_displays_over_a_single_display_fullscreen_pause()
    {
        // Act
        var state = AutoPause.Evaluate(
            new AutoPauseConditions(false, false, @"\\.\DISPLAY2", true),
            AllEnabledSettings
        );

        // Assert
        state.Should().Be(new AutoPauseState(PauseReason.Whitelist, null));
    }

    [Fact]
    public void I_observe_no_failure_when_evaluating_with_no_displays_connected()
    {
        // Arrange
        // E.g. booted with the monitor off, or into a remote session before a display is attached
        var monitors = System.Array.Empty<string>();

        // Act
        var state = AutoPause.Evaluate(
            new AutoPauseConditions(false, false, null, false),
            AllEnabledSettings
        );

        var context = CreateContext(isGloballyPaused: state.IsGloballyPaused);
        var configurations = monitors
            .Select(m =>
                MonitorColorResolver.Resolve(context, null, isPaused: m == state.PausedMonitor)
            )
            .ToArray();

        // Assert
        state.Should().Be(AutoPauseState.None);
        configurations.Should().BeEmpty();
    }

    [Fact]
    public void I_observe_no_display_paused_when_the_fullscreen_display_is_no_longer_connected()
    {
        // Arrange
        // The fullscreen window was on a display that has since been unplugged
        var state = AutoPause.Evaluate(
            new AutoPauseConditions(false, false, @"\\.\DISPLAY7", false),
            AllEnabledSettings
        );

        var connectedMonitors = new[] { @"\\.\DISPLAY1", @"\\.\DISPLAY2" };
        var context = CreateContext(isGloballyPaused: state.IsGloballyPaused);

        // Act
        var configurations = connectedMonitors
            .Select(m =>
                MonitorColorResolver.Resolve(context, null, isPaused: m == state.PausedMonitor)
            )
            .ToArray();

        // Assert
        configurations.Should().AllBeEquivalentTo(ActiveConfiguration);
    }
}
