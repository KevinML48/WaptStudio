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
        HistoryService = new HistoryService();
        PackageInspectorService = new PackageInspectorService();
        WaptCommandService = new WaptCommandService(CommandExecutionService, SettingsService);
        PackageValidationService = new PackageValidationService(PackageInspectorService, WaptCommandService, SettingsService);
        PackageUpdateService = new PackageUpdateService(PackageInspectorService, SettingsService);
    }

    public ISettingsService SettingsService { get; }

    public ILogService LogService { get; }

    public ICommandExecutionService CommandExecutionService { get; }

    public IHistoryService HistoryService { get; }

    public IPackageInspectorService PackageInspectorService { get; }

    public IWaptCommandService WaptCommandService { get; }

    public IPackageValidationService PackageValidationService { get; }

    public IPackageUpdateService PackageUpdateService { get; }

    public Task<AppSettings> LoadSettingsAsync()
        => SettingsService.LoadAsync();

    public Task InitializeAsync()
        => HistoryService.InitializeAsync();
}
