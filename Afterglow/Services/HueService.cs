using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Afterglow.Core;
using Afterglow.Core.Utils;
using Afterglow.PlatformInterop;
using Afterglow.Utils;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using PowerKit.Extensions;
using Timer = Afterglow.PlatformInterop.Timer;

namespace Afterglow.Services;

/// <summary>
/// Makes Philips Hue lights in the chosen rooms follow the color cycle while this PC is on,
/// through the Hue bridge on the local network. Lights are only turned on when the user asks;
/// lights that are on follow the cycle, and go back to how they were when following stops (e.g.
/// when it's turned off, or Afterglow is). When the PC shuts down or goes to sleep, they're left
/// as they are, put back, or turned off, as chosen.
/// A light that's changed by something else (the Hue app, a switch, a scene) is left alone
/// until it's turned off and on again. While it's turned off, nothing runs at all.
/// </summary>
public partial class HueService : ObservableObject, IDisposable
{
    private const string DiscoveryHost = "discovery.meethue.com";

    // How often the lights are checked (e.g. for being turned on) and adjusted
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    // Checked less often while the bridge can't be reached
    private static readonly TimeSpan UnreachablePollInterval = TimeSpan.FromSeconds(30);

    private static readonly TimeSpan RoomsRefreshInterval = TimeSpan.FromMinutes(1);

    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(5);

    // The button has to be pressed within this time after clicking Connect
    private static readonly TimeSpan PairingDuration = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan PairingAttemptInterval = TimeSpan.FromSeconds(1);

    // How long shutting down (or going to sleep) may wait for the lights to be put back
    private static readonly TimeSpan RestoreTimeout = TimeSpan.FromSeconds(4);

    // After waking up, the network may take a moment to come back
    private static readonly TimeSpan ResumeDelay = TimeSpan.FromSeconds(10);

    // The discovery service only allows a request every 15 minutes or so
    private static readonly TimeSpan RediscoveryInterval = TimeSpan.FromMinutes(15);

    // While the cycle preview plays, lights keep up with it (as fast as the bridge allows)
    private static readonly TimeSpan PreviewPollInterval = TimeSpan.FromSeconds(1);
    private const int PreviewTransitionMilliseconds = 1000;

    // How soon lights catch up when what the screen shows changes (e.g. the slider is moved)
    private static readonly TimeSpan TargetChangeDelay = TimeSpan.FromSeconds(0.3);

    // The bridge handles at most a few requests at once
    private const int MaxParallelRequests = 3;

    // The bridge drops commands to lights when it gets more than about 10 a second, so they're
    // spaced out
    private static readonly TimeSpan SendSpacing = TimeSpan.FromMilliseconds(110);
    private static readonly TimeSpan MaxSendSpacing = TimeSpan.FromMilliseconds(500);

    // When the bridge says it's busy, a command is tried again after a moment (a few times)
    private static readonly TimeSpan BusyRetryDelay = TimeSpan.FromSeconds(1);
    private const int MaxBusyRetries = 2;

    // Lights are checked soon after they were set (once their transitions are over), so that any
    // that didn't get it are set again
    private static TimeSpan GetVerifyDelay(IEnumerable<HueCommand> commands) =>
        HueLightController.GetSettleTime(commands.Max(c => c.TransitionMilliseconds))
        + TimeSpan.FromSeconds(0.5);

    private readonly SettingsService _settingsService;
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _send;
    private readonly Func<DateTimeOffset> _getNow;

    // Whether it runs on its own (on timers, and on system events), rather than only when called
    private readonly bool _isScheduled;
    private readonly IDisposable _enabledSubscription;
    private readonly HueLightController _controller = new();
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    private HttpClient? _bridgeClient;

    // When the next command may be sent (spaced out, for the bridge to keep up), in milliseconds
    // of Environment.TickCount64
    private readonly TimeSpan _sendSpacing;
    private readonly Lock _sendSlotLock = new();
    private long _nextSendSlot;

    // The spacing right now (in milliseconds), which grows while the bridge says it's busy, and
    // shrinks back gradually afterwards
    private long _currentSpacing;

    // The certificate the bridge must present (null while connecting for the first time), and
    // the last one seen at each address (while connecting). Used on the thread pool.
    private volatile string? _expectedCertificateHash;
    private readonly System.Collections.Concurrent.ConcurrentDictionary<
        string,
        string
    > _seenCertificateHashes = new(StringComparer.OrdinalIgnoreCase);

    private bool _isStarted;
    private bool _isActive;
    private bool _isFollowing;
    private bool _isCycleEnabled = true;
    private bool _isSuspended;
    private bool _isConnecting;
    private bool _isPolling;

    // Set while Afterglow starts (or the PC wakes up), when lights may be turned on
    private bool _isStarting;

    // A look shown on the lights for a moment (while it's picked in the settings), until when
    private ColorConfiguration? _previewLook;
    private DateTimeOffset _previewUntil;
    private IDisposable? _previewEnd;

    // Following can't go on until the bridge is connected again (e.g. its key was revoked)
    private HueStatus? _blockingStatus;

    // Increases whenever following starts or stops, so that work from before is dropped
    private int _generation;

    private Timer? _pollTimer;
    private IDisposable? _pendingPoll;
    private DateTimeOffset _pendingPollAt;
    private IDisposable? _pendingResume;
    private IDisposable? _pauseTimer;
    private IDisposable? _sessionSubscription;
    private IDisposable? _sleepSubscription;
    private CancellationTokenSource? _connectCancellation;

    // The bridge the lights are following through (where they're put back, even if a different
    // bridge has been chosen since)
    private (string Address, string Key)? _followedBridge;

    // Set while the bridge's address is updated after it moved, which isn't a different bridge
    private bool _isMovingBridge;

    private IReadOnlyList<HueLight> _lastLights = [];
    private DateTimeOffset _lastRoomsRefresh = DateTimeOffset.MinValue;
    private DateTimeOffset _lastAttempt = DateTimeOffset.MinValue;
    private DateTimeOffset _lastDiscovery = DateTimeOffset.MinValue;
    private DateTimeOffset? _unreachableSince;

    private (DateOnly Day, GeoLocation Location, SolarTimes Times)? _solarTimesCache;

    // Where the lights get what to follow from (the dashboard), if set
    private Func<HueTarget>? _getTarget;
    private bool _isPreview;
    private bool _isScreenPaused;

    // The user changed something since the lights were last adjusted (so that they're adjusted
    // because of it)
    private bool _hasPendingUserChange;

    // What the lights were last adjusted to, and when
    private ColorConfiguration? _lastTarget;
    private DateTimeOffset _lastPollStart = DateTimeOffset.MinValue;

    public HueService(SettingsService settingsService)
        : this(settingsService, null, () => DateTimeOffset.Now, true) { }

    // Allows talking to something else (e.g. in tests)
    internal HueService(
        SettingsService settingsService,
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? send,
        Func<DateTimeOffset> getNow,
        bool isScheduled = false
    )
    {
        _settingsService = settingsService;
        _send = send ?? SendToNetworkAsync;
        _getNow = getNow;
        _isScheduled = isScheduled;
        _sendSpacing = isScheduled ? SendSpacing : TimeSpan.Zero;
        _currentSpacing = (long)_sendSpacing.TotalMilliseconds;

        _enabledSubscription = settingsService.WatchProperty(o => o.IsHueEnabled, _ => Evaluate());
    }

