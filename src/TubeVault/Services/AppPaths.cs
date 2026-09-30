namespace TubeVault;

internal static class AppPaths
{
    public static string AppDirectory { get; } =
        Path.TrimEndingDirectorySeparator(Path.GetFullPath(AppContext.BaseDirectory));

    public static bool IsPortable { get; } =
        File.Exists(Path.Combine(AppDirectory, "portable.flag"));

    public static string DataDirectory { get; } = IsPortable
        ? Path.Combine(AppDirectory, "data")
        : Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TubeVault");

    public static string ToolsDirectory { get; } = Path.Combine(DataDirectory, "tools");

    public static string ConfigDirectory { get; } = Path.Combine(DataDirectory, "config");

    public static string LogsDirectory { get; } = Path.Combine(DataDirectory, "logs");

    public static string SettingsFilePath { get; } =
        Path.Combine(ConfigDirectory, "settings.json");

    public static string YtDlpPath { get; } = Path.Combine(ToolsDirectory, "yt-dlp.exe");

    public static string FfmpegPath { get; } = Path.Combine(ToolsDirectory, "ffmpeg.exe");

    public static string FfprobePath { get; } = Path.Combine(ToolsDirectory, "ffprobe.exe");
}
