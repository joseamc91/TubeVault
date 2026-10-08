# Producto TubeVault

## Objetivo

TubeVault permite obtener música y otro audio disponible públicamente en enlaces compatibles con yt-dlp y guardarlo como MP3. Está orientado principalmente a YouTube y YouTube Music.

El producto oculta herramientas, codecs y parámetros técnicos. Su usuario objetivo es una persona que quiere pegar un enlace, comprobar el contenido y descargarlo sin conocer yt-dlp, FFmpeg o la línea de comandos.

## Flujo de uso

1. En el primer uso, TubeVault prepara sus componentes si es necesario.
2. Pegar una URL.
3. Pulsar **Analizar**.
4. Revisar la información detectada.
5. Elegir carpeta y calidad.
6. Pulsar **Descargar MP3**.
7. Seguir el progreso global.
8. Recibir un resultado claro y, si procede, abrir la carpeta.

## Contenido individual

Un vídeo individual se entiende como fuente de audio. Antes de descargar se muestran:

- título;
- canal o artista;
- duración;
- fecha de publicación cuando está disponible.

El resultado conserva el título proporcionado por YouTube como base del nombre, sujeto al saneado necesario de yt-dlp para Windows.

## Playlists

Cada elemento de una playlist aparece marcado por defecto. El usuario puede
desmarcar canciones, seleccionar toda la lista o deseleccionarla y descargar
únicamente la selección. Si no queda ninguna canción marcada, la descarga se
deshabilita.

La interfaz muestra nombre, creador, cantidad de elementos y una lista desplazable. La duración se enseña cuando ya viene en el análisis plano; TubeVault no realiza una consulta adicional por cada elemento solo para obtenerla.

Los nombres finales incluyen el índice original, por ejemplo `01 - Título.mp3`.
Una selección de los elementos 02, 05 y 08 conserva esos números; no se renumera.
El usuario puede indicar una subcarpeta. Si el campo queda vacío, los archivos se
guardan directamente en la carpeta principal.

## Formato y metadata

- El formato de salida es MP3.
- La metadata se incrusta mediante las capacidades nativas de yt-dlp y FFmpeg.
- Puede incluir título, artista, álbum, fecha/año y número de pista cuando la fuente los proporciona.
- TubeVault no corrige ni infiere metadata mediante heurísticas o IA.

## Carátulas (2026.10.002 Public Preview)

La opción **Incrustar carátula en los MP3** está activada por defecto y puede
desactivarse en Ajustes. Al aceptar se guarda la preferencia; cancelar descarta
el cambio pendiente. Si se desactiva, no se obtiene ni procesa una imagen para el MP3.

TubeVault utiliza la thumbnail del contenido: cada canción individual obtiene
su propia imagen, también cuando forma parte de una playlist. La transformación
es siempre un recorte central cuadrado 1:1, sin análisis inteligente ni selección
de zona, seguido de una reducción a un máximo de 500×500 px. Nunca amplía imágenes
pequeñas. La preview y la portada del MP3 comparten esta misma regla.

La portada final se convierte a JPEG de calidad 90, con la transparencia aplanada,
y se incrusta como portada frontal. El audio se copia sin recodificación,
se preserva la metadata existente y se mantiene ID3v2.4. Los archivos intermedios
permanecen en la carpeta temporal privada del intento y se limpian al finalizar.

Si no existe una thumbnail válida o falla únicamente el procesamiento de artwork,
se conserva el MP3 válido sin portada. Los MP3 ya existentes nunca se modifican
retroactivamente; la opción solo afecta a nuevas descargas.

## Calidad

La interfaz ofrece tres opciones:

- **Alta:** nivel interno `0`.
- **Media:** nivel interno `2`.
- **Baja:** nivel interno `4`.

Son niveles de calidad MP3 utilizados por yt-dlp/FFmpeg y no se muestran como parámetros técnicos al usuario. **Media** es el valor predeterminado. La selección se recuerda entre sesiones.

## Destino y archivos existentes

La carpeta de destino se elige con el selector estándar de Windows y se recuerda.

TubeVault nunca sobrescribe un MP3 existente. Si encuentra el nombre final, omite la descarga y lo contabiliza como **ya existente**. En playlists, el resumen separa descargados, existentes y fallidos.

## Progreso

La aplicación usa una sola barra global. En una playlist combina la posición del
elemento dentro de la selección y su progreso aproximado. Los elementos desmarcados
no cuentan en el progreso ni en el resumen final.

## Reintentos y validación

Cada canción dispone de un máximo de tres intentos totales. Los reintentos internos de yt-dlp están desactivados para que esa regla sea controlada por TubeVault.

Después de descargar:

1. se comprueba que el MP3 exista;
2. se comprueba que no esté vacío;
3. ffprobe confirma que contiene audio.

Un fallo definitivo de un elemento de playlist no interrumpe los siguientes.

