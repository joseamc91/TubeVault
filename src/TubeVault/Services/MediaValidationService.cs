using System.Diagnostics;
using System.Text;

namespace TubeVault;

internal sealed class MediaValidationService
{
    private readonly LogService log;
    private readonly DependencyService dependencies;

    public MediaValidationService(LogService log, DependencyService dependencies)
    {
        this.log = log;
        this.dependencies = dependencies;
    }

    public async Task<bool> IsValidMp3Async(
        string filePath,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath) || new FileInfo(filePath).Length == 0)
        {
            log.Error("Validar MP3", ("Archivo", filePath), ("Resultado", "Vacío o inexistente"));
            return false;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = dependencies.FfprobePath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        AddArguments(startInfo, filePath);

        using var process = new Process { StartInfo = startInfo };

        try
        {
            if (!process.Start())
            {
                return false;
            }

            var outputTask = process.StandardOutput.ReadToEndAsync(CancellationToken.None);
            var errorTask = process.StandardError.ReadToEndAsync(CancellationToken.None);

            try
            {
                await process.WaitForExitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                KillProcess(process);

                try
                {
                    await process
                        .WaitForExitAsync(CancellationToken.None)
                        .WaitAsync(TimeSpan.FromSeconds(5));
                    await Task
                        .WhenAll(outputTask, errorTask)
                        .WaitAsync(TimeSpan.FromSeconds(5));
                }
                catch (Exception exception)
                {
                    log.Error(
                        "Detener ffprobe cancelado",
                        ("Proceso", dependencies.FfprobePath),
                        ("Detalle", exception.Message));
                }

                throw;
            }

            var output = await outputTask;
            var error = await errorTask;
            var isValid = process.ExitCode == 0
                          && output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                              .Any(line => line.Trim().Equals("audio", StringComparison.OrdinalIgnoreCase));

            if (isValid)
            {
                log.Info(
                    "Validar MP3",
                    ("Archivo", filePath),
                    ("Proceso", dependencies.FfprobePath),
                    ("Código de salida", process.ExitCode),
                    ("Resultado", "Audio válido"));
            }
            else
            {
                log.Error(
                    "Validar MP3",
                    ("Archivo", filePath),
                    ("Proceso", dependencies.FfprobePath),
                    ("Código de salida", process.ExitCode),
                    ("stderr", LimitForLog(error)));
            }

            return isValid;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            log.Error(
                "Validar MP3",
                ("Archivo", filePath),
                ("Proceso", dependencies.FfprobePath),
                ("Detalle", exception.ToString()));
            return false;
        }
    }

    private static void AddArguments(ProcessStartInfo startInfo, string filePath)
    {
        startInfo.ArgumentList.Add("-v");
        startInfo.ArgumentList.Add("error");
        startInfo.ArgumentList.Add("-select_streams");
        startInfo.ArgumentList.Add("a:0");
        startInfo.ArgumentList.Add("-show_entries");
        startInfo.ArgumentList.Add("stream=codec_type");
        startInfo.ArgumentList.Add("-of");
        startInfo.ArgumentList.Add("default=noprint_wrappers=1:nokey=1");
        startInfo.ArgumentList.Add(filePath);
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
            // El proceso puede haber terminado entre la comprobación y Kill.
        }
    }

    private static string LimitForLog(string text)
    {
        const int maximumLength = 4000;
        return text.Length <= maximumLength ? text : text[..maximumLength] + "…";
    }
}
