namespace TubeVault;

internal sealed record DependencyProgress(
    string TextKey,
    long BytesDownloaded = 0,
    long? TotalBytes = null,
    bool IsDownload = false);
