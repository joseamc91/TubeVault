# Arquitectura de TubeVault

Este documento describe la implementación actual. TubeVault mantiene una arquitectura pequeña: una aplicación WinForms, modelos de datos simples y servicios concretos instanciados directamente por el formulario principal.

## Estructura del repositorio

```text
TubeVault.sln
src/TubeVault/
├── Program.cs
├── MainForm.cs
├── SettingsForm.cs
├── ComponentsForm.cs
├── DependencySetupForm.cs
├── AboutForm.cs
├── Controls/
├── Models/
├── Resources/
└── Services/
tools/
config/
logs/
dist/
installer/
scripts/
```

- `src/TubeVault/`: código de la aplicación.
- `Models/`: objetos que representan medios, playlists, progreso, resultados y preferencias.
- `Controls/`: controles visuales pequeños y reutilizables, como botones, tarjetas y artwork.
- `Resources/`: textos visibles en español e inglés mediante `.resx`.
- `Services/`: análisis, descarga, dependencias, validación, configuración, logs, temas y actualización.
- `tools/`: ejecutables locales de yt-dlp, FFmpeg y ffprobe.
- `config/`: configuración portable.
- `logs/`: logs técnicos diarios.
- `dist/`: publicaciones portables separadas por versión.
- `installer/`: proyecto WiX 5.0.2, separado de `TubeVault.sln`.
- `scripts/`: construcción reproducible del MSI desde un publish temporal.

## Composición de la aplicación

`Program.cs` inicia WinForms sin consola y abre `MainForm`.

Antes de habilitar el uso normal, `MainForm` pide a `DependencyBootstrapService`
que valide yt-dlp, FFmpeg y ffprobe. Si falta o falla alguno, abre
`DependencySetupForm`: una ventana modal con progreso, reintento y salida. La UI
solo muestra estados comprensibles; el diagnóstico completo queda en el log.

`MainForm` construye la interfaz con controles WinForms estándar. También:

- instancia los servicios;
- mantiene el estado visual;
- inicia análisis y descargas con `async/await`;
- transforma resultados técnicos en mensajes comprensibles;
- actualiza progreso, temas e idioma;
- gestiona selección de carpeta, cancelación, nueva descarga y apertura del destino;
- mantiene la selección temporal de elementos de playlist en los checkboxes del `ListView`;
- abre `SettingsForm` de forma modal y aplica tema e idioma cuando devuelve `Aceptar`.

La ventana principal mantiene tres zonas verticales estables: cabecera y URL,
tarjeta central de altura fija y tarjeta inferior de acciones. Canción y playlist
comparten la misma tarjeta central; cada vista reserva una zona fija de artwork.
`ArtworkBox` muestra la miniatura con proporción completa o un placeholder musical
cuando no hay imagen, sin alterar el layout.

`SettingsForm` trabaja con una copia temporal del tema y el idioma. `Cancelar` la
descarta; `Aceptar` devuelve los valores a `MainForm`, que los persiste y aplica.
Una fila abre `ComponentsForm`, que concentra versiones, comprobaciones,
actualizaciones y reparación. Estas acciones son inmediatas e independientes del
resultado de Ajustes. Desde `SettingsForm` también se abre `AboutForm`.

No existe contenedor de inyección de dependencias. Las dependencias se crean explícitamente porque el tamaño actual no justifica infraestructura adicional.

## Instalador

`scripts/build-installer.ps1` obtiene `InformationalVersion`, publica la aplicación
self-contained `win-x64` en `artifacts/` y construye
`installer/TubeVault.Installer` con WiX 5.0.2. El MSI instala únicamente los
binarios y documentos legales bajo `%LocalAppData%\Programs\TubeVault`; los datos
runtime permanecen separados en `%LocalAppData%\TubeVault`.

El proyecto WiX no pertenece a `TubeVault.sln`, no usa custom actions y no incluye
`portable.flag`, herramientas descargadas ni datos runtime.

## Modelos

