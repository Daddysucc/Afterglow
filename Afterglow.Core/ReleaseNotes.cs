using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Afterglow.Core;

/// <summary>
/// Release notes (as written for a GitHub release, in Markdown), made readable as plain text.
/// </summary>
public static partial class ReleaseNotes
{
    /// <summary>
    /// Turns the notes into plain text: headings and paragraphs on their own (with lines that
    /// were only wrapped joined back up), list items as bullets, and links as their text. Lines
    /// that only make sense on GitHub (e.g. the link to the full list of commits) are left out.
    /// </summary>
    public static string ToPlainText(string markdown)
    {
        var blocks = new List<string>();
        var headings = new HashSet<int>();
        var current = new StringBuilder();

        void EndBlock()
        {
            if (current.Length > 0)
                blocks.Add(current.ToString());

            current.Clear();
        }

        foreach (var rawLine in markdown.Replace("\r\n", "\n").Split('\n'))
        {
            var line = rawLine.TrimEnd();
            var trimmed = line.TrimStart();

            // Only meant for GitHub
            if (
                trimmed.StartsWith("**Full Changelog**", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("Co-Authored-By:", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith(
                    "What changed in earlier versions",
                    StringComparison.OrdinalIgnoreCase
                )
                || trimmed.StartsWith("<!--", StringComparison.Ordinal)
            )
            {
                EndBlock();
                continue;
            }

            if (trimmed.Length == 0)
            {
                EndBlock();
                continue;
            }

            // A heading stands on its own
            if (HeadingRegex().Match(trimmed) is { Success: true } heading)
            {
                EndBlock();
                blocks.Add(Inline(heading.Groups[1].Value));
                headings.Add(blocks.Count - 1);
                continue;
            }

            // A list item starts a block of its own
            if (ListItemRegex().Match(trimmed) is { Success: true } item)
            {
                EndBlock();
                var indent = line.Length - trimmed.Length >= 2 ? "    " : "";
                current.Append(indent + "• " + Inline(item.Groups[1].Value));
                continue;
            }

            // Anything else continues what came before it (lines that were only wrapped)
            if (current.Length > 0)
                current.Append(' ');

            current.Append(Inline(trimmed));
        }

        EndBlock();

        // List items follow one another (and their heading) directly, everything else is
        // separated by a blank line
        var text = new StringBuilder();
        for (var i = 0; i < blocks.Count; i++)
        {
            if (i > 0)
            {
                var isListItem = blocks[i].TrimStart().StartsWith('•');
                var isAfterListItemOrHeading =
                    blocks[i - 1].TrimStart().StartsWith('•') || headings.Contains(i - 1);

                text.Append(isListItem && isAfterListItemOrHeading ? "\n" : "\n\n");
            }

            text.Append(blocks[i]);
        }

        return text.ToString().Trim();
    }

    /// <summary>
    /// Gets what's changed between two versions from the changelog (a Markdown file with a
    /// section per version, headed like "## 3.7.0", newest first): every version after the
    /// installed one, up to and including the available one, as plain text, each under a heading
    /// in the given format (e.g. "New in {0}"). Returns null if none of them is in it.
    /// </summary>
    public static string? GetChangesBetween(
        string changelog,
        Version installed,
        Version available,
        string headingFormat
    )
    {
        var sections = new List<(Version Version, string Text)>();
        Version? version = null;
        var text = new StringBuilder();

        void EndSection()
        {
            if (version is not null && text.ToString().Trim() is { Length: > 0 } content)
                sections.Add((version, content));

            text.Clear();
        }

        foreach (var line in changelog.Replace("\r\n", "\n").Split('\n'))
        {
            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                EndSection();
                version = Version.TryParse(line[3..].Trim().TrimStart('v', 'V'), out var parsed)
                    ? parsed
                    : null;
                continue;
            }

            // (anything before the first version, e.g. what the file is about, is left out)
            if (version is not null)
                text.Append(line).Append('\n');
        }

        EndSection();

        var changes = new StringBuilder();
        foreach (var (sectionVersion, content) in sections)
        {
            if (Normalize(sectionVersion) <= Normalize(installed))
                continue;

            if (Normalize(sectionVersion) > Normalize(available))
                continue;

            changes
                .Append("### ")
                .Append(string.Format(headingFormat, sectionVersion.ToString(3)))
                .Append("\n\n")
                .Append(content)
                .Append("\n\n");
        }

        return changes.Length > 0 ? ToPlainText(changes.ToString()) : null;
    }

    // (versions are compared by their first three parts, as releases are numbered)
    private static Version Normalize(Version version) =>
        new(version.Major, version.Minor, Math.Max(version.Build, 0));

    // Links become their text, and emphasis and code marks are dropped
    private static string Inline(string text)
    {
        text = LinkRegex().Replace(text, "$1");
        text = text.Replace("**", "").Replace("__", "").Replace("`", "");
        return text.Trim();
    }

    [GeneratedRegex(@"^#{1,6}\s+(.*)$")]
    private static partial Regex HeadingRegex();

    [GeneratedRegex(@"^[-*+]\s+(.*)$")]
    private static partial Regex ListItemRegex();

    [GeneratedRegex(@"\[([^\]]*)\]\([^)]*\)")]
    private static partial Regex LinkRegex();
}
