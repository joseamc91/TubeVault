using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace TubeVault;

internal sealed class DownloadService
{
    private const int MaximumAttempts = 3;
    private const string ProgressPrefix = "TVPROGRESS|";
    private const string FilePrefix = "TVFILE|";

    private readonly LogService log;
    private readonly YtDlpService ytDlp;
    private readonly DependencyService dependencies;
    private readonly MediaValidationService validation;
    private readonly CoverArtworkService coverArtwork;

    public DownloadService(
        LogService log,
        YtDlpService ytDlp,
        DependencyService dependencies,
        MediaValidationService validation)
    {
        this.log = log;
        this.ytDlp = ytDlp;
        this.dependencies = dependencies;
        this.validation = validation;
        coverArtwork = new CoverArtworkService(log, dependencies, validation);
    }

    public async Task<DownloadResult> DownloadAsync(
        string originalUrl,
        MediaInfo media,
        IReadOnlyList<PlaylistItemInfo>? selectedPlaylistItems,
        string destinationRoot,
        string playlistFolderName,
        AudioQuality audioQuality,
        bool embedCoverArtwork,
        IProgress<DownloadProgress> progress,
        CancellationToken cancellationToken)
    {
        await ytDlp.EnsureAvailableAsync(cancellationToken);
        await dependencies.EnsureAvailableAsync(cancellationToken);

        var destination = GetDestinationFolder(media, destinationRoot, playlistFolderName);
        Directory.CreateDirectory(destination);

        var items = CreateWorkItems(originalUrl, media, selectedPlaylistItems);
        var results = new List<DownloadItemResult>();

        log.Info(
            "Iniciar descarga",
            ("URL", originalUrl),
            ("Destino", destination),
            ("Tipo", media.Type == MediaType.Playlist ? "playlist" : "canción"),
            ("Calidad", audioQuality),
            ("Elementos", items.Count));

        try
        {
            foreach (var item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var result = await DownloadItemAsync(
                    item,
                    items.Count,
                    destination,
                    audioQuality,
                    embedCoverArtwork,
                    progress,
                    cancellationToken);
                results.Add(result);
            }
        }
        catch (OperationCanceledException)
        {
            log.Info(
                "Cancelar descarga",
                ("URL", originalUrl),
                ("Destino", destination));
            throw;
        }

        var downloadResult = new DownloadResult
        {
            DestinationFolder = destination,
            Items = results
        };

        log.Info(
            "Finalizar descarga",
            ("URL", originalUrl),
            ("Destino", destination),
            ("Descargados", downloadResult.DownloadedCount),
            ("Ya existentes", downloadResult.ExistingCount),
            ("Fallidos", downloadResult.FailedCount));

        return downloadResult;
    }

    private async Task<DownloadItemResult> DownloadItemAsync(
        DownloadWorkItem item,
        int totalItems,
        string destination,
        AudioQuality audioQuality,
        bool embedCoverArtwork,
        IProgress<DownloadProgress> progress,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(item.SourceUrl))
        {
            log.Error("Descargar elemento", ("Elemento", item.Title), ("Detalle", "URL no disponible"));
            return new DownloadItemResult(item.Title, DownloadItemStatus.Failed);
        }