- `MediaInfo`: resultado común del análisis; representa vídeo individual o playlist.
- `PlaylistItemInfo`: índice, título, duración opcional y URL de un elemento.
- `DownloadProgress`: progreso del elemento y progreso global.
- `DownloadItemResult`: estado final de un elemento: descargado, existente o fallido.
- `DownloadResult`: destino y resumen agregado de la operación.
- `AppSettings`: carpeta, calidad, fechas de comprobación de yt-dlp y FFmpeg, tema e idioma.
- `AudioQuality`, `AppTheme` y `AppLanguage`: opciones persistidas.
- `YtDlpUpdateInfo`: versiones local y disponible y si existe actualización.
- `FfmpegUpdateInfo`: versiones local y disponible y si existe actualización.
- `DependencyProgress`: clave de texto del paso de preparación visible.

## Servicios

### AppPaths

Centraliza `IsPortable`, `AppDirectory`, `DataDirectory`, `ToolsDirectory`,
`ConfigDirectory`, `LogsDirectory`, `SettingsFilePath` y las rutas de los tres
ejecutables administrados.

`AppDirectory` parte siempre de `AppContext.BaseDirectory`. La única señal de modo
portable es un archivo `portable.flag` en esa carpeta. Con el marcador,
`DataDirectory` coincide con la carpeta de la aplicación; sin él, apunta a
`%LocalAppData%\TubeVault`. No se usan heurísticas basadas en la ubicación o en
permisos.

### YtDlpService

Valida `tools/yt-dlp.exe` mediante su estructura de ejecutable y `--version`. Si
falta o está dañado, obtiene la publicación oficial, verifica `yt-dlp.exe` contra
`SHA2-256SUMS` y valida la versión descargada antes de sustituir el archivo activo.
La instalación anterior se conserva hasta completar la validación y se restaura
si la sustitución falla.

Analiza URLs ejecutando yt-dlp sin consola y leyendo JSON generado con `--dump-single-json`, `--skip-download` y `--flat-playlist`.

La opción `--no-playlist` hace que una URL de vídeo que también contiene una playlist se trate como vídeo individual. El tipo final se obtiene del JSON, no de reglas de dominio. Los errores técnicos se clasifican en categorías sencillas para la UI y se escriben completos en el log.

### DownloadService

Orquesta la descarga secuencial de uno o varios elementos. Para cada elemento:

1. calcula el nombre final con el mismo saneado de yt-dlp;
2. omite el trabajo si el MP3 ya existe;
3. crea una carpeta privada `.tubevault-<id>`;
4. descarga solo audio y convierte a MP3 con el FFmpeg local;
5. incrusta metadata nativa con yt-dlp;
6. comunica progreso mediante una plantilla estable;
7. solicita validación a `MediaValidationService`;
8. mueve el MP3 validado al destino final;
9. elimina la carpeta privada.

Utiliza `--no-overwrites` y no permite reintentos internos de yt-dlp. Los intentos pertenecen a TubeVault y están limitados a tres totales por elemento.

### DependencyService

Administra `ffmpeg.exe` y `ffprobe.exe` como una unidad. Valida ambos con
`-version`; si cualquiera falta o falla, descarga el paquete essentials de
Gyan.dev por HTTPS, verifica su SHA-256, extrae únicamente ambos ejecutables y los
valida antes de instalarlos juntos. La sustitución usa copias temporales y rollback.

También consulta la versión publicada de FFmpeg y permite actualizar manualmente
el par completo desde Ajustes.

### DependencyBootstrapService

Coordina la validación inicial y la reparación sin mezclar descargas con la UI.
Repara yt-dlp de forma independiente y reinstala el paquete FFmpeg completo cuando
falla ffmpeg o ffprobe. Una validación final confirma que los tres procesos arrancan.

### MediaValidationService

Comprueba que el MP3 exista y no esté vacío. Ejecuta el ffprobe local y exige que la salida contenga una pista de audio. Una validación fallida provoca otro intento, si todavía queda alguno.

