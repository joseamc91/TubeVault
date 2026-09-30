# Code signing policy

## Estado actual

TubeVault `2026.09.008` sigue sin firma digital. No existe certificado asignado
al proyecto ni integración activa con ningún proveedor de firma.

La solicitud a SignPath Foundation fue rechazada el **30/09/2026** porque el
proyecto todavía no alcanzaba suficiente visibilidad/confianza pública.
SignPath indicó que se puede volver a solicitar en el futuro si obtiene más
señales públicas de adopción y actividad. No hay una aprobación pendiente ni
garantía de aceptación futura.

La distribución actual es TubeVault Portable: un ZIP y un artifact temporal de
GitHub Actions. Los artifacts no son releases públicas ni builds firmadas.

TubeVault Portable `2026.09.008` se probó correctamente de principio a fin en
**un equipo Windows 11 con Smart App Control activo**. La prueba no garantiza el
mismo comportamiento en todos los equipos. No se recomienda desactivar Smart
App Control ni SmartScreen, añadir exclusiones o desbloquear archivos para
ejecutar la preview.

## Roles del proyecto

TubeVault es actualmente un proyecto mantenido por una sola persona:

- **Author / Committer:** `joseamc91`.
- **Reviewer:** `joseamc91`.
- **Approver for signing requests:** `joseamc91`.

No existen equipos o colaboradores adicionales en estos roles. Las
contribuciones externas deberán ser revisadas por el mantenedor.

## Alcance de una futura firma

Cualquier futura integración firmará únicamente el binario propio distribuido
por TubeVault, `TubeVault.exe`, generado desde el código y los scripts del
repositorio. El ZIP Portable no incluye las herramientas externas.

No se firmarán con un certificado de TubeVault:

- `yt-dlp.exe`;
- `ffmpeg.exe`;
- `ffprobe.exe`;
- binarios o librerías pertenecientes a proyectos upstream.

No existe configuración técnica de firma activa. Si una futura solicitud a
SignPath Foundation fuese aprobada, la atribución aplicable sería:

> Free code signing provided by SignPath.io, certificate by SignPath Foundation.

Esta atribución condicional no describe un servicio recibido actualmente.

## Trazabilidad y aprobación

- Los artefactos oficiales deberán conservar trazabilidad commit → tag → build
  → release desde el repositorio público.
- Cada futura solicitud de firma de una release requerirá aprobación manual de
  `joseamc91`.
- Las credenciales y secretos no se almacenarán en el repositorio, logs ni artifacts.
- Solo se firmarán releases oficiales identificables.

Estos principios aún no están implementados como un pipeline de firma.

## Privacidad

Consulte la [política de privacidad](../PRIVACY.md). TubeVault no opera un
backend propio ni incluye telemetría o analytics propios. Solo realiza las
conexiones necesarias para sus funciones y las comprobaciones o actualizaciones
de componentes descritas en esa política.
