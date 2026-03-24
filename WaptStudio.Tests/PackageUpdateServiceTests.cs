using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WaptStudio.Core.Models;
using WaptStudio.Core.Configuration;
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
        var settingsService = new TestSettingsService(new AppSettings { CreateBackups = true, BackupsDirectory = Path.Combine(_rootDirectory, "backups") });
        var service = new PackageUpdateService(inspector, settingsService, new BackupRestoreService(settingsService));

        var plan = await service.PreviewReplacementAsync(packageInfo, newInstallerSource);

        Assert.Equal("cd48-waptstudio", plan.PackageId);
        Assert.Equal("24.09.00.0", plan.CurrentVersion);
        Assert.Equal("24.09.00.0", plan.TargetVersion);
        Assert.Equal("7z2409.msi", plan.CurrentInstallerName);
        Assert.Equal("7z2501.msi", plan.TargetInstallerName);
        Assert.Equal("WaptStudio 24.09.00.0", plan.CurrentVisibleName);
        Assert.Equal("WaptStudio 24.09.00.0", plan.TargetVisibleName);
        Assert.Equal(packageFolder, plan.TargetPackageFolder);
        Assert.Equal("cd48-waptstudio_24.09.00.0_windows_DEV.wapt", plan.ExpectedWaptFileName);
        Assert.True(plan.BackupWillBeCreated);
        Assert.Contains("7z2409.msi", plan.FilesDeleted);
        Assert.Contains("setup.py", plan.FilesModified);
        Assert.Contains("control", plan.FilesModified);
        Assert.Contains(plan.Warnings, warning => warning.Contains("Version du package preservee", StringComparison.Ordinal));
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
        var settingsService = new TestSettingsService(new AppSettings { CreateBackups = true, BackupsDirectory = Path.Combine(_rootDirectory, "backups") });
        var service = new PackageUpdateService(inspector, settingsService, new BackupRestoreService(settingsService));

        var result = await service.ReplaceInstallerAsync(packageInfo, newInstallerSource);
        var updatedPackageFolder = packageFolder;

        Assert.True(result.Success);
        Assert.NotNull(result.UpdatedPackageInfo);
        Assert.Equal("1.0.0", result.UpdatedPackageInfo!.Version);
        Assert.EndsWith("app-2.0.0.msi", result.UpdatedPackageInfo.InstallerPath, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(updatedPackageFolder, result.UpdatedPackageFolder);
        Assert.False(result.PackageFolderRenamed);
        Assert.Equal(updatedPackageFolder, result.UpdatedPackageInfo.PackageFolder);
        Assert.Equal("tis.package", result.UpdatedPackageInfo.PackageName);
        Assert.False(File.Exists(oldInstaller));
        Assert.True(File.Exists(Path.Combine(updatedPackageFolder, "app-2.0.0.msi")));

        var setupContent = await File.ReadAllTextAsync(Path.Combine(updatedPackageFolder, "setup.py"));
        var controlContent = await File.ReadAllTextAsync(Path.Combine(updatedPackageFolder, "control"));

        Assert.Contains("1.0.0", setupContent);
        Assert.DoesNotContain("version = '2.0.0'", setupContent, StringComparison.Ordinal);
        Assert.Contains("app-2.0.0.msi", setupContent);
        Assert.Contains("Installing: app-2.0.0.msi version 1.0.0", setupContent);
        Assert.Contains("version: 1.0.0", controlContent);
        Assert.Contains("app-2.0.0.msi", controlContent);
        Assert.Contains("name: TIS Package 1.0.0", controlContent);
        Assert.Contains("description: Application TIS Package version 1.0.0", controlContent);
        Assert.Contains("description_fr: Application TIS Package version 1.0.0 FR", controlContent);
        Assert.Contains("package: tis.package", controlContent);
        Assert.NotNull(result.BackupDirectory);
        Assert.True(Directory.Exists(result.BackupDirectory!));
        Assert.True(File.Exists(Path.Combine(result.BackupDirectory!, "snapshot", "app-1.0.0.msi")));
        Assert.True(File.Exists(Path.Combine(result.BackupDirectory!, "snapshot", "setup.py")));
        Assert.True(File.Exists(Path.Combine(result.BackupDirectory!, "snapshot", "control")));
        Assert.Contains(result.ChangeSummaryLines, line => line.Contains("Version: 1.0.0 -> 1.0.0", StringComparison.Ordinal));
        Assert.DoesNotContain(result.ChangeSummaryLines, line => line.Contains("Nom: TIS Package 1.0.0 -> TIS Package 2.0.0", StringComparison.Ordinal));
        Assert.DoesNotContain(result.ChangeSummaryLines, line => line.Contains("Description: Application TIS Package version 1.0.0 -> Application TIS Package version 2.0.0", StringComparison.Ordinal));
        Assert.NotNull(result.SynchronizationPlan);
        Assert.Equal("tis.package_1.0.0.wapt", result.SynchronizationPlan!.ExpectedWaptFileName);
        Assert.Contains("app-1.0.0.msi", result.SynchronizationPlan.FilesDeleted);
        Assert.Contains(result.SynchronizationPlan.Warnings, warning => warning.Contains("Version du package preservee", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ReplaceInstallerAsync_FixesConcatenatedInstallerNameInSetupPy()
    {
        var packageFolder = Path.Combine(_rootDirectory, "tis-7zip_2501-wapt");
        Directory.CreateDirectory(packageFolder);

        var corruptedInstaller = Path.Combine(packageFolder, "7z2501.msi");
        var newInstallerSource = Path.Combine(_rootDirectory, "7z2501.msi");

        await File.WriteAllTextAsync(corruptedInstaller, "installer");
        await File.WriteAllTextAsync(newInstallerSource, "installer");
        await File.WriteAllTextAsync(Path.Combine(packageFolder, "setup.py"),
            "package = 'tis-7zip'\nversion = '2501'\nprint(\"Installing: 7z2501.msi\")\ninstall_msi_if_needed('7z2501.msi7z2301-x64.msi')\n");
        await File.WriteAllTextAsync(Path.Combine(packageFolder, "control"),
            "package: tis-7zip\nversion: 2501\nname: 7-Zip 2501\nfilename: 7z2501.msi\n");

        var inspector = new PackageInspectorService();
        var packageInfo = await inspector.AnalyzePackageAsync(packageFolder);
        var settingsService = new TestSettingsService(new AppSettings { CreateBackups = true, BackupsDirectory = Path.Combine(_rootDirectory, "backups") });
        var service = new PackageUpdateService(inspector, settingsService, new BackupRestoreService(settingsService));

        var result = await service.ReplaceInstallerAsync(packageInfo, newInstallerSource);

        Assert.True(result.Success);
        var setupContent = await File.ReadAllTextAsync(Path.Combine(result.UpdatedPackageFolder!, "setup.py"));
        Assert.Contains("install_msi_if_needed('7z2501.msi')", setupContent);
        Assert.DoesNotContain("7z2301", setupContent);
        Assert.DoesNotContain("7z2501.msi7z2301", setupContent);
    }

    [Fact]
    public async Task ReplaceInstallerAsync_HandlesExeToExeReplacement()
    {
        var packageFolder = Path.Combine(_rootDirectory, "tis-app_1.0.0-wapt");
        Directory.CreateDirectory(packageFolder);

        var oldInstaller = Path.Combine(packageFolder, "app-setup-1.0.0.exe");
        var newInstallerSource = Path.Combine(_rootDirectory, "app-setup-2.0.0.exe");

        await File.WriteAllTextAsync(oldInstaller, "old");
        await File.WriteAllTextAsync(newInstallerSource, "new");
        await File.WriteAllTextAsync(Path.Combine(packageFolder, "setup.py"),
            "package = 'tis-app'\nversion = '1.0.0'\ninstall_exe_if_needed('app-setup-1.0.0.exe')\n");
        await File.WriteAllTextAsync(Path.Combine(packageFolder, "control"),
            "package: tis-app\nversion: 1.0.0\nname: TIS App 1.0.0\nfilename: app-setup-1.0.0.exe\n");

        var inspector = new PackageInspectorService();
        var packageInfo = await inspector.AnalyzePackageAsync(packageFolder);
        var settingsService = new TestSettingsService(new AppSettings { CreateBackups = true, BackupsDirectory = Path.Combine(_rootDirectory, "backups") });
        var service = new PackageUpdateService(inspector, settingsService, new BackupRestoreService(settingsService));

        var result = await service.ReplaceInstallerAsync(packageInfo, newInstallerSource);
        var updatedPackageFolder = packageFolder;

        Assert.True(result.Success);
        var setupContent = await File.ReadAllTextAsync(Path.Combine(updatedPackageFolder, "setup.py"));
        Assert.Contains("install_exe_if_needed('app-setup-2.0.0.exe')", setupContent);
        Assert.DoesNotContain("app-setup-1.0.0.exe", setupContent);
    }

    [Fact]
    public async Task ReplaceInstallerAsync_PreservesStablePackageIdentityAndRepairsFolderPrefix()
    {
        var packageFolder = Path.Combine(_rootDirectory, "cd48-7-zip-24.09_2501_windows_DEV-wapt");
        Directory.CreateDirectory(packageFolder);

        var oldInstaller = Path.Combine(packageFolder, "7z2501.msi");
        var newInstallerSource = Path.Combine(_rootDirectory, "7z2502.msi");

        await File.WriteAllTextAsync(oldInstaller, "old");
        await File.WriteAllTextAsync(newInstallerSource, "new");
        await File.WriteAllTextAsync(Path.Combine(packageFolder, "setup.py"), "package = 'cd48-waptstudio'\nversion = '2501'\ninstall_msi_if_needed('7z2501.msi')\n");
        await File.WriteAllTextAsync(Path.Combine(packageFolder, "control"), "package: cd48-waptstudio\nversion: 2501\nname: CD48 7-Zip 24.09\ndescription: Package CD48 7-Zip 24.09 version 2501\nfilename: 7z2501.msi\n");

        var inspector = new PackageInspectorService();
        var packageInfo = await inspector.AnalyzePackageAsync(packageFolder);
        var settingsService = new TestSettingsService(new AppSettings { CreateBackups = true, BackupsDirectory = Path.Combine(_rootDirectory, "backups") });
        var service = new PackageUpdateService(inspector, settingsService, new BackupRestoreService(settingsService));

        var result = await service.ReplaceInstallerAsync(packageInfo, newInstallerSource);
        var updatedPackageFolder = Path.Combine(_rootDirectory, "cd48-waptstudio_2501_windows_DEV-wapt");

        Assert.True(result.Success);
        Assert.True(result.PackageFolderRenamed);
        Assert.Equal(updatedPackageFolder, result.UpdatedPackageFolder);
        Assert.Equal("cd48-waptstudio", result.UpdatedPackageInfo!.PackageName);
        Assert.Equal("2501", result.UpdatedPackageInfo.Version);
        Assert.Equal("cd48-waptstudio_2501_windows_DEV.wapt", result.UpdatedPackageInfo.ExpectedWaptFileName);

        var controlContent = await File.ReadAllTextAsync(Path.Combine(updatedPackageFolder, "control"));
        Assert.Contains("package: cd48-waptstudio", controlContent);
        Assert.DoesNotContain("package: cd48-7-zip-24.09", controlContent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PreviewReplacementAsync_DoesNotSilentlyDriftVersionFromInstallerName()
    {
        var packageFolder = Path.Combine(_rootDirectory, "cd48-waptstudio_2501_windows_DEV-wapt");
        Directory.CreateDirectory(packageFolder);

        var oldInstaller = Path.Combine(packageFolder, "7z2501.msi");
        var newInstallerSource = Path.Combine(_rootDirectory, "7z2502.msi");

        await File.WriteAllTextAsync(oldInstaller, "old");
        await File.WriteAllTextAsync(newInstallerSource, "new");
        await File.WriteAllTextAsync(Path.Combine(packageFolder, "setup.py"), "package = 'cd48-waptstudio'\nversion = '2501'\ninstall_msi_if_needed('7z2501.msi')\n");
        await File.WriteAllTextAsync(Path.Combine(packageFolder, "control"), "package: cd48-waptstudio\nversion: 2501\nname: WaptStudio 2501\nfilename: 7z2501.msi\n");

        var inspector = new PackageInspectorService();
        var packageInfo = await inspector.AnalyzePackageAsync(packageFolder);
        var settingsService = new TestSettingsService(new AppSettings { CreateBackups = true, BackupsDirectory = Path.Combine(_rootDirectory, "backups") });
        var service = new PackageUpdateService(inspector, settingsService, new BackupRestoreService(settingsService));

        var plan = await service.PreviewReplacementAsync(packageInfo, newInstallerSource);

        Assert.Equal("2501", plan.CurrentVersion);
        Assert.Equal("2501", plan.TargetVersion);
        Assert.Equal("cd48-waptstudio_2501_windows_DEV.wapt", plan.ExpectedWaptFileName);
        Assert.Contains(plan.Warnings, warning => warning.Contains("Version du package preservee", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ReplaceInstallerAsync_ThrowsWhenSetupPyCannotBeValidated()
    {
        var packageFolder = Path.Combine(_rootDirectory, "tis-app_1.0.0-wapt");
        Directory.CreateDirectory(packageFolder);

        var newInstallerSource = Path.Combine(_rootDirectory, "app-setup-2.0.0.exe");
        await File.WriteAllTextAsync(newInstallerSource, "new");
        await File.WriteAllTextAsync(Path.Combine(packageFolder, "setup.py"), "package = 'tis-app'\nversion = '1.0.0'\nprint('no installer call here')\n");
        await File.WriteAllTextAsync(Path.Combine(packageFolder, "control"), "package: tis-app\nversion: 1.0.0\nfilename: app-setup-1.0.0.exe\n");

        var inspector = new PackageInspectorService();
        var packageInfo = await inspector.AnalyzePackageAsync(packageFolder);
        var settingsService = new TestSettingsService(new AppSettings { CreateBackups = true, BackupsDirectory = Path.Combine(_rootDirectory, "backups") });
        var service = new PackageUpdateService(inspector, settingsService, new BackupRestoreService(settingsService));

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReplaceInstallerAsync(packageInfo, newInstallerSource));
    }

    [Fact]
    public async Task ReplaceInstallerAsync_NeverProducesEmptyInstallerArgument()
    {
        var packageFolder = Path.Combine(_rootDirectory, "tis-tool_1.0-wapt");
        Directory.CreateDirectory(packageFolder);

        var oldInstaller = Path.Combine(packageFolder, "tool-1.0.msi");
        var newInstallerSource = Path.Combine(_rootDirectory, "tool-2.0.msi");

        await File.WriteAllTextAsync(oldInstaller, "old");
        await File.WriteAllTextAsync(newInstallerSource, "new");
        await File.WriteAllTextAsync(Path.Combine(packageFolder, "setup.py"),
            "package = 'tis-tool'\nversion = '1.0'\ninstall_msi_if_needed('tool-1.0.msi')\n");
        await File.WriteAllTextAsync(Path.Combine(packageFolder, "control"),
            "package: tis-tool\nversion: 1.0\nfilename: tool-1.0.msi\n");

        var inspector = new PackageInspectorService();
        var packageInfo = await inspector.AnalyzePackageAsync(packageFolder);
        var settingsService = new TestSettingsService(new AppSettings { CreateBackups = true, BackupsDirectory = Path.Combine(_rootDirectory, "backups") });
        var service = new PackageUpdateService(inspector, settingsService, new BackupRestoreService(settingsService));

        var result = await service.ReplaceInstallerAsync(packageInfo, newInstallerSource);

        Assert.True(result.Success);
        var setupContent = await File.ReadAllTextAsync(Path.Combine(result.UpdatedPackageFolder!, "setup.py"));
        Assert.Contains("install_msi_if_needed('tool-2.0.msi')", setupContent);
        Assert.DoesNotContain("install_msi_if_needed('')", setupContent);
        Assert.DoesNotContain("install_msi_if_needed(\"\")", setupContent);
    }

    [Fact]
    public async Task ReplaceInstallerAsync_NeverConcatenatesOldAndNewInstallerName()
    {
        var packageFolder = Path.Combine(_rootDirectory, "tis-7zip_24.09-wapt");
        Directory.CreateDirectory(packageFolder);

        var oldInstaller = Path.Combine(packageFolder, "7z2409.msi");
        var newInstallerSource = Path.Combine(_rootDirectory, "7z2501.msi");

        await File.WriteAllTextAsync(oldInstaller, "old");
        await File.WriteAllTextAsync(newInstallerSource, "new");
        await File.WriteAllTextAsync(Path.Combine(packageFolder, "setup.py"),
            "package = 'tis-7zip'\nversion = '24.09'\nprint(\"Installing: 7z2409.msi\")\ninstall_msi_if_needed('7z2409.msi')\n");
        await File.WriteAllTextAsync(Path.Combine(packageFolder, "control"),
            "package: tis-7zip\nversion: 24.09\nname: 7-Zip 24.09\nfilename: 7z2409.msi\n");

        var inspector = new PackageInspectorService();
        var packageInfo = await inspector.AnalyzePackageAsync(packageFolder);
        var settingsService = new TestSettingsService(new AppSettings { CreateBackups = true, BackupsDirectory = Path.Combine(_rootDirectory, "backups") });
        var service = new PackageUpdateService(inspector, settingsService, new BackupRestoreService(settingsService));

        var result = await service.ReplaceInstallerAsync(packageInfo, newInstallerSource);

        Assert.True(result.Success);
        var setupContent = await File.ReadAllTextAsync(Path.Combine(result.UpdatedPackageFolder!, "setup.py"));
        Assert.Contains("install_msi_if_needed('7z2501.msi')", setupContent);
        Assert.DoesNotContain("7z2501.msi7z2409", setupContent);
        Assert.DoesNotContain("7z2409.msi7z2501", setupContent);
        Assert.DoesNotContain("7z2409", setupContent);

        Assert.Contains("Installing: 7z2501.msi", setupContent);
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
        private readonly AppSettings _settings;

        public TestSettingsService(AppSettings? settings = null)
        {
            _settings = settings ?? new AppSettings { CreateBackups = false };
        }

        public Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_settings);

        public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
