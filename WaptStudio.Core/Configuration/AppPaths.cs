using System;
using System.IO;
using WaptStudio.Core.Models;

namespace WaptStudio.Core.Configuration;

public static class AppPaths
{
    public static string BaseDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WaptStudio");

    public static string ConfigDirectory => Path.Combine(BaseDirectory, "config");

    public static string DataDirectory => Path.Combine(BaseDirectory, "data");

    public static string LogsDirectory => Path.Combine(BaseDirectory, "logs");

    public static string BackupsDirectory => Path.Combine(BaseDirectory, "backups");

    public static string SettingsFilePath => Path.Combine(ConfigDirectory, "appsettings.json");

    public static string HistoryDatabasePath => Path.Combine(DataDirectory, "history.db");

    public static string ResolveLogsDirectory(AppSettings? settings = null)
        => string.IsNullOrWhiteSpace(settings?.LogsDirectory) ? LogsDirectory : settings.LogsDirectory!;

    public static string ResolveBackupsDirectory(AppSettings? settings = null)
        => string.IsNullOrWhiteSpace(settings?.BackupsDirectory) ? BackupsDirectory : settings.BackupsDirectory!;

    public static void EnsureCreated(AppSettings? settings = null)
    {
        Directory.CreateDirectory(BaseDirectory);
        Directory.CreateDirectory(ConfigDirectory);
        Directory.CreateDirectory(DataDirectory);
        Directory.CreateDirectory(ResolveLogsDirectory(settings));
        Directory.CreateDirectory(ResolveBackupsDirectory(settings));
    }
}
