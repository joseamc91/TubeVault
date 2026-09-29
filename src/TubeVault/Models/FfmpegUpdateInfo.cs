namespace TubeVault;

internal sealed record FfmpegUpdateInfo(
    string LocalVersion,
    string AvailableVersion,
    bool IsUpdateAvailable);