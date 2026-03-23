using System.Collections.Generic;

namespace WaptStudio.Core.Models;

public sealed class PackageUpdateResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string? BackupDirectory { get; set; }

    public PackageInfo? UpdatedPackageInfo { get; set; }

    public List<string> UpdatedFiles { get; } = new();
}
