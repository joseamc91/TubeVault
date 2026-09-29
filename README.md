# TubeVault

TubeVault es una aplicación portable para Windows que permite a usuarios no técnicos analizar enlaces de YouTube o YouTube Music y descargar su audio en MP3.

## Tecnología y versión

- C# con WinForms.
- .NET 10 para Windows (`net10.0-windows`).
- Versión actual publicada: `2026.09.006`.
- Publicación `win-x64` self-contained: no requiere instalar .NET.
- Sin paquetes NuGet adicionales.
- Licencia: `GPL-3.0-only`.
- Componentes de terceros: TubeVault utiliza yt-dlp, FFmpeg y el runtime .NET. Consulte [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) para información sobre licencias y código fuente.

## Estado actual

TubeVault incluye análisis real mediante yt-dlp, vídeos individuales y playlists, descarga MP3, metadata nativa, progreso global, tres intentos totales por canción, detección de archivos existentes, validación con ffprobe, limpieza privada de temporales y cancelación segura del árbol de procesos.

La versión `2026.09.006` está cerrada y publicada como release portable inmutable.
Mejora la preparación con progreso HTTP real, separa la gestión de componentes,
añade un estado visual de éxito y señala actualizaciones pendientes discretamente.

La versión `2026.09.005` está cerrada y publicada como release portable inmutable. Añade preparación, validación,
reparación y actualización segura de los componentes locales. Puede arrancar con
`tools/` vacío o inexistente y guía al usuario mediante una ventana sencilla.

La versión `2026.09.004` está cerrada y publicada como release portable inmutable. Rediseña la ventana principal con una tarjeta central estable para canciones y playlists, una zona fija de carátula con placeholder y un bloque inferior común de acciones.

La versión `2026.09.003` está cerrada y publicada como release portable inmutable.

La versión 2026.09.003 mantiene las funciones de 2026.09.002 y añade:

- selección parcial de canciones en playlists mediante checkboxes;
- acciones para seleccionar o deseleccionar toda la lista;
- contador de elementos seleccionados;
- progreso y resumen calculados únicamente sobre la selección;
- conservación del índice original de cada elemento en el nombre del MP3.

La versión 2026.09.002 añadió:

- perfiles de calidad MP3 Alta / Media / Baja ajustados;
- interfaz completa en Español y English;
- tema Claro y tema Oscuro azul;
- About con versiones de componentes;
- tooltips en las acciones principales;
- comprobación no invasiva de actualizaciones de yt-dlp;
- textos de resultado más breves y claros;
- builds portables versionadas sin sobrescribir versiones anteriores.

El antiguo modo `demo:` fue eliminado completamente del código de producción. Todos los enlaces se analizan ahora mediante yt-dlp.

## Estructura

```text
TubeVault/
├── TubeVault.sln
├── src/
│   └── TubeVault/
│       ├── Program.cs
│       ├── MainForm.cs
│       ├── AboutForm.cs
│       ├── Controls/
│       ├── Models/
│       ├── Resources/
│       │   ├── UiText.resx
│       │   └── UiText.en.resx
│       └── Services/
├── config/                 Configuración local de desarrollo
├── tools/                  yt-dlp, FFmpeg y ffprobe
├── logs/                   Registros técnicos diarios
└── dist/
│   ├── TubeVault-2026.09.001.zip
│   ├── TubeVault-2026.09.002.zip
│   ├── TubeVault-2026.09.003.zip
│   ├── TubeVault-2026.09.004.zip
│   ├── TubeVault-2026.09.005/
│   └── TubeVault-2026.09.006/
```

## Calidad MP3

La interfaz muestra un selector horizontal `Alta | Media | Baja`, con una única opción activa. Internamente se aplican estos niveles de conversión VBR de FFmpeg a través de yt-dlp:

- **Alta:** `--audio-quality 0`
- **Media:** `--audio-quality 2`
- **Baja:** `--audio-quality 4`

El valor predeterminado es **Media** y se recuerda entre ejecuciones. El mismo perfil se aplica a canciones individuales y playlists.

### Comparativa técnica 0 / 2 / 4

Prueba realizada con el mismo contenido público corto: `"Sintel" Trailer, Durian Open Movie Project`, del proyecto abierto de Blender. Los tres archivos fueron validados mediante el ffprobe local.

| Calidad | Valor | Tamaño | Bitrate aprox. | Sample rate | Canales | Duración |
|---|---:|---:|---:|---:|---:|---:|
| Alta | 0 | 1,416 MiB | 227 kbps | 48.000 Hz | 2 | 52,224 s |
| Media | 2 | 1,054 MiB | 169 kbps | 48.000 Hz | 2 | 52,224 s |
| Baja | 4 | 0,852 MiB | 137 kbps | 48.000 Hz | 2 | 52,224 s |

Los tres resultados contienen audio MP3 válido. Conservan título, artista y fecha; álbum y número de pista no estaban disponibles en el contenido original. Duración, frecuencia y canales coinciden, y los tamaños mantienen el orden lógico Alta → Media → Baja.

## Tema e idioma

La cabecera ofrece un único botón de **Ajustes**. Su ventana permite elegir el tema
**Claro** u **Oscuro azul**, seleccionar **Español** o **English** y abrir las filas
**Componentes y actualizaciones** y **Acerca de TubeVault**. La gestión técnica se
realiza en una ventana independiente y compacta. Tema e idioma solo se aplican al
pulsar **Aceptar**; **Cancelar** descarta esos cambios.

El tema oscuro utiliza azul marino, superficies azul grisáceo, texto blanco suave y el azul de TubeVault como acento. No usa negro puro ni librerías visuales externas.

