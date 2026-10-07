using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace TubeVault;

internal sealed class CoverArtworkService
{
    private readonly LogService log;
    private readonly DependencyService dependencies;
    private readonly MediaValidationService validation;

    public CoverArtworkService(LogService log, DependencyService dependencies, MediaValidationService validation)
    {
        this.log = log;
        this.dependencies = dependencies;
        this.validation = validation;
    }

    public async Task<bool> TryEmbedAsync(string mp3Path, string workDirectory, CancellationToken cancellationToken)
    {
        var prefix = Path.Combine(workDirectory, $".tubevault-cover-{Guid.NewGuid():N}");
        var pngPath = prefix + ".png";
        var jpegPath = prefix + ".jpg";
        var outputPath = prefix + ".mp3";
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var root = Path.GetFullPath(workDirectory);
            if (!Path.GetFileName(root).StartsWith(".tubevault-", StringComparison.Ordinal)
                || !string.Equals(Path.GetDirectoryName(Path.GetFullPath(mp3Path)), root, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("La carátula solo puede procesarse en la carpeta privada del intento.");
            }
            var thumbnails = Directory.EnumerateFiles(root, "cover-source.*")
                .Where(path => !Path.GetExtension(path).Equals(".part", StringComparison.OrdinalIgnoreCase)
                    && !Path.GetExtension(path).Equals(".mp3", StringComparison.OrdinalIgnoreCase)
                    && new FileInfo(path).Length > 0).ToArray();
            if (thumbnails.Length == 0)
            {
                log.Info("Carátula no disponible");
                return false;
            }

            log.Info("Preparar carátula");
            var prepared = false;
            foreach (var thumbnail in thumbnails)
            {
                try
                {
                    Bitmap cover;
                    try { cover = LoadCover(thumbnail); }
                    catch (Exception exception) when (exception is ArgumentException or OutOfMemoryException or ExternalException)
                    {
                        await RunFfmpegAsync(["-i", thumbnail, "-map", "0:v:0", "-frames:v", "1", "-update", "1", pngPath], cancellationToken);
                        cover = LoadCover(pngPath);
                    }
                    using (cover) SaveJpeg(cover, jpegPath);
                    prepared = true;
                    break;
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception exception)
                {
                    log.Error("Preparar carátula: variante no válida", ("Detalle", exception.Message));
                }
            }
            if (!prepared)
            {
                log.Info("Carátula no disponible; se conserva MP3 sin portada");
                return false;
            }
            cancellationToken.ThrowIfCancellationRequested();

            log.Info("Incrustar carátula");
            await RunFfmpegAsync([
                "-i", mp3Path, "-i", jpegPath,
                "-map", "0:a:0", "-map", "1:v:0", "-c", "copy",
                "-map_metadata", "0", "-map_chapters", "0",
                "-metadata:s:v:0", "title=Album cover", "-metadata:s:v:0", "comment=Cover (front)",
                "-disposition:v:0", "attached_pic", "-id3v2_version", "4", "-f", "mp3", outputPath
            ], cancellationToken);
            if (!await validation.HasValidCoverArtworkAsync(outputPath, cancellationToken))
                throw new InvalidDataException("La salida no contiene audio y portada frontal válidos.");
            cancellationToken.ThrowIfCancellationRequested();

            // Renombrado dentro de la misma carpeta: el original no se toca antes de validar la salida completa.
            File.Move(outputPath, mp3Path, overwrite: true);
            log.Info("Carátula incrustada correctamente");
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception)
        {
            log.Error("Fallo de carátula; se conserva MP3 sin portada", ("Detalle", exception.ToString()));
            return false;
        }
        finally
        {
            foreach (var path in new[] { pngPath, jpegPath, outputPath })
            {
                try { if (File.Exists(path)) File.Delete(path); }
                catch (Exception exception) { log.Error("Limpiar carátula temporal", ("Detalle", exception.Message)); }
            }
        }
    }

    private static Bitmap LoadCover(string path)
    {
        using var stream = File.OpenRead(path);
        using var image = Image.FromStream(stream);
        return CoverArtworkProcessor.CreateCover(image);
    }

    private static void SaveJpeg(Bitmap cover, string path)
    {
        using var flattened = new Bitmap(cover.Width, cover.Height, PixelFormat.Format24bppRgb);
        using (var graphics = Graphics.FromImage(flattened))
        {
            graphics.Clear(Color.White);
            graphics.DrawImageUnscaled(cover, 0, 0);
        }
        var codec = ImageCodecInfo.GetImageEncoders().First(item => item.MimeType == "image/jpeg");
        using var parameters = new EncoderParameters(1);
        parameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 90L);
        flattened.Save(path, codec, parameters);
    }

    private async Task RunFfmpegAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        var result = await MediaProcessRunner.RunAsync(dependencies.FfmpegPath,
            new[] { "-hide_banner", "-loglevel", "error", "-nostdin", "-y" }.Concat(arguments).ToArray(), cancellationToken);
        if (result.ExitCode != 0)
            throw new InvalidOperationException($"FFmpeg terminó con código {result.ExitCode}: {result.Error[..Math.Min(result.Error.Length, 2000)]}");
    }
}