    [ObservableProperty]
    public partial HueStatus Status { get; private set; }

    // How many lights are following the cycle right now
    [ObservableProperty]
    public partial int FollowedLightCount { get; private set; }

    // How many of them haven't taken what they were last set to (e.g. out of the bridge's reach)
    [ObservableProperty]
    public partial int UnresponsiveLightCount { get; private set; }

    // The lights in the chosen rooms, and how each is doing (while lights are followed)
    [ObservableProperty]
    public partial IReadOnlyList<HueLightInfo> Lights { get; private set; } = [];

    // The bridge's rooms and zones (once it's connected and they've been loaded)
    [ObservableProperty]
    public partial IReadOnlyList<HueRoom> Rooms { get; private set; } = [];

    public bool IsConnecting => _isConnecting;

    internal bool IsFollowing => _isFollowing;

    public bool IsPaired =>
        TryGetBridgeAddress() is not null
        && !string.IsNullOrWhiteSpace(_settingsService.HueCertificateHash)
        && !string.IsNullOrWhiteSpace(GetAppKey());

    // The key decrypted from the settings, kept along with what it was decrypted from (so that it's
    // only decrypted again once that changes, rather than on every request and every check)
    private (string? Protected, string? Key)? _appKey;

    private string? GetAppKey()
    {
        var protectedKey = _settingsService.HueProtectedAppKey;

        if (_appKey is not { } cached || cached.Protected != protectedKey)
        {
            cached = (protectedKey, SecretProtection.TryUnprotect(protectedKey));
            _appKey = cached;
        }

        return cached.Key;
    }

    private string? TryGetBridgeAddress() => TryNormalizeAddress(_settingsService.HueBridgeAddress);

    /// <summary>
    /// Starts following, if it's turned on (once the settings have been loaded).
    /// </summary>
    public void Start()
    {
        _isStarted = true;

        // (lights are turned on along with Afterglow, if chosen, but only if they follow right
        // away, rather than whenever they first do later on)
        _isStarting = true;
        Evaluate();
        _isStarting = false;
    }

    /// <summary>
    /// Whether Afterglow itself is on (lights only follow it while it is).
    /// </summary>
    public void SetCycleEnabled(bool isEnabled)
    {
        if (_isCycleEnabled == isEnabled)
            return;

        _isCycleEnabled = isEnabled;
        _hasPendingUserChange = true;
        Evaluate();
    }

    // When lights were paused from the tray until (lights go back to how they were meanwhile), or
    // null while they aren't. A pause without an end is until it's resumed.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPausedByUser))]
    [NotifyPropertyChangedFor(nameof(CanPause))]
    public partial DateTimeOffset? PausedUntil { get; private set; }

    public bool IsPausedByUser => PausedUntil is not null;

