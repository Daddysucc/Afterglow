using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace Afterglow.Core;

/// <summary>
/// A light on a Philips Hue bridge, as reported by its API (CLIP v2).
/// </summary>
public readonly record struct HueLight(
    string Id,
    // The device it belongs to (rooms list devices, not lights)
    string? DeviceId,
    bool IsOn,
    // 0 to 100, or null if it can't be dimmed
    double? Brightness,
    // Null when it's showing a color rather than a white (color temperature)
    int? Mirek,
    // Its color temperature range, or null if it has no adjustable white
    int? MirekMinimum,
    int? MirekMaximum,
    double? ColorX,
    double? ColorY,
    // As named in the Hue app
    string? Name = null
)
{
    public bool SupportsColorTemperature => MirekMinimum is not null && MirekMaximum is not null;

    public bool SupportsColor => ColorX is not null && ColorY is not null;

    public bool SupportsBrightness => Brightness is not null;
}

/// <summary>
/// A room on a Philips Hue bridge, with the devices (and lights) in it.
/// </summary>
public sealed record HueRoom(
    string Id,
    string Name,
    IReadOnlyList<string> DeviceIds,
    IReadOnlyList<string> LightIds
)
{
    public bool Contains(HueLight light) => Contains(light.Id, light.DeviceId);

    public bool Contains(string lightId, string? deviceId) =>
        LightIds.Contains(lightId) || (deviceId is not null && DeviceIds.Contains(deviceId));
}

/// <summary>
/// A Hue bridge found on the network.
/// </summary>
public readonly record struct HueBridge(string Id, string Address);

/// <summary>
/// What to set a light to. Each part is only set if it's there.
/// </summary>
public readonly record struct HueCommand(
    string LightId,
    int? Mirek,
    double? Brightness,
    double? ColorX,
    double? ColorY,
    int TransitionMilliseconds,
    // Turns it on or off (or leaves it as it is, without)
    bool? IsOn = null
);

