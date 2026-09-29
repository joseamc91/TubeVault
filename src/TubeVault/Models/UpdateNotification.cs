namespace TubeVault;

// Centraliza la decisión que comparten los indicadores de MainForm y Ajustes.
internal static class UpdateNotification
{
    public static bool HasPendingUpdate(
        YtDlpUpdateInfo? ytDlp,
        FfmpegUpdateInfo? ffmpeg)
    {
        return ytDlp?.IsUpdateAvailable == true
               || ffmpeg?.IsUpdateAvailable == true;
    }
}
