# Contribuir a TubeVault

Gracias por ayudar a mejorar TubeVault. El proyecto prioriza cambios pequeños,
legibles y fáciles de validar para una aplicación dirigida a personas no
técnicas.

## Entorno

- Windows 10/11 x64 compatible con .NET 10.
- .NET SDK `10.0.401`. `global.json` fija el SDK y admite únicamente parches
  compatibles de su misma banda.
- La aplicación no usa `PackageReference`.

Desde la raíz del proyecto:

```powershell
dotnet restore
dotnet build .\TubeVault.sln -c Release
```

La salida de desarrollo se genera en
`src/TubeVault/bin/Release/net10.0-windows`.

## Estructura principal

- `src/TubeVault/`: aplicación WinForms.
- `src/TubeVault/Models/`: modelos simples del dominio y de la UI.
- `src/TubeVault/Services/`: análisis, descarga, dependencias, configuración,
  logs, temas y localización.
- `src/TubeVault/Controls/`: controles WinForms reutilizables.
- `src/TubeVault/Resources/`: textos localizados en español e inglés.
- `scripts/`: empaquetado del único ZIP Portable.
- `docs/`: producto, arquitectura y proceso de release.

Consulta `AGENTS.md`, `ARCHITECTURE.md` y `docs/PRODUCT.md` antes de cambiar el
comportamiento.

## Convenciones

- Mantén el código minimalista, con nombres claros y funciones pequeñas.
- Usa comentarios en castellano solo para explicar intención, decisiones o
  riesgos que no sean evidentes.
- Evita refactors amplios, nuevas abstracciones y dependencias sin una necesidad
  concreta.
- No muestres stderr, comandos, excepciones ni stack traces en la interfaz.
- Mantén paridad de claves y significado entre `UiText.resx` y
  `UiText.en.resx`.
- No cambies reglas estables de descarga fuera del alcance de la contribución.
- Los cambios aportados al código propio de TubeVault se integran bajo
  `GPL-3.0-only`.

## Archivos que no deben versionarse

No incluyas `dist/`, `bin/`, `obj/`, herramientas descargadas, binarios de
terceros, `data/`, configuración personal, logs, archivos multimedia, temporales ni datos
locales. Respeta `.gitignore`.

Las releases cerradas son inmutables. Una contribución normal no debe crear ni
modificar `dist/`, ni incorporar `yt-dlp.exe`, `ffmpeg.exe` o `ffprobe.exe`.

## Validación esperada

- Documentación: revisar contenido, enlaces y coherencia; no es necesario
  compilar si no cambia una afirmación técnica.
- Código o recursos: restaurar y compilar Release sin errores ni advertencias
  nuevas.
- Textos o UI: comprobar español/inglés y temas claro/oscuro en los estados
  afectados.
- Configuración o dependencias: probar el caso válido y el fallo recuperable sin
  dejar archivos temporales.
- Descarga, playlist o cancelación: ejecutar solo las pruebas funcionales
  proporcionales al cambio.

No ejecutes `dotnet publish` durante el desarrollo normal. La publicación se
reserva al cierre explícito de una versión según `docs/RELEASE_PROCESS.md`.

GitHub Actions compila la aplicación y prepara un único ZIP Portable como
artifact temporal. La firma no está implementada.
