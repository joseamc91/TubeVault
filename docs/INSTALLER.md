# Instalador MSI

TubeVault dispone de una primera infraestructura MSI de prueba basada en
WiX Toolset `5.0.2`. El instalador todavía no constituye una release pública.

## Modelo de instalación

- Instalación x64 para el usuario actual, sin selector per-machine.
- Programa en `%LocalAppData%\Programs\TubeVault`.
- Datos de ejecución en `%LocalAppData%\TubeVault`.
- Acceso directo **TubeVault** en el menú Inicio.
- Sin `portable.flag`, yt-dlp, FFmpeg ni ffprobe dentro del MSI.
- La desinstalación retira el programa y el acceso directo, pero conserva por
  ahora `%LocalAppData%\TubeVault` y nunca toca las carpetas MP3 del usuario.

La versión pública `2026.09.007` se convierte automáticamente en ProductVersion
MSI `26.9.7`: año menos 2000, mes y revisión. El script valida los límites de
Windows Installer y no duplica manualmente la versión del proyecto.

## Construcción

Desde la raíz del repositorio:

```powershell
.\scripts\build-installer.ps1
```

El script publica la aplicación self-contained `win-x64` en un staging bajo
`artifacts/`, valida el payload y genera el MSI en `artifacts/installer/`.
`portable.flag` no forma parte del publish normal ni del instalador.

El workflow `TubeVault CI` ejecuta este script y sube el MSI como un artifact
independiente del publish de la aplicación. Los artifacts de GitHub Actions son
temporales: no constituyen una release pública ni firmada.

Siguen pendientes la UI del instalador, GitHub Releases, la distribución pública
y la firma de código.
