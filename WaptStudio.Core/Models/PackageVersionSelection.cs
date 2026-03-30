namespace WaptStudio.Core.Models;

public sealed class PackageVersionSelection
{
    public PackageVersionStrategy Strategy { get; init; } = PackageVersionStrategy.KeepCurrentVersion;

    public string? ExplicitVersion { get; init; }

    public PackageFolderUpdateMode? FolderUpdateMode { get; init; }
}