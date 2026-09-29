using System.Text.Json;
using System.Text.Json.Serialization;

namespace TubeVault;

internal sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly object settingsLock = new();
    private readonly LogService log;
    private readonly string settingsPath = AppPaths.SettingsFilePath;

    public SettingsService(LogService log)
    {
        this.log = log;
    }

    public string LoadDestinationFolder()
    {
        lock (settingsLock)
        {
            var settings = LoadSettings();

            if (Directory.Exists(settings.LastDestinationFolder))
            {
                return Path.GetFullPath(settings.LastDestinationFolder);
            }

            settings.LastDestinationFolder = GetDefaultDestinationFolder();
            SaveSettings(settings);
            return settings.LastDestinationFolder;
        }
    }

    public AudioQuality LoadAudioQuality()
    {
        lock (settingsLock)
        {
            var settings = LoadSettings();
            settings.AudioQuality = Enum.IsDefined(settings.AudioQuality)
                ? settings.AudioQuality
                : AudioQuality.Medium;
            SaveSettings(settings);
            return settings.AudioQuality;
        }
    }

    public AppTheme LoadTheme()
    {
        lock (settingsLock)
        {
            var settings = LoadSettings();
            settings.Theme = Enum.IsDefined(settings.Theme) ? settings.Theme : AppTheme.Light;
            SaveSettings(settings);
            return settings.Theme;
        }
    }

    public AppLanguage LoadLanguage()
    {
        lock (settingsLock)
        {
            var settings = LoadSettings();
            settings.Language = Enum.IsDefined(settings.Language)
                ? settings.Language
                : AppLanguage.Spanish;
            SaveSettings(settings);
            return settings.Language;
        }
    }

    public bool ShouldCheckYtDlpUpdate(DateTimeOffset now)
    {
        lock (settingsLock)
        {
            var lastCheck = LoadSettings().LastYtDlpUpdateCheck;
            return lastCheck is null || now - lastCheck.Value >= TimeSpan.FromDays(10);
        }
    }

    public DateTimeOffset? LoadLastYtDlpUpdateCheck()
    {
        lock (settingsLock)
        {
            return LoadSettings().LastYtDlpUpdateCheck;
        }
    }

    public bool ShouldCheckFfmpegUpdate(DateTimeOffset now)
    {
        lock (settingsLock)
        {
            var lastCheck = LoadSettings().LastFfmpegUpdateCheck;
            return lastCheck is null || now - lastCheck.Value >= TimeSpan.FromDays(30);
        }
    }

    public DateTimeOffset? LoadLastFfmpegUpdateCheck()
    {
        lock (settingsLock)
        {
            return LoadSettings().LastFfmpegUpdateCheck;
        }
    }

    public void SaveDestinationFolder(string folderPath)
    {
        UpdateSettings(settings => settings.LastDestinationFolder = Path.GetFullPath(folderPath));
    }

    public void SaveAudioQuality(AudioQuality quality)
    {
        UpdateSettings(settings => settings.AudioQuality = Enum.IsDefined(quality)
            ? quality
            : AudioQuality.Medium);
    }

    public void SaveTheme(AppTheme theme)
    {
        UpdateSettings(settings => settings.Theme = Enum.IsDefined(theme) ? theme : AppTheme.Light);
    }

    public void SaveLanguage(AppLanguage language)
    {
        UpdateSettings(settings => settings.Language = Enum.IsDefined(language)
            ? language
            : AppLanguage.Spanish);
    }

    public void SaveYtDlpUpdateCheck(DateTimeOffset checkedAt)
    {
        UpdateSettings(settings => settings.LastYtDlpUpdateCheck = checkedAt);
    }

    public void SaveFfmpegUpdateCheck(DateTimeOffset checkedAt)
    {
        UpdateSettings(settings => settings.LastFfmpegUpdateCheck = checkedAt);
    }

    private void UpdateSettings(Action<AppSettings> update)
    {
        lock (settingsLock)
        {
            try
            {
                var settings = LoadSettings();
                update(settings);
                SaveSettings(settings);
            }
            catch (Exception exception)
            {
                log.Error(
                    "Guardar configuración",
                    ("Archivo", settingsPath),
                    ("Detalle", exception.Message));
            }
        }
    }

    private AppSettings LoadSettings()
    {
        try
        {
            if (File.Exists(settingsPath))
            {
                var json = File.ReadAllText(settingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions)
                       ?? new AppSettings();
            }
        }
        catch (Exception exception)
        {
            log.Error(
                "Cargar configuración",
                ("Archivo", settingsPath),
                ("Detalle", exception.Message));
        }

        return new AppSettings();
    }

    private void SaveSettings(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(AppPaths.ConfigDirectory);
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(settingsPath, json);
        }
        catch (Exception exception)
        {
            log.Error(
                "Guardar configuración",
                ("Archivo", settingsPath),
                ("Detalle", exception.Message));
        }
    }

    private static string GetDefaultDestinationFolder()
    {
        var musicFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);

        if (Directory.Exists(musicFolder))
        {
            return musicFolder;
        }

        var userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Directory.Exists(userFolder) ? userFolder : AppPaths.DataDirectory;
    }
}
