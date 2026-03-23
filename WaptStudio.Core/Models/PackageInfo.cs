using System.Collections.Generic;

namespace WaptStudio.Core.Models;

public sealed class PackageInfo
{
    public string PackageFolder { get; set; } = string.Empty;

    public string? PackageName { get; set; }

    public string? VisibleName { get; set; }

    public string? Description { get; set; }

    public string? DescriptionFr { get; set; }

    public string? Version { get; set; }

    public string? SetupPyPath { get; set; }

    public string? ControlFilePath { get; set; }

    public string? InstallerPath { get; set; }

    public string? InstallerType { get; set; }

    public string? ReferencedInstallerName { get; set; }

    public bool HasSetupPy => !string.IsNullOrWhiteSpace(SetupPyPath);

    public bool HasControlFile => !string.IsNullOrWhiteSpace(ControlFilePath);

    public bool HasInstaller => !string.IsNullOrWhiteSpace(InstallerPath);

    public List<string> DetectedExecutables { get; } = new();

    public List<string> Warnings { get; } = new();
}
