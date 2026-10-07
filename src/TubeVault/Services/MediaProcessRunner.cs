using System.Diagnostics;
using System.Text;

namespace TubeVault;

// Comparte ejecución y cancelación de las operaciones locales de FFmpeg/ffprobe.
internal static class MediaProcessRunner
{
    public static async Task<(int ExitCode, string Output, string Error)> RunAsync(
        string executable, IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
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
        foreach (var argument in arguments) startInfo.ArgumentList.Add(argument);
        using var process = new Process { StartInfo = startInfo };
        if (!process.Start()) throw new InvalidOperationException("No se pudo iniciar el proceso de medios.");
        var output = process.StandardOutput.ReadToEndAsync(CancellationToken.None);
        var error = process.StandardError.ReadToEndAsync(CancellationToken.None);
        try
        {
            await process.WaitForExitAsync(cancellationToken);
            return (process.ExitCode, await output, await error);
        }
        catch (OperationCanceledException)
        {
            try
            {
                if (!process.HasExited) process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException) { }
            finally
            {
                // No se limpian archivos mientras algún proceso conserva las tuberías abiertas.
                await process.WaitForExitAsync(CancellationToken.None);
                await Task.WhenAll(output, error);
            }
            throw;
        }
    }
}
