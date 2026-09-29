namespace TubeVault;

internal sealed record PlaylistItemInfo(
    int Index,
    string Title,
    double? DurationSeconds,
    string SourceUrl);
