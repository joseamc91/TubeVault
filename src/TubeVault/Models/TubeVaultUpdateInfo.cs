namespace TubeVault;

internal sealed record TubeVaultUpdateInfo(
    string LocalVersion,
    string AvailableVersion,
    string ReleaseUrl,
    bool IsUpdateAvailable,
    bool IsLocalVersionNewer)
{
    public DateTimeOffset CheckedAt { get; init; } = DateTimeOffset.UtcNow;
}