    // Whether lights are controlled at all (Hue control is turned on, and connected to a bridge),
    // so that pausing them makes sense
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanPause))]
    public partial bool IsAvailable { get; private set; }

    // Whether lights can be paused for a while (they're available, and not paused already)
    public bool CanPause => IsAvailable && !IsPausedByUser;

    // Whether lights are being followed right now (so that ones that are off can be turned on)
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanTurnOnLights))]
    public partial bool IsFollowingLights { get; private set; }

    // How many lights in the chosen rooms are off (except those left out), which are what
    // turning on the lights turns on
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanTurnOnLights))]
    public partial int TurnOnLightCount { get; private set; }

    // Whether there are lights to turn on (while lights are followed)
    public bool CanTurnOnLights => IsFollowingLights && TurnOnLightCount > 0;

    // Lights that are off are turned on with the next check (as the user asked for it)
    private bool _hasPendingTurnOn;

    /// <summary>
    /// Turns on the lights in the chosen rooms that are off (except those left out), straight to
    /// what they follow. They're turned off again when Afterglow lets go of them.
    /// </summary>
    public void TurnOnLights()
    {
        if (!_isFollowing)
            return;

        _hasPendingTurnOn = true;
        _hasPendingUserChange = true;
        QueuePoll(TimeSpan.Zero);
    }

    /// <summary>
    /// Puts the lights back and stops following for the given time (or until resumed, without
    /// one), without affecting the screens.
    /// </summary>
    public void Pause(TimeSpan? duration = null)
    {
        _pauseTimer?.Dispose();
        _pauseTimer = null;

        PausedUntil = duration is { } d ? _getNow() + d : DateTimeOffset.MaxValue;

        if (_isScheduled && duration is { } delay)
        {
            _pauseTimer = Timer.QueueDelayedAction(
                delay,
                () => Dispatcher.UIThread.Post(() => Evaluate())
            );
        }

        _hasPendingUserChange = true;
        Evaluate();
    }

    /// <summary>
    /// Makes the lights follow again after being paused.
    /// </summary>
    public void Resume()
    {
        _pauseTimer?.Dispose();
        _pauseTimer = null;
        PausedUntil = null;

        _hasPendingUserChange = true;
        Evaluate();
    }

    /// <summary>
    /// Notes that what the lights follow was changed by the user (e.g. with a hotkey), rather
    /// than by the cycle moving on, so that lights catching up with it aren't taken for changed
    /// by something else.
    /// </summary>
    public void NotifyUserChange() => _hasPendingUserChange = true;

    /// <summary>
    /// Sets where the lights get what to follow from (what the screen shows). Without it, they
    /// follow the cycle as it is at the time.
    /// </summary>
    public void SetTargetSource(Func<HueTarget> getTarget)
    {
        _getTarget = getTarget;
        NotifyTargetChanged();
    }

    /// <summary>
    /// Lets the lights catch up with what the screen shows, after it changed. Called often (on
    /// every update of the screen), so it does as little as possible.
    /// </summary>
    public void NotifyTargetChanged()
    {
        if (!_isActive || _getTarget is null)
            return;

        var target = _getTarget();
        var isScreenPaused = target.IsScreenPaused && _settingsService.IsHuePausedWithScreen;

        if (target.IsPreview != _isPreview || isScreenPaused != _isScreenPaused)
        {
            _isPreview = target.IsPreview;
            _isScreenPaused = isScreenPaused;
            Evaluate();
            QueuePoll(TimeSpan.Zero);
            return;
        }

        if (!_isFollowing)
            return;

        // (any change the user made counts, so that lights get exactly what they chose)
        if (
            !_hasPendingUserChange
            && _lastTarget is { } last
            && !IsVisiblyDifferent(last, target.Configuration)
        )
        {
            return;
        }

        // During the preview, as often as the bridge keeps up (one round is done before the
        // next starts). Otherwise, soon after it changed (e.g. once the slider is let go of).
        var now = _getNow();
        var delay = _isPreview ? PreviewPollInterval - (now - _lastPollStart) : TargetChangeDelay;
        if (delay < TimeSpan.Zero)
            delay = TimeSpan.Zero;

        // Unless a check is coming up soon enough anyway
        if (_pendingPoll is not null && _pendingPollAt <= now + delay)
            return;

        QueuePoll(delay);
    }

    // Smaller changes are left for the regular check
    private static bool IsVisiblyDifferent(ColorConfiguration a, ColorConfiguration b) =>
        Math.Abs(
            HueLightController.ToMirek(a.Temperature) - HueLightController.ToMirek(b.Temperature)
        ) >= 2
        || Math.Abs(a.Brightness - b.Brightness) >= 0.01;

    private HueTarget GetTarget(DateTimeOffset now) =>
        _getTarget?.Invoke()
        ?? new HueTarget(GetTargetConfiguration(_settingsService, now), false, false);

    private void Evaluate()
    {
        // A pause from the tray that's over (also caught up on after waking from sleep)
        if (PausedUntil is { } pausedUntil && _getNow() >= pausedUntil)
        {
            _pauseTimer?.Dispose();
            _pauseTimer = null;
            PausedUntil = null;
        }

        var isActive = _isStarted && _settingsService.IsHueEnabled;

        if (isActive && !_isActive)
        {
            _isActive = true;
            ((INotifyPropertyChanged)_settingsService).PropertyChanged += OnSettingChanged;

            if (_isScheduled)
            {
                _sessionSubscription = SessionEvents.RegisterSessionEnding(OnSessionEnding);
                _sleepSubscription = SessionEvents.RegisterSleep(OnSleep);
            }

            // Not kept up to date while it was off
            if (_getTarget is not null)
            {
                var target = _getTarget();
                _isPreview = target.IsPreview;
                _isScreenPaused = target.IsScreenPaused && _settingsService.IsHuePausedWithScreen;
            }
        }
        else if (!isActive && _isActive)
        {
            _isActive = false;
            ((INotifyPropertyChanged)_settingsService).PropertyChanged -= OnSettingChanged;

            // A pause from the tray ends along with Hue control
            _pauseTimer?.Dispose();
            _pauseTimer = null;
            PausedUntil = null;

            // Lights left as they were for a pause are left as they are (as they're no longer
            // followed, and nothing puts them back)
            if (!_isFollowing)
                _controller.Reset();

            // Lights paused to a bright white are left as they are
            _controller.ForgetPausedLights();

            _sessionSubscription?.Dispose();
            _sessionSubscription = null;
            _sleepSubscription?.Dispose();
            _sleepSubscription = null;
            _pendingResume?.Dispose();
            _pendingResume = null;
            _isSuspended = false;

            _connectCancellation?.Cancel();
        }

        // The preview plays even while Afterglow is off (and overrides pauses), like on the screen
        var shouldFollow =
            _isActive
            && (_isCycleEnabled || _isPreview)
            && !_isScreenPaused
            && PausedUntil is null
            && !_isSuspended
            && !_isConnecting
            && _blockingStatus is null
            && _settingsService.HueRoomIds is { Count: > 0 }
            && IsPaired;

        if (shouldFollow && !_isFollowing)
        {
            StartFollowing();
        }
        else if (!shouldFollow && _isFollowing)
        {
            // Only paused (by the user, along with the screen, or as Afterglow is off), rather
            // than let go of
            var isPause =
                _isActive
                && !_isSuspended
                && !_isConnecting
                && _blockingStatus is null
                && _settingsService.HueRoomIds is { Count: > 0 }
                && IsPaired;

            StopFollowing(false, isPause);
        }

        if (!_isFollowing && !_isConnecting)
        {
            Status =
                !_isActive ? HueStatus.Off
                : _blockingStatus is { } blockingStatus ? blockingStatus
                : !IsPaired ? HueStatus.NotConnected
                : _settingsService.HueRoomIds is not { Count: > 0 } ? HueStatus.NoRooms
                : PausedUntil is not null ? HueStatus.PausedByUser
                : _isScreenPaused && _isCycleEnabled && !_isSuspended ? HueStatus.PausedWithScreen
                : HueStatus.Paused;
        }

        // Pausing lights is offered while Hue control is turned on, and connected to a bridge
        IsAvailable = _isActive && IsPaired;

        // Without a bridge, there are no lights to list
        if (!IsAvailable && Lights.Count > 0)
            Lights = [];
    }

    private void OnSettingChanged(object? sender, PropertyChangedEventArgs args)
    {
        // Settings are only changed by the user (e.g. a slider is moved)
        _hasPendingUserChange = true;

        switch (args.PropertyName)
        {
            // A different bridge (or a new connection to it)
            case nameof(SettingsService.HueBridgeAddress)
            or nameof(SettingsService.HueProtectedAppKey)
            or nameof(SettingsService.HueCertificateHash):
                if (_isMovingBridge)
                {
                    if (
                        _isFollowing
                        && TryGetBridgeAddress() is { } address
                        && GetAppKey() is { } key
                    )
                        _followedBridge = (address, key);

                    break;
                }

                _blockingStatus = null;
                Rooms = [];
                Lights = [];
                _lastRoomsRefresh = DateTimeOffset.MinValue;

                // (lights left as they were for a pause are forgotten, as they're no longer
                // followed through that bridge)
                if (_isFollowing)
                    StopFollowing(false);
                else
                    _controller.Reset();

                Evaluate();
                break;

            // Which lights follow, and how
            case nameof(SettingsService.HueRoomIds)
            or nameof(SettingsService.HueExcludedLightIds)
            or nameof(SettingsService.IsHueTemperatureFollowed)
            or nameof(SettingsService.IsHueBrightnessFollowed):
                Evaluate();
                QueuePoll(TimeSpan.FromSeconds(0.5));
                break;

            case nameof(SettingsService.IsHuePausedWithScreen):
                NotifyTargetChanged();
                break;

            // Shown on the lights soon after the slider moves
            case nameof(SettingsService.LightWarmth)
            or nameof(SettingsService.LightDimming):
                QueuePoll(TargetChangeDelay);
                break;

            // Shown on the lights that are paused to it right now, as it's changed
            case nameof(SettingsService.HuePauseTemperature)
            or nameof(SettingsService.HuePauseBrightness):
                if (!_isFollowing && _settingsService.HuePauseAction == HuePauseAction.Custom)
                    QueuePausedLookUpdate();
                break;

            // Settings that were reset or reloaded as a whole report every property at once
            case null
            or "":
                _blockingStatus = null;
                Evaluate();
                break;
        }
    }

    private void StartFollowing()
    {
        _isFollowing = true;
        IsFollowingLights = true;

        // Along with Afterglow (or the PC waking up), all the lights are turned on, if chosen
        if (_isStarting && _settingsService.IsHueTurnedOnAtStart)
        {
            _hasPendingTurnOn = true;
            _hasPendingUserChange = true;
        }
        _generation++;
        _unreachableSince = null;
        _lastAttempt = DateTimeOffset.MinValue;
        _expectedCertificateHash = _settingsService.HueCertificateHash;
        _followedBridge = (TryGetBridgeAddress()!, GetAppKey()!);

        Status = HueStatus.Following;

        if (!_isScheduled)
            return;

        _pollTimer = new Timer(
            TimeSpan.Zero,
            PollInterval,
            () => Dispatcher.UIThread.Post(() => _ = PollAsync())
        );
        _pollTimer.Start();
    }

    // Puts the lights back, either in the background, or right away (when the PC is shutting
    // down or going to sleep, as it doesn't wait for long)
    // (a pause leaves lights that Afterglow turned on at a neutral white, rather than off; and when
    // the PC shuts down or goes to sleep, lights are left, put back, or turned off, as chosen)
    private void StopFollowing(
        bool isUrgent,
        bool isPause = false,
        HueShutdownAction? shutdownAction = null
    )
    {
        _isFollowing = false;
        IsFollowingLights = false;
        _hasPendingTurnOn = false;
        _previewLook = null;
        _previewEnd?.Dispose();
        _previewEnd = null;
        TurnOnLightCount = 0;
        _generation++;

        _pollTimer?.Dispose();
        _pollTimer = null;
        _pendingPoll?.Dispose();
        _pendingPoll = null;
        FollowedLightCount = 0;
        UnresponsiveLightCount = 0;

        // The lights stay listed (as paused), so that they can still be chosen meanwhile
        Lights = Lights.Select(l => l with { State = HueLightState.NotFollowed }).ToArray();

        var now = _getNow();
        var commands = shutdownAction switch
        {
            HueShutdownAction.Leave => _controller.TurnOff(_lastLights, now, true),
            HueShutdownAction.TurnOff => _controller.TurnOff(_lastLights, now, false),
            HueShutdownAction.Custom => _controller.Restore(
                _lastLights,
                now,
                null,
                true,
                GetLook(
                    _settingsService.HueShutdownTemperature,
                    _settingsService.HueShutdownBrightness
                )
            ),
            _ when isPause => GetPauseAction() switch
            {
                // (still remembered, to carry on afterwards)
                HuePauseAction.Leave => [],
                HuePauseAction.TurnOff => _controller.Restore(
                    _lastLights,
                    now,
                    null,
                    true,
                    null,
                    true
                ),
                HuePauseAction.Custom => _controller.Restore(
                    _lastLights,
                    now,
                    null,
                    true,
                    GetLook(
                        _settingsService.HuePauseTemperature,
                        _settingsService.HuePauseBrightness
                    )
                ),
                _ => _controller.Restore(_lastLights, now, null, true),
            },
            _ => _controller.Restore(_lastLights, now),
        };
        _lastLights = [];

        if (commands.Count == 0 || _followedBridge is not { } bridge)
            return;

        var task = Task.Run(() => SendCommandsAsync(bridge, commands, true));

        if (isUrgent)
        {
            try
            {
                task.Wait(RestoreTimeout);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to put the Hue lights back: {ex.Message}");
            }
        }
    }

    // How long a look being picked is shown on the lights at most (until the slider is let go of)
    private static readonly TimeSpan LookPreviewTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Shows a look on the lights being followed while it's picked in the settings (while a
    /// slider is held), until <see cref="EndLookPreview" /> sets them back to what they follow.
    /// </summary>
    public void PreviewLook(double temperature, double brightness)
    {
        if (!_isFollowing)
            return;

        var now = _getNow();
        var isStarting = _previewLook is null || now >= _previewUntil;

        _previewLook = GetLook(temperature, brightness);
        _previewUntil = now + LookPreviewTimeout;

        // Shown right away, and again as it changes (as often as the bridge keeps up)
        var delay = isStarting ? TimeSpan.Zero : PreviewPollInterval / 2 - (now - _lastPollStart);
        if (_pendingPoll is null || _pendingPollAt > now + delay)
            QueuePoll(delay < TimeSpan.Zero ? TimeSpan.Zero : delay);

        // Set back after a while anyway (in case the slider being let go of went unnoticed)
        _previewEnd?.Dispose();
        _previewEnd = _isScheduled
            ? Timer.QueueDelayedAction(
                LookPreviewTimeout + TimeSpan.FromSeconds(0.1),
                () => Dispatcher.UIThread.Post(() => QueuePoll(TimeSpan.Zero))
            )
            : null;
    }

    /// <summary>
    /// Sets the lights back to what they follow, once a look is no longer being picked.
    /// </summary>
    public void EndLookPreview()
    {
        if (_previewLook is null)
            return;

        _previewUntil = _getNow();
        _previewEnd?.Dispose();
        _previewEnd = null;
        QueuePoll(TimeSpan.Zero);
    }

    // How long a light changed by something else can be left alone before it's followed again
    public static TimeSpan MinimumReclaimDuration { get; } = TimeSpan.FromMinutes(5);
    public static TimeSpan MaximumReclaimDuration { get; } = TimeSpan.FromHours(6);

    private TimeSpan GetReclaimDuration() =>
        _settingsService.HueReclaimDuration < MinimumReclaimDuration ? MinimumReclaimDuration
        : _settingsService.HueReclaimDuration > MaximumReclaimDuration ? MaximumReclaimDuration
        : _settingsService.HueReclaimDuration;

    // A look of the user's choosing, within what lights can show
    internal static ColorConfiguration GetLook(double temperature, double brightness) =>
        new(
            double.IsFinite(temperature)
                ? Math.Clamp(temperature, MinimumLookTemperature, MaximumLookTemperature)
                : 2700,
            double.IsFinite(brightness) ? Math.Clamp(brightness, 0.01, 1) : 1
        );

    // The range of a custom look's color temperature (what Hue lights can show as whites, and a
    // little warmer, as color)
    public const double MinimumLookTemperature = 1800;
    public const double MaximumLookTemperature = 6500;

    private HuePauseAction GetPauseAction() =>
        Enum.IsDefined(_settingsService.HuePauseAction)
            ? _settingsService.HuePauseAction
            : HuePauseAction.Restore;

    private HueShutdownAction GetShutdownAction() =>
        Enum.IsDefined(_settingsService.HueShutdownAction)
            ? _settingsService.HueShutdownAction
            : HueShutdownAction.Leave;

    // The look of lights paused to a custom look is being changed (and set on them, one change
    // after another, skipping to the latest)
    private bool _isUpdatingPausedLook;
    private bool _hasPendingPausedLook;

    private void QueuePausedLookUpdate()
    {
        if (!_isActive || _followedBridge is null || !_controller.HasPausedLights)
            return;

        _hasPendingPausedLook = true;
        if (!_isUpdatingPausedLook)
            _ = UpdatePausedLookAsync();
    }

    private async Task UpdatePausedLookAsync()
    {
        _isUpdatingPausedLook = true;
        try
        {
            while (_hasPendingPausedLook && !_isFollowing && _followedBridge is { } bridge)
            {
                _hasPendingPausedLook = false;

                var lights = await GetLightsAsync();
                if (_isFollowing)
                    break;

                var commands = _controller.GetPausedLookCommands(
                    lights,
                    GetLook(
                        _settingsService.HuePauseTemperature,
                        _settingsService.HuePauseBrightness
                    )
                );

                if (commands.Count > 0)
                    await SendCommandsAsync(bridge, commands, false, () => !_isFollowing);
            }
        }
        catch (Exception ex) when (ex is HueRequestException or JsonException)
        {
            Debug.WriteLine($"Failed to change the paused Hue lights: {ex.Message}");
        }
        finally
        {
            _isUpdatingPausedLook = false;
        }
    }

    // When the PC shuts down or goes to sleep while lights are paused, what's chosen for that
    // applies to the lights Afterglow is still responsible for (right away, as it doesn't wait)
    private void ShutDownWhilePaused()
    {
        if (!_isActive || _followedBridge is not { } bridge || !_controller.HasPausedLights)
            return;

        var action = GetShutdownAction();
        var look =
            action == HueShutdownAction.Custom
                ? GetLook(
                    _settingsService.HueShutdownTemperature,
                    _settingsService.HueShutdownBrightness
                )
                : (ColorConfiguration?)null;

        // (on the thread pool, as the UI thread waits for it)
        var task = Task.Run(async () =>
        {
            var lights = await GetLightsAsync().ConfigureAwait(false);
            var commands = _controller.ShutDownWhilePaused(lights, _getNow(), action, look);
            if (commands.Count > 0)
                await SendCommandsAsync(bridge, commands, true).ConfigureAwait(false);
        });

        try
        {
            task.Wait(RestoreTimeout);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to change the paused Hue lights: {ex.Message}");
        }
    }

    // As if the PC were shutting down (e.g. in tests)
    internal void SimulateSessionEnding() => OnSessionEnding(true);

    private void OnSessionEnding(bool isEnding)
    {
        if (isEnding)
        {
            _isSuspended = true;
            if (_isFollowing)
                StopFollowing(true, shutdownAction: GetShutdownAction());
            else
                ShutDownWhilePaused();
        }
        else
        {
            _isSuspended = false;
        }

        Evaluate();
    }

    private void OnSleep(bool isSuspending)
    {
        _pendingResume?.Dispose();
        _pendingResume = null;

        if (isSuspending)
        {
            _isSuspended = true;
            if (_isFollowing)
                StopFollowing(true, shutdownAction: GetShutdownAction());
            else
                ShutDownWhilePaused();

            Evaluate();
        }
        else if (_isSuspended)
        {
            _pendingResume = Timer.QueueDelayedAction(
                ResumeDelay,
                () =>
                    Dispatcher.UIThread.Post(() =>
                    {
                        _isSuspended = false;

                        // (lights are turned on along with the PC, if chosen)
                        _isStarting = true;
                        Evaluate();
                        _isStarting = false;
                    })
            );
        }
    }

    private void QueuePoll(TimeSpan delay)
    {
        if (!_isFollowing || !_isScheduled)
            return;

        _pendingPoll?.Dispose();
        _pendingPollAt = _getNow() + delay;

        IDisposable? pendingPoll = null;
        pendingPoll = Timer.QueueDelayedAction(
            delay,
            () =>
                Dispatcher.UIThread.Post(() =>
                {
                    if (_pendingPoll == pendingPoll)
                        _pendingPoll = null;

                    _ = PollAsync(true);
                })
        );
        _pendingPoll = pendingPoll;
    }

    /// <summary>
    /// Checks the lights and adjusts them. Must be called on the UI thread.
    /// </summary>
    internal async Task PollAsync(bool isForced = false)
    {
        if (!_isFollowing || _isPolling)
            return;

        var now = _getNow();

        // Not too often while the bridge can't be reached
        if (
            !isForced
            && _unreachableSince is not null
            && now - _lastAttempt < UnreachablePollInterval
        )
            return;

        _isPolling = true;
        _lastAttempt = now;
        _lastPollStart = now;
        var generation = _generation;

        try
        {
            var lights = await GetLightsAsync();
            if (generation != _generation)
                return;

            if (now - _lastRoomsRefresh >= RoomsRefreshInterval || Rooms.Count == 0)
            {
                var rooms = await GetRoomsAsync();
                if (generation != _generation)
                    return;

                Rooms = rooms;
                _lastRoomsRefresh = now;
            }

            _lastLights = lights;
            _unreachableSince = null;

            var roomIds = _settingsService.HueRoomIds ?? [];
            var selectedRooms = Rooms.Where(r => roomIds.Contains(r.Id)).ToArray();
            var excludedIds = _settingsService.HueExcludedLightIds ?? [];
            var roomLights = lights.Where(l => selectedRooms.Any(r => r.Contains(l))).ToArray();
            var selectedLights = roomLights.Where(l => !excludedIds.Contains(l.Id)).ToArray();
            var selectedIds = selectedLights.Select(l => l.Id).ToHashSet(StringComparer.Ordinal);

            // Taken now (rather than when the poll started), as the preview moves on meanwhile
            var target = GetTarget(now);
            _lastTarget = target.Configuration;
            _controller.Warmth = Math.Clamp(
                double.IsFinite(_settingsService.LightWarmth)
                    ? _settingsService.LightWarmth
                    : HueLightController.DefaultWarmth,
                HueLightController.MinimumWarmth,
                HueLightController.MaximumWarmth
            );
            _controller.ReclaimDelay = _settingsService.IsHueReclaimEnabled
                ? GetReclaimDuration()
                : null;
            _controller.Dimming = Math.Clamp(
                double.IsFinite(_settingsService.LightDimming)
                    ? _settingsService.LightDimming
                    : HueLightController.DefaultDimming,
                HueLightController.MinimumDimming,
                HueLightController.MaximumDimming
            );

            var isUserChange = _hasPendingUserChange || target.IsPreview;
            _hasPendingUserChange = false;
            var isTurningOn = _hasPendingTurnOn;
            _hasPendingTurnOn = false;

            // A look being picked in the settings is shown for a moment (rather than the cycle),
            // after which the lights are set back to exactly what they follow
            var previewLook =
                _previewLook is { } look && now < _previewUntil ? look : (ColorConfiguration?)null;
            if (previewLook is null && _previewLook is not null)
            {
                _previewLook = null;
                isUserChange = true;
            }

            if (previewLook is not null && isTurningOn)
            {
                _hasPendingTurnOn = true;
                isTurningOn = false;
            }

            // Lights that are no longer in the chosen rooms go back to how they were
            var commands = _controller
                .Restore(lights, now, id => !selectedIds.Contains(id))
                .Concat(
                    previewLook is { } shown
                        ? _controller.Preview(selectedLights, shown, now)
                        : _controller.Update(
                            selectedLights,
                            target.Configuration,
                            _settingsService.IsHueTemperatureFollowed,
                            _settingsService.IsHueBrightnessFollowed,
                            now,
                            target.IsPreview
                                ? PreviewTransitionMilliseconds
                                : HueLightController.FollowTransitionMilliseconds,
                            isUserChange,
                            isTurningOn
                        )
                )
                .ToArray();

            FollowedLightCount = _controller.FollowedLightCount;
            UnresponsiveLightCount = _controller.UnresponsiveLightCount;
            TurnOnLightCount = isTurningOn ? 0 : selectedLights.Count(l => !l.IsOn);

            var lightInfos = roomLights
                .Select(l => new HueLightInfo(
                    l.Id,
                    l.DeviceId,
                    l.Name,
                    l.IsOn,
                    excludedIds.Contains(l.Id),
                    _controller.GetState(l.Id),
                    l.SupportsColorTemperature || l.SupportsColor || l.SupportsBrightness,
                    (
                        _settingsService.IsHueTemperatureFollowed
                        && (l.SupportsColorTemperature || l.SupportsColor)
                    ) || (_settingsService.IsHueBrightnessFollowed && l.SupportsBrightness),
                    _controller.GetReclaimTime(l.Id)
                ))
                .ToArray();

            // Only replaced when something changed, as it's shown
            if (!lightInfos.SequenceEqual(Lights))
                Lights = lightInfos;
            Status = HueStatus.Following;

            if (commands.Length > 0 && _followedBridge is { } bridge)
            {
                var failed = await SendCommandsAsync(
                    bridge,
                    commands,
                    false,
                    () => generation == _generation
                );
                if (generation == _generation)
                {
                    foreach (var id in failed)
                        _controller.MarkFailed(id);

                    // Checked again once they had time to get there, so that any that didn't
                    // are set again (the preview checks often enough on its own)
                    if (!target.IsPreview && _pendingPoll is null)
                        QueuePoll(GetVerifyDelay(commands));
                }
            }
        }
        catch (Exception ex) when (ex is HueRequestException or JsonException)
        {
            Debug.WriteLine($"Failed to reach the Hue bridge: {ex.Message}");

            if (generation != _generation)
                return;

            var status = (ex as HueRequestException)?.Status ?? HueStatus.Unreachable;

            if (status is HueStatus.Unauthorized or HueStatus.CertificateChanged)
            {
                // Following can't go on until it's connected again (and nothing can be put back)
                _blockingStatus = status;
                _controller.Reset();
                _lastLights = [];
                StopFollowing(false);
                Evaluate();
                return;
            }

            _unreachableSince ??= now;
            Status = status;

            await TryRediscoverAsync(now);
        }
        finally
        {
            _isPolling = false;
        }
    }

    // If the bridge's address changed (e.g. it was given a new one by the router), finds it
    // again by its ID
    private async Task TryRediscoverAsync(DateTimeOffset now)
    {
        if (
            _unreachableSince is not { } since
            || now - since < TimeSpan.FromMinutes(1)
            || now - _lastDiscovery < RediscoveryInterval
            || string.IsNullOrWhiteSpace(_settingsService.HueBridgeId)
        )
        {
            return;
        }

        _lastDiscovery = now;

        var bridges = await DiscoverAsync();
        var bridge = bridges.FirstOrDefault(b =>
            string.Equals(b.Id, _settingsService.HueBridgeId, StringComparison.OrdinalIgnoreCase)
        );

        // Its identity is still checked through its certificate
        if (
            bridge.Address is { } address
            && !string.Equals(
                address,
                _settingsService.HueBridgeAddress,
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            _isMovingBridge = true;
            try
            {
                _settingsService.HueBridgeAddress = address;
            }
            finally
            {
                _isMovingBridge = false;
            }

            _settingsService.Save();
            QueuePoll(TimeSpan.FromSeconds(1));
        }
    }

    /// <summary>
    /// Connects to the bridge: finds it first if there's no address, then waits for its button
    /// to be pressed. Must be called on the UI thread.
    /// </summary>
    public async Task ConnectAsync()
    {
        if (!_isActive || _isConnecting)
            return;

        _isConnecting = true;
        OnPropertyChanged(nameof(IsConnecting));

        // Lights go back while connecting, and follow again afterwards
        Evaluate();

        using var cancellation = new CancellationTokenSource();
        _connectCancellation = cancellation;

        try
        {
            if (TryGetBridgeAddress() is null)
            {
                Status = HueStatus.Searching;
                _lastDiscovery = _getNow();

                var bridges = await DiscoverAsync();
                if (cancellation.IsCancellationRequested)
                    return;

                if (bridges.Count == 0)
                {
                    Status = HueStatus.NoBridgeFound;
                    return;
                }

                _settingsService.HueBridgeAddress = bridges[0].Address;
            }

            if (TryGetBridgeAddress() is not { } address)
            {
                Status = HueStatus.NotConnected;
                return;
            }

            // The first certificate seen is trusted from now on. It's only presented when a
            // connection is made, and open ones are reused, so new ones are made from now on
            // (and the old ones are closed once anything still using them is done).
            _expectedCertificateHash = null;
            _seenCertificateHashes.Clear();
            if (Interlocked.Exchange(ref _bridgeClient, null) is { } previousClient)
            {
                _ = Task.Delay(TimeSpan.FromSeconds(30))
                    .ContinueWith(_ => previousClient.Dispose(), TaskScheduler.Default);
            }

            Status = HueStatus.WaitingForButton;
            var deadline = _getNow() + PairingDuration;

            while (!cancellation.IsCancellationRequested)
            {
                var (key, isWaiting) = await TryPairAsync(address, cancellation.Token);
                if (cancellation.IsCancellationRequested)
                    return;

                // (the certificate of the bridge that gave the key, rather than of any other one
                // talked to meanwhile, e.g. while putting lights back)
                if (
                    key is not null
                    && _seenCertificateHashes.TryGetValue(
                        new Uri($"https://{address}/").IdnHost,
                        out var certificateHash
                    )
                )
                {
                    var bridgeId = await TryGetBridgeIdAsync(address, cancellation.Token);

                    _blockingStatus = null;
                    _settingsService.HueBridgeId = bridgeId ?? _settingsService.HueBridgeId;
                    _settingsService.HueCertificateHash = certificateHash;
                    _settingsService.HueProtectedAppKey = SecretProtection.Protect(key);
                    _settingsService.Save();

                    _expectedCertificateHash = certificateHash;
                    Status = HueStatus.Connected;

                    await RefreshRoomsAsync();
                    return;
                }

                if (!isWaiting)
                {
                    // Not a Hue bridge (or not one that answers)
                    Status = HueStatus.NoBridgeFound;
                    return;
                }

                if (_getNow() >= deadline)
                {
                    Status = HueStatus.ButtonNotPressed;
                    return;
                }

                await Task.Delay(PairingAttemptInterval, cancellation.Token);
            }
        }
        catch (OperationCanceledException) { }
        catch (HueRequestException ex)
        {
            Status = ex.Status == HueStatus.Unreachable ? HueStatus.NoBridgeFound : ex.Status;
        }
        finally
        {
            _connectCancellation = null;
            _isConnecting = false;
            OnPropertyChanged(nameof(IsConnecting));

            var status = Status;
            Evaluate();

            // Keeps showing how connecting went (unless it was cancelled, or the lights now follow)
            if (
                !cancellation.IsCancellationRequested
                && !_isFollowing
                && _isActive
                && status != HueStatus.Connected
            )
                Status = status;
        }
    }

    public void CancelConnect() => _connectCancellation?.Cancel();

    /// <summary>
    /// Follows a light again that was left alone, as it seemed to be changed by something else.
    /// </summary>
    public void FollowAgain(string lightId)
    {
        _controller.FollowAgain(lightId);

        // Something the user did, so that it isn't let go again while it catches up
        _hasPendingUserChange = true;
        QueuePoll(TimeSpan.Zero);
    }

    /// <summary>
    /// Forgets the bridge (keeping its address, to connect to it again), after putting the
    /// lights back.
    /// </summary>
    public void Disconnect()
    {
        CancelConnect();

        // Puts the lights back (through the bridge they follow through), as it's a different
        // connection from now on
        _settingsService.HueProtectedAppKey = null;
        _settingsService.HueCertificateHash = null;
        _settingsService.HueBridgeId = null;
        _settingsService.Save();

        _blockingStatus = null;
        Evaluate();
    }

    // Whether the bridge needs connecting to again (e.g. its key was revoked)
    public bool NeedsReconnecting => _blockingStatus is not null;

    // The key, or whether it's still waiting for the button to be pressed
    private async Task<(string? Key, bool IsWaiting)> TryPairAsync(
        string address,
        CancellationToken cancellationToken
    )
    {
        var deviceName = Environment.MachineName;
        if (deviceName.Length > 19)
            deviceName = deviceName[..19];

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri($"https://{address}/api")
        )
        {
            Content = new StringContent(
                JsonSerializer.Serialize(
                    new Dictionary<string, string> { ["devicetype"] = "Afterglow#" + deviceName },
                    HueSerializerContext.Default.DictionaryStringString
                ),
                Encoding.UTF8,
                "application/json"
            ),
        };

        var json = await SendForTextAsync(request, cancellationToken);
        var (key, errorType) = HueJson.ParsePairing(json);

        // 101: the button hasn't been pressed yet
        return (key, key is null && errorType == 101);
    }

    private async Task<string?> TryGetBridgeIdAsync(
        string address,
        CancellationToken cancellationToken
    )
    {
        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                new Uri($"https://{address}/api/0/config")
            );

            using var document = JsonDocument.Parse(
                await SendForTextAsync(request, cancellationToken)
            );
            return
                document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("bridgeid", out var id)
                && id.ValueKind == JsonValueKind.String
                ? id.GetString()
                : null;
        }
        catch (Exception ex) when (ex is HueRequestException or JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Loads the bridge's rooms and zones (e.g. when the settings are shown). Must be called on
    /// the UI thread.
    /// </summary>
    public async Task RefreshRoomsAsync()
    {
        if (!_isActive || !IsPaired || _isConnecting)
            return;

        _expectedCertificateHash = _settingsService.HueCertificateHash;

        try
        {
            Rooms = await GetRoomsAsync();
            _lastRoomsRefresh = _getNow();

            if (!_isFollowing && !_isConnecting && Status is HueStatus.Unreachable)
                Evaluate();
        }
        catch (HueRequestException ex)
        {
            Debug.WriteLine($"Failed to load the Hue rooms: {ex.Message}");

            if (ex.Status is HueStatus.Unauthorized or HueStatus.CertificateChanged)
            {
                _blockingStatus = ex.Status;
                Evaluate();
            }
            else if (!_isFollowing && !_isConnecting)
            {
                Status = ex.Status;
            }
        }
    }

    private async Task<IReadOnlyList<HueLight>> GetLightsAsync() =>
        HueJson.ParseLights(await GetResourceAsync("light"));

    private async Task<IReadOnlyList<HueRoom>> GetRoomsAsync()
    {
        var rooms = HueJson.ParseRooms(await GetResourceAsync("room"));
        var zones = HueJson.ParseRooms(await GetResourceAsync("zone"));

        return rooms.Concat(zones).ToArray();
    }

    private async Task<string> GetResourceAsync(string type)
    {
        if (TryGetBridgeAddress() is not { } address || GetAppKey() is not { } key)
            throw new HueRequestException(HueStatus.NotConnected, "Not connected.");

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            new Uri($"https://{address}/clip/v2/resource/{type}")
        );
        request.Headers.Add("hue-application-key", key);

        return await SendForTextAsync(request);
    }

    // Returns the lights that couldn't be set (so that they're tried again). May run on any
    // thread, as it doesn't touch the settings.
    private async Task<IReadOnlyList<string>> SendCommandsAsync(
        (string Address, string Key) bridge,
        IReadOnlyList<HueCommand> commands,
        bool isParallel,
        Func<bool>? shouldContinue = null
    )
    {
        var (address, key) = bridge;
        var failed = new List<string>();

        // Without returning to the UI thread, which may be waiting for this (when shutting down)
        await _writeLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (isParallel)
            {
                using var throttle = new SemaphoreSlim(MaxParallelRequests);
                var results = await Task.WhenAll(
                        commands.Select(async command =>
                        {
                            await throttle.WaitAsync().ConfigureAwait(false);
                            try
                            {
                                return (
                                    command.LightId,
                                    IsSent: await TrySendCommandAsync(address, key, command)
                                        .ConfigureAwait(false)
                                );
                            }
                            finally
                            {
                                throttle.Release();
                            }
                        })
                    )
                    .ConfigureAwait(false);

                failed.AddRange(results.Where(r => !r.IsSent).Select(r => r.LightId));
            }
            else
            {
                foreach (var command in commands)
                {
                    if (shouldContinue?.Invoke() == false)
                        break;

                    if (!await TrySendCommandAsync(address, key, command).ConfigureAwait(false))
                        failed.Add(command.LightId);
                }
            }
        }
        finally
        {
            _writeLock.Release();
        }

        return failed;
    }

    // Waits until the next command may be sent, so that they reach the bridge spaced out (even
    // when several are sent at once)
    private async Task WaitForSendSlotAsync()
    {
        if (_sendSpacing <= TimeSpan.Zero)
            return;

        long wait;
        lock (_sendSlotLock)
        {
            var now = Environment.TickCount64;
            var slot = Math.Max(now, _nextSendSlot);
            _nextSendSlot = slot + _currentSpacing;
            wait = slot - now;
        }

        if (wait > 0)
            await Task.Delay(TimeSpan.FromMilliseconds(wait)).ConfigureAwait(false);
    }

    // Spaces commands further apart while the bridge says it's busy (up to a limit), and back to
    // normal gradually once it keeps up
    private void AdjustSpacing(bool isBusy)
    {
        if (_sendSpacing <= TimeSpan.Zero)
            return;

        var normal = (long)_sendSpacing.TotalMilliseconds;
        lock (_sendSlotLock)
        {
            _currentSpacing = isBusy
                ? Math.Min(_currentSpacing * 2, (long)MaxSendSpacing.TotalMilliseconds)
                : Math.Max(_currentSpacing - 10, normal);
        }
    }

    private async Task<bool> TrySendCommandAsync(string address, string key, HueCommand command)
    {
        for (var attempt = 0; ; attempt++)
        {
            await WaitForSendSlotAsync().ConfigureAwait(false);

            try
            {
                using var request = new HttpRequestMessage(
                    HttpMethod.Put,
                    new Uri(
                        $"https://{address}/clip/v2/resource/light/{Uri.EscapeDataString(command.LightId)}"
                    )
                )
                {
                    Content = new StringContent(
                        HueJson.ToRequestBody(command),
                        Encoding.UTF8,
                        "application/json"
                    ),
                };
                request.Headers.Add("hue-application-key", key);

                var response = await SendForTextAsync(request).ConfigureAwait(false);

                // The bridge can accept a request, but report that it couldn't carry it out
                if (HueJson.ParseErrors(response) is { Count: > 0 } errors)
                {
                    Debug.WriteLine(
                        $"Hue light {command.LightId} wasn't set: {string.Join("; ", errors)}"
                    );
                    return false;
                }

                AdjustSpacing(false);
                return true;
            }
            catch (HueRequestException ex) when (ex.IsBusy && attempt < MaxBusyRetries)
            {
                AdjustSpacing(true);
                await Task.Delay(BusyRetryDelay).ConfigureAwait(false);
            }
            catch (HueRequestException ex)
            {
                if (ex.IsBusy)
                    AdjustSpacing(true);

                Debug.WriteLine($"Failed to set Hue light {command.LightId}: {ex.Message}");
                return false;
            }
        }
    }

    // Throws OperationCanceledException only when the given token is cancelled (and gives up
    // after a while on its own)
    private async Task<string> SendForTextAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default
    )
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(RequestTimeout);

        try
        {
            using var response = await _send(request, timeout.Token).ConfigureAwait(false);

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                throw new HueRequestException(HueStatus.Unauthorized, "The key was rejected.");

            if (!response.IsSuccessStatusCode)
            {
                throw new HueRequestException(
                    HueStatus.Unreachable,
                    $"Status {(int)response.StatusCode}."
                )
                {
                    IsBusy =
                        response.StatusCode
                            is HttpStatusCode.TooManyRequests
                                or HttpStatusCode.ServiceUnavailable,
                };
            }

            return await response.Content.ReadAsStringAsync(timeout.Token).ConfigureAwait(false);
        }
        catch (HttpRequestException ex) when (ex.InnerException is AuthenticationException)
        {
            throw new HueRequestException(HueStatus.CertificateChanged, ex.Message);
        }
        // Not being able to reach it (e.g. it's off, or the PC isn't on the same network)
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
        {
            cancellationToken.ThrowIfCancellationRequested();

            throw new HueRequestException(HueStatus.Unreachable, ex.Message);
        }
    }

    // Bridges on the network, as registered with the Hue discovery service
    private async Task<IReadOnlyList<HueBridge>> DiscoverAsync()
    {
        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                new Uri($"https://{DiscoveryHost}/")
            );
            return HueJson.ParseDiscovery(await SendForTextAsync(request));
        }
        catch (Exception ex) when (ex is HueRequestException or JsonException)
        {
            Debug.WriteLine($"Failed to search for Hue bridges: {ex.Message}");
            return [];
        }
    }

    private Task<HttpResponseMessage> SendToNetworkAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    ) =>
        request.RequestUri?.Host == DiscoveryHost
            ? Http.Client.SendAsync(request, cancellationToken)
            : GetBridgeClient().SendAsync(request, cancellationToken);

    // Hue bridges have their own certificates, which no public authority vouches for, so the
    // first one seen is trusted (like SSH does), and has to stay the same afterwards
    private HttpClient GetBridgeClient() =>
        _bridgeClient ??= new HttpClient(
            new SocketsHttpHandler
            {
                ConnectTimeout = RequestTimeout,
                PooledConnectionIdleTimeout = TimeSpan.FromSeconds(30),
                SslOptions =
                {
                    RemoteCertificateValidationCallback = (sender, certificate, _, _) =>
                        certificate is not null
                        && CheckCertificate(
                            GetCertificateHash(certificate),
                            // The handler passes the TLS stream here (not the request), which
                            // knows the host it connects to, but not the port
                            sender switch
                            {
                                SslStream stream => stream.TargetHostName,
                                HttpRequestMessage message => message.RequestUri?.IdnHost,
                                _ => null,
                            }
                        ),
                },
            }
        )
        {
            Timeout = Timeout.InfiniteTimeSpan,
        };

    // Notes the certificate a bridge presented (at the given host, without a port), and whether
    // it's the one expected
    internal bool CheckCertificate(string hash, string? host)
    {
        if (!string.IsNullOrEmpty(host))
            _seenCertificateHashes[host] = hash;

        return IsCertificateAccepted(hash, _expectedCertificateHash);
    }

    internal static string GetCertificateHash(X509Certificate certificate) =>
        Convert.ToHexString(SHA256.HashData(certificate.GetRawCertData()));

    internal static bool IsCertificateAccepted(string hash, string? expectedHash) =>
        expectedHash is null
        || string.Equals(hash, expectedHash, StringComparison.OrdinalIgnoreCase);

    // An IP address or host name, optionally with a port (but without a scheme or path)
    internal static string? TryNormalizeAddress(string? address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return null;

        var value = address.Trim();

        if (value.Contains("://", StringComparison.Ordinal))
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
                return null;

            value = uri.Authority;
        }

        value = value.TrimEnd('/');

        return
            Uri.TryCreate($"https://{value}/", UriKind.Absolute, out var result)
            && result.AbsolutePath == "/"
            ? result.Authority
            : null;
    }

    /// <summary>
    /// The base color cycle at the given time, which lights follow when nothing else says what
    /// to (e.g. in tests): without the temporary offsets, and without pauses for fullscreen apps
    /// and the like, which only concern the screens.
    /// </summary>
    internal ColorConfiguration GetTargetConfiguration(SettingsService settings, DateTimeOffset now)
    {
        SolarTimes solarTimes;
        if (settings is { IsManualSunriseSunsetEnabled: false, Location: { } location })
        {
            var day = DateOnly.FromDateTime(now.LocalDateTime);
            if (_solarTimesCache is not { } cache || cache.Day != day || cache.Location != location)
            {
                cache = (day, location, SolarTimes.Calculate(location, now));
                _solarTimesCache = cache;
            }

            solarTimes = cache.Times;
        }
        else
        {
            solarTimes = new SolarTimes(settings.ManualSunrise, settings.ManualSunset);
        }

        return settings.IsEveningConfigurationEnabled
            ? Cycle.InterpolateConfiguration(
                solarTimes,
                settings.DayConfiguration,
                settings.EveningConfiguration,
                settings.NightConfiguration,
                settings.NightStartTime,
                settings.ConfigurationTransitionDuration,
                settings.ConfigurationTransitionOffset,
                now
            )
            : Cycle.InterpolateConfiguration(
                solarTimes,
                settings.DayConfiguration,
                settings.NightConfiguration,
                settings.ConfigurationTransitionDuration,
                settings.ConfigurationTransitionOffset,
                now
            );
    }

    public void Dispose()
    {
        _isStarted = false;
        _connectCancellation?.Cancel();

        // Puts the lights back right away, as the app is closing
        if (_isFollowing)
            StopFollowing(true);

        Evaluate();
        _enabledSubscription.Dispose();
        _bridgeClient?.Dispose();
    }

    private sealed class HueRequestException(HueStatus status, string message) : Exception(message)
    {
        public HueStatus Status { get; } = status;

        // The bridge is busy (it gets too many requests), so it can be tried again shortly
        public bool IsBusy { get; init; }
    }
}

