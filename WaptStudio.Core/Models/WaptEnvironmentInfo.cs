using System;
using System.Collections.Generic;

namespace WaptStudio.Core.Models;

public sealed class WaptEnvironmentInfo
{
    public string? ConfiguredExecutablePath { get; init; }

    public string? EffectiveExecutablePath { get; init; }

    public string? AutoDetectedExecutablePath { get; init; }

    public string ExecutableDetectionSource { get; init; } = "not-found";

    public bool IsWaptExecutableAvailable { get; init; }

    public IReadOnlyList<string> CheckedExecutablePaths { get; init; } = Array.Empty<string>();

    public string BaseDirectory { get; init; } = string.Empty;

    public string ConfigDirectory { get; init; } = string.Empty;

    public string DataDirectory { get; init; } = string.Empty;

    public string CacheDirectory { get; init; } = string.Empty;

    public string LogsDirectory { get; init; } = string.Empty;

    public string BackupsDirectory { get; init; } = string.Empty;

    public string SettingsFilePath { get; init; } = string.Empty;

    public string HistoryDatabasePath { get; init; } = string.Empty;

    public string? SigningKeyPath { get; init; }

    public bool IsSigningKeyAvailable { get; init; }
}