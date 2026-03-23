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
        var settingsService = new TestSettingsService();
        var service = new WaptCommandService(commandExecutionService, settingsService);

        var result = await service.BuildPackageAsync(@"C:\Packages\Sample");

        Assert.True(result.IsDryRun);
        Assert.True(result.IsSuccess);
        Assert.False(commandExecutionService.WasCalled);
        Assert.Contains("build-package", result.ExecutedCommand);
        Assert.Contains("C:\\Packages\\Sample", result.ExecutedCommand.Replace("\"", string.Empty));
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
        public Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new AppSettings
            {
                WaptExecutablePath = @"C:\WAPT\wapt-get.exe",
                BuildPackageArguments = "build-package {packageFolder}",
                DryRunEnabled = true
            });

        public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
