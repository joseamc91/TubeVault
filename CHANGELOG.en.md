# Changelog

Versions `2026.09.001`–`2026.09.006` were local development releases.
Since `2026.09.007`, TubeVault has been published through GitHub Releases.

## 2026.10.002 — Public Preview

- New option to embed cover art in MP3 files.
- Covers use a centered square 1:1 crop.
- Maximum size of 500×500 px, without enlarging smaller images.
- TubeVault's preview also displays the square cover.
- Each song in a playlist uses its own thumbnail.
- Cover art is enabled by default and can be disabled in Settings.
- Audio is not re-encoded when adding a cover.
- Cover-only failures do not invalidate a successful MP3 download.

## 2026.10.001 — Stable

- Periodic automatic checks for new Stable versions of TubeVault, without downloading or installing the application.
- New manual update check from Components and updates.
- Discreet indicator when a newer TubeVault version is available.
- Direct access to the available GitHub Release.
- New TubeVault card in Components and updates.
- Refined visual integration of component cards.

## 2026.09.009 — Stable

- New TubeVault visual identity.
- New application icon.
- Wordmark integrated into the interface, adapted to light and dark modes.
- New public repository presentation with a bilingual README.
- First Stable version.

## 2026.09.008 — Public Preview

- TubeVault is now distributed exclusively as a portable application.
- New self-contained Single File executable.
- Data is now stored under `data/`.
- Removed MSI, WiX, installed mode, Portable Classic and `portable.flag`.
- Distribution simplified to a single ZIP.

## 2026.09.007 — Public Preview

- First public TubeVault release on GitHub.
- Added a per-user x64 MSI installer.
- Added installed mode with data stored in `%LocalAppData%`.
- Added MSI builds through GitHub Actions.
- Testing with Smart App Control demonstrated the limitations of an unsigned build.

## 2026.09.006

- Real progress during component preparation, with percentage and MB when available.
- Component downloads via streaming.
- Simplified Settings and a new Components and updates window.
- New visual state when a download completes.
- Quick actions to open the folder or start a new download.
- Discreet indicator when updates are available.

## 2026.09.005

- yt-dlp, FFmpeg and ffprobe are no longer included in the distribution.
- Automatic component preparation on the first run.
- Verification using checksums and version checks.
- Selective repair of damaged or missing components.
- Safe updates for yt-dlp and FFmpeg/ffprobe.
- Atomic replacement with recovery of the previous version if an error occurs.

## 2026.09.004

- Redesigned the main window with a stable content card.
- New fixed area for artwork and a placeholder.
- Improved the distinct presentation of songs and playlists.
- More robust thumbnail loading, with alternatives when an image fails.
- More compact interface for metadata and lists.

## 2026.09.003

- Individual song selection within playlists.
- Actions to select or deselect the entire playlist.
- Selected item counter.
- Progress and summary calculated only for the selection.
- Preservation of the original playlist indices.
- Initial support for thumbnail previews.

## 2026.09.002

- Added High, Medium and Low MP3 quality profiles.
- Full interface in Spanish and English.
- Added Light and Dark themes.
- New About TubeVault window.
- yt-dlp update checks.
- Settings saved between sessions.
- Improved tooltips, statuses and user messages.

## 2026.09.001

- First functional version of TubeVault.
- Portable WinForms application for Windows.
- Reading of video and playlist information before downloading.
- MP3 downloads from individual videos and playlists.
- Native metadata through yt-dlp and FFmpeg.
- Detection of existing files.
- Retries and final validation through ffprobe.
- Safe cancellation, temporary file cleanup and daily logs.
