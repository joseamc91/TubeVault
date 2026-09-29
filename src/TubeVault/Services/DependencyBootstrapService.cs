namespace TubeVault;

// Coordina los tres componentes sin mezclar su preparación con la UI.
internal sealed class DependencyBootstrapService
{
    private readonly YtDlpService ytDlp;
    private readonly DependencyService ffmpeg;
    private readonly LogService log;

    public DependencyBootstrapService(
        YtDlpService ytDlp,
        DependencyService ffmpeg,
        LogService log)
    {
        this.ytDlp = ytDlp;
        this.ffmpeg = ffmpeg;
        this.log = log;
    }

    public async Task<bool> ValidateDependenciesAsync(
        CancellationToken cancellationToken = default)
    {
        var ytDlpTask = ytDlp.ValidateAsync(cancellationToken);
        var ffmpegTask = ffmpeg.ValidateAsync(cancellationToken);
        await Task.WhenAll(ytDlpTask, ffmpegTask);
        return await ytDlpTask && await ffmpegTask;
    }

    public async Task EnsureDependenciesAsync(
        IProgress<DependencyProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        progress?.Report(new DependencyProgress("SetupChecking"));

        if (!await ytDlp.ValidateAsync(cancellationToken))
        {
            progress?.Report(new DependencyProgress("SetupDownloadingYtDlp"));
            await ytDlp.EnsureAvailableAsync(cancellationToken, progress);
        }

        if (!await ffmpeg.ValidateAsync(cancellationToken))
        {
            progress?.Report(new DependencyProgress("SetupDownloadingFfmpeg"));
            await ffmpeg.EnsureAvailableAsync(cancellationToken, progress);
        }

        progress?.Report(new DependencyProgress("SetupVerifying"));

        if (!await ValidateDependenciesAsync(cancellationToken))
        {
            throw new YtDlpException(
                YtDlpErrorKind.Preparation,
                "Los componentes instalados no superan la validación final.");
        }

        progress?.Report(new DependencyProgress("SetupCompleted"));
        log.Info("Preparar componentes", ("Resultado", "Correcto"));
    }

    public async Task RepairDependenciesAsync(
        IProgress<DependencyProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        log.Info("Reparar componentes", ("Inicio", DateTimeOffset.Now));
        progress?.Report(new DependencyProgress("SetupRepairing"));
        await EnsureDependenciesAsync(progress, cancellationToken);
    }
}
