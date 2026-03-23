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
    public async Task PreviewReplacementAsync_BuildsCoherentSynchronizationPlan()
    {
        var packageFolder = Path.Combine(_rootDirectory, "cd48-waptstudio_24.09.00.0_Windows_DEV-wapt");
        Directory.CreateDirectory(packageFolder);

        var oldInstaller = Path.Combine(packageFolder, "7z2409.msi");
        var newInstallerSource = Path.Combine(_rootDirectory, "7z2501.msi");

        await File.WriteAllTextAsync(oldInstaller, "old");
        await File.WriteAllTextAsync(newInstallerSource, "new");
        await File.WriteAllTextAsync(Path.Combine(packageFolder, "setup.py"), "package = 'cd48-waptstudio'\nversion = '24.09.00.0'\nprint(\"Installing: 7z2409.msi\")\ninstall_msi_if_needed('7z2409.msi')\n");
        await File.WriteAllTextAsync(Path.Combine(packageFolder, "control"), "package: cd48-waptstudio\nversion: 24.09.00.0\nname: WaptStudio 24.09.00.0\ndescription: WaptStudio package 24.09.00.0\ndescription_fr: WaptStudio package 24.09.00.0 FR\nfilename: 7z2409.msi\n");

        var inspector = new PackageInspectorService();
        var packageInfo = await inspector.AnalyzePackageAsync(packageFolder);
        var service = new PackageUpdateService(inspector, new TestSettingsService());

        var plan = await service.PreviewReplacementAsync(packageInfo, newInstallerSource);

        Assert.Equal("cd48-waptstudio", plan.PackageId);
        Assert.Equal("24.09.00.0", plan.CurrentVersion);
        Assert.Equal("2501", plan.TargetVersion);
        Assert.Equal("7z2409.msi", plan.CurrentInstallerName);
        Assert.Equal("7z2501.msi", plan.TargetInstallerName);
        Assert.Equal("WaptStudio 24.09.00.0", plan.CurrentVisibleName);
        Assert.Equal("WaptStudio 2501", plan.TargetVisibleName);
        Assert.Equal(Path.Combine(_rootDirectory, "cd48-waptstudio_2501_Windows_DEV-wapt"), plan.TargetPackageFolder);
        Assert.Equal("cd48-waptstudio_2501.wapt", plan.ExpectedWaptFileName);
    }

    [Fact]
    public async Task ReplaceInstallerAsync_ReplacesInstallerAndUpdatesFiles()
    {
        var packageFolder = Path.Combine(_rootDirectory, "tis-package_1.0.0-wapt");
        Directory.CreateDirectory(packageFolder);

        var oldInstaller = Path.Combine(packageFolder, "app-1.0.0.msi");
        var newInstallerSource = Path.Combine(_rootDirectory, "app-2.0.0.msi");

        await File.WriteAllTextAsync(oldInstaller, "old");
        await File.WriteAllTextAsync(newInstallerSource, "new");
        await File.WriteAllTextAsync(Path.Combine(packageFolder, "setup.py"), "package = 'tis.package'\nversion = '1.0.0'\nprint(\"Installing: app-1.0.0.msi version 1.0.0\")\ninstall_msi_if_needed('app-1.0.0.msi')\n");
        await File.WriteAllTextAsync(Path.Combine(packageFolder, "control"), "package: tis.package\nversion: 1.0.0\nname: TIS Package 1.0.0\ndescription: Application TIS Package version 1.0.0\ndescription_fr: Application TIS Package version 1.0.0 FR\nfilename: app-1.0.0.msi\n");

        var inspector = new PackageInspectorService();
        var packageInfo = await inspector.AnalyzePackageAsync(packageFolder);
        var service = new PackageUpdateService(inspector, new TestSettingsService());

        var result = await service.ReplaceInstallerAsync(packageInfo, newInstallerSource);
        var updatedPackageFolder = Path.Combine(_rootDirectory, "tis-package_2.0.0-wapt");

        Assert.True(result.Success);
        Assert.NotNull(result.UpdatedPackageInfo);
        Assert.Equal("2.0.0", result.UpdatedPackageInfo!.Version);
        Assert.EndsWith("app-2.0.0.msi", result.UpdatedPackageInfo.InstallerPath, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(updatedPackageFolder, result.UpdatedPackageFolder);
        Assert.True(result.PackageFolderRenamed);
        Assert.Equal(updatedPackageFolder, result.UpdatedPackageInfo.PackageFolder);
        Assert.Equal("tis.package", result.UpdatedPackageInfo.PackageName);
        Assert.False(File.Exists(oldInstaller));
        Assert.True(File.Exists(Path.Combine(updatedPackageFolder, "app-2.0.0.msi")));

        var setupContent = await File.ReadAllTextAsync(Path.Combine(updatedPackageFolder, "setup.py"));
        var controlContent = await File.ReadAllTextAsync(Path.Combine(updatedPackageFolder, "control"));

        Assert.Contains("2.0.0", setupContent);
        Assert.Contains("app-2.0.0.msi", setupContent);
        Assert.Contains("Installing: app-2.0.0.msi version 2.0.0", setupContent);
        Assert.Contains("2.0.0", controlContent);
        Assert.Contains("app-2.0.0.msi", controlContent);
        Assert.Contains("name: TIS Package 2.0.0", controlContent);
        Assert.Contains("description: Application TIS Package version 2.0.0", controlContent);
        Assert.Contains("description_fr: Application TIS Package version 2.0.0 FR", controlContent);
        Assert.Contains("package: tis.package", controlContent);
        Assert.True(File.Exists(Path.Combine(updatedPackageFolder, "app-1.0.0.msi.bak")));
        Assert.Contains(result.ChangeSummaryLines, line => line.Contains("Version: 1.0.0 -> 2.0.0", StringComparison.Ordinal));
        Assert.Contains(result.ChangeSummaryLines, line => line.Contains("Nom: TIS Package 1.0.0 -> TIS Package 2.0.0", StringComparison.Ordinal));
        Assert.Contains(result.ChangeSummaryLines, line => line.Contains("Description: Application TIS Package version 1.0.0 -> Application TIS Package version 2.0.0", StringComparison.Ordinal));
        Assert.Contains(result.ChangeSummaryLines, line => line.Contains("Dossier:", StringComparison.Ordinal));
        Assert.NotNull(result.SynchronizationPlan);
        Assert.Equal("tis.package_2.0.0.wapt", result.SynchronizationPlan!.ExpectedWaptFileName);
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
