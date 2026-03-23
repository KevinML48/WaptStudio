using System.Collections.Generic;

namespace WaptStudio.Core.Models;

public sealed class PackageUpdateResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string? BackupDirectory { get; set; }

    public string? PreviousPackageFolder { get; set; }

    public string? UpdatedPackageFolder { get; set; }

    public string? SuggestedPackageFolder { get; set; }

    public bool PackageFolderRenamed { get; set; }

    public PackageInfo? UpdatedPackageInfo { get; set; }

    public PackageSynchronizationPlan? SynchronizationPlan { get; set; }

    public List<string> UpdatedFiles { get; } = new();

    public List<string> ChangeSummaryLines { get; } = new();
}
