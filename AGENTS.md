# Guía para agentes

TubeVault es una aplicación portable de Windows para descargar como MP3 el audio de vídeos y playlists compatibles con yt-dlp.
Está diseñada para personas no técnicas: la interfaz debe ocultar herramientas, formatos y parámetros internos.

## Tecnología

- C# y WinForms.
- .NET moderno para Windows x64.
- Sin paquetes NuGet salvo necesidad justificada.
- Binarios locales de yt-dlp, FFmpeg y ffprobe.

## Mapa de documentación

- [README.md](README.md): estado, ejecución y build actual.
- [ARCHITECTURE.md](ARCHITECTURE.md): estructura y flujos técnicos reales.
- [docs/PRODUCT.md](docs/PRODUCT.md): comportamiento funcional y límites del producto.
- [docs/RELEASE_PROCESS.md](docs/RELEASE_PROCESS.md): versionado y publicación portable.

## Principios de código

- Priorizar minimalismo y legibilidad.
- Usar nombres claros y funciones pequeñas.
- Escribir comentarios útiles en castellano que expliquen intención o riesgos.
- No comentar lo evidente línea por línea.
- Evitar abstracciones, patrones y dependencias que no resuelvan una necesidad real.
- No introducir DI, factories, repositories o capas empresariales sin petición expresa.
- No hacer refactors amplios ni cambios fuera del alcance solicitado.
- Preservar el comportamiento estable que la tarea no necesite modificar.
- No ampliar el producto por iniciativa propia.

## Experiencia de usuario

- La UI debe seguir siendo sencilla para usuarios sin conocimientos técnicos.
- No exponer yt-dlp, FFmpeg, ffprobe, codecs ni argumentos de consola como decisiones de uso.
- Mostrar mensajes breves, comprensibles y traducidos.
- Nunca mostrar stderr, comandos, códigos internos, excepciones o stack traces en la UI.
- Guardar el detalle técnico necesario en los logs.
- Mantener coherencia entre español e inglés y entre los temas claro y oscuro.

## Invariantes de descarga

- El resultado del producto es audio MP3.
- Nunca sobrescribir un MP3 existente.
- Un archivo existente se contabiliza como existente, no como fallo.
- Cada elemento tiene un máximo de tres intentos totales.
- Un fallo definitivo en una playlist no detiene los elementos siguientes.
- La cancelación detiene la playlist y conserva los elementos ya completados.
- Cada MP3 nuevo debe existir, tener tamaño mayor que cero y superar ffprobe.
- ffprobe debe confirmar al menos una pista de audio válida.
- Cada intento usa una carpeta privada `.tubevault-*` asociada únicamente a ese elemento.
- Limpiar la carpeta privada después de éxito, fallo o cancelación.
- Nunca borrar globalmente por extensión ni recorrer la carpeta destino eliminando temporales ajenos.
- La cancelación debe esperar al cierre del árbol de procesos antes de limpiar.

## Dependencias y procesos

- Usar siempre `data/tools/yt-dlp.exe`, `data/tools/ffmpeg.exe` y `data/tools/ffprobe.exe`.
- No depender del `PATH` del sistema ni instalar componentes globalmente.
- No ejecutar mediante CMD o PowerShell.
- Crear procesos sin consola visible y usar `ProcessStartInfo.ArgumentList`.
- Al cancelar, terminar el árbol de procesos y esperar su salida de forma asíncrona.

## Datos portables

- La configuración vive en `data/config/settings.json` junto a la aplicación.
- Los logs viven en `data/logs/TubeVault_YYYY-MM-DD.log` y se conservan como máximo quince.
- Las herramientas viven en `data/tools/`.
- Una publicación debe resolver estas rutas desde su propia carpeta.
- No introducir rutas absolutas al equipo de desarrollo.

## Repositorio y releases

- Durante el desarrollo normal, compilar y probar desde `src/TubeVault/bin/Release/net10.0-windows`; no ejecutar `dotnet publish` ni crear o modificar `dist/`.
- Preparar el ZIP Portable en `artifacts/` solo cuando se solicite expresamente; no modificar `dist/` histórico.
- Una versión formalmente cerrada y publicada en `dist/` es un artefacto inmutable.
- Nunca sobrescribir ni limpiar una release anterior ya cerrada.
- Seguir [docs/RELEASE_PROCESS.md](docs/RELEASE_PROCESS.md) para una nueva publicación.

## Cierre de tareas

- Si se modifica código, compilar y ejecutar pruebas proporcionales al riesgo del cambio.
- No repetir descargas o regresiones costosas si el área afectada ya está validada.
- Durante un cierre explícito, probar una extracción separada del ZIP Portable, sin modificar el ZIP final.
- Comprobar entonces que el ZIP inicial no contiene `data/` ni datos de ejecución.
- Informar archivos cambiados, pruebas ejecutadas, resultados y limitaciones.
- Detenerse al completar exactamente el alcance solicitado.
