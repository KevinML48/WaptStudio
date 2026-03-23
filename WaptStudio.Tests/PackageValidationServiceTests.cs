using System.Threading;
using System.Threading.Tasks;
using WaptStudio.Core.Models;
using WaptStudio.Core.Services;
using WaptStudio.Core.Services.Interfaces;
using Xunit;

namespace WaptStudio.Tests;

public sealed class PackageValidationServiceTests
{
    [Fact]
    public async Task ValidateAsync_ReturnsErrorsWhenWaptIsNotConfigured()
    {
        var inspector = new PackageInspectorService();
        var packageFolder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "WaptStudio.Tests.Validation", System.Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(packageFolder);

        try
        {
            await System.IO.File.WriteAllTextAsync(System.IO.Path.Combine(packageFolder, "setup.py"), "package = 'tis.package'\nversion = '1.0.0'\n");
            await System.IO.File.WriteAllTextAsync(System.IO.Path.Combine(packageFolder, "control"), "package: tis.package\nversion: 1.0.0\n");
            await System.IO.File.WriteAllTextAsync(System.IO.Path.Combine(packageFolder, "installer.exe"), "binary-placeholder");

            var service = new PackageValidationService(inspector, new TestWaptCommandService(), new TestSettingsService());
            var result = await service.ValidateAsync(packageFolder);

            Assert.False(result.IsValid);
            Assert.Contains(result.Issues, issue => issue.Severity == "ERROR");
        }
        finally
        {
            if (System.IO.Directory.Exists(packageFolder))
            {
                System.IO.Directory.Delete(packageFolder, recursive: true);
            }
        }
    }

    private sealed class TestWaptCommandService : IWaptCommandService
    {
        public Task<CommandExecutionResult> CheckWaptAvailabilityAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(CreateSkippedAvailabilityResult());

        public Task<CommandExecutionResult> BuildPackageAsync(string packageFolder, WaptExecutionContext? executionContext = null, CancellationToken cancellationToken = default)
            => Task.FromResult(CreateResult(packageFolder));

        public Task<CommandExecutionResult> SignPackageAsync(string packageFolder, WaptExecutionContext? executionContext = null, CancellationToken cancellationToken = default)
            => Task.FromResult(CreateResult(packageFolder));

        public Task<CommandExecutionResult> UploadPackageAsync(string packageFolder, string? waptFilePath = null, WaptExecutionContext? executionContext = null, CancellationToken cancellationToken = default)
            => Task.FromResult(CreateResult(packageFolder));

        public Task<CommandExecutionResult> ValidatePackageWithWaptAsync(string packageFolder, CancellationToken cancellationToken = default)
            => Task.FromResult(CreateResult(packageFolder));

        private static CommandExecutionResult CreateSkippedAvailabilityResult() => new()
        {
            FileName = "wapt-get.exe",
            ExitCode = 1,
            IsSkipped = true,
            StandardError = "WAPT non configure dans le contexte de test.",
            Duration = System.TimeSpan.Zero,
            StartedAt = System.DateTimeOffset.Now
        };

        private static CommandExecutionResult CreateResult(string packageFolder) => new()
        {
            FileName = "wapt-get.exe",
            WorkingDirectory = packageFolder,
            ExitCode = 1,
            IsSkipped = true,
            StandardError = "Validation WAPT non executee dans le contexte de test.",
            Duration = System.TimeSpan.Zero,
            StartedAt = System.DateTimeOffset.Now
        };
    }

    private sealed class TestSettingsService : ISettingsService
    {
        public Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new AppSettings { WaptExecutablePath = string.Empty });

        public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
