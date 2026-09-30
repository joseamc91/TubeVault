<p align="center">
  <img src="docs/assets/Banner_README.png" alt="TubeVault" width="100%">
</p>

<p align="center">
  <a href="README.md">EspaÃ±ol</a> Â· <strong>English</strong>
</p>

<p align="center">
  <a href="https://github.com/joseamc91/TubeVault/actions/workflows/ci.yml">
    <img src="https://github.com/joseamc91/TubeVault/actions/workflows/ci.yml/badge.svg" alt="CI">
  </a>
  <a href="https://github.com/joseamc91/TubeVault/releases/latest">
    <img src="https://img.shields.io/github/v/release/joseamc91/TubeVault?label=stable" alt="Stable release">
  </a>
  <img src="https://img.shields.io/badge/Windows-10%20%7C%2011%20x64-0078D4" alt="Windows 10/11 x64">
  <a href="LICENSE">
    <img src="https://img.shields.io/github/license/joseamc91/TubeVault" alt="GPL-3.0">
  </a>
</p>

<p align="center">
  TubeVault makes it easy to download audio from YouTube and YouTube Music as MP3 files on Windows, in a portable app.
</p>

<p align="center">
  <a href="https://github.com/joseamc91/TubeVault/releases/latest/download/TubeVault-2026.09.009-win-x64-portable.zip">
    <img src="https://img.shields.io/badge/â¬‡%20DESCARGAR%20TUBEVAULT-3B82F6?style=for-the-badge" alt="Download TubeVault">
  </a>
</p>

<p align="center">
  <strong>Portable Â· Self-contained Â· No .NET installation required</strong>
</p>

<p align="center">
  <img src="docs/assets/Screenshot_Themes.png" alt="TubeVault in light and dark modes" width="900">
</p>

## What TubeVault does

- Downloads individual videos or playlists as MP3 files.
- Reads the available video or playlist information before downloading, such as title, channel, duration or date.
- Lets you select which songs to download from a playlist.
- Offers three MP3 quality profiles: High, Medium and Low.
- Includes Spanish and English interfaces, with Light and Dark modes.
- Automatically manages yt-dlp, FFmpeg and ffprobe.

## How to use it

1. Download the latest version of TubeVault.
2. Extract the ZIP into any folder.
3. Run `TubeVault.exe`.
4. Paste a YouTube or YouTube Music link and choose what you want to download.

On the first run, TubeVault automatically prepares the components it needs. You do not need to install yt-dlp, FFmpeg, ffprobe or .NET manually.

## Portable

TubeVault does not need to be installed.

The application and its data stay within its own folder. During use, a `data/` folder is created next to `TubeVault.exe` to store settings, tools and logs.

You can move TubeVault to another location by keeping the whole folder together.

## Compatibility

- Windows 11 x64.
- Windows 10 x64.
- Self-contained build for `win-x64`.

Windows 11 is the main platform for development and testing.

## Security

TubeVault does not yet have a digital code signature.

The stable version has been successfully tested on a Windows 11 computer with Smart App Control enabled. This does not guarantee the same behavior on every computer or Windows configuration.

You do not need to disable Smart App Control or SmartScreen, or add exclusions, to use TubeVault.

## Documentation

For more detailed information:

- [Privacy](PRIVACY.md)
- [GPL-3.0 license](LICENSE)
- [Third-party components](THIRD-PARTY-NOTICES.md)
- [Security](SECURITY.md)
- [Contributing](CONTRIBUTING.md)
- [Architecture](ARCHITECTURE.md)
- [Product documentation](docs/PRODUCT.md)
- [Release process](docs/RELEASE_PROCESS.md)
- [Code signing policy](docs/CODE_SIGNING_POLICY.md)

