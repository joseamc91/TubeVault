using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace TubeVault;

// Administra el paquete conjunto de FFmpeg y ffprobe sin depender del PATH.
internal sealed class DependencyService
{
    private const string OfficialDownloadUrl =
        "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip";

    private static readonly HttpClient HttpClient = CreateHttpClient();

    private readonly LogService log;
    private readonly SemaphoreSlim preparationLock = new(1, 1);
    private readonly string toolsDirectory;
    private readonly string downloadUrl;
    private readonly string checksumUrl;
    private readonly string versionUrl;

    public DependencyService(
        LogService log,
        string? toolsDirectory = null,
        string? downloadUrl = null,
        string? checksumUrl = null,
        string? versionUrl = null)
    {
        this.log = log;
        this.toolsDirectory = toolsDirectory ?? AppPaths.ToolsDirectory;
        this.downloadUrl = downloadUrl ?? OfficialDownloadUrl;
        this.checksumUrl = checksumUrl ?? this.downloadUrl + ".sha256";
        this.versionUrl = versionUrl ?? this.downloadUrl + ".ver";
    }

    public string FfmpegPath => Path.Combine(toolsDirectory, "ffmpeg.exe");

    public string FfprobePath => Path.Combine(toolsDirectory, "ffprobe.exe");

    public bool IsAvailable => IsValidFile(FfmpegPath) && IsValidFile(FfprobePath);

    public async Task<bool> ValidateAsync(CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
        {
            return false;
        }

        try
        {
            var ffmpegTask = RunVersionProcessAsync(FfmpegPath, cancellationToken);
            var ffprobeTask = RunVersionProcessAsync(FfprobePath, cancellationToken);
            await Task.WhenAll(ffmpegTask, ffprobeTask);

            log.Info(
                "Validar FFmpeg",
                ("FFmpeg", ParseVersion(await ffmpegTask)),
                ("ffprobe", ParseVersion(await ffprobeTask)),
                ("Resultado", "Correcto"));
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            log.Error(
                "Validar FFmpeg",
                ("FFmpeg", FfmpegPath),
                ("ffprobe", FfprobePath),
                ("Detalle", exception.ToString()));
            return false;
        }
    }

