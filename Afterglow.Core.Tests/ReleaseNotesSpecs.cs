using AwesomeAssertions;
using Xunit;

namespace Afterglow.Core.Tests;

public class ReleaseNotesSpecs
{
    [Fact]
    public void I_can_read_the_release_notes_as_plain_text()
    {
        // Arrange (as the release workflow writes them, from the commit messages, followed by
        // what GitHub adds)
        const string markdown = """
            ## What's changed

            ### Only change the accent color while the user is away

            Changing the Windows accent color makes every application re-theme at
            once, which stalls them all for up to a couple of seconds.

            - Uninstalling puts back the user's own wallpaper and accent color before
              deleting the files that say what they were.
            - Stepping through the day shows the **accent color** right away again.

            Co-Authored-By: Someone <someone@example.com>

            **Full Changelog**: https://github.com/owner/repo/compare/v1.0.0...v1.1.0
            """;

        // Act
        var text = ReleaseNotes.ToPlainText(markdown);

        // Assert
        text.Should()
            .Be(
                "What's changed\n\n"
                    + "Only change the accent color while the user is away\n\n"
                    + "Changing the Windows accent color makes every application re-theme at once, "
                    + "which stalls them all for up to a couple of seconds.\n\n"
                    + "• Uninstalling puts back the user's own wallpaper and accent color before "
                    + "deleting the files that say what they were.\n"
                    + "• Stepping through the day shows the accent color right away again."
            );
    }

    [Fact]
    public void I_can_read_links_in_the_release_notes_as_their_text()
    {
        // Act
        var text = ReleaseNotes.ToPlainText(
            "See the [code signing policy](https://example.com/policy) for details."
        );

        // Assert
        text.Should().Be("See the code signing policy for details.");
    }

    private const string Changelog = """
        # Changelog

        What's new in each version, newest first.

        ## 3.7.0

        - Settings are easier to find.
        - Numbers next to sliders can be typed in.

        ## 3.6.5

        - Turning the dynamic wallpaper off puts a solid color back properly.

        ## 3.6.4

        - The accent color only changes while you're away.
        """;

    [Fact]
    public void I_can_see_what_changed_since_my_version_when_I_skipped_some()
    {
        // Act
        var changes = ReleaseNotes.GetChangesBetween(
            Changelog,
            new System.Version(3, 6, 3),
            new System.Version(3, 7, 0),
            "New in {0}"
        );

        // Assert (newest first, and nothing from before the first version)
        changes
            .Should()
            .Be(
                "New in 3.7.0\n"
                    + "• Settings are easier to find.\n"
                    + "• Numbers next to sliders can be typed in.\n\n"
                    + "New in 3.6.5\n"
                    + "• Turning the dynamic wallpaper off puts a solid color back properly.\n\n"
                    + "New in 3.6.4\n"
                    + "• The accent color only changes while you're away."
            );
    }

    [Fact]
    public void I_can_see_only_what_changed_since_my_version()
    {
        // Act
        var changes = ReleaseNotes.GetChangesBetween(
            Changelog,
            new System.Version(3, 6, 5),
            new System.Version(3, 7, 0),
            "New in {0}"
        );

        // Assert
        changes
            .Should()
            .Be(
                "New in 3.7.0\n"
                    + "• Settings are easier to find.\n"
                    + "• Numbers next to sliders can be typed in."
            );
    }

    [Fact]
    public void I_get_nothing_from_the_changelog_if_it_does_not_mention_the_new_version()
    {
        // Act
        var changes = ReleaseNotes.GetChangesBetween(
            Changelog,
            new System.Version(3, 7, 0),
            new System.Version(3, 8, 0),
            "New in {0}"
        );

        // Assert
        changes.Should().BeNull();
    }

    [Fact]
    public void I_get_nothing_from_release_notes_with_only_github_links()
    {
        // Act
        var text = ReleaseNotes.ToPlainText(
            "**Full Changelog**: https://github.com/owner/repo/compare/v1.0.0...v1.1.0"
        );

        // Assert
        text.Should().BeEmpty();
    }
}
