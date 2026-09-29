namespace TubeVault;

internal sealed record DownloadProgress(
    int ItemIndex,
    int TotalItems,
    double ItemPercent,
    double GlobalPercent,
    string Title,
    bool IsValidating = false);
