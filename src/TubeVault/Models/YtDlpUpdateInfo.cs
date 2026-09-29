namespace TubeVault;

internal sealed record YtDlpUpdateInfo(
    string LocalVersion,
    string AvailableVersion,
    bool IsUpdateAvailable);
