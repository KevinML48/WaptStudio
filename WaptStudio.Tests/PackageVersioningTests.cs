using WaptStudio.Core.Utilities;
using Xunit;

namespace WaptStudio.Tests;

public sealed class PackageVersioningTests
{
    [Fact]
    public void TryIncrementPackageRevision_IncrementsExistingRevision()
    {
        var success = PackageVersioning.TryIncrementPackageRevision("11.0.0-1", out var nextVersion, out var errorMessage);

        Assert.True(success, errorMessage);
        Assert.Equal("11.0.0-2", nextVersion);
    }

    [Fact]
    public void TryIncrementPackageRevision_AddsRevisionWhenMissing()
    {
        var success = PackageVersioning.TryIncrementPackageRevision("9.1.1", out var nextVersion, out var errorMessage);

        Assert.True(success, errorMessage);
        Assert.Equal("9.1.1-1", nextVersion);
    }

    [Fact]
    public void TryNormalizeExplicitVersion_StartsAtRevisionOne_WhenCurrentVersionUsesRevision()
    {
        var success = PackageVersioning.TryNormalizeExplicitVersion("11.0.0", "9.1.1-1", out var normalizedVersion, out var errorMessage);

        Assert.True(success, errorMessage);
        Assert.Equal("11.0.0-1", normalizedVersion);
    }

    [Fact]
    public void TryNormalizeExplicitVersion_RejectsInvalidFormat()
    {
        var success = PackageVersioning.TryNormalizeExplicitVersion("v11", "9.1.1-1", out _, out var errorMessage);

        Assert.False(success);
        Assert.Contains("Format de version invalide", errorMessage);
    }
}