# Inventario del repositorio público

## Publicar

- `TubeVault.sln`.
- `src/`, incluidos el proyecto, código fuente, controles, modelos y recursos propios.
- `README.md`, `LICENSE`, `THIRD-PARTY-NOTICES.md`, `SECURITY.md`, `CONTRIBUTING.md` y `PRIVACY.md`.
- `.gitignore`, `.editorconfig`, `.gitattributes`, `global.json`, `AGENTS.md` y `ARCHITECTURE.md`.
- `docs/` y los archivos de configuración necesarios para compilar.
- `.gitkeep` únicamente en directorios vacíos que deban conservarse.

## No publicar

- `dist/`, `bin/`, `obj/` y otros artefactos de build o release.
- Herramientas descargadas y binarios de terceros dentro de `tools/`.
- Configuración personal, logs, rutas o datos locales del usuario.
- Temporales, backups, descargas y archivos generados durante pruebas o uso.
- Imágenes de referencia locales que no formen parte documentada del producto.

## Completado

- Git inicializado y primer commit creado.
- Repositorio público creado en <https://github.com/joseamc91/TubeVault>.
- `origin` configurado, rama principal `main` y primer push realizado.
- `RepositoryUrl` y metadata pública del proyecto.
- GitHub Actions operativo mediante `.github/workflows/ci.yml`.
- Primera ejecución completada correctamente en un runner Windows alojado por GitHub.
- Publish self-contained `win-x64` y artifact `TubeVault-win-x64` generados correctamente.
- Artifact probado manualmente en Windows: bootstrap de dependencias, análisis real y descarga MP3 real correctos.
- Separación de datos instalada/portable mediante `portable.flag`; el publish normal no incluye el marcador.
- Primera infraestructura MSI de prueba con WiX 5.0.2 y script de build reproducible.

## Pendiente

- GitHub Releases.
- Validación manual de instalación/desinstalación y futura UI del instalador.
- Distribución pública del MSI mediante GitHub Releases.
- Configuración operativa de SignPath y firma de código.