Los textos visibles se centralizan en recursos `.resx`. Etiquetas, estados, errores, resultados, tooltips, About y actualización de yt-dlp están disponibles en ambos idiomas.

## Componentes y actualizaciones

Antes de habilitar el análisis, TubeVault valida los tres ejecutables locales. Si
falta alguno o no puede iniciarse, muestra **Preparando TubeVault** y recupera solo
lo necesario. yt-dlp se instala por separado; FFmpeg y ffprobe siempre se preparan
juntos a partir del mismo paquete.

TubeVault comprueba en segundo plano la publicación estable más reciente del repositorio oficial de yt-dlp cuando nunca se ha comprobado antes o han pasado al menos **10 días**.

- Si está actualizado, no muestra nada.
- Si hay una versión nueva, el botón de Ajustes muestra un indicador discreto y **Componentes y actualizaciones** ofrece **ACTUALIZAR / UPDATE**.
- Nunca actualiza sin una acción explícita del usuario.
- Descarga a un archivo temporal y ejecuta `yt-dlp --version` sobre él.
- Solo reemplaza el ejecutable después de validarlo.
- Conserva una copia de seguridad durante la sustitución y restaura la versión anterior si falla.
- No requiere reiniciar la aplicación.

La fuente utilizada es exclusivamente la publicación oficial de yt-dlp en GitHub.

FFmpeg se comprueba como máximo cada **30 días**. La ventana de componentes permite consultar su
versión, buscar una publicación nueva, actualizar el par FFmpeg/ffprobe y reparar
manualmente los componentes. Una actualización fallida conserva los ejecutables
válidos anteriores.

## About

Se abre desde la fila **Acerca de TubeVault** dentro de Ajustes y muestra:

- TubeVault `2026.09.006`;
- versión detectada de yt-dlp;
- versión detectada de FFmpeg;
- arquitectura x64;
- runtime .NET 10;
- autor `joseamc91`;
- plataforma Windows;
- créditos y nota breve de licencias para yt-dlp y FFmpeg.

No se muestra un enlace de GitHub porque todavía no existe una URL pública definida para TubeVault.

## Configuración y logs

La configuración portable se guarda en `config/settings.json` y contiene:

```json
{
  "LastDestinationFolder": "C:\\Users\\usuario\\Music",
  "AudioQuality": "Medium",
  "LastYtDlpUpdateCheck": "2026-09-27T16:30:00+00:00",
  "LastFfmpegUpdateCheck": "2026-09-27T16:30:00+00:00",
  "Theme": "Light",
  "Language": "Spanish"
}
```

Los archivos creados por 2026.09.001 siguen siendo compatibles. Si faltan propiedades nuevas se aplican estos valores:

- calidad: `Medium`;
- tema: `Light`;
- idioma: `Spanish`;
- últimas comprobaciones: nulas, lo que provoca las primeras comprobaciones en segundo plano.

Los detalles técnicos se escriben en `logs/TubeVault_YYYY-MM-DD.log`. Se conserva un máximo de 15 logs diarios.

## Dependencias administradas

- `tools/yt-dlp.exe`: descarga oficial de yt-dlp, verificada mediante el checksum publicado y `--version`.
- `tools/ffmpeg.exe` y `tools/ffprobe.exe`: mismo paquete essentials de Gyan.dev, verificado mediante SHA-256 y `-version`.
- Las descargas se validan en carpetas privadas antes de sustituir una instalación válida.
- No se modifica el `PATH` ni se instala nada globalmente.

## Build portable

Cada versión se publica en una carpeta independiente. Para 2026.09.006:

1. Copiar o comprimir `dist/TubeVault-2026.09.006/` completa.
2. Descomprimir conservando la estructura.
3. Ejecutar `TubeVault.exe`.

`config` y `logs` se entregan vacíos. Se crean dentro de la propia carpeta portable en el primer arranque.

## Compatibilidad

- Objetivo principal: Windows 11 x64.
- Objetivo portable adicional: Windows 10 x64.
- Microsoft limita el soporte oficial de .NET 10 en Windows 10 a ediciones LTSC/Enterprise compatibles. Windows 10 Home y Pro 22H2 finalizaron su soporte general el 14 de octubre de 2025.
- Referencia oficial: [compatibilidad de .NET en Windows](https://learn.microsoft.com/dotnet/core/install/windows#supported-versions).

## Limitaciones conocidas

- No hay instalador ni firma digital.
- No hay actualización automática de TubeVault. Las actualizaciones de los componentes requieren una acción explícita.
- No se incluyen cookies ni autenticación.
- No se incrustan carátulas ni se corrige metadata mediante reglas propias.
- La build no es single-file: deben conservarse todos los archivos y subcarpetas publicados.

## Compilación

Con el SDK de .NET 10 instalado:

```powershell
dotnet build .\TubeVault.sln -c Release
```

Comando de referencia para la release cerrada:

```powershell
dotnet publish .\src\TubeVault\TubeVault.csproj -c Release -r win-x64 --self-contained true -o .\dist\TubeVault-2026.09.006
```

## Documentación para desarrollo

- [Guía para agentes](AGENTS.md)
- [Arquitectura](ARCHITECTURE.md)
- [Producto](docs/PRODUCT.md)
- [Proceso de release](docs/RELEASE_PROCESS.md)

## Documentación pública

- [Licencia GPL-3.0-only](LICENSE)
- [Componentes de terceros](THIRD-PARTY-NOTICES.md)
- [Seguridad](SECURITY.md)
- [Contribuciones](CONTRIBUTING.md)
- [Privacidad](PRIVACY.md)
- [Uso responsable](docs/LEGAL.md)
- [Política futura de firma](docs/CODE_SIGNING_POLICY.md)
