using System;
using System.IO;
using WaptStudio.Core.Models;

namespace WaptStudio.Core.Configuration;

public static class AppPaths
{
    public const string BaseDirectoryOverrideEnvironmentVariable = "WAPTSTUDIO_HOME";

    public static string BaseDirectory => ResolveBaseDirectory();

    public static string ConfigDirectory => Path.Combine(BaseDirectory, "config");

    public static string DataDirectory => Path.Combine(BaseDirectory, "data");

    public static string CacheDirectory => Path.Combine(BaseDirectory, "cache");

    public static string LogsDirectory => Path.Combine(BaseDirectory, "logs");

    public static string BackupsDirectory => Path.Combine(BaseDirectory, "backups");

    public static string SettingsFilePath => Path.Combine(ConfigDirectory, "appsettings.json");

    public static string HistoryDatabasePath => Path.Combine(DataDirectory, "history.db");

    public static string StartupErrorLogPath => Path.Combine(BaseDirectory, "startup-error.log");

    public static string ResolveLogsDirectory(AppSettings? settings = null)
        => string.IsNullOrWhiteSpace(settings?.LogsDirectory) ? LogsDirectory : settings.LogsDirectory!;

    public static string ResolveBackupsDirectory(AppSettings? settings = null)
        => string.IsNullOrWhiteSpace(settings?.BackupsDirectory) ? BackupsDirectory : settings.BackupsDirectory!;

    public static string ResolveCacheDirectory(AppSettings? settings = null)
        => string.IsNullOrWhiteSpace(settings?.CacheDirectory) ? CacheDirectory : settings.CacheDirectory!;

    public static void EnsureCreated(AppSettings? settings = null)
    {
        Directory.CreateDirectory(BaseDirectory);
        Directory.CreateDirectory(ConfigDirectory);
        Directory.CreateDirectory(DataDirectory);
        Directory.CreateDirectory(ResolveCacheDirectory(settings));
        Directory.CreateDirectory(ResolveLogsDirectory(settings));
        Directory.CreateDirectory(ResolveBackupsDirectory(settings));
    }

    private static string ResolveBaseDirectory()
    {
        var overrideDirectory = Environment.GetEnvironmentVariable(BaseDirectoryOverrideEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(overrideDirectory))
        {
            return Path.GetFullPath(overrideDirectory);
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WaptStudio");
    }
}
