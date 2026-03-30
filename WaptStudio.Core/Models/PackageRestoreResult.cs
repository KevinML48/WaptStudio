using System.Collections.Generic;

namespace WaptStudio.Core.Models;

public sealed class PackageRestoreResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string? RestoredFromBackupDirectory { get; set; }

    public List<string> RestoredFiles { get; } = new();
}