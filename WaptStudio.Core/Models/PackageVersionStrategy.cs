namespace WaptStudio.Core.Models;

public enum PackageVersionStrategy
{
    KeepCurrentVersion = 0,
    IncrementPackageRevision = 1,
    SetExplicitVersion = 2
}