[System.Text.Json.Serialization.JsonSerializable(typeof(Dictionary<string, string>))]
internal partial class HueSerializerContext : System.Text.Json.Serialization.JsonSerializerContext;

/// <summary>
/// What the lights follow: the cycle as the screen shows it right now.
/// </summary>
/// <param name="Configuration">Color temperature and brightness (including the cycle preview and any adjustments).</param>
/// <param name="IsScreenPaused">Whether the screen is paused on its own (e.g. for a fullscreen application).</param>
/// <param name="IsPreview">Whether the cycle preview is playing (which also runs while Afterglow is off).</param>
public readonly record struct HueTarget(
    ColorConfiguration Configuration,
    bool IsScreenPaused,
    bool IsPreview
);

/// <summary>
/// A light in the chosen rooms, and how it's doing.
/// </summary>
public readonly record struct HueLightInfo(
    string Id,
    string? DeviceId,
    string? Name,
    bool IsOn,
    bool IsExcluded,
    HueLightState State,
    // Whether it can be adjusted at all (unlike e.g. a smart plug), and whether anything it can
    // do is followed (e.g. a white-only light doesn't follow the color temperature)
    bool IsAdjustable = true,
    bool CanFollow = true,
    // When it's followed again, after being left alone as it was changed by something else
    DateTimeOffset? ReclaimAt = null
);

public enum HueStatus
{
    // Turned off
    Off,

    // Not connected to a bridge yet
    NotConnected,

    // Looking for a bridge on the network
    Searching,
    NoBridgeFound,

    // Waiting for the button on the bridge to be pressed
    WaitingForButton,
    ButtonNotPressed,

    // Just connected
    Connected,

    // Connected, but no rooms are chosen
    NoRooms,

    // Connected, but Afterglow is turned off
    Paused,

    // Connected, but lights are paused from the tray (for a while, or until resumed)
    PausedByUser,

    // Connected, but the screen is paused on its own (e.g. for a fullscreen application), and
    // the lights are set to pause along with it
    PausedWithScreen,

    Following,

    // The bridge can't be reached (tried again later)
    Unreachable,

    // The bridge no longer accepts the key (e.g. Afterglow was removed in the Hue app)
    Unauthorized,

    // Something other than the bridge answered at its address
    CertificateChanged,
}
