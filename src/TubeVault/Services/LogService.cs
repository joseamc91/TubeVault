using System.Text;

namespace TubeVault;

internal sealed class LogService
{
    private const int MaximumLogFiles = 15;
    private readonly object writeLock = new();

    public LogService()
    {
        PrepareLogDirectory();
    }

    public void Info(string action, params (string Name, object? Value)[] details)
    {
        Write("INFO", action, details);
    }

    public void Error(string action, params (string Name, object? Value)[] details)
    {
        Write("ERROR", action, details);
    }

    private void PrepareLogDirectory()
    {
        try
        {
            Directory.CreateDirectory(AppPaths.LogsDirectory);
            File.AppendAllText(GetCurrentLogPath(), string.Empty, Encoding.UTF8);
            DeleteOldLogs();
        }
        catch
        {
            // Un problema de log nunca debe impedir que TubeVault se abra.
        }
    }

    private void DeleteOldLogs()
    {
        var logs = Directory
            .GetFiles(AppPaths.LogsDirectory, "TubeVault_????-??-??.log")
            .OrderBy(path => Path.GetFileName(path), StringComparer.Ordinal)
            .ToList();

        foreach (var oldLog in logs.Take(Math.Max(0, logs.Count - MaximumLogFiles)))
        {
            File.Delete(oldLog);
        }
    }

    private void Write(
        string level,
        string action,
        IEnumerable<(string Name, object? Value)> details)
    {
        try
        {
            var text = new StringBuilder()
                .Append('[')
                .Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                .Append("] ")
                .AppendLine(level)
                .Append("Acción: ")
                .AppendLine(action);

            foreach (var (name, value) in details)
            {
                if (value is null)
                {
                    continue;
                }

                text.Append(name).Append(": ").AppendLine(value.ToString());
            }

            text.AppendLine();

            lock (writeLock)
            {
                File.AppendAllText(GetCurrentLogPath(), text.ToString(), Encoding.UTF8);
            }
        }
        catch
        {
            // El registro es auxiliar: sus fallos se ignoran deliberadamente.
        }
    }

    private static string GetCurrentLogPath()
    {
        var fileName = $"TubeVault_{DateTime.Now:yyyy-MM-dd}.log";
        return Path.Combine(AppPaths.LogsDirectory, fileName);
    }
}
