# Componentes de terceros

TubeVault está licenciado bajo **GNU General Public License v3.0 only**
(`GPL-3.0-only`). La licencia de TubeVault no sustituye ni modifica las licencias
de los componentes de terceros indicados en este documento.

## yt-dlp

- **Proyecto:** yt-dlp.
- **Repositorio y código fuente:** <https://github.com/yt-dlp/yt-dlp>
- **Licencia del proyecto:** Unlicense.
- **Licencia del ejecutable oficial para Windows:** el proyecto indica que sus
  ejecutables empaquetados con PyInstaller contienen componentes GPLv3+ y que el
  trabajo combinado se distribuye bajo GPLv3+.
- **Licencia y avisos oficiales:**
  - <https://github.com/yt-dlp/yt-dlp/blob/master/LICENSE>
  - <https://github.com/yt-dlp/yt-dlp#licensing>
  - <https://github.com/yt-dlp/yt-dlp/blob/master/THIRD_PARTY_LICENSES.txt>

TubeVault **no incluye ni redistribuye `yt-dlp.exe`**. Durante la preparación
inicial descarga el ejecutable desde las publicaciones oficiales de yt-dlp y
comprueba su integridad mediante la información oficial de checksum utilizada
por la aplicación. TubeVault no firma ni modifica ese ejecutable.

## FFmpeg / ffprobe

- **Proyecto:** FFmpeg, incluidos los ejecutables `ffmpeg` y `ffprobe`.
- **Sitio y código fuente oficiales:**
  - <https://ffmpeg.org/>
  - <https://github.com/FFmpeg/FFmpeg>
- **Proveedor actual del build para Windows:** Gyan.dev, paquete release
  essentials de 64 bits.
- **Página del proveedor:** <https://www.gyan.dev/ffmpeg/builds/>
- **Licencia aplicable al build utilizado:** GNU General Public License version
  3 (GPLv3), según declara Gyan.dev para sus builds estáticos. El build está
  configurado con `--enable-gpl` y `--enable-version3`; la documentación oficial
  de FFmpeg indica que esta combinación activa los términos GPLv3 aplicables.
- **Licencia y documentación oficiales:**
  - <https://github.com/FFmpeg/FFmpeg/blob/master/LICENSE.md>
  - <https://github.com/FFmpeg/FFmpeg/blob/master/COPYING.GPLv3>
  - <https://ffmpeg.org/legal.html>

TubeVault **no incluye ni redistribuye `ffmpeg.exe` ni `ffprobe.exe`**. Durante
la preparación inicial descarga el paquete configurado desde Gyan.dev y verifica
su checksum publicado antes de instalar ambos ejecutables localmente. TubeVault
no firma ni modifica esos ejecutables.

## .NET Runtime / Windows Forms

Las publicaciones self-contained de TubeVault sí incluyen componentes
redistribuibles de Microsoft .NET, entre ellos el runtime, las bibliotecas de
Windows Desktop y Windows Forms necesarias para ejecutar la aplicación sin una
instalación previa de .NET.

La documentación oficial de .NET distingue entre:

- el código fuente de .NET Runtime y Windows Forms, publicado principalmente
  bajo la licencia MIT;
- las distribuciones de producto y runtime packs para Windows, sujetas a los
  **Microsoft .NET Library License Terms**;
- componentes adicionales cubiertos por los avisos de terceros de cada proyecto.

Fuentes y términos oficiales:

- **Información general de licencias de .NET:**
  <https://github.com/dotnet/core/blob/main/license-information.md>
- **Microsoft .NET Library License Terms:**
  <https://dotnet.microsoft.com/en-us/dotnet_library_license.htm>
- **.NET Runtime — código fuente y licencia MIT:**
  - <https://github.com/dotnet/runtime>
  - <https://github.com/dotnet/runtime/blob/main/LICENSE.TXT>
- **Avisos de terceros de .NET Runtime:**
  <https://github.com/dotnet/runtime/blob/main/THIRD-PARTY-NOTICES.TXT>
- **Windows Forms — código fuente y licencia MIT:**
  - <https://github.com/dotnet/winforms>
  - <https://github.com/dotnet/winforms/blob/main/LICENSE.TXT>
- **Avisos de terceros de Windows Forms:**
  <https://github.com/dotnet/winforms/blob/main/THIRD-PARTY-NOTICES.TXT>

No se intenta enumerar aquí cada DLL incluida en una publicación self-contained.
Los términos y avisos enlazados son los mantenidos por Microsoft y los proyectos
.NET correspondientes.