/// <summary>
/// Reads the responses of a Hue bridge (and of the Hue discovery service).
/// </summary>
public static class HueJson
{
    private static string? GetString(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static double? GetNumber(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.Number
            ? value.GetDouble()
            : null;

    private static JsonElement GetObject(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.Object
            ? value
            : default;

    private static IEnumerable<JsonElement> GetData(JsonDocument document) =>
        document.RootElement.ValueKind == JsonValueKind.Object
        && document.RootElement.TryGetProperty("data", out var data)
        && data.ValueKind == JsonValueKind.Array
            ? data.EnumerateArray()
            : [];

    /// <summary>
    /// Reads the response of GET /clip/v2/resource/light. Entries that can't be read are skipped.
    /// </summary>
    public static IReadOnlyList<HueLight> ParseLights(string json)
    {
        using var document = JsonDocument.Parse(json);
        var lights = new List<HueLight>();

        foreach (var item in GetData(document))
        {
            if (GetString(item, "id") is not { } id)
                continue;

            var on = GetObject(item, "on");
            var isOn =
                on.ValueKind == JsonValueKind.Object
                && on.TryGetProperty("on", out var onValue)
                && onValue.ValueKind == JsonValueKind.True;

            var colorTemperature = GetObject(item, "color_temperature");
            var schema = GetObject(colorTemperature, "mirek_schema");
            var isMirekValid =
                colorTemperature.ValueKind == JsonValueKind.Object
                && colorTemperature.TryGetProperty("mirek_valid", out var valid)
                && valid.ValueKind == JsonValueKind.True;

            var xy = GetObject(GetObject(item, "color"), "xy");

            lights.Add(
                new HueLight(
                    id,
                    GetString(GetObject(item, "owner"), "rid"),
                    isOn,
                    GetNumber(GetObject(item, "dimming"), "brightness"),
                    isMirekValid && GetNumber(colorTemperature, "mirek") is { } mirek
                        ? (int)mirek
                        : null,
                    schema.ValueKind == JsonValueKind.Object
                        ? (int?)(GetNumber(schema, "mirek_minimum") ?? 153)
                        : null,
                    schema.ValueKind == JsonValueKind.Object
                        ? (int?)(GetNumber(schema, "mirek_maximum") ?? 500)
                        : null,
                    GetNumber(xy, "x"),
                    GetNumber(xy, "y"),
                    GetString(GetObject(item, "metadata"), "name")
                )
            );
        }

        return lights;
    }

    /// <summary>
    /// Reads the response of GET /clip/v2/resource/room, sorted by name.
    /// </summary>
    public static IReadOnlyList<HueRoom> ParseRooms(string json)
    {
        using var document = JsonDocument.Parse(json);
        var rooms = new List<HueRoom>();

        foreach (var item in GetData(document))
        {
            if (GetString(item, "id") is not { } id)
                continue;

            var deviceIds = new List<string>();
            var lightIds = new List<string>();

            if (
                item.TryGetProperty("children", out var children)
                && children.ValueKind == JsonValueKind.Array
            )
            {
                foreach (var child in children.EnumerateArray())
                {
                    if (GetString(child, "rid") is not { } childId)
                        continue;

                    if (GetString(child, "rtype") == "light")
                        lightIds.Add(childId);
                    else
                        deviceIds.Add(childId);
                }
            }

            var name = GetString(GetObject(item, "metadata"), "name");
            rooms.Add(
                new HueRoom(id, string.IsNullOrWhiteSpace(name) ? id : name, deviceIds, lightIds)
            );
        }

        return rooms.OrderBy(r => r.Name, StringComparer.CurrentCultureIgnoreCase).ToArray();
    }

    /// <summary>
    /// Reads the response of the discovery service (https://discovery.meethue.com).
    /// </summary>
    public static IReadOnlyList<HueBridge> ParseDiscovery(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
            return [];

        return document
            .RootElement.EnumerateArray()
            .Select(item =>
                (Id: GetString(item, "id"), Address: GetString(item, "internalipaddress"))
            )
            .Where(b => !string.IsNullOrWhiteSpace(b.Address))
            .Select(b => new HueBridge(b.Id ?? b.Address!, b.Address!))
            .ToArray();
    }

    /// <summary>
    /// Reads the response to a request for an application key: the key once the bridge's button
    /// has been pressed, or the error type (101 while it hasn't been).
    /// </summary>
    public static (string? Key, int? ErrorType) ParsePairing(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
            return (null, null);

        foreach (var item in document.RootElement.EnumerateArray())
        {
            if (GetString(GetObject(item, "success"), "username") is { Length: > 0 } key)
                return (key, null);

            if (GetNumber(GetObject(item, "error"), "type") is { } type)
                return (null, (int)type);
        }

        return (null, null);
    }

    /// <summary>
    /// Errors that the bridge reports in a (CLIP v2) response, if any.
    /// </summary>
    public static IReadOnlyList<string> ParseErrors(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (
                document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty("errors", out var errors)
                || errors.ValueKind != JsonValueKind.Array
            )
            {
                return [];
            }

            return errors
                .EnumerateArray()
                .Select(e => GetString(e, "description") ?? e.ToString())
                .ToArray();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    /// <summary>
    /// The body of PUT /clip/v2/resource/light/{id}.
    /// </summary>
    public static string ToRequestBody(HueCommand command)
    {
        var parts = new List<string>();

        if (command.IsOn is { } isOn)
            parts.Add($"\"on\":{{\"on\":{(isOn ? "true" : "false")}}}");

        if (command.Mirek is { } mirek)
            parts.Add(
                $"\"color_temperature\":{{\"mirek\":{mirek.ToString(CultureInfo.InvariantCulture)}}}"
            );

        if (command.ColorX is { } x && command.ColorY is { } y)
        {
            parts.Add(
                $"\"color\":{{\"xy\":{{\"x\":{x.ToString("0.####", CultureInfo.InvariantCulture)},\"y\":{y.ToString("0.####", CultureInfo.InvariantCulture)}}}}}"
            );
        }

        if (command.Brightness is { } brightness)
            parts.Add(
                $"\"dimming\":{{\"brightness\":{brightness.ToString("0.#", CultureInfo.InvariantCulture)}}}"
            );

        parts.Add(
            $"\"dynamics\":{{\"duration\":{command.TransitionMilliseconds.ToString(CultureInfo.InvariantCulture)}}}"
        );

        return "{" + string.Join(",", parts) + "}";
    }
}

/// <summary>
/// Decides how Hue lights follow the cycle, the way similar apps (e.g. Adaptive Lighting) do it:
/// only lights that are on are adjusted (never turned on); a light that's turned on is adjusted
/// right away; a light that's changed by something else (the Hue app, a switch, a scene) is left
/// alone until it's turned off and on again; and when following stops, each light still being
/// followed goes back to how it was before.
/// </summary>
public sealed class HueLightController
{
    // How quickly lights change: gently while following, and quickly when they're first matched
    // (e.g. right after being turned on) or put back
    public const int FollowTransitionMilliseconds = 4000;
    public const int InitialTransitionMilliseconds = 1000;
    public const int RestoreTransitionMilliseconds = 1000;
    public const int PreviewTransitionMilliseconds = 400;

    // Smaller changes aren't sent, as they couldn't be seen anyway (and each one sent is more
    // traffic for the bridge, which drops commands when it gets too many). Each is about the
    // smallest difference the eyes can tell: 5 mireds of white (about 30 K for warm whites, more
    // for cool ones), a little less than that in color, and a small share of the brightness (as
    // the eyes see brightness relatively), though never less than lights can show.
    private const int MirekStep = 5;
    private const double ColorStep = 0.004;
    private const double RelativeBrightnessStep = 0.03;
    private const double MinimumBrightnessStep = 0.5;

    // Whether a change in brightness (0 to 100) is large enough to be sent (or a change at all,
    // when it's exactly what the user chose)
    private static bool IsBrightnessStep(double from, double to, bool isExact) =>
        isExact
            ? to != from
            : Math.Abs(to - from)
                >= Math.Max(MinimumBrightnessStep, Math.Min(from, to) * RelativeBrightnessStep);

    // Differences that a light can report back after being set (it rounds what it's given)
    private const int MirekTolerance = 3;
    private const double ColorTolerance = 0.01;
    private const double BrightnessTolerance = 2;

    // After a light is set because of something the user did (e.g. moving a slider, or playing
    // the cycle preview), it isn't taken for changed by something else for a while (whatever it
    // shows, as it can still be catching up with all that was sent)
    private static readonly TimeSpan UserChangeGrace = TimeSpan.FromSeconds(30);

    // How long what a light was set to is remembered, to tell a light that missed what it was
    // set to after it from one that something else changed
    private static readonly TimeSpan RecentStateLifetime = TimeSpan.FromMinutes(2);
    private const int MaxRecentStates = 64;

    // How long a light that missed what it was set to (the given number of times in a row) waits
    // to be set again: right away the first time, then 10 s, doubling up to 2 min
    public static TimeSpan GetRetryDelay(int missedCount) =>
        missedCount <= 1
            ? TimeSpan.Zero
            : TimeSpan.FromSeconds(Math.Min(10 * Math.Pow(2, missedCount - 2), 120));

    // How much warmer than the screen's tint lights are set, by default and at most
    public const double DefaultWarmth = 4;
    public const double MinimumWarmth = 1;
    public const double MaximumWarmth = 8;

    /// <summary>
    /// How long a light changed by something else is left alone before it's followed again
    /// (counted from when it was last changed), or null to leave it alone until it's turned off
    /// and on.
    /// </summary>
    public TimeSpan? ReclaimDelay { get; set; }

    /// <summary>
    /// When a light left alone (as it was changed by something else) is followed again, if it is.
    /// </summary>
    public DateTimeOffset? GetReclaimTime(string lightId) =>
        ReclaimDelay is { } delay && _states.TryGetValue(lightId, out var state) && state.IsReleased
            ? state.ReleasedAt + delay
            : null;

    /// <summary>
    /// How much warmer lights are set than the screen's tint, to look about as warm as it (see
    /// <see cref="ToLightColor" />).
    /// </summary>
    public double Warmth { get; set; } = DefaultWarmth;

    // How strongly lights dim as the screen dims, by default and at least and most. At the
    // default, a light gives off as much (relative to its full brightness) as the screen does
    // (relative to its untinted white): see ToLightBrightness.
    public const double DefaultDimming = ScreenGamma;
    public const double MinimumDimming = 0.5;
    public const double MaximumDimming = 6;

    /// <summary>
    /// How strongly lights dim as the screen dims (see <see cref="ToLightBrightness" />).
    /// </summary>
    public double Dimming { get; set; } = DefaultDimming;

    // How long a light may take to report what it was set to, once its transition is over
    private static readonly TimeSpan SettleMargin = TimeSpan.FromSeconds(2);

    /// <summary>
    /// How long after being set (with the given transition) a light is checked for having got there.
    /// </summary>
    public static TimeSpan GetSettleTime(int transitionMilliseconds) =>
        TimeSpan.FromMilliseconds(transitionMilliseconds) + SettleMargin;

    private sealed class LightState
    {
        // How it was before being followed (what it goes back to), for each part that's followed
        public int? OriginalMirek { get; set; }
        public double? OriginalX { get; set; }
        public double? OriginalY { get; set; }
        public double? OriginalBrightness { get; set; }

        // What it was last set to (or was already at), for each part that's followed
        public int? SentMirek { get; set; }
        public double? SentX { get; set; }
        public double? SentY { get; set; }
        public double? SentBrightness { get; set; }

        // Set to a white or a color (to follow the color temperature)
        public bool HasSentColor => SentMirek is not null || SentX is not null;
        public DateTimeOffset SentAt { get; set; } = DateTimeOffset.MinValue;

        // When it should show what it was last set to (once its transition is over)
        public DateTimeOffset SettledAt { get; set; } = DateTimeOffset.MinValue;

        // When it was last set because of something the user did
        public DateTimeOffset LastUserChangeAt { get; set; } = DateTimeOffset.MinValue;

        // Whites, colors, and brightnesses it was recently set to,
        // any of which it can still show if what was sent after didn't get there (the bridge
        // drops commands when it gets too many, and lights out of its reach miss them), or got
        // there late
        public List<(int? Mirek, double? X, double? Y, DateTimeOffset At)> RecentColors { get; } =
        [];
        public List<(double Brightness, DateTimeOffset At)> RecentBrightnesses { get; } = [];

        // Notes what it's set to now (all of it, as only part of it may have been sent)
        public void Remember(
            int? mirek,
            double? x,
            double? y,
            double? brightness,
            DateTimeOffset now
        )
        {
            // Older ones are forgotten, except the last of each (which it still shows if what's
            // sent now doesn't get there, however long ago that was)
            if (mirek is not null || x is not null)
            {
                RemoveOld(RecentColors, c => c.At, now);
                RecentColors.Add((mirek, x, y, now));
            }

            if (brightness is { } b)
            {
                RemoveOld(RecentBrightnesses, c => c.At, now);
                RecentBrightnesses.Add((b, now));
            }

            // Never too many, even when it's set very often (e.g. during the cycle preview)
            if (RecentColors.Count > MaxRecentStates)
                RecentColors.RemoveRange(0, RecentColors.Count - MaxRecentStates);
            if (RecentBrightnesses.Count > MaxRecentStates)
                RecentBrightnesses.RemoveRange(0, RecentBrightnesses.Count - MaxRecentStates);
        }

        private static void RemoveOld<T>(
            List<T> items,
            Func<T, DateTimeOffset> getAt,
            DateTimeOffset now
        )
        {
            for (var i = items.Count - 2; i >= 0; i--)
            {
                if (now - getAt(items[i]) > RecentStateLifetime)
                    items.RemoveAt(i);
            }
        }

        // What a preview changed that isn't followed (its white or color, and its brightness), as
        // it was before, to be put back once the preview is over
        public (
            bool HasColor,
            int? Mirek,
            double? X,
            double? Y,
            double? Brightness
        )? BeforePreview { get; set; }

        // Was just turned on (by Afterglow), so it may still read as off until it's read again
        public bool IsJustTurnedOn { get; set; }

        // Was seen showing what it was set to (so it can't still be waiting for the first time)
        public bool IsConfirmed { get; set; }

        // Times in a row that it was found not to have taken what it was set to
        public int MissedCount { get; set; }

        // Changed by something else, so it's left alone until it's turned off
        public bool IsReleased { get; set; }

        // When it was last seen changed by something else (while it's left alone), and how
        public DateTimeOffset ReleasedAt { get; set; }
        public HueLight? ReleasedAs { get; set; }

        // Followed again after being left alone for a while, so it changes back gently
        public bool IsReclaimed { get; set; }

        // What it was last set to didn't get there (e.g. the bridge was busy), so it's sent again
        public bool IsUnconfirmed { get; set; }

        public bool IsFollowed => !IsReleased && (HasSentColor || SentBrightness is not null);
    }

    private readonly Dictionary<string, LightState> _states = new(StringComparer.Ordinal);

    // Lights that were turned on by Afterglow (when asked to), which are turned off again when
    // it lets go of them (or, when they're only paused, set to a neutral white instead)
    private readonly HashSet<string> _turnedOnLightIds = new(StringComparer.Ordinal);

    // How lights paused to a custom look were before Afterglow followed them (what they go back
    // to: what's followed once they're let go of again, and what isn't as soon as they're
    // followed again), and whether Afterglow had turned them on
    private readonly record struct PausedLight(
        bool HasColor,
        int? Mirek,
        double? X,
        double? Y,
        bool HasBrightness,
        double? Brightness,
        bool IsTurnedOn,
        // Turned off for the pause, so it's turned back on afterwards
        bool IsTurnedOff = false
    );

    private readonly Dictionary<string, PausedLight> _pausedLights = new(StringComparer.Ordinal);

    // The neutral white that lights turned on by Afterglow are left at when they're paused:
    // what Hue lights show when they're first turned on (about 2700 K, at full brightness)
    public const int NeutralMirek = 366;
    public const double NeutralBrightness = 100;

    // Lights currently being followed
    public int FollowedLightCount => _states.Values.Count(s => s.IsFollowed);

    /// <summary>
    /// How a light is doing (one that's on, in the chosen rooms).
    /// </summary>
    public HueLightState GetState(string lightId) =>
        !_states.TryGetValue(lightId, out var state) ? HueLightState.NotFollowed
        : state.IsReleased ? HueLightState.LeftAlone
        : !state.IsFollowed ? HueLightState.NotFollowed
        : state.MissedCount > 0 ? HueLightState.NotResponding
        : HueLightState.Following;

    // Lights being followed that haven't taken what they were last set to (e.g. as they're out
    // of the bridge's reach)
    public int UnresponsiveLightCount =>
        _states.Values.Count(s => s.IsFollowed && s.MissedCount > 0);

    /// <summary>
    /// Works out what to send to the given lights (those in the chosen rooms, as they are now), so
    /// that they follow the given color temperature and brightness.
    /// </summary>
    public IReadOnlyList<HueCommand> Update(
        IReadOnlyList<HueLight> lights,
        ColorConfiguration target,
        bool isTemperatureFollowed,
        bool isBrightnessFollowed,
        DateTimeOffset now,
        int followTransitionMilliseconds = FollowTransitionMilliseconds,
        // Whether it's because of something the user did (rather than the cycle moving on)
        bool isUserChange = false,
        // Whether lights that are off are turned on (when the user asked for it)
        bool isTurningOn = false
    )
    {
        var commands = new List<HueCommand>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var light in lights)
        {
            seen.Add(light.Id);

            // A light that's off is only turned on when the user asks for it (and then straight
            // to the cycle, with no flash of another white first). Once it's turned on again,
            // it's followed afresh (even if it was changed by something else before).
            if (!light.IsOn)
            {
                _states.Remove(light.Id);
                _turnedOnLightIds.Remove(light.Id);

                // Turned off for a pause, so it's turned back on (as it was on before), and still
                // goes back to how it was before Afterglow, once it's let go of
                var isTurnedOffForPause =
                    _pausedLights.Remove(light.Id, out var pausedOff) && pausedOff.IsTurnedOff;

                if (
                    (isTurningOn || isTurnedOffForPause)
                    && TryTurnOn(light, target, isTemperatureFollowed, isBrightnessFollowed, now)
                        is { } turnOn
                )
                {
                    commands.Add(turnOn);

                    if (isTurnedOffForPause && _states.TryGetValue(light.Id, out var turnedOnState))
                    {
                        RestorePausedOriginals(turnedOnState, pausedOff);
                        if (!pausedOff.IsTurnedOn)
                            _turnedOnLightIds.Remove(light.Id);
                    }
                }

                continue;
            }

            var followsTemperature =
                isTemperatureFollowed && (light.SupportsColorTemperature || light.SupportsColor);
            var followsBrightness = isBrightnessFollowed && light.SupportsBrightness;

            var isNew = false;
            if (!_states.TryGetValue(light.Id, out var state))
            {
                if (!followsTemperature && !followsBrightness)
                    continue;

                state = new LightState();
                _states[light.Id] = state;
                isNew = true;
            }

            // (it reads as on now)
            state.IsJustTurnedOn = false;

            // Left alone, as it was changed by something else: followed again after a while (if
            // chosen), counted from when it was last changed
            if (state.IsReleased)
            {
                if (state.ReleasedAs is not { } releasedAs || !IsSameLook(light, releasedAs))
                {
                    state.ReleasedAs = light;
                    state.ReleasedAt = now;
                }

                if (ReclaimDelay is not { } delay || now - state.ReleasedAt < delay)
                    continue;

                state.IsReleased = false;
                state.ReleasedAs = null;
                state.IsUnconfirmed = true;
                state.IsReclaimed = true;
                state.MissedCount = 0;
                // (it can show anything while it catches up, rather than be let go of again)
                state.LastUserChangeAt = now;
            }

            // Checked once it had time to get to what it was last set to
            if (!state.IsUnconfirmed && now >= state.SettledAt)
            {
                if (!IsChangedElsewhere(light, state))
                {
                    state.MissedCount = 0;

                    // (once there's something it was set to, which it shows)
                    if (state.HasSentColor || state.SentBrightness is not null)
                        state.IsConfirmed = true;
                }
                // Still as it was before, so it didn't get what it was set to (rather than being
                // changed by something else, e.g. it's out of the bridge's reach for now), and
                // it's set again
                // (or it was being changed quickly just before, after which it can show anything
                // on the way, and still be catching up)
                else if (IsMissed(light, state) || now - state.LastUserChangeAt < UserChangeGrace)
                {
                    state.MissedCount++;
                    state.IsUnconfirmed = true;
                }
                // Something else changed it
                else
                {
                    state.IsReleased = true;
                    state.ReleasedAt = now;
                    state.ReleasedAs = light;
                    continue;
                }
            }

            // Got there after all (e.g. it was late, or the bridge reported a problem that didn't
            // stop it)
            if (state.IsUnconfirmed && state.MissedCount > 0 && !IsChangedElsewhere(light, state))
            {
                state.IsUnconfirmed = false;
                state.MissedCount = 0;
                state.IsConfirmed = true;
            }

            // A light that keeps missing what it's set to is set again less and less often (until
            // it gets there), rather than on every check
            if (state.IsUnconfirmed && now - state.SentAt < GetRetryDelay(state.MissedCount))
                continue;

            var isUnconfirmed = state.IsUnconfirmed;
            state.IsUnconfirmed = false;

            var isFirst = !state.HasSentColor && state.SentBrightness is null;
            var isFollowing = false;
            var isRestoring = false;

            int? commandMirek = null;
            double? commandX = null;
            double? commandY = null;
            double? commandBrightness = null;

            if (followsTemperature)
            {
                var (mirek, x, y) = ToLightColor(target.Temperature, Warmth, light);

                if (!state.HasSentColor)
                {
                    state.OriginalMirek = light.Mirek;
                    (state.OriginalX, state.OriginalY) = light.Mirek is null
                        ? (light.ColorX, light.ColorY)
                        : (null, null);

                    if (!IsShowing(light, mirek, x, y))
                        (commandMirek, commandX, commandY) = (mirek, x, y);
                }
                else if (isUnconfirmed || IsVisiblyDifferent(state, mirek, x, y, isUserChange))
                {
                    (commandMirek, commandX, commandY) = (mirek, x, y);
                }
                else
                {
                    (mirek, x, y) = (state.SentMirek, state.SentX, state.SentY);
                }

                (state.SentMirek, state.SentX, state.SentY) = (mirek, x, y);
                isFollowing |= commandMirek is not null || commandX is not null;
            }
            // No longer followed, so it goes back to how it was
            else if (state.HasSentColor)
            {
                (commandMirek, commandX, commandY) = GetOriginalColor(state);
                (state.SentMirek, state.SentX, state.SentY) = (null, null, null);
                isRestoring = true;
            }

            if (followsBrightness)
            {
                var brightness = ToLightBrightness(target.Brightness, Dimming, target.Temperature);

                if (state.SentBrightness is not { } sentBrightness)
                {
                    state.OriginalBrightness = light.Brightness;

                    if (
                        light.Brightness is not { } currentBrightness
                        || IsBrightnessStep(currentBrightness, brightness, isUserChange)
                    )
                    {
                        commandBrightness = brightness;
                    }

                    state.SentBrightness = brightness;
                }
                else if (
                    isUnconfirmed || IsBrightnessStep(sentBrightness, brightness, isUserChange)
                )
                {
                    commandBrightness = brightness;
                    state.SentBrightness = brightness;
                }

                isFollowing |= commandBrightness is not null;
            }
            else if (state.SentBrightness is not null)
            {
                commandBrightness = state.OriginalBrightness;
                state.SentBrightness = null;
                isRestoring = true;
            }

            // What a preview changed that isn't followed goes back to how it was
            if (state.BeforePreview is { } beforePreview)
            {
                state.BeforePreview = null;

                if (beforePreview.HasColor && !state.HasSentColor)
                    (commandMirek, commandX, commandY) = (
                        beforePreview.Mirek,
                        beforePreview.X,
                        beforePreview.Y
                    );

                if (
                    beforePreview.Brightness is { } previewBrightness
                    && state.SentBrightness is null
                )
                    commandBrightness = previewBrightness;
            }

            // Followed again after being paused to a custom look: it goes back to how it was before
            // that (rather than to the custom look)
            if (isNew && _pausedLights.Remove(light.Id, out var paused))
            {
                // (what's followed goes back to it later, and what isn't goes back to it now)
                if (paused.HasColor && state.HasSentColor)
                {
                    state.OriginalMirek = paused.Mirek;
                    state.OriginalX = paused.X;
                    state.OriginalY = paused.Y;
                }
                else if (paused.HasColor && (paused.Mirek is not null || paused.X is not null))
                {
                    (commandMirek, commandX, commandY) = (paused.Mirek, paused.X, paused.Y);
                }

                if (paused.HasBrightness && state.SentBrightness is not null)
                    state.OriginalBrightness = paused.Brightness;
                else if (paused.HasBrightness && paused.Brightness is { } pausedBrightness)
                    commandBrightness = pausedBrightness;

                if (paused.IsTurnedOn)
                    _turnedOnLightIds.Add(light.Id);
            }

            // A light that already showed what it's set to (so nothing was sent) still counts as
            // having been set to it, as that's what it shows if what's sent next doesn't get there
            if (commandMirek is null && commandX is null && commandBrightness is null)
            {
                if (isFirst && state.IsFollowed)
                    state.Remember(
                        state.SentMirek,
                        state.SentX,
                        state.SentY,
                        state.SentBrightness,
                        now
                    );

                continue;
            }

            commands.Add(
                new HueCommand(
                    light.Id,
                    commandMirek,
                    commandBrightness,
                    commandX,
                    commandY,
                        // Gently while following, but quickly to catch up after missing what it was
                        // set to (so that it gets there, and is checked again, sooner)
                        isFollowing && !isFirst && (!isUnconfirmed || state.IsReclaimed)
                            ? followTransitionMilliseconds
                        : isFollowing || isRestoring ? InitialTransitionMilliseconds
                        : RestoreTransitionMilliseconds
                )
            );

            if (isUserChange && isFollowing)
                state.LastUserChangeAt = now;

            state.SentAt = now;
            state.SettledAt = now + GetSettleTime(commands[^1].TransitionMilliseconds);
            state.IsReclaimed = false;
            state.Remember(state.SentMirek, state.SentX, state.SentY, state.SentBrightness, now);
        }

        // Lights that are gone (e.g. removed from the bridge or the room) are forgotten
        foreach (var id in _states.Keys.Where(id => !seen.Contains(id)).ToArray())
            _states.Remove(id);

        return commands;
    }

    // Turns a light on, straight to what it follows (as it was off, it goes back to off)
    private HueCommand? TryTurnOn(
        HueLight light,
        ColorConfiguration target,
        bool isTemperatureFollowed,
        bool isBrightnessFollowed,
        DateTimeOffset now
    )
    {
        var followsTemperature =
            isTemperatureFollowed && (light.SupportsColorTemperature || light.SupportsColor);
        var followsBrightness = isBrightnessFollowed && light.SupportsBrightness;

        var (mirek, x, y) = followsTemperature
            ? ToLightColor(target.Temperature, Warmth, light)
            : (null, null, null);
        double? brightness = followsBrightness
            ? ToLightBrightness(target.Brightness, Dimming, target.Temperature)
            : null;

        var state = new LightState
        {
            SentMirek = mirek,
            SentX = x,
            SentY = y,
            SentBrightness = brightness,
            SentAt = now,
            SettledAt = now + GetSettleTime(InitialTransitionMilliseconds),
            // As the user asked for it, it isn't let go of while it's catching up
            LastUserChangeAt = now,
            IsJustTurnedOn = true,
        };

        state.Remember(mirek, x, y, brightness, now);
        _states[light.Id] = state;
        _turnedOnLightIds.Add(light.Id);

        return new HueCommand(
            light.Id,
            mirek,
            brightness,
            x,
            y,
            InitialTransitionMilliseconds,
            true
        );
    }

    // A light followed again after a pause goes back to how it was before Afterglow (rather than
    // to how it was during the pause), once it's let go of
    private static void RestorePausedOriginals(LightState state, PausedLight paused)
    {
        if (paused.HasColor && state.HasSentColor)
        {
            state.OriginalMirek = paused.Mirek;
            state.OriginalX = paused.X;
            state.OriginalY = paused.Y;
        }

        if (paused.HasBrightness && state.SentBrightness is not null)
            state.OriginalBrightness = paused.Brightness;
    }

    // The neutral white a light turned on by Afterglow is left at while it's paused
    private static HueCommand GetNeutralCommand(HueLight light) =>
        GetFixedCommand(
            light,
            new ColorConfiguration(1_000_000.0 / NeutralMirek, NeutralBrightness / 100),
            RestoreTransitionMilliseconds
        );

    /// <summary>
    /// What sets a light to the given white (its color temperature is that of the light itself,
    /// not of a screen) and brightness (0 to 1): a white while the light has one that warm, and
    /// otherwise a color, if it can show colors.
    /// </summary>
    public static HueCommand GetFixedCommand(
        HueLight light,
        ColorConfiguration look,
        int transitionMilliseconds
    )
    {
        var mired = 1_000_000 / Math.Clamp(look.Temperature, 1000, 20_000);

        int? mirek = null;
        double? x = null;
        double? y = null;

        if (
            light is { MirekMinimum: { } minimum, MirekMaximum: { } maximum }
            && (mired <= maximum + 0.5 || !light.SupportsColor)
        )
        {
            mirek = Math.Clamp((int)Math.Round(mired), minimum, maximum);
        }
        else if (light.SupportsColor)
        {
            var (cx, cy) = WhitePoint.GetChromaticity(1_000_000 / mired);
            (x, y) = (Math.Round(cx, 4), Math.Round(cy, 4));
        }

        return new HueCommand(
            light.Id,
            mirek,
            light.SupportsBrightness ? ToLightBrightness(look.Brightness, 1) : null,
            x,
            y,
            transitionMilliseconds
        );
    }

    private static (int? Mirek, double? X, double? Y) GetOriginalColor(LightState state) =>
        state.OriginalMirek is { } mirek ? (mirek, null, null)
        : state.OriginalX is { } x && state.OriginalY is { } y ? (null, x, y)
        : (null, null, null);

    // Whether a light shows something it was recently set to (or, until it was seen following,
    // how it was before), rather than what it was last set to, so that didn't get there (yet), rather than something else
    // changing it. Its color and brightness are checked separately, as only part of what was
    // sent can get there.
    private static bool IsMissed(HueLight light, LightState state)
    {
        // Anything between what it was recently set to counts too, as it can be caught partway
        // through a transition (and the bridge can be left showing that, if the light's report
        // of where it ended up gets lost)
        var isKnownColor =
            !state.HasSentColor
            || (
                !state.IsConfirmed
                && IsSameColor(light, state.OriginalMirek, state.OriginalX, state.OriginalY)
            )
            || state.RecentColors.Any(c => IsSameColor(light, c.Mirek, c.X, c.Y))
            || IsWithinRecentColors(light, state);

        var isKnownBrightness =
            state.SentBrightness is null
            || (
                !state.IsConfirmed
                && IsClose(light.Brightness, state.OriginalBrightness, BrightnessTolerance)
            )
            || (
                light.Brightness is { } brightness
                && state.RecentBrightnesses.Count > 0
                && brightness
                    >= state.RecentBrightnesses.Min(b => b.Brightness) - BrightnessTolerance
                && brightness
                    <= state.RecentBrightnesses.Max(b => b.Brightness) + BrightnessTolerance
            );

        return isKnownColor && isKnownBrightness;
    }

    // Whether a light shows a white between the whites, or a color between the colors (within
    // the area they span), that it was recently set to
    private static bool IsWithinRecentColors(HueLight light, LightState state)
    {
        if (light.Mirek is { } mirek)
        {
            var mireks = state
                .RecentColors.Where(c => c.Mirek is not null)
                .Select(c => c.Mirek!.Value)
                .ToArray();

            return mireks.Length > 0
                && mirek >= mireks.Min() - MirekTolerance
                && mirek <= mireks.Max() + MirekTolerance;
        }

        if (light is { ColorX: { } x, ColorY: { } y })
        {
            var colors = state
                .RecentColors.Where(c => c.X is not null && c.Y is not null)
                .Select(c => (X: c.X!.Value, Y: c.Y!.Value))
                .ToArray();

            return colors.Length > 0
                && x >= colors.Min(c => c.X) - ColorTolerance
                && x <= colors.Max(c => c.X) + ColorTolerance
                && y >= colors.Min(c => c.Y) - ColorTolerance
                && y <= colors.Max(c => c.Y) + ColorTolerance;
        }

        return false;
    }

    private static bool IsSameColor(HueLight light, int? mirek, double? x, double? y) =>
        (light.Mirek, mirek) switch
        {
            ({ } a, { } b) => Math.Abs(a - b) <= MirekTolerance,
            (null, null) => x is not null
                && IsClose(light.ColorX, x, ColorTolerance)
                && IsClose(light.ColorY, y, ColorTolerance),
            _ => false,
        };

    private static bool IsClose(double? a, double? b, double tolerance) =>
        (a, b) switch
        {
            ({ } x, { } y) => Math.Abs(x - y) <= tolerance,
            (null, null) => true,
            _ => false,
        };

    // Whether a light still shows the same white or color, and brightness, as before (within what
    // it rounds to)
    private static bool IsSameLook(HueLight light, HueLight before) =>
        (
            (
                before.Mirek is null
                && before.ColorX is null
                && light.Mirek is null
                && light.ColorX is null
            )
            || IsSameColor(
                light,
                before.Mirek,
                before.Mirek is null ? before.ColorX : null,
                before.Mirek is null ? before.ColorY : null
            )
        ) && IsClose(light.Brightness, before.Brightness, BrightnessTolerance);

    // Whether a light already shows the given white or color
    private static bool IsShowing(HueLight light, int? mirek, double? x, double? y) =>
        mirek is { } m ? light.Mirek == m
        : x is { } cx && y is { } cy
            ? light.Mirek is null
                && light.ColorX is { } lx
                && light.ColorY is { } ly
                && Math.Abs(lx - cx) < ColorStep
                && Math.Abs(ly - cy) < ColorStep
        : true;

    // Whether a new white or color is different enough from what was last sent to be sent
    // (or different at all, when it's exactly what the user chose)
    private static bool IsVisiblyDifferent(
        LightState state,
        int? mirek,
        double? x,
        double? y,
        bool isExact
    ) =>
        (state.SentMirek, mirek) switch
        {
            ({ } sent, { } m) => isExact ? sent != m : Math.Abs(sent - m) >= MirekStep,
            (null, null) => x is { } cx
                && y is { } cy
                && (
                    state.SentX is not { } sx
                    || state.SentY is not { } sy
                    || (
                        isExact
                            ? sx != cx || sy != cy
                            : Math.Abs(sx - cx) >= ColorStep || Math.Abs(sy - cy) >= ColorStep
                    )
                ),
            // Changes between a white and a color
            _ => true,
        };

    private static bool IsChangedElsewhere(HueLight light, LightState state)
    {
        // Changed to a color, or to another white
        if (
            state.SentMirek is { } sentMirek
            && (light.Mirek is not { } mirek || Math.Abs(mirek - sentMirek) > MirekTolerance)
        )
        {
            return true;
        }

        // Changed to a white, or to another color
        if (
            state.SentX is { } sentX
            && state.SentY is { } sentY
            && (
                light.Mirek is not null
                || light.ColorX is not { } colorX
                || light.ColorY is not { } colorY
                || Math.Abs(colorX - sentX) > ColorTolerance
                || Math.Abs(colorY - sentY) > ColorTolerance
            )
        )
        {
            return true;
        }

        if (
            state.SentBrightness is { } sentBrightness
            && (
                light.Brightness is not { } brightness
                || Math.Abs(brightness - sentBrightness) > BrightnessTolerance
            )
        )
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Works out what puts the lights back as they were before they were followed, and stops
    /// following them (all of them, or only those matching the given condition). Only lights
    /// that are still on, and weren't changed by something else, are put back (and only what was
    /// changed on them). Lights that Afterglow turned on are turned off again, unless they're
    /// only paused, as the room then still needs light: they're left at a neutral white instead
    /// (and are no longer turned off later).
    /// </summary>
    public IReadOnlyList<HueCommand> Restore(
        IReadOnlyList<HueLight> lights,
        DateTimeOffset now,
        Func<string, bool>? predicate = null,
        bool isPause = false,
        // While paused, lights are set to this look, rather than put back (if one is chosen)
        ColorConfiguration? pauseLook = null,
        // While paused, lights are turned off, rather than put back (and turned on afterwards)
        bool isPauseTurningOff = false
    )
    {
        var commands = new List<HueCommand>();
        var current = new Dictionary<string, HueLight>(StringComparer.Ordinal);
        foreach (var light in lights)
            current[light.Id] = light;

        foreach (var (id, state) in _states.ToArray())
        {
            if (predicate is not null && !predicate(id))
                continue;

            _states.Remove(id);
            var isTurnedOn = _turnedOnLightIds.Remove(id);

            if (!current.TryGetValue(id, out var light))
                continue;

            // (a light Afterglow turned on is still off in what was read of it just before)
            var isJustTurnedOn = state.IsJustTurnedOn && !light.IsOn;
            if (!light.IsOn && !isJustTurnedOn)
                continue;

            // (lights Afterglow turned on are put back even when neither their color nor their
            // brightness is followed, as it turned them on)
            if (!state.IsFollowed && !(isTurnedOn && !state.IsReleased))
                continue;

            // Changed by something else since it was last set (and not noticed yet), unlike a
            // light that's still as it was before (which didn't get what it was set to)
            if (
                !state.IsUnconfirmed
                && now >= state.SettledAt
                && !isJustTurnedOn
                && IsChangedElsewhere(light, state)
                && !IsMissed(light, state)
                && now - state.LastUserChangeAt >= UserChangeGrace
            )
            {
                continue;
            }

            // Paused to a look of the user's choosing, or turned off for the pause: how it was before
            // is remembered, for when it's followed again
            if (isPause && (pauseLook is not null || isPauseTurningOff))
            {
                // (what isn't followed is as it is now, as the look changes that too)
                var (pausedMirek, pausedX, pausedY) = state.HasSentColor
                    ? (state.OriginalMirek, state.OriginalX, state.OriginalY)
                    : (
                        light.Mirek,
                        light.Mirek is null ? light.ColorX : null,
                        light.Mirek is null ? light.ColorY : null
                    );

                _pausedLights[id] = new PausedLight(
                    true,
                    pausedMirek,
                    pausedX,
                    pausedY,
                    true,
                    state.SentBrightness is not null ? state.OriginalBrightness : light.Brightness,
                    isTurnedOn,
                    pauseLook is null
                );

                commands.Add(
                    pauseLook is { } look
                        ? GetFixedCommand(light, look, RestoreTransitionMilliseconds)
                        : new HueCommand(
                            id,
                            null,
                            null,
                            null,
                            null,
                            RestoreTransitionMilliseconds,
                            false
                        )
                );
                continue;
            }

            // Turned on by Afterglow: off again, or a neutral white while it's only paused
            if (isTurnedOn)
            {
                commands.Add(
                    isPause
                        ? GetNeutralCommand(light)
                        : new HueCommand(
                            id,
                            null,
                            null,
                            null,
                            null,
                            RestoreTransitionMilliseconds,
                            false
                        )
                );
                continue;
            }

            var (mirek, x, y) = state.HasSentColor ? GetOriginalColor(state) : (null, null, null);

            var brightness = state.SentBrightness is not null ? state.OriginalBrightness : null;

            // (along with what a preview changed that isn't followed)
            if (state.BeforePreview is { } beforePreview)
            {
                if (beforePreview.HasColor && mirek is null && x is null)
                    (mirek, x, y) = (beforePreview.Mirek, beforePreview.X, beforePreview.Y);

                brightness ??= beforePreview.Brightness;
            }

            if (mirek is null && x is null && brightness is null)
                continue;

            commands.Add(
                new HueCommand(id, mirek, brightness, x, y, RestoreTransitionMilliseconds)
            );
        }

        return commands;
    }

    /// <summary>
    /// Works out what turns off the lights that Afterglow turned on, or all the lights it follows
    /// (those that are still as it set them), and stops following those. With only the ones it
    /// turned on, every other light is left as it is, and still remembered (e.g. when the PC goes
    /// to sleep, so that it carries on afterwards).
    /// </summary>
    public IReadOnlyList<HueCommand> TurnOff(
        IReadOnlyList<HueLight> lights,
        DateTimeOffset now,
        bool isOnlyTurnedOn
    )
    {
        var commands = new List<HueCommand>();
        var current = new Dictionary<string, HueLight>(StringComparer.Ordinal);
        foreach (var light in lights)
            current[light.Id] = light;

        foreach (var id in isOnlyTurnedOn ? _turnedOnLightIds.ToArray() : _states.Keys.ToArray())
        {
            var isTurnedOn = _turnedOnLightIds.Remove(id);

            if (!_states.TryGetValue(id, out var state))
                continue;

            _states.Remove(id);

            if (state.IsReleased || !current.TryGetValue(id, out var light))
                continue;

            // Already off (unlike a light Afterglow just turned on, which is still off in what was
            // read of it just before)
            if (!light.IsOn && !state.IsJustTurnedOn)
                continue;

            if (
                light.IsOn
                && !state.IsUnconfirmed
                && now >= state.SettledAt
                && IsChangedElsewhere(light, state)
                && !IsMissed(light, state)
                && now - state.LastUserChangeAt >= UserChangeGrace
            )
            {
                continue;
            }

            commands.Add(
                new HueCommand(id, null, null, null, null, RestoreTransitionMilliseconds, false)
            );
        }

        return commands;
    }

    /// <summary>
    /// Notes that what was sent to a light didn't get there, so that it's sent again (rather than
    /// taken for a change made by something else).
    /// </summary>
    public void MarkFailed(string lightId)
    {
        if (_states.TryGetValue(lightId, out var state))
        {
            state.IsUnconfirmed = true;
            state.MissedCount++;
        }
    }

    /// <summary>
    /// Follows a light again that was left alone (after it seemed to be changed by something
    /// else), keeping what it goes back to.
    /// </summary>
    public void FollowAgain(string lightId)
    {
        if (!_states.TryGetValue(lightId, out var state) || !state.IsReleased)
            return;

        state.IsReleased = false;
        state.ReleasedAs = null;
        state.IsUnconfirmed = true;
        state.MissedCount = 0;
    }

    /// <summary>
    /// Works out what shows the given look on the lights being followed for a moment (e.g. while
    /// the user picks it), after which they're set back to what they follow, without being
    /// taken for changed by something else meanwhile.
    /// </summary>
    public IReadOnlyList<HueCommand> Preview(
        IReadOnlyList<HueLight> lights,
        ColorConfiguration look,
        DateTimeOffset now
    )
    {
        var commands = new List<HueCommand>();

        foreach (var light in lights)
        {
            if (
                !light.IsOn
                || !_states.TryGetValue(light.Id, out var state)
                || state.IsReleased
                || !state.IsFollowed
            )
            {
                continue;
            }

            // (what isn't followed is noted first, to be put back afterwards)
            state.BeforePreview ??= (
                !state.HasSentColor,
                light.Mirek,
                light.Mirek is null ? light.ColorX : null,
                light.Mirek is null ? light.ColorY : null,
                state.SentBrightness is null ? light.Brightness : null
            );

            commands.Add(GetFixedCommand(light, look, PreviewTransitionMilliseconds));

            // Set back afterwards (right away, as it didn't miss anything)
            state.IsUnconfirmed = true;
            state.LastUserChangeAt = now;
            state.SentAt = now;
        }

        return commands;
    }

    /// <summary>
    /// Works out what sets the lights paused to a custom look (that are still on) to the given look,
    /// e.g. as it's changed while they're paused.
    /// </summary>
    public IReadOnlyList<HueCommand> GetPausedLookCommands(
        IReadOnlyList<HueLight> lights,
        ColorConfiguration look
    ) =>
        lights
            .Where(l =>
                l.IsOn && _pausedLights.TryGetValue(l.Id, out var paused) && !paused.IsTurnedOff
            )
            .Select(l => GetFixedCommand(l, look, PreviewTransitionMilliseconds))
            .ToArray();

    /// <summary>
    /// Works out what the PC shutting down (or going to sleep) does to the lights Afterglow is still
    /// responsible for while they're paused: those left as they are for the pause, and those set to
    /// a look of the user's choosing (lights put back for the pause are no longer its own).
    /// </summary>
    public IReadOnlyList<HueCommand> ShutDownWhilePaused(
        IReadOnlyList<HueLight> lights,
        DateTimeOffset now,
        HueShutdownAction action,
        ColorConfiguration? look
    )
    {
        // Left as they are for the pause (still followed, as far as this goes)
        var commands = (
            action switch
            {
                HueShutdownAction.Restore => Restore(lights, now),
                HueShutdownAction.TurnOff => TurnOff(lights, now, false),
                HueShutdownAction.Custom when look is { } shutdownLook => Restore(
                    lights,
                    now,
                    null,
                    true,
                    shutdownLook
                ),
                _ => TurnOff(lights, now, true),
            }
        ).ToList();

        // Set to a look of the user's choosing for the pause (or turned off, which stay off)
        foreach (var light in lights)
        {
            if (
                !light.IsOn
                || !_pausedLights.TryGetValue(light.Id, out var paused)
                || paused.IsTurnedOff
            )
            {
                continue;
            }

            switch (action)
            {
                case HueShutdownAction.Restore:
                    _pausedLights.Remove(light.Id);
                    commands.Add(
                        paused.IsTurnedOn
                            ? new HueCommand(
                                light.Id,
                                null,
                                null,
                                null,
                                null,
                                RestoreTransitionMilliseconds,
                                false
                            )
                            : new HueCommand(
                                light.Id,
                                paused.Mirek,
                                light.SupportsBrightness ? paused.Brightness : null,
                                paused.Mirek is null ? paused.X : null,
                                paused.Mirek is null ? paused.Y : null,
                                RestoreTransitionMilliseconds
                            )
                    );
                    break;

                case HueShutdownAction.TurnOff:
                    _pausedLights.Remove(light.Id);
                    commands.Add(
                        new HueCommand(
                            light.Id,
                            null,
                            null,
                            null,
                            null,
                            RestoreTransitionMilliseconds,
                            false
                        )
                    );
                    break;

                // (still remembered how it was before, for after waking up)
                case HueShutdownAction.Custom when look is { } shutdownLook:
                    commands.Add(
                        GetFixedCommand(light, shutdownLook, RestoreTransitionMilliseconds)
                    );
                    break;

                // Left as it is, other than a light Afterglow turned on, which is turned off
                default:
                    if (paused.IsTurnedOn)
                    {
                        _pausedLights.Remove(light.Id);
                        commands.Add(
                            new HueCommand(
                                light.Id,
                                null,
                                null,
                                null,
                                null,
                                RestoreTransitionMilliseconds,
                                false
                            )
                        );
                    }

                    break;
            }
        }

        return commands;
    }

    // Whether anything is still Afterglow's to look after while lights are paused
    public bool HasPausedLights => _pausedLights.Count > 0 || _states.Count > 0;

    /// <summary>
    /// Forgets how lights paused to a custom look were before (e.g. once Hue control is turned
    /// off, as they're then left as they are).
    /// </summary>
    public void ForgetPausedLights() => _pausedLights.Clear();

    /// <summary>
    /// Forgets every light without putting it back.
    /// </summary>
    public void Reset()
    {
        _states.Clear();
        _turnedOnLightIds.Clear();
        _pausedLights.Clear();
    }

    // Levels sent to a display are gamma-encoded, so the light it gives off goes with the level
    // raised to this power
    private const double ScreenGamma = 2.2;

    /// <summary>
    /// Gets the brightness (1 to 100) that a light is set to, for the given screen brightness (0
    /// to 1) and color temperature, and dimming strength.
    /// </summary>
    /// <remarks>
    /// A screen gives off much less light than its brightness level suggests: the level is
    /// gamma-encoded (half brightness is about a fifth of the light), and a warm tint cuts the
    /// green and blue channels. At 1, a light follows the screen's brightness level as it is; at
    /// the default (2.2) it gives off as much light (relative to its full brightness) as the
    /// screen does (relative to its untinted white); in between, and beyond, it goes by the same
    /// curve. Full brightness of an untinted screen always stays full.
    /// </remarks>
    public static double ToLightBrightness(
        double screenBrightness,
        double dimming,
        double screenKelvin = 6600
    )
    {
        var level = Math.Pow(Math.Clamp(screenBrightness, 0, 1), dimming);

        // How much of the light that the tint holds back is held back from the light too
        var tintShare = Math.Max(dimming - 1, 0) / (ScreenGamma - 1);
        var tint = Math.Pow(WhitePoint.GetLuminance(screenKelvin), tintShare);

        return Math.Clamp(Math.Round(level * tint * 100, 1), 1, 100);
    }

    public static int ToMirek(double kelvin) => (int)Math.Round(1_000_000 / Math.Max(kelvin, 1));

    // A tint on a screen looks much warmer than a light of the same color temperature, as the
    // eyes judge the screen against its usual white, but adjust to the light that fills the
    // room. So the screen's own white (no tint) is matched to a neutral white light, and warmer
    // tints are matched to lights that are warmer still (by the given factor, in mireds, which
    // is how the eyes see color temperature change). Cooler tints go up to a daylight white.
    private const double NeutralScreenMired = 1_000_000 / 6500.0;
    private const double CoolestScreenMired = 1_000_000 / 20_000.0;
    private const double NeutralLightMired = 1_000_000 / 4000.0;
    private const double CoolestLightMired = 1_000_000 / 6500.0;

    // Warmer than this, white is a deep orange (and still within what color lights can show)
    private const double WarmestLightKelvin = 1000;

    /// <summary>
    /// Gets the color temperature (in mireds) that a light is set to, so that it looks about as
    /// warm as a screen tinted to the given color temperature.
    /// </summary>
    public static double ToLightMired(double screenKelvin, double warmth)
    {
        var screenMired = 1_000_000 / Math.Max(screenKelvin, 1);

        var mired =
            screenMired >= NeutralScreenMired
                ? NeutralLightMired + (screenMired - NeutralScreenMired) * warmth
                : NeutralLightMired
                    - (NeutralLightMired - CoolestLightMired)
                        * Math.Min(
                            (NeutralScreenMired - screenMired)
                                / (NeutralScreenMired - CoolestScreenMired),
                            1
                        );

        return Math.Min(mired, 1_000_000 / WarmestLightKelvin);
    }

    /// <summary>
    /// Gets what a light is set to, so that it looks about as warm as a screen tinted to the
    /// given color temperature: a white, while the light has one that's warm enough, and
    /// otherwise a color (if it can show colors), or else its warmest white.
    /// </summary>
    public static (int? Mirek, double? X, double? Y) ToLightColor(
        double screenKelvin,
        double warmth,
        HueLight light
    )
    {
        var mired = ToLightMired(screenKelvin, warmth);

        if (light is { MirekMinimum: { } minimum, MirekMaximum: { } maximum })
        {
            if (mired <= maximum + 0.5 || !light.SupportsColor)
                return (Math.Clamp((int)Math.Round(mired), minimum, maximum), null, null);
        }
        else if (!light.SupportsColor)
        {
            return (null, null, null);
        }

        var (x, y) = WhitePoint.GetChromaticity(1_000_000 / mired);
        return (null, Math.Round(x, 4), Math.Round(y, 4));
    }
}

/// <summary>
/// How a light (that's on, in the chosen rooms) is doing.
/// </summary>
public enum HueLightState
{
    // Not (yet) followed, e.g. it can't be adjusted, or it was only just turned on
    NotFollowed,

    Following,

    // Doesn't take what it's set to (e.g. it's out of the bridge's reach), so it's tried again
    NotResponding,

    // Changed by something else, so it's left alone until it's turned off and on
    LeftAlone,
}

/// <summary>
/// What happens to the lights Afterglow follows when the PC shuts down (or restarts, or signs
/// out) or goes to sleep.
/// </summary>
public enum HueShutdownAction
{
    // Left as they are (other than lights Afterglow turned on, which are turned off again)
    Leave,

    // Put back as they were before Afterglow followed them
    Restore,

    // Turned off
    TurnOff,

    // Set to a color temperature and brightness of the user's choosing
    Custom,
}

/// <summary>
/// What happens to the lights Afterglow follows while they're paused (from the tray, while
/// Afterglow itself is paused, or along with the screen).
/// </summary>
public enum HuePauseAction
{
    // Left as they are (and followed again afterwards)
    Leave,

    // Put back as they were before Afterglow followed them
    Restore,

    // Turned off (and turned back on afterwards)
    TurnOff,

    // Set to a color temperature and brightness of the user's choosing
    Custom,
}
