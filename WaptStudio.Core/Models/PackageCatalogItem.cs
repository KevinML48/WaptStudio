using System;

namespace WaptStudio.Core.Models;

public sealed class PackageCatalogItem
{
    public string PackageId { get; set; } = string.Empty;

    public string VisibleName { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public PackageCategory Category { get; set; }

    public string Maturity { get; set; } = "Inconnue";

    public ReadinessVerdict ReadinessVerdict { get; set; }

    public string ReadinessLabel { get; set; } = string.Empty;

    public DateTime LastModifiedUtc { get; set; }

    public string PackageFolder { get; set; } = string.Empty;

    public string PrimaryInstallerName { get; set; } = string.Empty;

    public PackageInfo PackageInfo { get; set; } = new();
}