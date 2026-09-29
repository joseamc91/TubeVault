# Política de firma de código

## Estado actual

TubeVault todavía no está firmado digitalmente y no dispone de un pipeline de
firma. Las releases actuales no deben presentarse como artefactos firmados ni
verificados por SignPath.

## Objetivo futuro

El repositorio público oficial está disponible en
<https://github.com/joseamc91/TubeVault>. Como evolución futura, se pretende:

- generar builds oficiales mediante GitHub Actions;
- solicitar firma a SignPath Foundation si el proyecto resulta aprobado;
- mantener trazabilidad entre commit, tag, build y artefacto publicado;
- requerir aprobación manual antes de publicar una release firmada;
- firmar únicamente el código y los artefactos propios de TubeVault.

yt-dlp, FFmpeg y ffprobe se descargan desde sus fuentes configuradas durante la
ejecución. No se firmarán ni se presentarán como binarios propios de TubeVault.

## Principios

- Solo podrán firmarse artefactos oficiales generados desde el repositorio
  público y asociados a una versión identificable.
- Los builds de release deberán ejecutarse sobre infraestructura confiable y con
  pasos documentados y reproducibles.
- Las dependencias de terceros permanecerán separadas de los artefactos propios.
- Las credenciales y secretos de firma no se almacenarán en el repositorio ni en
  logs o artefactos de build.
- Cada release firmada deberá corresponder a un tag o versión y conservar su
  trazabilidad hasta el código fuente.
- Una release requerirá aprobación explícita antes de su distribución pública.

Esta política describe una intención de proyecto. No afirma que GitHub Actions,
SignPath, certificados ni workflows de firma estén configurados actualmente.
