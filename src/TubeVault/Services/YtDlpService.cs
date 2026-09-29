using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace TubeVault;

internal sealed class YtDlpService
{
    private const string OfficialReleaseApiUrl =
        "https://api.github.com/repos/yt-dlp/yt-dlp/releases/latest";
    private const string OfficialReleaseDownloadBaseUrl =
        "https://github.com/yt-dlp/yt-dlp/releases/download";

    private static readonly HttpClient HttpClient = CreateHttpClient();

    private readonly LogService log;
    private readonly SemaphoreSlim preparationLock = new(1, 1);
    private readonly string executablePath;
    private readonly string releaseApiUrl;
    private readonly string releaseDownloadBaseUrl;

    public YtDlpService(
        LogService log,
        string? executablePath = null,
        string? releaseApiUrl = null,
        string? releaseDownloadBaseUrl = null)
    {
        this.log = log;
        this.executablePath = executablePath ?? AppPaths.YtDlpPath;
        this.releaseApiUrl = releaseApiUrl ?? OfficialReleaseApiUrl;
        this.releaseDownloadBaseUrl = releaseDownloadBaseUrl
                                      ?? OfficialReleaseDownloadBaseUrl;
    }

    public bool IsAvailable => IsPlausibleWindowsExecutable(executablePath);

    public string ExecutablePath => executablePath;

    public async Task EnsureAvailableAsync(
        CancellationToken cancellationToken = default,
        IProgress<DependencyProgress>? progress = null)
    {
        if (await ValidateAsync(cancellationToken))
        {
            return;
        }

        await preparationLock.WaitAsync(cancellationToken);

        try
        {
            if (await ValidateAsync(cancellationToken))
            {
                return;
            }

            await InstallLatestCoreAsync(expectedVersion: null, cancellationToken, progress);
        }
        finally
        {
            preparationLock.Release();
        }
    }

    public async Task<bool> ValidateAsync(CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
        {
            return false;
        }

        try
        {
            var version = await GetVersionAsync(executablePath, cancellationToken);
            log.Info("Validar yt-dlp", ("Versión", version), ("Resultado", "Correcto"));
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            log.Error(
                "Validar yt-dlp",
                ("Proceso", executablePath),
                ("Detalle", exception.ToString()));
            return false;
        }
    }

    public async Task<string> InstallLatestAsync(
        string? expectedVersion = null,
        CancellationToken cancellationToken = default,
        IProgress<DependencyProgress>? progress = null)
    {
        await preparationLock.WaitAsync(cancellationToken);

        try
        {
            return await InstallLatestCoreAsync(expectedVersion, cancellationToken, progress);
        }
        finally
        {
            preparationLock.Release();
        }
    }

