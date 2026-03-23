using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WaptStudio.Core.Models;
using WaptStudio.Core.Services;
using WaptStudio.Core.Services.Interfaces;
using Xunit;

namespace WaptStudio.Tests;

public sealed class PackageUpdateServiceTests : IDisposable
{
    private readonly string _rootDirectory;

    public PackageUpdateServiceTests()
    {
        _rootDirectory = Path.Combine(Path.GetTempPath(), "WaptStudio.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_rootDirectory);
    }

    [Fact]
    public async Task ReplaceInstallerAsync_ReplacesInstallerAndUpdatesFiles()
    {
        var packageFolder = Path.Combine(_rootDirectory, "pkg");
        Directory.CreateDirectory(packageFolder);

        var oldInstaller = Path.Combine(packageFolder, "app-1.0.0.msi");
        var newInstallerSource = Path.Combine(_rootDirectory, "app-2.0.0.msi");

        await File.WriteAllTextAsync(oldInstaller, "old");
        await File.WriteAllTextAsync(newInstallerSource, "new");
        await File.WriteAllTextAsync(Path.Combine(packageFolder, "setup.py"), "package = 'tis.package'\nversion = '1.0.0'\ninstaller = 'app-1.0.0.msi'\n");
        await File.WriteAllTextAsync(Path.Combine(packageFolder, "control"), "package: tis.package\nversion: 1.0.0\nfilename: app-1.0.0.msi\n");

        var inspector = new PackageInspectorService();
        var packageInfo = await inspector.AnalyzePackageAsync(packageFolder);
        var service = new PackageUpdateService(inspector, new TestSettingsService());

        var result = await service.ReplaceInstallerAsync(packageInfo, newInstallerSource);

        Assert.True(result.Success);
        Assert.NotNull(result.UpdatedPackageInfo);
        Assert.Equal("2.0.0", result.UpdatedPackageInfo!.Version);
        Assert.EndsWith("app-2.0.0.msi", result.UpdatedPackageInfo.InstallerPath, StringComparison.OrdinalIgnoreCase);
        Assert.False(File.Exists(oldInstaller));
        Assert.True(File.Exists(Path.Combine(packageFolder, "app-2.0.0.msi")));

        var setupContent = await File.ReadAllTextAsync(Path.Combine(packageFolder, "setup.py"));
        var controlContent = await File.ReadAllTextAsync(Path.Combine(packageFolder, "control"));

        Assert.Contains("2.0.0", setupContent);
        Assert.Contains("app-2.0.0.msi", setupContent);
        Assert.Contains("2.0.0", controlContent);
        Assert.Contains("app-2.0.0.msi", controlContent);
        Assert.True(File.Exists(Path.Combine(packageFolder, "app-1.0.0.msi.bak")));
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootDirectory))
        {
            Directory.Delete(_rootDirectory, recursive: true);
        }
    }

    private sealed class TestSettingsService : ISettingsService
    {
        public Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new AppSettings { CreateBackups = false });

        public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
