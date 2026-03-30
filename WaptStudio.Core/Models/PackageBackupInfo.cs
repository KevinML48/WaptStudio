using System;
using System.Collections.Generic;

namespace WaptStudio.Core.Models;

public sealed class PackageBackupInfo
{
    public string PackageKey { get; set; } = string.Empty;

    public string SourcePackageFolder { get; set; } = string.Empty;

    public string BackupDirectory { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public string Reason { get; set; } = string.Empty;

    public List<string> Files { get; } = new();
}