        for (var attempt = 1; attempt <= MaximumAttempts; attempt++)
        {
            var workDirectory = Path.Combine(destination, $".tubevault-{Guid.NewGuid():N}");

            try
            {
                Directory.CreateDirectory(workDirectory);

                // El cálculo usa la misma plantilla que la descarga. Así se respeta
                // exactamente el saneado de nombres de yt-dlp en Windows.
                var expectedName = await ResolveExpectedFileNameAsync(
                    item.SourceUrl,
                    item.FilePrefix,
                    cancellationToken);
                var expectedPath = Path.Combine(destination, expectedName);

                if (File.Exists(expectedPath))
                {
                    progress.Report(CreateProgress(item, totalItems, 100));
                    log.Info(
                        "Omitir archivo existente",
                        ("Elemento", item.Title),
                        ("Archivo", expectedPath));
                    return new DownloadItemResult(
                        item.Title,
                        DownloadItemStatus.AlreadyExists,
                        expectedPath);
                }

                log.Info(
                    "Descargar elemento",
                    ("Elemento", item.Title),
                    ("URL", item.SourceUrl),
                    ("Intento", $"{attempt} de {MaximumAttempts}"),
                    ("Destino", destination),
                    ("Proceso", ytDlp.ExecutablePath));

                var processResult = await RunDownloadProcessAsync(
                    item,
                    totalItems,
                    workDirectory,
                    audioQuality,
                    embedCoverArtwork,
                    progress,
                    cancellationToken);

                if (processResult.ExitCode != 0)
                {
                    log.Error(
                        "Proceso de descarga",
                        ("Elemento", item.Title),
                        ("Proceso", ytDlp.ExecutablePath),
                        ("Código de salida", processResult.ExitCode),
                        ("stderr", LimitForLog(processResult.StandardError)));

                    throw new InvalidOperationException(
                        $"yt-dlp terminó con código {processResult.ExitCode}.");
                }

                var mp3Path = FindDownloadedMp3(workDirectory, processResult.OutputFilePath);
                progress.Report(CreateProgress(item, totalItems, embedCoverArtwork ? 99 : 100, isValidating: true));

                if (mp3Path is null
                    || !await validation.IsValidMp3Async(mp3Path, cancellationToken))
                {
                    throw new InvalidDataException("El MP3 descargado no contiene audio válido.");
                }

                var finalPath = Path.Combine(destination, Path.GetFileName(mp3Path));

                if (File.Exists(finalPath))
                {
                    log.Info("Omitir archivo existente", ("Elemento", item.Title), ("Archivo", finalPath));
                    return new DownloadItemResult(item.Title, DownloadItemStatus.AlreadyExists, finalPath);
                }

                if (embedCoverArtwork)
                {
                    if (await TryDownloadThumbnailAsync(item, workDirectory, cancellationToken))
                        await coverArtwork.TryEmbedAsync(mp3Path, workDirectory, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                    if (File.Exists(finalPath))
                    {
                        log.Info("Omitir archivo existente", ("Elemento", item.Title), ("Archivo", finalPath));
                        return new DownloadItemResult(item.Title, DownloadItemStatus.AlreadyExists, finalPath);
                    }
                }

                File.Move(mp3Path, finalPath);
                progress.Report(CreateProgress(item, totalItems, 100));

                log.Info(
                    "Descarga validada",
                    ("Elemento", item.Title),
                    ("Intento", attempt),
                    ("Archivo", finalPath),
                    ("Código de salida", processResult.ExitCode));

                return new DownloadItemResult(item.Title, DownloadItemStatus.Downloaded, finalPath);
            }
            catch (OperationCanceledException)
            {
                log.Info(
                    "Cancelar elemento",
                    ("Elemento", item.Title),
                    ("Intento", attempt));
                throw;
            }
            catch (Exception exception)
            {
                log.Error(
                    "Fallo de descarga",
                    ("Elemento", item.Title),
                    ("URL", item.SourceUrl),
                    ("Intento", $"{attempt} de {MaximumAttempts}"),
                    ("Detalle", exception.ToString()));

                if (attempt == MaximumAttempts)
                {
                    return new DownloadItemResult(item.Title, DownloadItemStatus.Failed);
                }
            }
            finally
            {
                await DeleteOwnedWorkDirectoryAsync(workDirectory, item.Title);
            }
        }

        return new DownloadItemResult(item.Title, DownloadItemStatus.Failed);
    }

