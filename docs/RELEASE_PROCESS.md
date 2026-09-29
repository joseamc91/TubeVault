# Proceso de release

## Versionado

TubeVault utiliza:

```text
AAAA.MM.REVISION
```

Ejemplos:

- `2026.09.001`: primera revisión publicada en septiembre de 2026.
- `2026.09.002`: segunda revisión publicada en septiembre de 2026.
- `2026.10.001`: primera revisión publicada en octubre de 2026.

La revisión usa tres dígitos y vuelve a `001` al cambiar el mes de versión.

## Desarrollo

Una versión puede acumular varios cambios antes de publicarse. Durante este periodo:

1. Compilar en configuración Release.
2. Probar desde `src/TubeVault/bin/Release/net10.0-windows`.
3. Ejecutar solo las pruebas proporcionales al cambio realizado.
4. No ejecutar `dotnet publish`.
5. No crear, regenerar ni modificar carpetas de `dist/`.
6. No verificar hashes de releases anteriores ni limpiar sus carpetas `config/` o `logs/`.
7. No repetir pruebas desde `dist/` hasta que se solicite expresamente cerrar la versión.

Una misma versión puede pasar por varios ciclos de cambio, build Release y prueba local. Solo la aprobación explícita del usuario inicia su publicación.

`2026.09.006` fue aprobada, cerrada y publicada formalmente. Las publicaciones correspondientes a las versiones `2026.09.001`–`2026.09.006` son inmutables y no deben volver a modificarse.

## Inmutabilidad

- La inmutabilidad comienza cuando el usuario declara expresamente que una versión queda cerrada o publicada.
- Una release cerrada no se modifica ni se sobrescribe.
- Nunca se eliminan o limpian archivos dentro de una versión histórica para preparar otra.
- Cada versión nueva recibe su propia carpeta `dist/TubeVault-AAAA.MM.REVISION/`.
- El número de versión del proyecto, About, README y nombre de carpeta deben coincidir.

## Tipo de publicación

La publicación actual es:

- `win-x64`;
- self-contained;
- no single-file;
- sin instalador.

El usuario debe poder descomprimir la carpeta y abrir `TubeVault.exe` sin instalar .NET manualmente.

## Contenido esperado

Las releases hasta `2026.09.004` incluyen los ejecutables de `tools/`. Desde
`2026.09.005`, TubeVault distribuye `tools/` vacío y obtiene,
validar y reparar sus componentes en el primer uso. No se copian ejecutables a esa
release salvo que una decisión de producto posterior cambie expresamente esta regla.

```text
dist/TubeVault-AAAA.MM.REVISION/
├── TubeVault.exe
├── TubeVault.dll
├── runtime y dependencias de .NET
├── tools/                  vacío desde 2026.09.005
├── config/
└── logs/
```

`config/` y `logs/` deben entregarse vacíos. No incluir MP3, temporales, arneses de prueba, settings personales ni logs de validación.

## Cierre de versión

Este proceso solo se ejecuta cuando el usuario pide expresamente cerrar, publicar o generar la release.

1. Confirmar el número de versión aprobado.
2. Actualizar metadatos del proyecto, About y documentación que muestre esa versión.
3. Compilar Release y resolver errores o advertencias pertinentes.
4. Ejecutar pruebas proporcionales a los cambios realizados.
5. Publicar `win-x64` self-contained en una carpeta nueva; detenerse si ya contiene una release cerrada anterior.
6. Aplicar la política de `tools/` correspondiente a la versión: incluidos hasta 004; directorio vacío desde 005.
7. Probar desde la carpeta publicada, no desde `src/TubeVault/bin`.
8. Limpiar datos generados por la prueba dentro de la release.
9. Verificar la independencia del árbol de desarrollo.
10. Verificar que versiones anteriores de `dist/` no cambiaron.
11. Considerar desde ese momento la carpeta publicada como congelada e inmutable.

## Comando de referencia

Solo durante el cierre de versión, desde la raíz del repositorio y sustituyendo la versión del destino:

```powershell
dotnet publish .\src\TubeVault\TubeVault.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -o .\dist\TubeVault-AAAA.MM.REVISION
```

Confirmar siempre que el contenido final de `tools/` coincide con la política de la versión publicada.

## Pruebas proporcionales

No toda modificación exige repetir descargas extensas. Elegir pruebas según el área afectada:

- documentación: revisar enlaces y contenido, sin recompilar;
- textos o layout: compilar y revisar los estados visuales afectados;
- settings: comprobar valor predeterminado, lectura antigua y persistencia;
- análisis: probar vídeo, playlist y URL mixta;
- descarga o validación: probar canción corta, existente, ffprobe y limpieza;
- playlist: usar una lista pequeña y comprobar continuidad ante fallos;
- cancelación: confirmar cierre de procesos, conservación de completados y ausencia de temporales;
- updater: validar comprobación, sustitución segura y conservación del ejecutable anterior ante fallo.

Evitar pruebas de red costosas si el cambio no puede afectar al motor y existe una validación reciente aplicable.

## Verificación desde dist

Durante el cierre de una release que modifica código ejecutable:

- abrir `TubeVault.exe` desde su carpeta de `dist`;
- comprobar que no aparece consola;
- confirmar que `tools`, `config` y `logs` se resuelven bajo esa misma carpeta;
- confirmar que no accede accidentalmente a `src/TubeVault/bin` ni a otra copia;
- ejecutar el caso funcional mínimo relevante;
- comprobar que no quedan `.tubevault-*`, `.part`, `.webm` u otros temporales propios;
- comprobar que no quedan procesos yt-dlp, FFmpeg o ffprobe huérfanos;
- volver a dejar `config/` y `logs/` vacíos.

## Registro de resultados

La entrega de una release debe indicar:

- archivos modificados;
- tipo y ruta de publicación;
- resultado de compilación;
- pruebas ejecutadas y no ejecutadas;
- tamaño aproximado;
- estado de releases anteriores;
- limitaciones conocidas.

## Evolución del proceso

Git, tags y GitHub Releases son una evolución prevista para mejorar trazabilidad y distribución. No debe asumirse que estén implantados: antes de usarlos hay que comprobar el estado real del repositorio y acordar el flujo correspondiente.
