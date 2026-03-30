using System.Collections.Generic;

namespace WaptStudio.Core.Models;

public sealed class PackageSynchronizationPlan
{
    public string PackageId { get; set; } = string.Empty;

    public PackageVersionStrategy VersionStrategy { get; set; } = PackageVersionStrategy.KeepCurrentVersion;

    public string VersionStrategyLabel { get; set; } = string.Empty;

    public PackageFolderUpdateMode FolderUpdateMode { get; set; } = PackageFolderUpdateMode.UpdateCurrentFolder;

    public string FolderUpdateModeLabel { get; set; } = string.Empty;

    public string? SuggestedVersion { get; set; }

    public string? SuggestedVersionedPackageFolder { get; set; }

    public string? CurrentVersion { get; set; }

    public string? TargetVersion { get; set; }

    public string? CurrentInstallerName { get; set; }

    public string? TargetInstallerName { get; set; }

    public string? CurrentVisibleName { get; set; }

    public string? TargetVisibleName { get; set; }

    public string? CurrentDescription { get; set; }

    public string? TargetDescription { get; set; }

    public string? CurrentDescriptionFr { get; set; }

    public string? TargetDescriptionFr { get; set; }

    public string CurrentPackageFolder { get; set; } = string.Empty;

    public string TargetPackageFolder { get; set; } = string.Empty;

    public bool PackageFolderRenamePlanned { get; set; }

    public bool PackageFolderRenamePossible { get; set; }

    public bool PackageFolderClonePlanned { get; set; }

    public bool PackageFolderClonePossible { get; set; }

    public string ExpectedWaptFileName { get; set; } = string.Empty;

    public string? ExpectedWaptFileNameNote { get; set; }

    public string? CurrentInstallerType { get; set; }

    public string? TargetInstallerType { get; set; }

    public string? BackupDirectory { get; set; }

    public bool BackupWillBeCreated { get; set; }

    public List<string> SummaryLines { get; } = new();

    public List<string> Warnings { get; } = new();

    public List<string> FilesDeleted { get; } = new();

    public List<string> FilesModified { get; } = new();
}