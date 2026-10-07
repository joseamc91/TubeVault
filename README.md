<p align="center">
  <img src="docs/assets/Banner_README.png" alt="TubeVault" width="100%">
</p>

<p align="center">
  <strong>Español</strong> · <a href="README.en.md">English</a>
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
  TubeVault permite descargar audio de YouTube y YouTube Music en MP3 desde Windows, de forma sencilla y portable.
</p>

<p align="center">
  <a href="https://github.com/joseamc91/TubeVault/releases/latest/download/TubeVault-2026.10.001-win-x64-portable.zip">
    <img src="https://img.shields.io/badge/⬇%20DESCARGAR%20TUBEVAULT-3B82F6?style=for-the-badge" alt="Descargar TubeVault">
  </a>
</p>

<p align="center">
  <strong>Portable · Self-contained · No requiere instalar .NET</strong>
</p>

<p align="center">
  <img src="docs/assets/Screenshot_Themes.png" alt="TubeVault en modo claro y oscuro" width="900">
</p>

## Qué hace TubeVault

- Descarga vídeos individuales o playlists como archivos MP3.
- Lee antes de descargar la información disponible del vídeo o playlist, como título, canal, duración o fecha.
- Permite seleccionar qué canciones descargar dentro de una playlist.
- Ofrece tres perfiles de calidad MP3: Alta, Media y Baja.
- Incluye interfaz en Español e English y modos Claro y Oscuro.
- Gestiona automáticamente yt-dlp, FFmpeg y ffprobe.
- Comprueba si existe una nueva versión Stable de TubeVault y permite abrir su release, sin descargar ni instalar la aplicación automáticamente.

## Cómo usarlo

1. Descarga la última versión de TubeVault.
2. Extrae el ZIP en cualquier carpeta.
3. Ejecuta `TubeVault.exe`.
4. Pega un enlace de YouTube o YouTube Music y elige qué quieres descargar.

En la primera ejecución, TubeVault prepara automáticamente los componentes que necesita. No es necesario instalar yt-dlp, FFmpeg, ffprobe ni .NET manualmente.

## Portable

TubeVault no necesita instalación.

La aplicación y sus datos permanecen dentro de su propia carpeta. Durante el uso se crea una carpeta `data/` junto a `TubeVault.exe` para guardar configuración, herramientas y logs.

Puedes mover TubeVault a otra ubicación conservando toda la carpeta.

## Compatibilidad

- Windows 11 x64.
- Windows 10 x64.
- Publicación self-contained para `win-x64`.

Windows 11 es la plataforma principal de desarrollo y pruebas.

## Seguridad

TubeVault todavía no dispone de firma digital de código.

La versión estable ha sido probada correctamente en un equipo con Windows 11 y Smart App Control activo. Esto no garantiza el mismo comportamiento en todos los equipos o configuraciones de Windows.

No es necesario desactivar Smart App Control, SmartScreen ni añadir exclusiones para utilizar TubeVault.

## Documentación

Para información más detallada:

- [Changelog](CHANGELOG.md)
- [Privacidad](PRIVACY.md)
- [Licencia GPL-3.0](LICENSE)
- [Componentes de terceros](THIRD-PARTY-NOTICES.md)
- [Seguridad](SECURITY.md)
- [Contribuciones](CONTRIBUTING.md)
- [Arquitectura](ARCHITECTURE.md)
- [Documentación del producto](docs/PRODUCT.md)
- [Proceso de releases](docs/RELEASE_PROCESS.md)
- [Política de firma de código](docs/CODE_SIGNING_POLICY.md)
