using System;
using System.IO;
using System.Linq;
using AwesomeAssertions;
using LightBulb.Core;
using LightBulb.Services;
using Xunit;

namespace LightBulb.Tests;

public class WhitelistSpecs : IDisposable
{
    private const string Left = @"\\.\DISPLAY1";
    private const string Center = @"\\.\DISPLAY2";
    private const string Right = @"\\.\DISPLAY3";

    private static readonly string[] ConnectedMonitors = [Left, Center, Right];

    private static AutoPauseSettings PerMonitorSettings { get; } =
        new(
            IsStreamingAutoPauseEnabled: true,
            IsPauseWhenScreenLockedEnabled: true,
            IsPauseWhenFullScreenEnabled: true,
            IsPerMonitorFullScreenPauseEnabled: true,
            IsApplicationWhitelistEnabled: true,
            IsPerMonitorWhitelistPauseEnabled: true
        );

    private static AutoPauseConditions Conditions(string? whitelistedMonitor) =>
        new(
            IsRemoteStreamingActive: false,
            IsScreenLocked: false,
            FullScreenMonitor: null,
            IsWhitelistMatched: whitelistedMonitor is not null,
            whitelistedMonitor,
            ConnectedMonitors
        );

    // ---- Title keywords ----

    [Theory]
    [InlineData("Netflix - Google Chrome")]
    [InlineData("Network Connections")]
    [InlineData("INTERNET")]
    public void I_can_match_a_window_by_part_of_its_title_ignoring_case(string title)
    {
        // Act & assert
        WindowTitleKeywords.IsMatch(title, ["net"]).Should().BeTrue();
    }

    [Theory]
    [InlineData("Spotify Premium")]
    [InlineData("")]
    [InlineData(null)]
    public void I_cannot_match_a_window_whose_title_does_not_contain_a_keyword(string? title)
    {
        // Act & assert
        WindowTitleKeywords.IsMatch(title, ["net", "youtube"]).Should().BeFalse();
    }

    [Fact]
    public void I_cannot_match_every_window_with_a_blank_keyword()
    {
        // Act & assert
        WindowTitleKeywords.IsMatch("Anything at all", ["", "   ", "\t"]).Should().BeFalse();
    }

    [Fact]
    public void I_can_match_a_window_with_a_keyword_that_has_surrounding_spaces()
    {
        // Act & assert
        WindowTitleKeywords.IsMatch("Netflix", ["  netf  "]).Should().BeTrue();
    }

    [Fact]
    public void I_get_keywords_trimmed_without_blanks_or_duplicates()
    {
        // Act
        var keywords = WindowTitleKeywords.Normalize([
            "  Netflix ",
            "",
            "   ",
            null,
            "netflix",
            "YouTube",
            "youtube  ",
        ]);

        // Assert
        keywords.Should().Equal("Netflix", "YouTube");
    }

    [Fact]
    public void I_get_no_keywords_from_a_missing_list()
    {
        // Act & assert
        WindowTitleKeywords.Normalize(null).Should().BeEmpty();
    }

    // ---- Rules ----

    private const string GamePath = @"C:\Games\Game\game.exe";

    [Fact]
    public void I_can_match_a_window_by_its_application_or_its_title()
    {
        // Arrange
        var rules = WhitelistRules.Create(true, [GamePath], true, ["netflix"]);

        // Act & assert
        rules.IsMatch(() => "Untitled", () => @"c:\games\game\GAME.EXE").Should().BeTrue();
        rules.IsMatch(() => "Netflix", () => @"C:\Browser\browser.exe").Should().BeTrue();
        rules.IsMatch(() => "Untitled", () => @"C:\Browser\browser.exe").Should().BeFalse();
    }

    [Fact]
    public void I_cannot_match_by_application_when_application_matching_is_off()
    {
        // Arrange
        var rules = WhitelistRules.Create(false, [GamePath], true, ["netflix"]);

        // Act & assert
        rules.IsMatch(() => "Untitled", () => GamePath).Should().BeFalse();
        rules.IsMatch(() => "Netflix", () => GamePath).Should().BeTrue();
    }