    private async Task<string> ResolveExpectedFileNameAsync(
        string url,
        string filePrefix,
        CancellationToken cancellationToken)
    {
        var startInfo = CreateYtDlpStartInfo();

        AddCommonArguments(startInfo);
        AddArgument(startInfo, "--skip-download");
        AddArgument(startInfo, "--print", "filename");
        AddArgument(startInfo, "-o", filePrefix + "%(title)s.mp3");
        AddArgument(startInfo, "--", url);

        var result = await RunProcessAsync(startInfo, null, cancellationToken);

        if (result.ExitCode != 0)
        {
            log.Error(
                "Calcular nombre de archivo",
                ("URL", url),
                ("Proceso", ytDlp.ExecutablePath),
                ("Código de salida", result.ExitCode),
                ("stderr", LimitForLog(result.StandardError)));

            throw new InvalidOperationException(
                "No se ha podido calcular el nombre del archivo.");
        }

        var fileName = result.StandardOutput
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(Path.GetFileName)
            .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name));

        return string.IsNullOrWhiteSpace(fileName)
            ? throw new InvalidDataException("yt-dlp no ha devuelto un nombre de archivo.")
            : fileName;
    }

    private async Task<ProcessRunResult> RunDownloadProcessAsync(
        DownloadWorkItem item,
        int totalItems,
        string workDirectory,
        AudioQuality audioQuality,
        bool embedCoverArtwork,
        IProgress<DownloadProgress> progress,
        CancellationToken cancellationToken)
    {
        var startInfo = CreateYtDlpStartInfo();

        AddCommonArguments(startInfo);
        AddArgument(startInfo, "--extract-audio");
        AddArgument(startInfo, "--audio-format", "mp3");
        AddArgument(startInfo, "--audio-quality", GetAudioQualityValue(audioQuality));
        AddArgument(startInfo, "--embed-metadata");
        AddArgument(startInfo, "--ffmpeg-location", AppPaths.ToolsDirectory);
        AddArgument(startInfo, "--no-overwrites");
        AddArgument(startInfo, "--no-post-overwrites");
        AddArgument(startInfo, "--newline");
        AddArgument(startInfo, "--progress");
        AddArgument(startInfo, "--progress-delta", "0.2");
        AddArgument(startInfo, "--progress-template", "download:TVPROGRESS|%(progress.status)s|%(progress._percent_str)s");
        AddArgument(startInfo, "--print", "after_move:TVFILE|%(filepath)s");
        AddArgument(startInfo, "--retries", "0");
        AddArgument(startInfo, "--fragment-retries", "0");
        AddArgument(startInfo, "--extractor-retries", "0");
        AddArgument(startInfo, "--file-access-retries", "0");
        AddArgument(startInfo, "--paths", workDirectory);
        AddArgument(startInfo, "-o", item.FilePrefix + "%(title)s.%(ext)s");
        AddArgument(startInfo, "--", item.SourceUrl);

        return await RunProcessAsync(
            startInfo,
            line => ProcessOutputLine(line, item, totalItems, progress, embedCoverArtwork),
            cancellationToken);
    }

    private ProcessStartInfo CreateYtDlpStartInfo()
    {
        return new ProcessStartInfo
        {
            FileName = ytDlp.ExecutablePath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
    }

    private async Task<bool> TryDownloadThumbnailAsync(DownloadWorkItem item, string workDirectory, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(45));
        try
        {
            // Se consulta el vídeo individual después de validar el audio. Un fallo de thumbnail nunca lo reinicia.
            var startInfo = CreateThumbnailStartInfo(item.SourceUrl, workDirectory);
            var result = await RunProcessAsync(startInfo, null, timeout.Token);
            if (result.ExitCode == 0) return true;
            log.Info("Carátula no disponible", ("Elemento", item.Title), ("Código de salida", result.ExitCode));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            log.Info("Carátula no disponible", ("Elemento", item.Title), ("Resultado", "Tiempo de espera agotado"));
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception)
        {
            log.Error("Carátula no disponible", ("Elemento", item.Title), ("Detalle", exception.Message));
        }
        return false;
    }

    private ProcessStartInfo CreateThumbnailStartInfo(string sourceUrl, string workDirectory)
    {
        var startInfo = CreateYtDlpStartInfo();
        AddCommonArguments(startInfo);
        AddArgument(startInfo, "--skip-download");
        AddArgument(startInfo, "--write-thumbnail");
        AddArgument(startInfo, "--no-cache-dir");
        AddArgument(startInfo, "--socket-timeout", "15");
        AddArgument(startInfo, "--retries", "0");
        AddArgument(startInfo, "--extractor-retries", "0");
        AddArgument(startInfo, "--paths", workDirectory);
        AddArgument(startInfo, "--paths", "thumbnail:" + workDirectory);
        AddArgument(startInfo, "-o", "thumbnail:cover-source.%(ext)s");
        AddArgument(startInfo, "--", sourceUrl);
        return startInfo;
    }

    private static void AddCommonArguments(ProcessStartInfo startInfo)
    {
        AddArgument(startInfo, "--ignore-config");
        AddArgument(startInfo, "--no-playlist");
        AddArgument(startInfo, "--no-warnings");
        AddArgument(startInfo, "--no-colors");
        AddArgument(startInfo, "--encoding", "utf-8");
    }

    private static string GetAudioQualityValue(AudioQuality quality)
    {
        return quality switch
        {
            AudioQuality.High => "0",
            AudioQuality.Medium => "2",
            AudioQuality.Low => "4",
            _ => "2"
        };
    }

    private async Task<ProcessRunResult> RunProcessAsync(
        ProcessStartInfo startInfo,
        Action<string>? outputLineHandler,
        CancellationToken cancellationToken)
    {
        using var process = new Process { StartInfo = startInfo };

        if (!process.Start())
        {
            throw new InvalidOperationException("No se ha podido iniciar yt-dlp.");
        }

        var output = new StringBuilder();
        // Estas lecturas deben continuar hasta que el proceso y sus hijos cierren
        // las tuberías. Cancelarlas antes puede dejar a FFmpeg usando el archivo.
        var errorTask = process.StandardError.ReadToEndAsync(CancellationToken.None);

        var outputTask = Task.Run(async () =>
        {
            while (await process.StandardOutput.ReadLineAsync(CancellationToken.None) is { } line)
            {
                output.AppendLine(line);
                outputLineHandler?.Invoke(line);
            }
        }, CancellationToken.None);

        try
        {
            await process.WaitForExitAsync(cancellationToken);
            await outputTask;
            return new ProcessRunResult(
                process.ExitCode,
                output.ToString(),
                await errorTask,
                GetOutputFilePath(output.ToString()));
        }
        catch (OperationCanceledException)
        {
            await StopProcessTreeAsync(process, outputTask, errorTask);

            throw;
        }
    }

    private static void ProcessOutputLine(
        string line,
        DownloadWorkItem item,
        int totalItems,
        IProgress<DownloadProgress> progress,
        bool embedCoverArtwork)
    {
        if (!line.StartsWith(ProgressPrefix, StringComparison.Ordinal))
        {
            return;
        }

        var parts = line.Split('|');

        if (parts.Length < 3)
        {
            return;
        }

        var percentageText = parts[2].Trim().TrimEnd('%').Trim();

        if (double.TryParse(
                percentageText,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var percentage))
        {
            progress.Report(CreateProgress(item, totalItems, Math.Clamp(percentage, 0, embedCoverArtwork ? 99 : 100)));
        }
    }

    private static DownloadProgress CreateProgress(
        DownloadWorkItem item,
        int totalItems,
        double itemPercent,
        bool isValidating = false)
    {
        var global = ((item.ProgressIndex - 1) + itemPercent / 100) / totalItems * 100;
        return new DownloadProgress(
            item.ProgressIndex,
            totalItems,
            itemPercent,
            Math.Clamp(global, 0, 100),
            item.Title,
            isValidating);
    }

    private static string? FindDownloadedMp3(string workDirectory, string? reportedPath)
    {
        if (!string.IsNullOrWhiteSpace(reportedPath)
            && File.Exists(reportedPath)
            && Path.GetExtension(reportedPath).Equals(".mp3", StringComparison.OrdinalIgnoreCase))
        {
            return reportedPath;
        }

        return Directory
            .GetFiles(workDirectory, "*.mp3", SearchOption.TopDirectoryOnly)
            .SingleOrDefault();
    }

    private static string? GetOutputFilePath(string output)
    {
        return output
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Where(line => line.StartsWith(FilePrefix, StringComparison.Ordinal))
            .Select(line => line[FilePrefix.Length..].Trim())
            .LastOrDefault();
    }

    private static IReadOnlyList<DownloadWorkItem> CreateWorkItems(
        string originalUrl,
        MediaInfo media,
        IReadOnlyList<PlaylistItemInfo>? selectedPlaylistItems)
    {
        if (media.Type == MediaType.Video)
        {
            return [new DownloadWorkItem(1, media.Title, originalUrl, string.Empty)];
        }

        var width = Math.Max(2, media.Items.Count.ToString(CultureInfo.InvariantCulture).Length);
        var items = selectedPlaylistItems ?? media.Items;

        return items
            .Select((item, position) => new DownloadWorkItem(
                position + 1,
                item.Title,
                item.SourceUrl,
                item.Index.ToString($"D{width}", CultureInfo.InvariantCulture) + " - "))
            .ToList();
    }

    private static string GetDestinationFolder(
        MediaInfo media,
        string destinationRoot,
        string playlistFolderName)
    {
        if (media.Type != MediaType.Playlist || string.IsNullOrWhiteSpace(playlistFolderName))
        {
            return destinationRoot;
        }

        var invalidCharacters = Path.GetInvalidFileNameChars();
        var safeName = new string(playlistFolderName
            .Trim()
            .Select(character => invalidCharacters.Contains(character) ? '_' : character)
            .ToArray())
            .TrimEnd(' ', '.');

        return string.IsNullOrWhiteSpace(safeName)
            ? destinationRoot
            : Path.Combine(destinationRoot, safeName);
    }

    private async Task DeleteOwnedWorkDirectoryAsync(string workDirectory, string title)
    {
        const int maximumAttempts = 5;
        var folderName = Path.GetFileName(workDirectory);

        // La ruta se crea internamente para cada intento. Esta comprobación evita
        // que un error futuro pueda convertir la limpieza en un borrado general.
        if (!folderName.StartsWith(".tubevault-", StringComparison.Ordinal))
        {
            log.Error(
                "Limpiar temporales",
                ("Elemento", title),
                ("Carpeta rechazada", workDirectory),
                ("Detalle", "La ruta no corresponde a una carpeta privada de TubeVault."));
            return;
        }

        var fileCount = 0;

        try
        {
            fileCount = Directory.Exists(workDirectory)
                ? Directory.GetFiles(workDirectory, "*", SearchOption.AllDirectories).Length
                : 0;
        }
        catch
        {
            // El recuento es informativo y no debe impedir la limpieza.
        }

        for (var attempt = 1; attempt <= maximumAttempts; attempt++)
        {
            try
            {
                if (!Directory.Exists(workDirectory))
                {
                    return;
                }

                Directory.Delete(workDirectory, recursive: true);

                log.Info(
                    "Limpiar temporales",
                    ("Elemento", title),
                    ("Carpeta privada", workDirectory),
                    ("Archivos eliminados", fileCount),
                    ("Intento de limpieza", $"{attempt} de {maximumAttempts}"));
                return;
            }
            catch (Exception exception) when (
                exception is IOException or UnauthorizedAccessException)
            {
                if (attempt == maximumAttempts)
                {
                    log.Error(
                        "Limpieza incompleta",
                        ("Elemento", title),
                        ("Carpeta privada", workDirectory),
                        ("Intentos", maximumAttempts),
                        ("Detalle", exception.ToString()));
                    return;
                }

                // Windows puede tardar brevemente en liberar un handle incluso
                // después de terminar el proceso. La espera es corta y acotada.
                await Task.Delay(TimeSpan.FromMilliseconds(150 * attempt));
            }
            catch (Exception exception)
            {
                log.Error(
                    "Limpieza incompleta",
                    ("Elemento", title),
                    ("Carpeta privada", workDirectory),
                    ("Intentos", attempt),
                    ("Detalle", exception.ToString()));
                return;
            }
        }
    }

    private async Task StopProcessTreeAsync(
        Process process,
        Task outputTask,
        Task<string> errorTask)
    {
        var processId = GetProcessId(process);
        var processExited = false;

        for (var attempt = 1; attempt <= 2 && !processExited; attempt++)
        {
            KillProcess(process);

            try
            {
                await process
                    .WaitForExitAsync(CancellationToken.None)
                    .WaitAsync(TimeSpan.FromSeconds(5));
                processExited = true;
            }
            catch (TimeoutException)
            {
                // Se vuelve a solicitar el cierre del árbol una sola vez.
            }
            catch (InvalidOperationException)
            {
                processExited = true;
            }
            catch (Exception exception)
            {
                if (attempt == 2)
                {
                    log.Error(
                        "Detener proceso cancelado",
                        ("Proceso", process.StartInfo.FileName),
                        ("PID", processId),
                        ("Detalle", exception.ToString()));
                }
            }
        }

        if (!processExited)
        {
            log.Error(
                "Detener proceso cancelado",
                ("Proceso", process.StartInfo.FileName),
                ("PID", processId),
                ("Detalle", "El proceso no confirmó su cierre dentro del tiempo límite."));
        }

        try
        {
            // El fin de ambas lecturas confirma que los procesos que heredaron
            // las tuberías también las han cerrado.
            await Task
                .WhenAll(outputTask, errorTask)
                .WaitAsync(TimeSpan.FromSeconds(5));
        }
        catch (Exception exception)
        {
            log.Error(
                "Cerrar salida de proceso cancelado",
                ("Proceso", process.StartInfo.FileName),
                ("PID", processId),
                ("Detalle", exception.Message));
        }
    }

    private static int? GetProcessId(Process process)
    {
        try
        {
            return process.Id;
        }
        catch
        {
            return null;
        }
    }

    private static void KillProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // El proceso puede haber terminado justo antes de Kill.
        }
    }

    private static void AddArgument(ProcessStartInfo startInfo, params string[] arguments)
    {
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }
    }

    private static string LimitForLog(string text)
    {
        const int maximumLength = 4000;
        return text.Length <= maximumLength ? text : text[..maximumLength] + "…";
    }

    private sealed record DownloadWorkItem(
        int ProgressIndex,
        string Title,
        string SourceUrl,
        string FilePrefix);

    private sealed record ProcessRunResult(
        int ExitCode,
        string StandardOutput,
        string StandardError,
        string? OutputFilePath);
}
