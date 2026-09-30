# Changelog

Las versiones `2026.09.001`–`2026.09.006` fueron releases locales de desarrollo.
Desde `2026.09.007`, TubeVault se publica mediante GitHub Releases.

## 2026.09.009 — Stable

- Nueva identidad visual de TubeVault.
- Nuevo icono de aplicación.
- Wordmark integrado en la interfaz, adaptado a modo claro y oscuro.
- Nueva presentación pública del repositorio con README bilingüe.
- Primera versión Stable.

## 2026.09.008 — Public Preview

- TubeVault pasa a distribuirse únicamente como aplicación portable.
- Nuevo ejecutable self-contained Single File.
- Los datos pasan a almacenarse bajo `data/`.
- Eliminados MSI, WiX, modo instalado, Portable Classic y `portable.flag`.
- Distribución simplificada a un único ZIP.

## 2026.09.007 — Public Preview

- Primera publicación pública de TubeVault en GitHub.
- Añadido instalador MSI x64 por usuario.
- Añadido modo instalado con datos en `%LocalAppData%`.
- Incorporada compilación del MSI mediante GitHub Actions.
- La prueba con Smart App Control evidenció las limitaciones de una build sin firma.

## 2026.09.006

- Progreso real durante la preparación de componentes, con porcentaje y MB cuando están disponibles.
- Descarga de componentes mediante streaming.
- Ajustes simplificados y nueva ventana de Componentes y actualizaciones.
- Nuevo estado visual al completar una descarga.
- Acciones rápidas para abrir la carpeta o iniciar una nueva descarga.
- Indicador discreto cuando hay actualizaciones pendientes.

## 2026.09.005

- yt-dlp, FFmpeg y ffprobe dejan de incluirse en la distribución.
- Preparación automática de componentes en el primer arranque.
- Verificación mediante checksums y comprobación de versiones.
- Reparación selectiva de componentes dañados o ausentes.
- Actualización segura de yt-dlp y FFmpeg/ffprobe.
- Sustitución atómica con recuperación de la versión anterior en caso de fallo.

## 2026.09.004

- Rediseñada la ventana principal con una tarjeta de contenido estable.
- Nueva zona fija para artwork y placeholder.
- Mejorada la presentación diferenciada de canciones y playlists.
- Carga de thumbnails más robusta, con alternativas cuando una imagen falla.
- Interfaz más compacta para metadata y listas.

## 2026.09.003

- Selección individual de canciones dentro de playlists.
- Acciones para seleccionar o deseleccionar toda la playlist.
- Contador de elementos seleccionados.
- Progreso y resumen calculados únicamente sobre la selección.
- Conservación de los índices originales de la playlist.
- Primer soporte de previsualización de thumbnails.

## 2026.09.002

- Añadidos los perfiles de calidad MP3 Alta, Media y Baja.
- Interfaz completa en Español y English.
- Añadidos los temas Claro y Oscuro.
- Nueva ventana Acerca de TubeVault.
- Comprobación de actualizaciones de yt-dlp.
- Configuración persistente entre ejecuciones.
- Mejorados tooltips, estados y mensajes para el usuario.

## 2026.09.001

- Primera versión funcional de TubeVault.
- Aplicación WinForms portable para Windows.
- Lectura de información de vídeos y playlists antes de descargar.
- Descarga MP3 de vídeos individuales y playlists.
- Metadata nativa mediante yt-dlp y FFmpeg.
- Detección de archivos existentes.
- Reintentos y validación final mediante ffprobe.
- Cancelación segura, limpieza de temporales y logs diarios.
