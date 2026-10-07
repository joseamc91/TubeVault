using System.Text.Json;

namespace TubeVault;

// Solo informa de la última Stable; nunca descarga ni sustituye la aplicación.
internal sealed class TubeVaultUpdateService : IDisposable
{
    private const string ReleaseApiUrl =
        "https://api.github.com/repos/joseamc91/TubeVault/releases/latest";

    private readonly HttpClient httpClient;
    private readonly LogService log;
    private readonly SemaphoreSlim checkLock = new(1, 1);

    public TubeVaultUpdateService(LogService log)
    {
        this.log = log;
        httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(AppMetadata.UserAgent);
        httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
    }

    public TubeVaultUpdateInfo? LatestCheck { get; private set; }

    // Los formularios comparten también los resultados que llegan mientras están abiertos.
    public event EventHandler? CheckStateChanged;

    public async Task<TubeVaultUpdateInfo> CheckAsync(CancellationToken cancellationToken = default)
    {
        await checkLock.WaitAsync(cancellationToken);
        try
        {
            var json = await httpClient.GetStringAsync(ReleaseApiUrl, cancellationToken);
            using var document = JsonDocument.Parse(json);
            var release = document.RootElement;
            if (release.GetProperty("prerelease").GetBoolean()
                || release.GetProperty("draft").GetBoolean())
            {
                throw new InvalidDataException("La respuesta no corresponde a una release Stable publicada.");
            }

            var tag = release.GetProperty("tag_name").GetString() ?? string.Empty;
            var availableVersion = tag.Trim();
            if (availableVersion.StartsWith('v') || availableVersion.StartsWith('V'))
            {
                availableVersion = availableVersion[1..];
            }
            var local = ParseVersion(AppMetadata.Version);
            var available = ParseVersion(availableVersion);
            var releaseUrl = release.GetProperty("html_url").GetString() ?? string.Empty;
            if (!Uri.TryCreate(releaseUrl, UriKind.Absolute, out var uri)
                || uri.Scheme != Uri.UriSchemeHttps
                || !uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase)
                || !uri.AbsolutePath.StartsWith("/joseamc91/TubeVault/releases/", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("La URL de la release no es válida.");
            }

            cancellationToken.ThrowIfCancellationRequested();
            var result = new TubeVaultUpdateInfo(
                AppMetadata.Version, availableVersion, releaseUrl, available > local, local > available);
            log.Info("Comprobar actualización TubeVault",
                ("Versión local", result.LocalVersion),
                ("Última Stable", result.AvailableVersion),
                ("Actualización disponible", result.IsUpdateAvailable),
                ("Versión local más reciente", result.IsLocalVersionNewer));
            LatestCheck = result;
            CheckStateChanged?.Invoke(this, EventArgs.Empty);
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            log.Error("Comprobar actualización TubeVault", ("Detalle", exception.ToString()));
            LatestCheck = null;
            CheckStateChanged?.Invoke(this, EventArgs.Empty);
            throw;
        }
        finally
        {
            checkLock.Release();
        }
    }

    private static Version ParseVersion(string value)
    {
        var parts = value.Split('.');
        if (parts.Length != 3 || parts[0].Length != 4 || parts[1].Length != 2 || parts[2].Length != 3
            || parts.Any(part => part.Any(character => character < '0' || character > '9'))
            || !Version.TryParse(value, out var version) || version.Major < 1000
            || version.Minor < 1 || version.Minor > 12)
        {
            throw new InvalidDataException("La versión no tiene el formato AAAA.MM.REVISION.");
        }

        return version;
    }

    public void Dispose() => httpClient.Dispose();
}
