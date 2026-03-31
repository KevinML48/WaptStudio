using System.Threading.Tasks;
using WaptStudio.Core.Models;
using WaptStudio.Core.Services;
using WaptStudio.Core.Services.Interfaces;

namespace WaptStudio.App.Bootstrap;

public sealed class AppRuntime
{
    public AppRuntime()
    {
        SettingsService = new SettingsService();
        LogService = new LogService(SettingsService);
        CommandExecutionService = new CommandExecutionService();
        WaptSessionService = new WaptSessionService();
        HistoryService = new HistoryService();
        PackageClassificationService = new PackageClassificationService();
        PackageInspectorService = new PackageInspectorService(PackageClassificationService);
        WaptCommandService = new WaptCommandService(CommandExecutionService, SettingsService);
        PackageValidationService = new PackageValidationService(PackageInspectorService, WaptCommandService, SettingsService);
        BackupRestoreService = new BackupRestoreService(SettingsService);
        PackageCatalogService = new PackageCatalogService(PackageInspectorService, PackageValidationService);
        PackageUpdateService = new PackageUpdateService(PackageInspectorService, SettingsService, BackupRestoreService);
    }

    public ISettingsService SettingsService { get; }

    public ILogService LogService { get; }

    public ICommandExecutionService CommandExecutionService { get; }

    public WaptSessionService WaptSessionService { get; }

    public IHistoryService HistoryService { get; }

    public IPackageInspectorService PackageInspectorService { get; }

    public IPackageClassificationService PackageClassificationService { get; }

    public IWaptCommandService WaptCommandService { get; }

    public IPackageValidationService PackageValidationService { get; }

    public IPackageUpdateService PackageUpdateService { get; }

    public IBackupRestoreService BackupRestoreService { get; }

    public IPackageCatalogService PackageCatalogService { get; }

    public Task<AppSettings> LoadSettingsAsync()
        => SettingsService.LoadAsync();

    public Task InitializeAsync()
        => HistoryService.InitializeAsync();
}
