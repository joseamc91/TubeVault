using System.Text.Json;

namespace TubeVault;

internal sealed class YtDlpUpdateService
{
    private const string OfficialReleaseApiUrl =
        "https://api.github.com/repos/yt-dlp/yt-dlp/releases/latest";

    private readonly HttpClient httpClient;
    private readonly LogService log;
    private readonly YtDlpService ytDlp;
    private readonly SemaphoreSlim updateLock = new(1, 1);
    private readonly string releaseApiUrl;

    public YtDlpUpdateService(
        LogService log,
        YtDlpService ytDlp,
        string? releaseApiUrl = null)
    {
        this.log = log;
        this.ytDlp = ytDlp;
        this.releaseApiUrl = releaseApiUrl ?? OfficialReleaseApiUrl;
        httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(AppMetadata.UserAgent);
    }

    public async Task<YtDlpUpdateInfo> CheckAsync(CancellationToken cancellationToken = default)
    {
        await ytDlp.EnsureAvailableAsync(cancellationToken);

        var localVersion = await YtDlpService.GetVersionAsync(
            ytDlp.ExecutablePath,
            cancellationToken);
        var json = await httpClient.GetStringAsync(releaseApiUrl, cancellationToken);
        using var document = JsonDocument.Parse(json);
        var availableVersion = document.RootElement.GetProperty("tag_name").GetString()?.TrimStart('v')
                               ?? throw new InvalidDataException("La versión publicada no está disponible.");
        var updateAvailable = IsNewerVersion(availableVersion, localVersion);

        log.Info(
            "Comprobar actualización yt-dlp",
            ("Versión local", localVersion),
            ("Versión disponible", availableVersion),
            ("Actualización disponible", updateAvailable));

        return new YtDlpUpdateInfo(localVersion, availableVersion, updateAvailable);
    }

    public async Task<string> GetInstalledVersionAsync(CancellationToken cancellationToken = default)
    {
        if (!ytDlp.IsAvailable)
        {
            return "—";
        }

        return await YtDlpService.GetVersionAsync(ytDlp.ExecutablePath, cancellationToken);
    }

    public async Task<string> UpdateAsync(
        string expectedVersion,
        CancellationToken cancellationToken = default)
    {
        await updateLock.WaitAsync(cancellationToken);

        try
        {
            log.Info("Iniciar actualización yt-dlp", ("Versión esperada", expectedVersion));
            var installedVersion = await ytDlp.InstallLatestAsync(
                expectedVersion,
                cancellationToken);
            log.Info("Actualización yt-dlp correcta", ("Versión instalada", installedVersion));
            return installedVersion;
        }
        catch (Exception exception)
        {
            log.Error(
                "Actualización yt-dlp fallida",
                ("Versión esperada", expectedVersion),
                ("Detalle", exception.ToString()));
            throw;
        }
        finally
        {
            updateLock.Release();
        }
    }

    private static bool IsNewerVersion(string availableVersion, string localVersion)
    {
        if (Version.TryParse(availableVersion, out var available)
            && Version.TryParse(localVersion, out var local))
        {
            return available > local;
        }

        return !availableVersion.Equals(localVersion, StringComparison.OrdinalIgnoreCase);
    }

}