    [Fact]
    public void I_cannot_match_by_title_when_title_matching_is_off()
    {
        // Arrange
        var rules = WhitelistRules.Create(true, [GamePath], false, ["netflix"]);

        // Act & assert
        rules.IsMatch(() => "Netflix", () => @"C:\Browser\browser.exe").Should().BeFalse();
        rules.IsMatch(() => "Netflix", () => GamePath).Should().BeTrue();
    }

    [Fact]
    public void I_do_not_get_window_titles_read_when_title_matching_is_off_or_has_no_keywords()
    {
        // Arrange
        var titleReads = 0;
        string? GetTitle()
        {
            titleReads++;
            return "Netflix";
        }

        var rulesOff = WhitelistRules.Create(true, [GamePath], false, ["netflix"]);
        var rulesEmpty = WhitelistRules.Create(true, [GamePath], true, ["", "  "]);

        // Act
        rulesOff.IsMatch(GetTitle, () => GamePath);
        rulesEmpty.IsMatch(GetTitle, () => GamePath);

        // Assert
        titleReads.Should().Be(0);
        rulesOff.IsTitleChecked.Should().BeFalse();
        rulesEmpty.IsTitleChecked.Should().BeFalse();
    }

    [Fact]
    public void I_do_not_get_window_processes_looked_up_when_a_title_already_matched()
    {
        // Arrange
        var rules = WhitelistRules.Create(true, [GamePath], true, ["netflix"]);
        var processLookups = 0;

        // Act
        var isMatch = rules.IsMatch(
            () => "Netflix",
            () =>
            {
                processLookups++;
                return GamePath;
            }
        );

        // Assert
        isMatch.Should().BeTrue();
        processLookups.Should().Be(0);
    }

    [Fact]
    public void I_get_no_windows_looked_at_when_both_kinds_of_matching_are_off()
    {
        // Act & assert
        WhitelistRules.Create(false, [GamePath], false, ["netflix"]).IsEmpty.Should().BeTrue();
        WhitelistRules.Create(true, [], true, []).IsEmpty.Should().BeTrue();
        WhitelistRules.Create(true, null, true, null).IsEmpty.Should().BeTrue();
    }

    // ---- Foreground window only ----

    private const string NetflixTitle = "Netflix - Google Chrome";
    private const string BrowserPath = @"C:\Browser\browser.exe";
    private const string EditorPath = @"C:\Windows\notepad.exe";

    private static readonly ColorConfiguration ActiveConfiguration = new(3400, 0.8);
    private static readonly ColorConfiguration PausedConfiguration = ColorConfiguration.Default;

    // Counts how often its details are looked up
    private sealed class FakeWindow(string title, string executableFilePath, string monitor)
        : IWhitelistWindow
    {
        public int Lookups { get; private set; }

        public string? TryGetTitle()
        {
            Lookups++;
            return title;
        }

        public string? TryGetExecutableFilePath()
        {
            Lookups++;
            return executableFilePath;
        }

        public string? TryGetMonitor()
        {
            Lookups++;
            return monitor;
        }
    }

    // A browser playing Netflix on the right display, a game on the center one, and an editor
    // on the left one
    private readonly FakeWindow _netflix = new(NetflixTitle, BrowserPath, Right);
    private readonly FakeWindow _game = new("Game", GamePath, Center);
    private readonly FakeWindow _editor = new("Untitled - Notepad", EditorPath, Left);

    private static WhitelistRules Rules { get; } =
        WhitelistRules.Create(true, [GamePath], true, ["netflix"]);

    // What each display shows after the window in focus is checked, as the dashboard does it
    private static ColorConfiguration[] Apply(
        ForegroundWhitelist whitelist,
        IWhitelistWindow? focusedWindow,
        AutoPauseSettings settings,
        out AutoPauseState state
    )
    {
        var match = whitelist.Update(Rules, focusedWindow);

        state = AutoPause.Evaluate(
            new AutoPauseConditions(
                false,
                false,
                null,
                match.IsMatched,
                match.Monitor,
                ConnectedMonitors
            ),
            settings
        );

        var context = new MonitorColorContext(
            ActiveConfiguration,
            PausedConfiguration,
            state.IsGloballyPaused,
            MinimumTemperature: 500,
            MaximumTemperature: 20_000,
            MinimumBrightness: 0.1,
            MaximumBrightness: 1,
            MonitorOffsetScaling.Fixed,
            ReferenceTemperature: 6600
        );

        var currentState = state;
        return
        [
            .. ConnectedMonitors.Select(m =>
                MonitorColorResolver.Resolve(
                    context,
                    null,
                    isPaused: currentState.IsMonitorPaused(m)
                )
            ),
        ];
    }

