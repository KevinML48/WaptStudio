using System;
using System.IO;
using System.Threading.Tasks;
using WaptStudio.Core.Services;
using Xunit;

namespace WaptStudio.Tests;

public sealed class PackageInspectorServiceTests : IDisposable
{
    private readonly string _rootDirectory;

    public PackageInspectorServiceTests()
    {
        _rootDirectory = Path.Combine(Path.GetTempPath(), "WaptStudio.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_rootDirectory);
    }

    [Fact]
    public async Task AnalyzePackageAsync_DetectsMetadataAndInstaller()
    {
        var packageFolder = Path.Combine(_rootDirectory, "pkg");
        Directory.CreateDirectory(packageFolder);

        await File.WriteAllTextAsync(Path.Combine(packageFolder, "setup.py"), "package = 'tis.package'\nversion = '1.2.3'\n");
        await File.WriteAllTextAsync(Path.Combine(packageFolder, "control"), "package: tis.package\nversion: 1.2.3\n");
        await File.WriteAllTextAsync(Path.Combine(packageFolder, "installer.msi"), "binary-placeholder");

        var service = new PackageInspectorService();
        var result = await service.AnalyzePackageAsync(packageFolder);

        Assert.Equal("tis.package", result.PackageName);
        Assert.Equal("1.2.3", result.Version);
        Assert.NotNull(result.InstallerPath);
        Assert.Equal("MSI", result.InstallerType);
        Assert.True(result.HasSetupPy);
        Assert.True(result.HasControlFile);
        Assert.True(result.HasInstaller);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootDirectory))
        {
            Directory.Delete(_rootDirectory, recursive: true);
        }
    }
}
