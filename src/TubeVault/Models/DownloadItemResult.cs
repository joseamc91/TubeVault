namespace TubeVault;

internal sealed record DownloadItemResult(
    string Title,
    DownloadItemStatus Status,
    string? FilePath = null);

internal enum DownloadItemStatus
{
    Downloaded,
    AlreadyExists,
    Failed
}
