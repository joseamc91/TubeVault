# Proceso de release

## Versionado y desarrollo

TubeVault utiliza `AAAA.MM.REVISION`, con una revisión de tres dígitos que vuelve
a `001` al cambiar el mes. La fuente de versión es `TubeVault.csproj`;
`InformationalVersion` determina el nombre público del ZIP.

Durante el desarrollo normal se compila Release y se prueba desde
`src/TubeVault/bin/Release/net10.0-windows`. Solo se ejecutan pruebas
proporcionales al cambio. El empaquetado se realiza cuando se solicita
expresamente o mediante CI; no crea por sí mismo una GitHub Release.

Las releases locales `2026.09.001`–`2026.09.006` y la release/tag pública
`2026.09.007` y `2026.09.008` son históricas e inmutables. Nunca se sobrescriben ni se limpian
para preparar una versión nueva. `dist/` no se utiliza para el empaquetado actual.

## Única distribución: TubeVault Portable

Desde la Public Preview publicada `2026.09.008` existe un único ZIP `win-x64` self-contained,
construido mediante .NET Single File. El usuario no necesita instalar .NET.
Aplicación y runtime se agrupan en `TubeVault.exe`; .NET puede utilizar sus
propios mecanismos internos de extracción.

El ZIP inicial contiene exactamente estos archivos en la raíz:

```text
TubeVault.exe
LICENSE
PRIVACY.md
THIRD-PARTY-NOTICES.md
```

No contiene PDB, herramientas descargadas, configuración, logs, temporales ni
`data/`. TubeVault obtiene y valida yt-dlp, FFmpeg y ffprobe durante la
preparación inicial. Al necesitar sus datos crea:

```text
data/config/settings.json
data/logs/TubeVault_YYYY-MM-DD.log
data/tools/yt-dlp.exe
data/tools/ffmpeg.exe
data/tools/ffprobe.exe
```

El destino de los MP3 lo elige el usuario. Para mover TubeVault conservando
configuración, herramientas y logs se mueve la carpeta completa, incluido
`data/`.

## Build y artifact

Desde la raíz, con el SDK fijado por `global.json`:

```powershell
dotnet restore .\TubeVault.sln
dotnet build .\TubeVault.sln -c Release --no-restore
.\scripts\build-portable.ps1
```

El builder publica el payload, elimina PDB, copia los documentos legales,
valida los cuatro archivos públicos y vuelve a inspeccionar el ZIP.
Muestra tamaño y SHA-256. La salida es:

`artifacts/portable/TubeVault-AAAA.MM.REVISION-win-x64-portable.zip`.

El workflow `TubeVault CI` conserva un build Release independiente, ejecuta el
builder, comprueba que existe exactamente un ZIP esperado no vacío y sube
únicamente ese ZIP como artifact
`TubeVault-AAAA.MM.REVISION-win-x64-portable`.
Los artifacts son temporales y no equivalen a releases públicas.
La CI portable de `2026.09.008` ya fue validada en GitHub.

## Validación y publicación

Solo la aprobación explícita del usuario autoriza crear tag y GitHub Release.

1. Confirmar versión, notas y commit objetivo.
2. Comprobar build y empaquetado en CI para ese código.
3. Inspeccionar ZIP, nombre, contenido y SHA-256.
4. Probar una extracción separada, conservando limpio el ZIP original.
5. Validar arranque, preparación, análisis y los flujos afectados por el cambio.
6. Confirmar datos bajo `data/` y destino MP3 elegido por el usuario.
7. Registrar pruebas, limitaciones y estado de firma.
8. Tras la autorización, crear el tag de versión y la GitHub Release sobre el
   código validado, adjuntando únicamente el ZIP Portable.
9. Mantener tag, release y asset publicado inmutables.

`2026.09.008` fue publicada como **Public Preview / Pre-release** y permanece
histórica e inmutable. `2026.09.009` se prepara como la primera **Stable / Latest**,
sin cambios funcionales respecto a 008. Sus notas están en
[2026.09.008](releases/2026.09.008.md) y [2026.09.009](releases/2026.09.009.md).

La estrategia mantiene una Stable como referencia pública. Las versiones de
desarrollo posteriores pueden publicarse como Pre-release; cuando una versión
esté suficientemente validada, sustituirá a la anterior como nueva Stable / Latest.
La publicación de 009 será la primera aplicación de esta estrategia. Cambiar
Latest no modifica los tags, releases ni assets ya publicados.
No presentar la build como firmada: consulte la
[Code signing policy](CODE_SIGNING_POLICY.md).

## Pruebas proporcionales

- Documentación: revisar contenido y enlaces sin recompilar.
- Textos y UI: comprobar ES/EN y temas claro/oscuro.
- Configuración y componentes: comprobar caso válido y recuperación.
- Descarga y playlists: validar los casos afectados, archivos existentes y ffprobe.
- Cancelación: comprobar cierre de procesos y limpieza privada.

La validación física de TubeVault Portable `2026.09.008` completó preparación,
análisis de YouTube Music, descarga MP3 y ffprobe en un equipo Windows 11 con
Smart App Control activo. Ese resultado no garantiza otros equipos; tampoco
valida por sí mismo cambios posteriores de empaquetado o CI.