    public async Task<string> GetFfmpegVersionAsync(CancellationToken cancellationToken = default)
    {
        if (!await ValidateAsync(cancellationToken))
        {
            return "—";
        }

        return ParseVersion(await RunVersionProcessAsync(FfmpegPath, cancellationToken));
    }

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
            if (!await ValidateAsync(cancellationToken))
            {
                await InstallPackageCoreAsync(expectedVersion: null, cancellationToken, progress);
            }
        }
        finally
        {
            preparationLock.Release();
        }
    }

    public async Task<FfmpegUpdateInfo> CheckForUpdateAsync(
        CancellationToken cancellationToken = default)
    {
        await EnsureAvailableAsync(cancellationToken);
        var localVersion = await GetFfmpegVersionAsync(cancellationToken);
        var availableVersion = (await HttpClient.GetStringAsync(versionUrl, cancellationToken)).Trim();

        if (string.IsNullOrWhiteSpace(availableVersion))
        {
            throw new InvalidDataException("La versión publicada de FFmpeg no está disponible.");
        }

        var updateAvailable = IsNewerVersion(availableVersion, localVersion);
        log.Info(
            "Comprobar actualización FFmpeg",
            ("Versión local", localVersion),
            ("Versión disponible", availableVersion),
            ("Actualización disponible", updateAvailable));

        return new FfmpegUpdateInfo(localVersion, availableVersion, updateAvailable);
    }

    public async Task<string> UpdateAsync(
        string expectedVersion,
        CancellationToken cancellationToken = default)
    {
        await preparationLock.WaitAsync(cancellationToken);

        try
        {
            log.Info("Iniciar actualización FFmpeg", ("Versión esperada", expectedVersion));
            return await InstallPackageCoreAsync(expectedVersion, cancellationToken, progress: null);
        }
        catch (Exception exception)
        {
            log.Error(
                "Actualización FFmpeg fallida",
                ("Versión esperada", expectedVersion),
                ("Detalle", exception.ToString()));
            throw;
        }
        finally
        {
            preparationLock.Release();
        }
    }

    private async Task<string> InstallPackageCoreAsync(
        string? expectedVersion,
        CancellationToken cancellationToken,
        IProgress<DependencyProgress>? progress)
    {
        Directory.CreateDirectory(toolsDirectory);

        var preparationDirectory = Path.Combine(
            toolsDirectory,
            $".ffmpeg-preparation-{Guid.NewGuid():N}");
        var zipPath = Path.Combine(preparationDirectory, "ffmpeg.zip");
        var stagedFfmpeg = Path.Combine(preparationDirectory, "ffmpeg.exe");
        var stagedFfprobe = Path.Combine(preparationDirectory, "ffprobe.exe");
        var backupFfmpeg = Path.Combine(preparationDirectory, "ffmpeg.previous.exe");
        var backupFfprobe = Path.Combine(preparationDirectory, "ffprobe.previous.exe");

        Directory.CreateDirectory(preparationDirectory);

        try
        {
            log.Info("Descargar FFmpeg", ("Origen", downloadUrl));
            await DownloadFileAsync(
                downloadUrl,
                zipPath,
                "SetupDownloadingFfmpeg",
                progress,
                cancellationToken);
            progress?.Report(new DependencyProgress("SetupVerifyingFfmpeg"));
            await VerifyChecksumAsync(zipPath, checksumUrl, cancellationToken);
            log.Info(
                "Verificar checksum FFmpeg",
                ("Origen", checksumUrl),
                ("Resultado", "Correcto"));
            ExtractExecutables(zipPath, stagedFfmpeg, stagedFfprobe);

            var stagedFfmpegOutput = await RunVersionProcessAsync(stagedFfmpeg, cancellationToken);
            await RunVersionProcessAsync(stagedFfprobe, cancellationToken);
            var stagedVersion = ParseVersion(stagedFfmpegOutput);

            if (!string.IsNullOrWhiteSpace(expectedVersion)
                && !VersionsMatch(stagedVersion, expectedVersion))
            {
                throw new InvalidDataException(
                    $"El paquete descargado informa la versión {stagedVersion}.");
            }

            progress?.Report(new DependencyProgress("SetupInstalling"));

            var ffmpegBackedUp = false;
            var ffprobeBackedUp = false;
            var ffmpegInstalled = false;
            var ffprobeInstalled = false;

            try
            {
                ffmpegBackedUp = BackupIfPresent(FfmpegPath, backupFfmpeg);
                ffprobeBackedUp = BackupIfPresent(FfprobePath, backupFfprobe);
                File.Move(stagedFfmpeg, FfmpegPath);
                ffmpegInstalled = true;
                File.Move(stagedFfprobe, FfprobePath);
                ffprobeInstalled = true;

                var installedFfmpeg = await RunVersionProcessAsync(FfmpegPath, cancellationToken);
                await RunVersionProcessAsync(FfprobePath, cancellationToken);
                var installedVersion = ParseVersion(installedFfmpeg);

                DeleteFile(backupFfmpeg);
                DeleteFile(backupFfprobe);

                log.Info(
                    "Instalar FFmpeg",
                    ("Versión", installedVersion),
                    ("FFmpeg", FfmpegPath),
                    ("ffprobe", FfprobePath),
                    ("Resultado", "Correcto"));
                return installedVersion;
            }
            catch
            {
                if (ffmpegInstalled || ffmpegBackedUp)
                {
                    DeleteFile(FfmpegPath);
                }

                if (ffprobeInstalled || ffprobeBackedUp)
                {
                    DeleteFile(FfprobePath);
                }

                RestoreIfPresent(backupFfmpeg, FfmpegPath);
                RestoreIfPresent(backupFfprobe, FfprobePath);
                log.Info("Rollback FFmpeg", ("Directorio", toolsDirectory));
                throw;
            }
        }
        catch (OperationCanceledException)
        {
            log.Info("Cancelar preparación de FFmpeg");
            throw;
        }
        catch (Exception exception)
        {
            log.Error(
                "Preparar FFmpeg",
                ("Origen", downloadUrl),
                ("Detalle", exception.ToString()));

            throw new YtDlpException(
                YtDlpErrorKind.Preparation,
                "No se ha podido preparar FFmpeg.",
                innerException: exception);
        }
        finally
        {
            await DeleteOwnedDirectoryAsync(preparationDirectory);
        }
    }

    private static async Task<string> RunVersionProcessAsync(
        string executablePath,
        CancellationToken cancellationToken)
    {
        if (!IsValidFile(executablePath))
        {
            throw new FileNotFoundException("El componente no está disponible.", executablePath);
        }

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
        startInfo.ArgumentList.Add("-version");

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

        var output = await outputTask;
        var error = await errorTask;

        if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(output))
        {
            throw new InvalidDataException(
                $"La validación terminó con código {process.ExitCode}: {error.Trim()}");
        }

        return output;
    }

    private static string ParseVersion(string output)
    {
        var firstLine = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault() ?? string.Empty;
        const string marker = "version ";
        var markerIndex = firstLine.IndexOf(marker, StringComparison.OrdinalIgnoreCase);

        if (markerIndex < 0)
        {
            return "—";
        }

        return firstLine[(markerIndex + marker.Length)..]
            .Split([' ', '-'], StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault() ?? "—";
    }

    private static bool IsNewerVersion(string availableVersion, string localVersion)
    {
        if (Version.TryParse(availableVersion, out var available)
            && Version.TryParse(localVersion, out var local))
        {
            return available > local;
        }

        return !VersionsMatch(localVersion, availableVersion);
    }

    private static bool VersionsMatch(string first, string second)
    {
        return first.Equals(second, StringComparison.OrdinalIgnoreCase)
               || first.StartsWith(second + "-", StringComparison.OrdinalIgnoreCase);
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
            throw new InvalidDataException("El paquete descargado está vacío.");
        }
    }

    private static async Task VerifyChecksumAsync(
        string zipPath,
        string checksumAddress,
        CancellationToken cancellationToken)
    {
        var checksumText = await HttpClient.GetStringAsync(checksumAddress, cancellationToken);
        var expectedChecksum = checksumText
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(expectedChecksum))
        {
            throw new InvalidDataException("No se ha recibido el checksum de FFmpeg.");
        }

        await using var stream = File.OpenRead(zipPath);
        var actualChecksum = Convert.ToHexString(
            await SHA256.HashDataAsync(stream, cancellationToken));

        if (!actualChecksum.Equals(expectedChecksum, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("El checksum de FFmpeg no coincide.");
        }
    }

    private static void ExtractExecutables(
        string zipPath,
        string ffmpegDestination,
        string ffprobeDestination)
    {
        using var archive = ZipFile.OpenRead(zipPath);
        ExtractEntry(archive, "/bin/ffmpeg.exe", ffmpegDestination);
        ExtractEntry(archive, "/bin/ffprobe.exe", ffprobeDestination);
    }

    private static void ExtractEntry(
        ZipArchive archive,
        string suffix,
        string destinationPath)
    {
        var entry = archive.Entries.FirstOrDefault(item =>
            item.FullName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));

        if (entry is null)
        {
            throw new InvalidDataException($"No se encuentra {suffix} en el paquete.");
        }

        entry.ExtractToFile(destinationPath);
    }

    private static bool BackupIfPresent(string source, string backup)
    {
        if (File.Exists(source))
        {
            File.Move(source, backup);
            return true;
        }

        return false;
    }

    private static void RestoreIfPresent(string backup, string destination)
    {
        if (File.Exists(backup))
        {
            File.Move(backup, destination);
        }
    }

    private static bool IsValidFile(string path)
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

    private static void DeleteFile(string path)
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
            // La siguiente operación usará otra carpeta privada.
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
                    "Limpiar preparación FFmpeg",
                    ("Directorio", path),
                    ("Detalle", exception.ToString()));
            }
        }
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd(AppMetadata.UserAgent);
        return client;
    }
}
