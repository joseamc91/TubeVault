namespace TubeVault;

internal sealed class AppSettings
{
    public string LastDestinationFolder { get; set; } = string.Empty;

    public AudioQuality AudioQuality { get; set; } = AudioQuality.Medium;

    public DateTimeOffset? LastYtDlpUpdateCheck { get; set; }

    public DateTimeOffset? LastFfmpegUpdateCheck { get; set; }

    public DateTimeOffset? LastTubeVaultUpdateCheck { get; set; }

    public AppTheme Theme { get; set; } = AppTheme.Light;

    public AppLanguage Language { get; set; } = AppLanguage.Spanish;
}
