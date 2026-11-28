# Notes for working on Afterglow

## Releasing a version

Before tagging a release (`vX.Y.Z`), add a section for it to the top of `Changelog.md`:

- Headed `## X.Y.Z` (no "v"), newest first.
- One to three bullets, each a single short line (well under 70 characters), about what a user
  notices, not how it was done. Keep it brief: the app shows every section between the user's
  version and the new one in a small dialog, so longer notes pile up quickly.
- Only the changes a user would care about since the previous release.

The release workflow uses that section as the GitHub release notes (it fails if the section is
missing), and the app downloads `Changelog.md` at the new version's tag to show what's changed
when its update notification is clicked.