### SettingsService

Lee y escribe `config/settings.json` con `System.Text.Json`. Los enums se guardan como texto. Aplica valores predeterminados cuando el archivo o campos nuevos no existen y evita que un fallo de configuración cierre la aplicación.

### LogService

Escribe texto plano thread-safe en `logs/TubeVault_YYYY-MM-DD.log`. Registra niveles `INFO` y `ERROR`, conserva un máximo de quince archivos y nunca permite que un fallo de escritura detenga TubeVault.

### ThemeService

Define las paletas Claro y Oscuro azul y las aplica recursivamente a los controles. `MainForm` completa el estilo de botones principales, secundarios y del selector segmentado de calidad.

### TextService

Obtiene textos de `Resources/UiText.resx` y `Resources/UiText.en.resx` mediante `ResourceManager`. El idioma puede cambiarse en ejecución; los textos dinámicos se vuelven a generar con el idioma activo.

### YtDlpUpdateService

Consulta la última publicación oficial de yt-dlp. La comprobación periódica se hace
en segundo plano cuando corresponde y señala el botón de Ajustes si existe una
versión nueva. La comprobación manual y la actualización se presentan dentro de
`ComponentsForm`; la actualización solo comienza al pulsar el botón de la UI.

La instalación segura y la verificación de checksum se reutilizan desde
`YtDlpService`, evitando dos implementaciones distintas del reemplazo.

## Flujo principal

```text
URL
  → YtDlpService analiza JSON
  → MainForm muestra vídeo o playlist
  → DownloadService crea trabajos secuenciales
  → yt-dlp extrae audio
  → FFmpeg convierte a MP3 e incrusta metadata
  → MediaValidationService usa ffprobe
  → se mueve el archivo validado
  → MainForm muestra el resumen
```

## Playlists

El análisis plano obtiene la lista sin hacer una consulta detallada por elemento. Las
duraciones pueden faltar. Todas las filas se marcan al mostrar un análisis nuevo.
`MainForm` entrega a `DownloadService` únicamente los `PlaylistItemInfo` seleccionados.

El servicio separa la posición dentro de la operación —utilizada para progreso— del
índice original de la playlist —utilizado en prefijos como `02 -` o `08 -`. El nombre
editable de subcarpeta se sanea; si queda vacío se usa directamente la carpeta principal.

Un fallo después de tres intentos se registra y se añade al resumen, pero el bucle continúa con el siguiente elemento.

## Progreso

yt-dlp emite líneas con un prefijo privado mediante `--progress-template`. El servicio combina porcentaje del elemento e índice para producir una única barra global, limitada entre 0 y 100.

## Cancelación y limpieza

La UI cancela mediante `CancellationToken`. `DownloadService` mata el árbol de procesos de yt-dlp/FFmpeg, espera su terminación y termina la lectura de stdout/stderr antes de limpiar.

Cada intento tiene su propia carpeta `.tubevault-*`. La limpieza comprueba el prefijo, nunca borra por extensión y reintenta de forma corta y acotada cuando Windows tarda en liberar handles. Los MP3 completados que ya están en el destino no forman parte de esa carpeta y se conservan.

## Datos instalados y portables

Sin `portable.flag`, una ejecución normal usa datos separados del programa:

```text
%LocalAppData%\TubeVault\
├── tools\yt-dlp.exe
├── tools\ffmpeg.exe
├── tools\ffprobe.exe
├── config\settings.json
└── logs\TubeVault_YYYY-MM-DD.log
```

Con `portable.flag` junto a `TubeVault.exe`, la raíz de datos es
`AppContext.BaseDirectory`:

```text
TubeVault.exe
portable.flag
tools/yt-dlp.exe
tools/ffmpeg.exe
tools/ffprobe.exe
config/settings.json
logs/TubeVault_YYYY-MM-DD.log
```

El publish normal no incorpora el marcador; un paquete portable debe añadirlo de
forma explícita. No se usa el `PATH`, el registro de Windows ni una base de datos.
