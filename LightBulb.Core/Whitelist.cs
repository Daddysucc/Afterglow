using System;
using System.Collections.Generic;
using System.Linq;

namespace LightBulb.Core;

public static class WindowTitleKeywords
{
    /// <summary>
    /// Trims the keywords, and drops empty ones and duplicates (ignoring case).
    /// </summary>
    public static IReadOnlyList<string> Normalize(IEnumerable<string?>? keywords) =>
        keywords is null
            ? []
            : keywords
                .Select(k => k?.Trim())
                .OfType<string>()
                .Where(k => k.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

    /// <summary>
    /// Whether the title contains any of the keywords, ignoring case (e.g. "net" matches both
    /// "Netflix - Google Chrome" and "Network Connections"). Blank keywords never match, so that
    /// an empty entry can't match every window.
    /// </summary>
    public static bool IsMatch(string? title, IReadOnlyList<string> keywords)
    {
        if (string.IsNullOrEmpty(title))
            return false;

        foreach (var keyword in keywords)
        {
            var trimmed = keyword.AsSpan().Trim();
            if (
                !trimmed.IsEmpty
                && title.AsSpan().Contains(trimmed, StringComparison.OrdinalIgnoreCase)
            )
                return true;
        }

        return false;
    }
}

/// <summary>
/// What makes a window whitelisted: belonging to one of the whitelisted applications, or having
/// a title that contains one of the whitelisted keywords. Each can be turned off on its own,
/// without losing its list.
/// </summary>
public sealed class WhitelistRules
{
    private readonly HashSet<string> _applicationFilePaths;
    private readonly IReadOnlyList<string> _titleKeywords;

    private WhitelistRules(
        HashSet<string> applicationFilePaths,
        IReadOnlyList<string> titleKeywords
    )
    {
        _applicationFilePaths = applicationFilePaths;
        _titleKeywords = titleKeywords;
    }

    public static WhitelistRules None { get; } =
        new(new HashSet<string>(StringComparer.OrdinalIgnoreCase), []);

    public static WhitelistRules Create(
        bool isApplicationMatchingEnabled,
        IEnumerable<string>? applicationFilePaths,
        bool isTitleMatchingEnabled,
        IEnumerable<string?>? titleKeywords
    ) =>
        new(
            isApplicationMatchingEnabled && applicationFilePaths is not null
                ? new HashSet<string>(
                    applicationFilePaths.Where(p => !string.IsNullOrWhiteSpace(p)),
                    StringComparer.OrdinalIgnoreCase
                )
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            isTitleMatchingEnabled ? WindowTitleKeywords.Normalize(titleKeywords) : []
        );

    // Whether windows' processes need to be looked up at all
    public bool IsApplicationChecked => _applicationFilePaths.Count > 0;

    // Whether windows' titles need to be read at all
    public bool IsTitleChecked => _titleKeywords.Count > 0;

    // Whether there's nothing to look for, in which case windows don't need to be looked at
    public bool IsEmpty => !IsApplicationChecked && !IsTitleChecked;

    /// <summary>
    /// Whether a window matches. Its title and executable are only looked up if needed.
    /// </summary>
    public bool IsMatch(Func<string?> getTitle, Func<string?> getExecutableFilePath) =>
        (IsTitleChecked && WindowTitleKeywords.IsMatch(getTitle(), _titleKeywords))
        || (
            IsApplicationChecked
            && getExecutableFilePath() is { } filePath
            && _applicationFilePaths.Contains(filePath)
        );

    /// <summary>
    /// Checks the window, and if it matches, finds the display that shows it.
    /// </summary>
    public WhitelistMatch Match(IWhitelistWindow window)
    {
        if (IsEmpty || !IsMatch(window.TryGetTitle, window.TryGetExecutableFilePath))
            return WhitelistMatch.None;

        return new WhitelistMatch(true, window.TryGetMonitor());
    }
}

/// <summary>
/// A window to check against the whitelist. Its details are only looked up when asked for,
/// as some are costly (e.g. the executable requires opening the window's process).
/// </summary>
public interface IWhitelistWindow
{
    string? TryGetTitle();

    string? TryGetExecutableFilePath();

    // Identifier of the display that shows the window (or the nearest one)
    string? TryGetMonitor();
}

/// <summary>
/// Whether the window in focus is whitelisted, and the display that shows it.
/// </summary>
public readonly record struct WhitelistMatch(bool IsMatched, string? Monitor)
{
    public static WhitelistMatch None { get; } = new(false, null);
}

/// <summary>
/// Follows the window in focus: only that window is checked, never the ones behind it, so moving
/// focus to a window that doesn't match clears the match right away. A window that can't be judged
/// (e.g. the taskbar, the desktop, or this app) keeps the previous result, so that clicking the
/// taskbar doesn't briefly lift the pause.
/// </summary>
public sealed class ForegroundWhitelist
{
    private WhitelistRules? _rules;

    public WhitelistMatch Current { get; private set; } = WhitelistMatch.None;

    /// <param name="foregroundWindow">The window in focus, or <c>null</c> if it can't be judged.</param>
    public WhitelistMatch Update(WhitelistRules rules, IWhitelistWindow? foregroundWindow)
    {
        if (foregroundWindow is not null)
            Current = rules.Match(foregroundWindow);
        // The previous result was for other rules (e.g. a keyword was removed while this app was
        // in focus), so it can't be kept
        else if (!ReferenceEquals(rules, _rules))
            Current = WhitelistMatch.None;

        _rules = rules;
        return Current;
    }

    public WhitelistMatch Reset()
    {
        _rules = null;
        return Current = WhitelistMatch.None;
    }
}
