using System.Threading;
using System.Threading.Tasks;
using WaptStudio.Core.Models;
using WaptStudio.Core.Services;
using WaptStudio.Core.Services.Interfaces;
using Xunit;

namespace WaptStudio.Tests;

public sealed class WaptCommandServiceTests
{
    [Fact]
    public async Task BuildPackageAsync_UsesDryRunWithoutExecutingProcess()
    {
        var commandExecutionService = new TrackingCommandExecutionService();
        var settingsService = new TestSettingsService(new AppSettings
        {
            WaptExecutablePath = @"C:\WAPT\wapt-get.exe",
            BuildPackageArguments = "build-package {packageFolder}",
            DryRunEnabled = true
        });
        var service = new WaptCommandService(commandExecutionService, settingsService);

        var result = await service.BuildPackageAsync(@"C:\Packages\Sample");

        Assert.True(result.IsDryRun);
        Assert.True(result.IsSuccess);
        Assert.False(result.IsConfigurationBlocked);
        Assert.False(commandExecutionService.WasCalled);
        Assert.Contains("build-package", result.ExecutedCommand);
        Assert.Contains("C:\\Packages\\Sample", result.ExecutedCommand.Replace("\"", string.Empty));
    }

    [Fact]
    public async Task BuildPackageAsync_BlocksForInteractiveCertificateOutsideDryRun()
    {
        var commandExecutionService = new TrackingCommandExecutionService();
        var settingsService = new TestSettingsService(new AppSettings
        {
            WaptExecutablePath = @"C:\Program Files (x86)\wapt\wapt-get.exe",
            BuildPackageArguments = "build-package {packageFolder}",
            DryRunEnabled = false
        });
        var service = new WaptCommandService(commandExecutionService, settingsService);

        var result = await service.BuildPackageAsync(@"C:\waptdev\sample");

        Assert.False(result.IsDryRun);
        Assert.False(result.IsSuccess);
        Assert.True(result.RequiresUserInteraction);
        Assert.False(result.IsConfigurationBlocked);
        Assert.False(commandExecutionService.WasCalled);
        Assert.Contains("Build reel interactif non supporte", result.StandardError);
        Assert.Contains("build-package", result.ExecutedCommand);
    }

    [Fact]
    public async Task SignPackageAsync_UsesDryRunWithoutExecutingProcess_WhenKeyIsMissing()
    {
        var commandExecutionService = new TrackingCommandExecutionService();
        var settingsService = new TestSettingsService(new AppSettings
        {
            WaptExecutablePath = @"C:\WAPT\wapt-get.exe",
            SignPackageArguments = "sign-package --private-key {signingKeyPath} {packageFolder}",
            EnableSigning = true,
            DryRunEnabled = true
        });
        var service = new WaptCommandService(commandExecutionService, settingsService);

        var result = await service.SignPackageAsync(@"C:\Packages\Sample");

        Assert.True(result.IsDryRun);
        Assert.True(result.IsSuccess);
        Assert.False(result.IsConfigurationBlocked);
        Assert.False(commandExecutionService.WasCalled);
        Assert.Contains("sign-package", result.ExecutedCommand);
    }

    [Fact]
    public async Task UploadPackageAsync_UsesDryRunWithoutExecutingProcess_WhenUploadIsDisabled()
    {
        var commandExecutionService = new TrackingCommandExecutionService();
        var settingsService = new TestSettingsService(new AppSettings
        {
            WaptExecutablePath = @"C:\WAPT\wapt-get.exe",
            UploadPackageArguments = "upload-package {overwriteFlag} {repositoryOption} {packageFolder}",
            EnableUpload = false,
            DryRunEnabled = true
        });
        var service = new WaptCommandService(commandExecutionService, settingsService);

        var result = await service.UploadPackageAsync(@"C:\Packages\Sample");

        Assert.True(result.IsDryRun);
        Assert.True(result.IsSuccess);
        Assert.False(result.IsConfigurationBlocked);
        Assert.False(commandExecutionService.WasCalled);
        Assert.Contains("upload-package", result.ExecutedCommand);
    }

    [Fact]
    public async Task SignPackageAsync_BlocksWhenSigningKeyIsMissingOutsideDryRun()
    {
        var commandExecutionService = new TrackingCommandExecutionService();
        var settingsService = new TestSettingsService(new AppSettings
        {
            WaptExecutablePath = @"C:\WAPT\wapt-get.exe",
            SignPackageArguments = "sign-package --private-key {signingKeyPath} {packageFolder}",
            EnableSigning = true,
            DryRunEnabled = false
        });
        var service = new WaptCommandService(commandExecutionService, settingsService);

        var result = await service.SignPackageAsync(@"C:\Packages\Sample");

        Assert.False(result.IsDryRun);
        Assert.False(result.IsSuccess);
        Assert.True(result.IsConfigurationBlocked);
        Assert.False(commandExecutionService.WasCalled);
        Assert.Equal("Cle de signature non renseignee.", result.StandardError);
    }

    [Fact]
    public async Task UploadPackageAsync_BlocksWhenUploadIsDisabledOutsideDryRun()
    {
        var commandExecutionService = new TrackingCommandExecutionService();
        var settingsService = new TestSettingsService(new AppSettings
        {
            WaptExecutablePath = @"C:\WAPT\wapt-get.exe",
            UploadPackageArguments = "upload-package {overwriteFlag} {repositoryOption} {packageFolder}",
            EnableUpload = false,
            DryRunEnabled = false
        });
        var service = new WaptCommandService(commandExecutionService, settingsService);

        var result = await service.UploadPackageAsync(@"C:\Packages\Sample");

        Assert.False(result.IsDryRun);
        Assert.False(result.IsSuccess);
        Assert.True(result.IsConfigurationBlocked);
        Assert.False(commandExecutionService.WasCalled);
        Assert.Equal("Upload desactive dans la configuration.", result.StandardError);
    }

    [Fact]
    public async Task UploadPackageAsync_BlocksWhenRepositoryIsMissingOutsideDryRun()
    {
        var commandExecutionService = new TrackingCommandExecutionService();
        var settingsService = new TestSettingsService(new AppSettings
        {
            WaptExecutablePath = @"C:\WAPT\wapt-get.exe",
            UploadPackageArguments = "upload-package {repositoryOption} {packageFolder}",
            EnableUpload = true,
            DryRunEnabled = false
        });
        var service = new WaptCommandService(commandExecutionService, settingsService);

        var result = await service.UploadPackageAsync(@"C:\Packages\Sample");

        Assert.False(result.IsDryRun);
        Assert.False(result.IsSuccess);
        Assert.True(result.IsConfigurationBlocked);
        Assert.False(commandExecutionService.WasCalled);
        Assert.Equal("Repository d'upload non renseigne.", result.StandardError);
    }

    private sealed class TrackingCommandExecutionService : ICommandExecutionService
    {
        public bool WasCalled { get; private set; }

        public Task<CommandExecutionResult> ExecuteAsync(string fileName, string arguments, string workingDirectory, int timeoutSeconds, CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            return Task.FromResult(new CommandExecutionResult());
        }
    }

    private sealed class TestSettingsService : ISettingsService
    {
        private readonly AppSettings _settings;

        public TestSettingsService(AppSettings settings)
        {
            _settings = settings;
        }

        public Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_settings);

        public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
