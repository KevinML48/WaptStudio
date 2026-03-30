using System;
using System.Collections.Generic;
using System.IO;
using WaptStudio.Core.Configuration;
using WaptStudio.Core.Models;

namespace WaptStudio.Core.Utilities;

public static class WaptEnvironmentDetector
{
    private static readonly string[] WaptEnvironmentRoots = ["WAPT_HOME", "WAPT_ROOT"];
    private static readonly string[] ProgramFilesVariables = ["ProgramFiles", "ProgramFiles(x86)", "ProgramW6432"];
    private static readonly string[] WaptRelativeExecutablePaths =
    [
        @"wapt\wapt-get.exe",
        @"tis-wapt\wapt-get.exe",
        @"WAPT\wapt-get.exe"
    ];

    public static WaptEnvironmentInfo Inspect(
        AppSettings? settings,
        Func<string, bool>? fileExists = null,
        Func<string, string?>? getEnvironmentVariable = null)
    {
        settings ??= new AppSettings();
        fileExists ??= File.Exists;
        getEnvironmentVariable ??= Environment.GetEnvironmentVariable;

        var checkedPaths = new List<string>();
        var configuredPath = NormalizeConfiguredPath(settings.WaptExecutablePath);
        var effectivePath = string.Empty;
        var autoDetectedPath = string.Empty;
        var detectionSource = "not-found";

        if (TryResolveConfiguredPath(configuredPath, fileExists, getEnvironmentVariable, checkedPaths, out effectivePath, out detectionSource))
        {
            autoDetectedPath = detectionSource == "configuration" ? string.Empty : effectivePath;
        }
        else if (TryResolveAutomaticPath(CommandExecutionResult.DefaultExecutableName, fileExists, getEnvironmentVariable, checkedPaths, out effectivePath, out detectionSource))
        {
            autoDetectedPath = effectivePath;
        }

        var signingKeyPath = string.IsNullOrWhiteSpace(settings.SigningKeyPath) ? null : settings.SigningKeyPath;

        return new WaptEnvironmentInfo
        {
            ConfiguredExecutablePath = configuredPath,
            EffectiveExecutablePath = string.IsNullOrWhiteSpace(effectivePath) ? null : effectivePath,
            AutoDetectedExecutablePath = string.IsNullOrWhiteSpace(autoDetectedPath) ? null : autoDetectedPath,
            ExecutableDetectionSource = detectionSource,
            IsWaptExecutableAvailable = !string.IsNullOrWhiteSpace(effectivePath),
            CheckedExecutablePaths = checkedPaths,
            BaseDirectory = AppPaths.BaseDirectory,
            ConfigDirectory = AppPaths.ConfigDirectory,
            DataDirectory = AppPaths.DataDirectory,
            CacheDirectory = AppPaths.ResolveCacheDirectory(settings),
            LogsDirectory = AppPaths.ResolveLogsDirectory(settings),
            BackupsDirectory = AppPaths.ResolveBackupsDirectory(settings),
            SettingsFilePath = AppPaths.SettingsFilePath,
            HistoryDatabasePath = AppPaths.HistoryDatabasePath,
            SigningKeyPath = signingKeyPath,
            IsSigningKeyAvailable = !string.IsNullOrWhiteSpace(signingKeyPath) && fileExists(signingKeyPath)
        };
    }

    public static string ResolveExecutablePath(AppSettings? settings)
        => Inspect(settings).EffectiveExecutablePath ?? settings?.WaptExecutablePath ?? CommandExecutionResult.DefaultExecutableName;

    private static bool TryResolveConfiguredPath(
        string? configuredPath,
        Func<string, bool> fileExists,
        Func<string, string?> getEnvironmentVariable,
        ICollection<string> checkedPaths,
        out string resolvedPath,
        out string detectionSource)
    {
        resolvedPath = string.Empty;
        detectionSource = "not-found";

        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return false;
        }

        AddCheckedPath(checkedPaths, configuredPath);
        if (fileExists(configuredPath))
        {
            resolvedPath = configuredPath;
            detectionSource = "configuration";
            return true;
        }

        if (IsBareExecutableName(configuredPath)
            && TryResolveAutomaticPath(configuredPath, fileExists, getEnvironmentVariable, checkedPaths, out resolvedPath, out detectionSource))
        {
            return true;
        }

        return false;
    }

    private static bool TryResolveAutomaticPath(
        string executableName,
        Func<string, bool> fileExists,
        Func<string, string?> getEnvironmentVariable,
        ICollection<string> checkedPaths,
        out string resolvedPath,
        out string detectionSource)
    {
        resolvedPath = string.Empty;
        detectionSource = "not-found";

        foreach (var candidate in BuildPathCandidates(executableName, getEnvironmentVariable))
        {
            AddCheckedPath(checkedPaths, candidate.Path);
            if (!fileExists(candidate.Path))
            {
                continue;
            }

            resolvedPath = candidate.Path;
            detectionSource = candidate.Source;
            return true;
        }

        return false;
    }

    private static IEnumerable<(string Path, string Source)> BuildPathCandidates(string executableName, Func<string, string?> getEnvironmentVariable)
    {
        foreach (var pathEntry in SplitPathVariable(getEnvironmentVariable("PATH")))
        {
            yield return (Path.Combine(pathEntry, executableName), "path");
        }

        foreach (var variableName in WaptEnvironmentRoots)
        {
            var value = getEnvironmentVariable(variableName);
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            yield return (Path.Combine(value, executableName), "common-location");
            yield return (Path.Combine(value, "wapt", executableName), "common-location");
        }

        foreach (var variableName in ProgramFilesVariables)
        {
            var root = getEnvironmentVariable(variableName);
            if (string.IsNullOrWhiteSpace(root))
            {
                continue;
            }

            foreach (var relativePath in WaptRelativeExecutablePaths)
            {
                yield return (Path.Combine(root, relativePath), "common-location");
            }
        }
    }

    private static IEnumerable<string> SplitPathVariable(string? pathVariable)
    {
        if (string.IsNullOrWhiteSpace(pathVariable))
        {
            yield break;
        }

        foreach (var entry in pathVariable.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!string.IsNullOrWhiteSpace(entry))
            {
                yield return entry;
            }
        }
    }

    private static string? NormalizeConfiguredPath(string? configuredPath)
        => string.IsNullOrWhiteSpace(configuredPath) ? null : configuredPath.Trim();

    private static bool IsBareExecutableName(string path)
        => !path.Contains(Path.DirectorySeparatorChar)
            && !path.Contains(Path.AltDirectorySeparatorChar)
            && !Path.IsPathRooted(path);

    private static void AddCheckedPath(ICollection<string> checkedPaths, string candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return;
        }

        foreach (var existing in checkedPaths)
        {
            if (string.Equals(existing, candidate, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        checkedPaths.Add(candidate);
    }
}