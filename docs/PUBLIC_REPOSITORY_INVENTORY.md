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

## Revisar antes del primer commit

- Inicialización de Git y revisión del primer staging.
- Creación del repositorio público y definición de su URL real.
- `RepositoryUrl` y metadata que dependa de esa URL.
- GitHub Actions y CI.
- Instalador y proceso público de distribución.
- Configuración operativa de SignPath y firma de releases.