    [Fact]
    public void I_observe_whitelisted_windows_in_the_background_never_pausing_their_displays()
    {
        // Arrange
        var whitelist = new ForegroundWhitelist();

        // Act
        // The editor is in focus, while Netflix and the game stay visible behind it
        var configurations = Apply(whitelist, _editor, PerMonitorSettings, out var state);

        // Assert
        state.IsAnyMonitorPaused.Should().BeFalse();
        configurations.Should().AllBeEquivalentTo(ActiveConfiguration);
        _netflix.Lookups.Should().Be(0);
        _game.Lookups.Should().Be(0);
    }

    [Fact]
    public void I_observe_only_the_display_of_a_focused_window_matching_by_title_being_paused()
    {
        // Arrange
        var whitelist = new ForegroundWhitelist();

        // Act
        var configurations = Apply(whitelist, _netflix, PerMonitorSettings, out var state);

        // Assert
        configurations
            .Should()
            .Equal(ActiveConfiguration, ActiveConfiguration, PausedConfiguration);

        // Not every display is paused, so the status keeps showing the schedule, while
        // grayscale is suspended
        state.IsGloballyPaused.Should().BeFalse();
        state.Reason.Should().Be(PauseReason.None);
        state.IsAnyMonitorPaused.Should().BeTrue();
    }

    [Fact]
    public void I_observe_only_the_display_of_a_focused_window_matching_by_application_being_paused()
    {
        // Arrange
        var whitelist = new ForegroundWhitelist();

        // Act
        var configurations = Apply(whitelist, _game, PerMonitorSettings, out _);

        // Assert
        configurations
            .Should()
            .Equal(ActiveConfiguration, PausedConfiguration, ActiveConfiguration);
    }

    [Fact]
    public void I_observe_all_displays_being_paused_by_a_matching_focused_window_when_not_paused_per_display()
    {
        // Arrange
        var whitelist = new ForegroundWhitelist();

        // Act
        var configurations = Apply(
            whitelist,
            _netflix,
            PerMonitorSettings with
            {
                IsPerMonitorWhitelistPauseEnabled = false,
            },
            out var state
        );

        // Assert
        state.Reason.Should().Be(PauseReason.Whitelist);
        configurations.Should().AllBeEquivalentTo(PausedConfiguration);
    }

    [Fact]
    public void I_observe_the_previous_display_restored_as_soon_as_focus_moves_to_a_window_that_does_not_match()
    {
        // Arrange
        var whitelist = new ForegroundWhitelist();
        Apply(whitelist, _netflix, PerMonitorSettings, out _);

        // Act
        // Netflix stays visible on the right display, but the editor on the left one gets focus
        var configurations = Apply(whitelist, _editor, PerMonitorSettings, out var state);

        // Assert
        state.IsAnyMonitorPaused.Should().BeFalse();
        configurations.Should().AllBeEquivalentTo(ActiveConfiguration);
    }

    [Fact]
    public void I_observe_the_pause_moving_along_when_focus_moves_to_another_matching_window()
    {
        // Arrange
        var whitelist = new ForegroundWhitelist();
        Apply(whitelist, _netflix, PerMonitorSettings, out _);

        // Act
        var configurations = Apply(whitelist, _game, PerMonitorSettings, out _);

        // Assert
        configurations
            .Should()
            .Equal(ActiveConfiguration, PausedConfiguration, ActiveConfiguration);
    }

    [Fact]
    public void I_observe_the_pause_kept_while_a_window_that_cannot_be_judged_is_in_focus()
    {
        // Arrange
        var whitelist = new ForegroundWhitelist();
        Apply(whitelist, _netflix, PerMonitorSettings, out _);

        // Act
        // E.g. the taskbar, the desktop, or this app's own window
        var configurations = Apply(whitelist, null, PerMonitorSettings, out _);

        // Assert
        configurations
            .Should()
            .Equal(ActiveConfiguration, ActiveConfiguration, PausedConfiguration);
    }

