# Code signing policy

## Estado actual

TubeVault todavía no ha sido aceptado por SignPath Foundation. No existe un
certificado asignado al proyecto ni una integración de firma activa, y ninguna
build o release actual debe presentarse como firmada o verificada por SignPath.

GitHub Actions genera actualmente el publish `win-x64` y el instalador MSI como
artifacts temporales y sin firma. Antes de solicitar la participación de
SignPath Foundation, el proyecto publicará una primera release sin firma en la
misma forma que se pretende firmar posteriormente.

## Proveedor previsto

Si TubeVault resulta aprobado, las futuras releases firmadas utilizarán el
servicio previsto por esta atribución:

> Free code signing provided by SignPath.io, certificate by SignPath Foundation.

La atribución identifica al proveedor previsto; no implica que TubeVault ya
haya sido aprobado, disponga de certificado o publique binarios firmados.

## Roles del proyecto

TubeVault es actualmente un proyecto mantenido por una sola persona:

- **Author / Committer:** `joseamc91`.
- **Reviewer:** `joseamc91`.
- **Approver for signing requests:** `joseamc91`.

No existen equipos o colaboradores adicionales que desempeñen estos roles. Las
contribuciones externas deberán ser revisadas por el mantenedor antes de
integrarse.

## Binarios previstos para firma

Una futura configuración de SignPath firmará únicamente binarios propios
generados desde el código y los scripts mantenidos por TubeVault:

- `TubeVault.exe`;
- `TubeVault.dll`;
- el instalador MSI de TubeVault.

No se firmarán con el certificado de TubeVault:

- `yt-dlp.exe`;
- `ffmpeg.exe`;
- `ffprobe.exe`;
- binarios o librerías pertenecientes a proyectos upstream.

El MSI actual no incluye yt-dlp, FFmpeg ni ffprobe. Todavía no existe una
configuración de deep signing.

## Trazabilidad y aprobación

- Los artefactos oficiales deberán generarse desde el repositorio público y
  conservar trazabilidad entre commit, tag, build y release.
- Cada futura solicitud de firma de una release requerirá aprobación manual.
- El approver actual para esas solicitudes será `joseamc91`.
- Las credenciales y secretos de firma no se almacenarán en el repositorio, los
  logs ni los artifacts.
- La firma solo se aplicará a releases oficiales identificables; los artifacts
  temporales de CI no son releases públicas.

Estas reglas describen la política prevista. La aprobación manual y la firma
todavía no están implementadas técnicamente.

## Privacidad

Consulte la [política de privacidad](../PRIVACY.md). TubeVault no opera un
backend propio ni incluye telemetría o analytics propios. Solo realiza las
conexiones necesarias para las funciones solicitadas por el usuario y para las
comprobaciones o actualizaciones de componentes descritas en esa política.
