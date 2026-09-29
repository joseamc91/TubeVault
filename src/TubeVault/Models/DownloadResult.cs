namespace TubeVault;

internal sealed class DownloadResult
{
    public required string DestinationFolder { get; init; }

    public required IReadOnlyList<DownloadItemResult> Items { get; init; }

    public int DownloadedCount => Items.Count(item => item.Status == DownloadItemStatus.Downloaded);

    public int ExistingCount => Items.Count(item => item.Status == DownloadItemStatus.AlreadyExists);

    public int FailedCount => Items.Count(item => item.Status == DownloadItemStatus.Failed);

    public IReadOnlyList<string> FailedTitles => Items
        .Where(item => item.Status == DownloadItemStatus.Failed)
        .Select(item => item.Title)
        .ToList();
}