    public static async Task<string> GetVersionAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        var result = await RunVersionProcessAsync(path, "--version", cancellationToken);
        return result.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)[0].Trim();
    }

    public async Task<MediaInfo> AnalyzeAsync(
        string url,
        CancellationToken cancellationToken = default)
    {
        await EnsureAvailableAsync(cancellationToken);

        var processResult = await RunAnalysisProcessAsync(url, cancellationToken);

        if (processResult.ExitCode != 0)
        {
            var kind = ClassifyError(processResult.StandardError);

            log.Error(
                "Analizar URL",
                ("URL", url),
                ("Proceso", executablePath),
                ("Código de salida", processResult.ExitCode),
                ("stderr", processResult.StandardError));

            throw new YtDlpException(
                kind,
                "yt-dlp no ha podido analizar el enlace.",
                processResult.ExitCode);
        }

        try
        {
            var media = ParseMediaInfo(processResult.StandardOutput);

            log.Info(
                "Analizar URL",
                ("URL", url),
                ("Proceso", executablePath),
                ("Código de salida", processResult.ExitCode),
                ("Resultado", media.Type == MediaType.Playlist ? "playlist" : "vídeo"),
                ("Título", media.Title),
                ("Elementos", media.Type == MediaType.Playlist ? media.Items.Count : null));

            return media;
        }
        catch (JsonException exception)
        {
            log.Error(
                "Interpretar resultado de yt-dlp",
                ("URL", url),
                ("Código de salida", processResult.ExitCode),
                ("Detalle", exception.Message),
                ("stdout", LimitForLog(processResult.StandardOutput)));

            throw new YtDlpException(
                YtDlpErrorKind.General,
                "La respuesta de yt-dlp no contiene JSON válido.",
                processResult.ExitCode,
                exception);
        }
    }

    private async Task<string> InstallLatestCoreAsync(
        string? expectedVersion,
        CancellationToken cancellationToken,
        IProgress<DependencyProgress>? progress)
    {
        var toolsDirectory = Path.GetDirectoryName(executablePath)
                             ?? throw new InvalidOperationException("La ruta de yt-dlp no es válida.");
        Directory.CreateDirectory(toolsDirectory);

        var preparationDirectory = Path.Combine(
            toolsDirectory,
            $".yt-dlp-preparation-{Guid.NewGuid():N}");
        var stagedPath = Path.Combine(preparationDirectory, "yt-dlp.exe");
        var backupPath = Path.Combine(preparationDirectory, "yt-dlp.previous.exe");
        Directory.CreateDirectory(preparationDirectory);

        try
        {
            var release = await GetLatestReleaseAsync(cancellationToken);
            var releaseVersion = release.Version;

            if (!string.IsNullOrWhiteSpace(expectedVersion)
                && !releaseVersion.Equals(expectedVersion, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    $"La versión publicada ({releaseVersion}) no coincide con la esperada ({expectedVersion}).");
            }

            var releaseBaseUrl =
                $"{releaseDownloadBaseUrl.TrimEnd('/')}/{Uri.EscapeDataString(release.Tag)}";
            var downloadUrl = releaseBaseUrl + "/yt-dlp.exe";
            var checksumUrl = releaseBaseUrl + "/SHA2-256SUMS";

            log.Info(
                "Descargar yt-dlp",
                ("Versión", releaseVersion),
                ("Origen", downloadUrl));

            await DownloadFileAsync(
                downloadUrl,
                stagedPath,
                "SetupDownloadingYtDlp",
                progress,
                cancellationToken);
            progress?.Report(new DependencyProgress("SetupVerifyingYtDlp"));
            var checksumText = await HttpClient.GetStringAsync(checksumUrl, cancellationToken);
            await VerifyChecksumAsync(stagedPath, checksumText, "yt-dlp.exe", cancellationToken);
            log.Info(
                "Verificar checksum yt-dlp",
                ("Versión", releaseVersion),
                ("Resultado", "Correcto"));
            var stagedVersion = await GetVersionAsync(stagedPath, cancellationToken);

            if (!stagedVersion.Equals(releaseVersion, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    $"El ejecutable descargado informa la versión {stagedVersion}.");
            }

            progress?.Report(new DependencyProgress("SetupInstalling"));

            if (File.Exists(executablePath))
            {
                File.Move(executablePath, backupPath);
            }

            try
            {
                File.Move(stagedPath, executablePath);
                var installedVersion = await GetVersionAsync(executablePath, cancellationToken);

                if (!installedVersion.Equals(releaseVersion, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException("La instalación final de yt-dlp no es válida.");
                }

                DeleteTemporaryFile(backupPath);

                log.Info(
                    "Instalar yt-dlp",
                    ("Versión", installedVersion),
                    ("Destino", executablePath),
                    ("Resultado", "Correcto"));
                return installedVersion;
            }
            catch
            {
                DeleteTemporaryFile(executablePath);

                if (File.Exists(backupPath))
                {
                    File.Move(backupPath, executablePath);
                }

                log.Info("Rollback yt-dlp", ("Destino", executablePath));
                throw;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            log.Error(
                "Preparar yt-dlp",
                ("Destino", executablePath),
                ("Detalle", exception.ToString()));

            throw new YtDlpException(
                YtDlpErrorKind.Preparation,
                "No se ha podido preparar yt-dlp.",
                innerException: exception);
        }
        finally
        {
            await DeleteOwnedDirectoryAsync(preparationDirectory);
        }
    }

    private async Task<(string Tag, string Version)> GetLatestReleaseAsync(
        CancellationToken cancellationToken)
    {
        var json = await HttpClient.GetStringAsync(releaseApiUrl, cancellationToken);
        using var document = JsonDocument.Parse(json);
        var tag = document.RootElement.GetProperty("tag_name").GetString()
                  ?? throw new InvalidDataException("La versión publicada de yt-dlp no está disponible.");
        return (tag, tag.TrimStart('v'));
    }

    private static async Task DownloadFileAsync(
        string url,
        string destinationPath,
        string textKey,
        IProgress<DependencyProgress>? progress,
        CancellationToken cancellationToken)
    {
        using var response = await HttpClient.GetAsync(
            url,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength;
        progress?.Report(new DependencyProgress(textKey, 0, totalBytes, IsDownload: true));

        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var destination = new FileStream(
            destinationPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            81920,
            useAsync: true);
        var buffer = new byte[81920];
        long downloadedBytes = 0;
        long lastReportedBytes = 0;
        var lastReport = Stopwatch.StartNew();

        while (true)
        {
            var read = await source.ReadAsync(buffer, cancellationToken);

            if (read == 0)
            {
                break;
            }

            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            downloadedBytes += read;

            if (downloadedBytes - lastReportedBytes >= 512 * 1024
                || lastReport.ElapsedMilliseconds >= 200)
            {
                progress?.Report(new DependencyProgress(
                    textKey,
                    downloadedBytes,
                    totalBytes,
                    IsDownload: true));
                lastReportedBytes = downloadedBytes;
                lastReport.Restart();
            }
        }

        await destination.FlushAsync(cancellationToken);

        progress?.Report(new DependencyProgress(
            textKey,
            downloadedBytes,
            totalBytes,
            IsDownload: true));

        if (downloadedBytes == 0)
        {
            throw new InvalidDataException("El archivo descargado está vacío.");
        }
    }

    private static async Task VerifyChecksumAsync(
        string filePath,
        string checksumText,
        string fileName,
        CancellationToken cancellationToken)
    {
        var expected = checksumText
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .FirstOrDefault(parts => parts.Length >= 2
                                     && parts[^1].TrimStart('*').Equals(
                                         fileName,
                                         StringComparison.OrdinalIgnoreCase))?
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(expected))
        {
            throw new InvalidDataException("No se encuentra el checksum oficial de yt-dlp.exe.");
        }

        await using var stream = File.OpenRead(filePath);
        var actual = Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken));

        if (!actual.Equals(expected, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("El checksum de yt-dlp no coincide.");
        }
    }

    private static async Task<string> RunVersionProcessAsync(
        string executable,
        string argument,
        CancellationToken cancellationToken)
    {
        if (!IsPlausibleWindowsExecutable(executable))
        {
            throw new FileNotFoundException("El ejecutable no está disponible.", executable);
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        startInfo.ArgumentList.Add(argument);

        using var process = new Process { StartInfo = startInfo };
        process.Start();
        var outputTask = process.StandardOutput.ReadToEndAsync(CancellationToken.None);
        var errorTask = process.StandardError.ReadToEndAsync(CancellationToken.None);

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(CancellationToken.None);
            }

            throw;
        }

        var output = (await outputTask).Trim();
        var error = (await errorTask).Trim();

        if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(output))
        {
            throw new InvalidDataException(
                $"La validación terminó con código {process.ExitCode}: {error}");
        }

        return output;
    }

    private async Task<ProcessResult> RunAnalysisProcessAsync(
        string url,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        foreach (var argument in GetAnalysisArguments(url))
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };

        try
        {
            if (!process.Start())
            {
                throw new InvalidOperationException("No se ha podido iniciar yt-dlp.");
            }

            var standardOutputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var standardErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            try
            {
                await process.WaitForExitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }

                throw;
            }

            return new ProcessResult(
                process.ExitCode,
                await standardOutputTask,
                await standardErrorTask);
        }
        catch (OperationCanceledException)
        {
            log.Info("Cancelar análisis", ("URL", url), ("Proceso", executablePath));
            throw;
        }
        catch (YtDlpException)
        {
            throw;
        }
        catch (Exception exception)
        {
            log.Error(
                "Ejecutar yt-dlp",
                ("URL", url),
                ("Proceso", executablePath),
                ("Detalle", exception.ToString()));

            throw new YtDlpException(
                YtDlpErrorKind.General,
                "No se ha podido ejecutar yt-dlp.",
                innerException: exception);
        }
    }

    private static IEnumerable<string> GetAnalysisArguments(string url)
    {
        yield return "--ignore-config";
        yield return "--dump-single-json";
        yield return "--skip-download";
        yield return "--flat-playlist";
        yield return "--no-playlist";
        yield return "--no-warnings";
        yield return "--no-colors";
        yield return "--no-progress";
        yield return "--encoding";
        yield return "utf-8";
        yield return "--default-search";
        yield return "error";
        yield return "--";
        yield return url;
    }

    private static MediaInfo ParseMediaInfo(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (IsPlaylist(root))
        {
            return ParsePlaylist(root);
        }

        var thumbnailUrls = GetThumbnailUrls(root);

        return new MediaInfo
        {
            Type = MediaType.Video,
            Title = GetText(root, "title"),
            Creator = GetFirstText(root, "uploader", "channel", "artist"),
            SourceUrl = GetFirstOptionalText(root, "webpage_url", "original_url") ?? string.Empty,
            DurationSeconds = GetNumber(root, "duration"),
            PublicationDate = GetDate(root, "upload_date", "release_date"),
            ThumbnailUrl = thumbnailUrls.FirstOrDefault(),
            ThumbnailUrls = thumbnailUrls
        };
    }

    private static bool IsPlaylist(JsonElement root)
    {
        return GetOptionalText(root, "_type") == "playlist"
               || root.TryGetProperty("entries", out var entries)
               && entries.ValueKind == JsonValueKind.Array;
    }

    private static MediaInfo ParsePlaylist(JsonElement root)
    {
        var items = new List<PlaylistItemInfo>();

        if (root.TryGetProperty("entries", out var entries)
            && entries.ValueKind == JsonValueKind.Array)
        {
            var index = 1;

            foreach (var entry in entries.EnumerateArray())
            {
                if (entry.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                {
                    items.Add(new PlaylistItemInfo(index++, "—", null, string.Empty));
                    continue;
                }

                items.Add(new PlaylistItemInfo(
                    index++,
                    GetText(entry, "title"),
                    GetNumber(entry, "duration"),
                    GetFirstOptionalText(entry, "webpage_url", "url") ?? string.Empty));
            }
        }

        var thumbnailUrls = GetThumbnailUrls(root);

        return new MediaInfo
        {
            Type = MediaType.Playlist,
            Title = GetText(root, "title"),
            Creator = GetFirstText(
                root,
                "uploader",
                "channel",
                "playlist_uploader",
                "playlist_channel"),
            SourceUrl = GetFirstOptionalText(root, "webpage_url", "original_url") ?? string.Empty,
            ThumbnailUrl = thumbnailUrls.FirstOrDefault(),
            ThumbnailUrls = thumbnailUrls,
            Items = items
        };
    }

    private static IReadOnlyList<string> GetThumbnailUrls(JsonElement element)
    {
        var directUrl = GetOptionalText(element, "thumbnail");
        var result = new List<string>();
        var knownUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (IsCompatiblePreviewUrl(directUrl))
        {
            result.Add(directUrl!);
            knownUrls.Add(directUrl!);
        }

        if (!element.TryGetProperty("thumbnails", out var thumbnails)
            || thumbnails.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        var candidates = new List<ThumbnailCandidate>();
        var order = 0;

        foreach (var thumbnail in thumbnails.EnumerateArray())
        {
            var url = GetOptionalText(thumbnail, "url");

            if (IsCompatiblePreviewUrl(url))
            {
                var width = GetNumber(thumbnail, "width") ?? 0;
                var height = GetNumber(thumbnail, "height") ?? 0;
                var preference = GetNumber(thumbnail, "preference") ?? double.MinValue;
                candidates.Add(new ThumbnailCandidate(
                    url!,
                    width * height,
                    preference,
                    order));
            }

            order++;
        }

        // El array no siempre viene ordenado y una URL de alta resolución puede no existir.
        // El cargador probará estas variantes de mayor a menor calidad hasta encontrar una válida.
        foreach (var candidate in candidates
                     .OrderByDescending(item => item.PixelCount)
                     .ThenByDescending(item => item.Preference)
                     .ThenByDescending(item => item.Order))
        {
            if (knownUrls.Add(candidate.Url))
            {
                result.Add(candidate.Url);
            }
        }

        return result;
    }

    private static bool IsCompatiblePreviewUrl(string? url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return Path.GetExtension(uri.AbsolutePath).ToLowerInvariant() is
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp";
    }

    private sealed record ThumbnailCandidate(
        string Url,
        double PixelCount,
        double Preference,
        int Order);

    private static string GetText(JsonElement element, string propertyName)
    {
        return GetOptionalText(element, propertyName) ?? "—";
    }

    private static string GetFirstText(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            var value = GetOptionalText(element, propertyName);

            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return "—";
    }

    private static string? GetFirstOptionalText(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            var value = GetOptionalText(element, propertyName);

            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static string? GetOptionalText(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property)
            || property.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var value = property.GetString();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static double? GetNumber(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property)
            || property.ValueKind != JsonValueKind.Number
            || !property.TryGetDouble(out var value))
        {
            return null;
        }

        return value;
    }

    private static DateOnly? GetDate(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            var value = GetOptionalText(element, propertyName);

            if (DateOnly.TryParseExact(
                    value,
                    "yyyyMMdd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var date))
            {
                return date;
            }
        }

        return null;
    }

    private static YtDlpErrorKind ClassifyError(string standardError)
    {
        var error = standardError.ToLowerInvariant();

        if (ContainsAny(
                error,
                "unable to download",
                "network is unreachable",
                "connection timed out",
                "failed to establish",
                "temporary failure in name resolution",
                "getaddrinfo failed",
                "connection refused"))
        {
            return YtDlpErrorKind.Network;
        }

        if (ContainsAny(
                error,
                "private video",
                "video unavailable",
                "content is not available",
                "removed by the uploader",
                "members-only",
                "login required"))
        {
            return YtDlpErrorKind.Unavailable;
        }

        if (ContainsAny(
                error,
                "unsupported url",
                "invalid url",
                "not a valid url",
                "no suitable extractor"))
        {
            return YtDlpErrorKind.InvalidUrl;
        }

        return YtDlpErrorKind.General;
    }

    private static bool ContainsAny(string text, params string[] values)
    {
        return values.Any(value => text.Contains(value, StringComparison.Ordinal));
    }

    private static void DeleteTemporaryFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Se volverá a sustituir en el siguiente intento de preparación.
        }
    }

    private async Task DeleteOwnedDirectoryAsync(string path)
    {
        for (var attempt = 1; attempt <= 4; attempt++)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    return;
                }

                Directory.Delete(path, recursive: true);
                return;
            }
            catch when (attempt < 4)
            {
                await Task.Delay(150 * attempt);
            }
            catch (Exception exception)
            {
                log.Error(
                    "Limpiar preparación yt-dlp",
                    ("Directorio", path),
                    ("Detalle", exception.ToString()));
            }
        }
    }

    private static bool IsPlausibleWindowsExecutable(string path)
    {
        try
        {
            using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);

            if (stream.Length < 64 || stream.ReadByte() != 'M' || stream.ReadByte() != 'Z')
            {
                return false;
            }

            stream.Position = 0x3C;
            Span<byte> offsetBytes = stackalloc byte[4];
            stream.ReadExactly(offsetBytes);
            var peOffset = BitConverter.ToInt32(offsetBytes);

            if (peOffset < 0 || peOffset > stream.Length - 4)
            {
                return false;
            }

            stream.Position = peOffset;
            Span<byte> signature = stackalloc byte[4];
            stream.ReadExactly(signature);
            return signature.SequenceEqual(new byte[] { (byte)'P', (byte)'E', 0, 0 });
        }
        catch
        {
            return false;
        }
    }

    private static string LimitForLog(string text)
    {
        const int maximumLength = 4000;
        return text.Length <= maximumLength ? text : text[..maximumLength] + "…";
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(3)
        };

        client.DefaultRequestHeaders.UserAgent.ParseAdd(AppMetadata.UserAgent);
        return client;
    }

    private sealed record ProcessResult(
        int ExitCode,
        string StandardOutput,
        string StandardError);
}
