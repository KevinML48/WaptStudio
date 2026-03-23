using System;

namespace WaptStudio.Core.Models;

public sealed class HistoryEntry
{
    public long Id { get; set; }

    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;

    public string ActionType { get; set; } = string.Empty;

    public string PackageFolder { get; set; } = string.Empty;

    public string? PackageName { get; set; }

    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string? ExecutedCommand { get; set; }

    public string? StandardOutput { get; set; }

    public string? StandardError { get; set; }

    public int ExitCode { get; set; }

    public int DurationMilliseconds { get; set; }

    public string WindowsUser { get; set; } = Environment.UserName;

    public string? VersionBefore { get; set; }

    public string? VersionAfter { get; set; }
}