    [Fact]
    public void I_observe_the_pause_lifted_when_the_whitelist_changes_while_a_window_that_cannot_be_judged_is_in_focus()
    {
        // Arrange
        var whitelist = new ForegroundWhitelist();
        whitelist.Update(Rules, _netflix);

        // Act
        // E.g. the keyword was removed in this app's settings
        var match = whitelist.Update(WhitelistRules.Create(true, [GamePath], true, []), null);

        // Assert
        match.IsMatched.Should().BeFalse();
    }

    [Fact]
    public void I_do_not_get_the_focused_window_looked_at_when_there_is_nothing_to_match()
    {
        // Arrange
        var whitelist = new ForegroundWhitelist();

        // Act
        var match = whitelist.Update(WhitelistRules.None, _netflix);

        // Assert
        match.IsMatched.Should().BeFalse();
        _netflix.Lookups.Should().Be(0);
    }

    [Fact]
    public void I_do_not_get_the_focused_window_title_read_when_title_matching_is_off()
    {
        // Arrange
        var whitelist = new ForegroundWhitelist();
        var rules = WhitelistRules.Create(true, [GamePath], false, ["netflix"]);

        // Act
        var match = whitelist.Update(rules, _netflix);

        // Assert
        // Only its executable was looked up, which didn't match
        match.IsMatched.Should().BeFalse();
        _netflix.Lookups.Should().Be(1);
    }

    [Fact]
    public void I_observe_display_names_matched_ignoring_case()
    {
        // Act
        var state = AutoPause.Evaluate(Conditions(@"\\.\display1"), PerMonitorSettings);

        // Assert
        state.IsMonitorPaused(Left).Should().BeTrue();
    }

    [Fact]
    public void I_observe_all_displays_being_paused_when_the_whitelisted_window_display_is_unknown()
    {
        // Act
        var state = AutoPause.Evaluate(
            new AutoPauseConditions(false, false, null, true, null, ConnectedMonitors),
            PerMonitorSettings
        );

        // Assert
        state.Reason.Should().Be(PauseReason.Whitelist);
    }

    [Fact]
    public void I_observe_no_display_paused_by_a_whitelisted_window_when_the_whitelist_is_off()
    {
        // Act
        var state = AutoPause.Evaluate(
            Conditions(Left),
            PerMonitorSettings with
            {
                IsApplicationWhitelistEnabled = false,
            }
        );

        // Assert
        state.IsAnyMonitorPaused.Should().BeFalse();
    }

    // ---- Settings ----

    private readonly string _directoryPath = Path.Combine(
        Path.GetTempPath(),
        "LightBulb.Tests." + Guid.NewGuid().ToString("N")
    );

    public WhitelistSpecs() => Directory.CreateDirectory(_directoryPath);

    // Settings are only loaded here, never saved (saving also writes to the registry)
    private SettingsService Load(string json)
    {
        var filePath = Path.Combine(_directoryPath, "Settings.json");
        File.WriteAllText(filePath, json);

        var settings = new SettingsService(filePath);
        settings.Load();

        return settings;
    }

    [Fact]
    public void I_get_all_whitelist_options_turned_on_by_default()
    {
        // Act
        var settings = Load("{}");

        // Assert
        settings.IsWhitelistedApplicationPauseEnabled.Should().BeTrue();
        settings.IsWhitelistedWindowTitlePauseEnabled.Should().BeTrue();
        settings.IsPerMonitorWhitelistPauseEnabled.Should().BeTrue();
        settings.WhitelistedWindowTitles.Should().BeNullOrEmpty();
    }

    [Fact]
    public void I_can_load_saved_title_keywords_and_get_them_cleaned_up()
    {
        // Act
        var settings = Load(
            """
            {
              "IsWhitelistedWindowTitlePauseEnabled": false,
              "WhitelistedWindowTitles": [" Netflix ", "", "netflix", null, "YouTube"]
            }
            """
        );

        // Assert
        settings.IsWhitelistedWindowTitlePauseEnabled.Should().BeFalse();
        settings.WhitelistedWindowTitles.Should().Equal("Netflix", "YouTube");
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
