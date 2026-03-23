using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WaptStudio.Core.Models;
using WaptStudio.Core.Services;
using WaptStudio.Core.Services.Interfaces;
using Xunit;

namespace WaptStudio.Tests;

public sealed class PackageCatalogAndBackupServiceTests : IDisposable
{
    private readonly string _rootDirectory;

    public PackageCatalogAndBackupServiceTests()
    {
        _rootDirectory = Path.Combine(Path.GetTempPath(), "WaptStudio.Tests.Catalog", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_rootDirectory);
    }

    [Fact]
    public async Task ScanAsync_DiscoversPackagesAndBuildsInventoryRows()
    {
        var msiPackage = CreatePackage("cd48-demo-msi", "1.0.0", "install_msi_if_needed('demo.msi')", "demo.msi");
        var exePackage = CreatePackage("cd48-demo-exe", "2.0.0", "install_exe_if_needed('demo.exe')", "demo.exe");

        var inspector = new PackageInspectorService(new PackageClassificationService());
        var validation = new PackageValidationService(inspector, new CatalogWaptCommandService(successAvailability: true), new CatalogSettingsService(_rootDirectory));
        var service = new PackageCatalogService(inspector, validation);

        var items = await service.ScanAsync(_rootDirectory, recursive: true, semiRecursiveDepth: 0);

        Assert.Equal(2, items.Count);
        Assert.Contains(items, item => item.PackageId == "cd48-demo-msi" && item.Category == PackageCategory.Msi);
        Assert.Contains(items, item => item.PackageId == "cd48-demo-exe" && item.Category == PackageCategory.Exe);
        Assert.All(items, item => Assert.False(string.IsNullOrWhiteSpace(item.ReadinessLabel)));
        Assert.Contains(items, item => string.Equals(item.PackageFolder, msiPackage, StringComparison.OrdinalIgnoreCase));
        Assert.Contains(items, item => string.Equals(item.PackageFolder, exePackage, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task BackupRestoreService_CreatesAndRestoresLatestBackup()
    {
        var packageFolder = CreatePackage("cd48-backup", "3.1.4", "install_msi_if_needed('demo.msi')", "demo.msi");
        var inspector = new PackageInspectorService(new PackageClassificationService());
        var package = await inspector.AnalyzePackageAsync(packageFolder);
        var settingsService = new CatalogSettingsService(_rootDirectory);
        var service = new BackupRestoreService(settingsService);

        var backup = await service.CreatePackageBackupAsync(package, "test-backup");
        await File.WriteAllTextAsync(Path.Combine(packageFolder, "setup.py"), "package='cd48-backup'\nversion='9.9.9'\n");

        var restore = await service.RestoreLatestBackupAsync(package);

        Assert.True(restore.Success);
        Assert.NotNull(backup.BackupDirectory);
        Assert.True(Directory.Exists(backup.BackupDirectory));
        Assert.Contains("setup.py", restore.RestoredFiles);
        var restoredSetup = await File.ReadAllTextAsync(Path.Combine(packageFolder, "setup.py"));
        Assert.Contains("3.1.4", restoredSetup);
    }

    private string CreatePackage(string packageId, string version, string installCall, string installerFileName)
    {
        var packageFolder = Path.Combine(_rootDirectory, packageId + "-wapt");
        Directory.CreateDirectory(packageFolder);
        File.WriteAllText(Path.Combine(packageFolder, installerFileName), "binary");
        File.WriteAllText(Path.Combine(packageFolder, "setup.py"), $"package = '{packageId}'\nversion = '{version}'\n{installCall}\n");
        Directory.CreateDirectory(Path.Combine(packageFolder, "WAPT"));
        File.WriteAllText(Path.Combine(packageFolder, "WAPT", "control"), $"package: {packageId}\nversion: {version}\nname: {packageId} {version}\ndescription: {packageId} {version}\ndescription_fr: {packageId} {version} FR\nfilename: {installerFileName}\n");
        return packageFolder;
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootDirectory))
        {
            Directory.Delete(_rootDirectory, recursive: true);
        }
    }

    private sealed class CatalogSettingsService : ISettingsService
    {
        private readonly AppSettings _settings;

        public CatalogSettingsService(string rootDirectory)
        {
            _settings = new AppSettings
            {
                CreateBackups = true,
                BackupsDirectory = Path.Combine(rootDirectory, "backups"),
                WaptExecutablePath = "wapt-get.exe",
                EnableUpload = true,
                AuditPackageArguments = "audit {packageId}",
                UninstallPackageArguments = "remove {packageId}"
            };
        }

        public Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_settings);

        public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class CatalogWaptCommandService : IWaptCommandService
    {
        private readonly bool _successAvailability;

        public CatalogWaptCommandService(bool successAvailability)
        {
            _successAvailability = successAvailability;
        }

        public Task<CommandExecutionResult> CheckWaptAvailabilityAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new CommandExecutionResult
            {
                FileName = "wapt-get.exe",
                ExitCode = _successAvailability ? 0 : 1,
                StandardOutput = _successAvailability ? "ok" : string.Empty,
                StandardError = _successAvailability ? string.Empty : "indisponible",
                StartedAt = DateTimeOffset.Now,
                Duration = TimeSpan.Zero
            });

        public Task<CommandExecutionResult> ValidatePackageWithWaptAsync(string packageFolder, CancellationToken cancellationToken = default)
            => Task.FromResult(new CommandExecutionResult
            {
                FileName = "wapt-get.exe",
                ExitCode = 0,
                StandardOutput = "validation ok",
                StartedAt = DateTimeOffset.Now,
                Duration = TimeSpan.Zero
            });

        public Task<CommandExecutionResult> BuildPackageAsync(string packageFolder, WaptExecutionContext? executionContext = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new CommandExecutionResult());

        public Task<CommandExecutionResult> SignPackageAsync(string packageFolder, WaptExecutionContext? executionContext = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new CommandExecutionResult());

        public Task<CommandExecutionResult> UploadPackageAsync(string packageFolder, string? waptFilePath = null, WaptExecutionContext? executionContext = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new CommandExecutionResult());

        public Task<CommandExecutionResult> AuditPackageAsync(string packageFolder, string? packageId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new CommandExecutionResult());

        public Task<CommandExecutionResult> UninstallPackageAsync(string packageFolder, string? packageId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new CommandExecutionResult());
    }
}