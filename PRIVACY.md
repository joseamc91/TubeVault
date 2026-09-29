# Privacidad

Este documento describe el comportamiento de TubeVault `2026.09.006` según su
código fuente actual. No sustituye las políticas de los servicios de terceros.

## Datos almacenados localmente

TubeVault no opera un backend propio y no incluye cuentas de usuario,
telemetría ni analytics propios.

La aplicación guarda junto al ejecutable:

- `config/settings.json`: carpeta de destino, calidad MP3, tema, idioma y fechas
  de las últimas comprobaciones de actualización de componentes;
- `logs/TubeVault_YYYY-MM-DD.log`: registros técnicos diarios; se conservan como
  máximo quince;
- `tools/`: copias locales de yt-dlp, FFmpeg y ffprobe descargadas durante la
  preparación;
- los MP3 creados, en la carpeta elegida por el usuario.

TubeVault no envía automáticamente la configuración ni los logs a su autor.
Estos archivos permanecen en el equipo hasta que el usuario los elimine.

## Contenido de los logs

Los logs pueden contener URLs analizadas o descargadas, títulos de contenido,
rutas locales y nombres de archivo, versiones de componentes, procesos usados,
códigos de salida, mensajes de error, fragmentos limitados de stderr y detalles
de excepciones.

Revisa y elimina datos personales o sensibles antes de compartir un log en un
reporte público. No compartas cookies, credenciales, tokens ni rutas personales
si no son imprescindibles.

## Conexiones de red

TubeVault realiza conexiones únicamente para sus funciones principales:

- ejecuta yt-dlp contra la URL indicada por el usuario, por ejemplo YouTube o
  YouTube Music, para analizar o descargar el contenido solicitado;
- consulta `api.github.com` y descarga desde `github.com` las releases oficiales,
  ejecutables y checksums de yt-dlp;
- consulta `www.gyan.dev` para la versión, el checksum y el paquete de FFmpeg;
- descarga previews desde las URLs de thumbnail devueltas por la metadata de
  yt-dlp. El dominio concreto depende del servicio y del contenido analizado.

No hay un proxy ni servidor de TubeVault entre la aplicación y esos servicios.
Los servicios remotos pueden recibir datos propios de una conexión de red, como
la dirección IP, el User-Agent y la solicitud realizada, y aplican sus propias
condiciones y políticas de privacidad. TubeVault no controla esas prácticas.

## Control del usuario

El usuario decide qué URL analiza, dónde guarda los MP3 y si inicia una descarga
o actualización manual. Puede borrar la configuración, los logs y las
herramientas locales cerrando TubeVault y eliminando sus respectivas carpetas.
La próxima ejecución puede volver a crear los datos necesarios y descargar los
componentes que falten.
