# Inventario del repositorio público

## Publicar

- `TubeVault.sln` y `src/`: proyecto, código, controles, modelos y recursos propios.
- `scripts/build-portable.ps1` y `.github/workflows/ci.yml`.
- `README.md`, `LICENSE`, `THIRD-PARTY-NOTICES.md`, `SECURITY.md`,
  `CONTRIBUTING.md` y `PRIVACY.md`.
- `.gitignore`, `.editorconfig`, `.gitattributes`, `global.json`,
  `AGENTS.md`, `ARCHITECTURE.md` y `docs/`.
- `.gitkeep` únicamente donde deba conservarse un directorio vacío.

## No publicar en el repositorio

- `dist/`, `artifacts/`, `bin/`, `obj/` y salidas de build/release.
- `data/`, herramientas descargadas y binarios de terceros.
- Configuración personal, logs, temporales, backups y archivos de pruebas.
- Imágenes de referencia locales ajenas al producto.

## Estado actual

- Git y repositorio público en <https://github.com/joseamc91/TubeVault>,
  `origin` configurado, rama `main` y metadata pública preparada.
- CI ejecutada previamente con éxito en runner Windows alojado por GitHub.
- Desde la candidata `2026.09.008`, única distribución TubeVault Portable:
  un ZIP self-contained `win-x64` y un artifact versionado.
- Builder y workflow simplificados en el worktree; esta revisión de CI debe
  validarse en GitHub tras su envío. Los artifacts son temporales.
- Prueba física de preparación, análisis y descarga MP3 con ffprobe correcta
  en un equipo Windows 11 con Smart App Control activo, sin garantía universal.
- Datos siempre bajo `<AppDirectory>/data`; el ZIP inicial contiene cuatro
  archivos públicos y ninguna herramienta descargada.
- La release/tag `2026.09.007` y publicaciones anteriores permanecen inmutables.
- Sin firma digital. SignPath Foundation rechazó la solicitud el 30/09/2026;
  podría volver a solicitarse con más señales públicas de adopción y actividad.

## Pendiente

- Validar en GitHub la CI simplificada a un único artifact Portable.
- Crear, con autorización, la pre-release `2026.09.008` y distribuir su único ZIP.
- Evaluar firma futura; no existe proveedor ni integración activa.
