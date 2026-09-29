namespace TubeVault;

internal sealed class MediaInfo
{
    public required MediaType Type { get; init; }

    public required string Title { get; init; }

    public required string Creator { get; init; }

    public required string SourceUrl { get; init; }

    public double? DurationSeconds { get; init; }

    public DateOnly? PublicationDate { get; init; }

    public string? ThumbnailUrl { get; init; }

    public IReadOnlyList<string> ThumbnailUrls { get; init; } = [];

    public IReadOnlyList<PlaylistItemInfo> Items { get; init; } = [];
}

internal enum MediaType
{
    Video,
    Playlist
}