## Cancelación

Cancelar detiene el elemento actual y la playlist completa. TubeVault termina el árbol de procesos, espera a que libere los archivos y limpia únicamente la carpeta temporal privada del intento actual.

Los elementos finalizados antes de cancelar permanecen en el destino. La aplicación queda preparada para otra operación.

## Errores y diagnóstico

La interfaz muestra mensajes breves para URL no válida, contenido no disponible, problemas de red, fallos de preparación o descarga y cancelación.

No muestra stderr, stack traces ni comandos. El detalle técnico se guarda en un log diario dentro de `data/logs/`, con un máximo de quince archivos.

## Apariencia e idioma

- Icono propio de TubeVault y wordmark integrado en la cabecera, adaptado a los temas Claro y Oscuro.
- Tema Claro.
- Tema Oscuro azul, sin negro puro.
- Interfaz completa en Español y English.
- Tema e idioma se eligen en Ajustes, se aplican al aceptar sin reiniciar y se recuerdan.
- Cancelar Ajustes descarta los cambios pendientes de tema e idioma.
- El selector de calidad es horizontal y muestra una única opción activa.
- La ventana principal mantiene estables la cabecera, la tarjeta de contenido y el bloque inferior de acciones.
- Canciones y playlists reservan una zona fija para la miniatura; si no existe o no puede cargarse, se muestra un placeholder discreto.

## About

La fila **Acerca de TubeVault** de Ajustes abre una ventana con la versión de TubeVault, las versiones detectadas de yt-dlp y FFmpeg, arquitectura, runtime, autor, plataforma y créditos breves.

## Comprobación de actualizaciones de TubeVault

TubeVault consulta la última Stable pública de `joseamc91/TubeVault` en GitHub.
Después de preparar los componentes, comprueba su versión en segundo plano si
nunca lo ha hecho o han pasado al menos 24 horas desde la última consulta correcta.
Tras una comprobación correcta no repite la consulta automática durante esas 24 horas.
Los fallos no guardan una fecha de comprobación ni interrumpen el arranque.

En **Componentes y actualizaciones**, la tarjeta de TubeVault permite comprobar
manualmente en cualquier momento. Si hay una Stable posterior, aparece un
indicador discreto en Ajustes y el botón **VER RELEASE** abre la publicación en
el navegador. Una versión local superior a la Stable pública no se considera
una actualización pendiente.

TubeVault no descarga, reemplaza ni instala automáticamente una nueva versión
de la aplicación; el usuario decide si obtiene el ZIP de la release.

## Actualización de yt-dlp

TubeVault comprueba periódicamente en segundo plano si existe una publicación estable más reciente. Si la encuentra, muestra un indicador discreto en Ajustes. La fila **Componentes y actualizaciones** abre una ventana independiente para consultar versiones, buscar actualizaciones, actualizar manualmente y reparar los componentes.

La actualización nunca es automática: solo se inicia cuando el usuario pulsa **ACTUALIZAR**. El nuevo ejecutable se valida antes de sustituir el anterior y un fallo conserva la versión instalada.

## Preparación y reparación de componentes

TubeVault administra yt-dlp, FFmpeg y ffprobe dentro de `data/tools/` junto a la aplicación. Antes de permitir
el análisis valida que los tres funcionen. Si falta yt-dlp, recupera únicamente ese
componente. Si falta o falla ffmpeg o ffprobe, reinstala ambos desde el mismo paquete.

TubeVault es siempre portable. Su raíz de datos es `<AppDirectory>/data`:
configuración en `data/config`, logs en `data/logs` y herramientas en
`data/tools`. El ZIP inicial no contiene datos de ejecución ni herramientas.
Para mover la aplicación conservando sus datos, se mueve la carpeta completa.

En un primer arranque sin herramientas se muestra una preparación guiada. Si no hay
conexión y todavía no existe un entorno válido, el usuario puede reintentar o salir;
no se habilita una aplicación incapaz de descargar. La ventana **Componentes y
actualizaciones** permite reparar el entorno manualmente.

FFmpeg se comprueba periódicamente, como máximo cada 30 días. Cuando existe una
versión nueva, la ventana de componentes permite actualizar juntos ffmpeg y ffprobe. Las comprobaciones
no sustituyen componentes por sí solas y un fallo conserva la instalación válida.

## Fuera de alcance actualmente

- Descarga de vídeo.
- Formatos distintos de MP3.
- Selector de bitrate numérico o calidad personalizada.
- Descargas simultáneas o paralelas.
- Edición manual, limpieza o inferencia mediante IA de metadata.
- Normalización de volumen, ReplayGain u otros procesados.
- Login, cookies, autenticación o contenido privado.
- Instalador y firma digital.
- Actualización automática de TubeVault.
- Modo oscuro negro u otros temas.
- Idiomas adicionales.
