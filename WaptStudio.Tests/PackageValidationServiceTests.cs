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

            var service = new PackageValidationService(inspector, new TestWaptCommandService(availabilitySuccess: false), new TestSettingsService(new AppSettings { WaptExecutablePath = string.Empty }));
            var result = await service.ValidateAsync(packageFolder);

            Assert.False(result.IsValid);
            Assert.Contains(result.Issues, issue => issue.Severity == "ERROR");
            Assert.Equal(ReadinessVerdict.Blocked, result.Verdict);
        }
        finally
        {
            if (System.IO.Directory.Exists(packageFolder))
            {
                System.IO.Directory.Delete(packageFolder, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ValidateAsync_ReturnsReadyWithWarningsAndActionFlags_WhenPackageIsUsable()
    {
        var inspector = new PackageInspectorService();
        var packageFolder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "WaptStudio.Tests.Validation", System.Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(packageFolder);

        try
        {
            await System.IO.File.WriteAllTextAsync(System.IO.Path.Combine(packageFolder, "setup.py"), "package = 'tis.package'\nversion = '1.0.0'\ninstall_msi_if_needed('installer.msi')\n");
            System.IO.Directory.CreateDirectory(System.IO.Path.Combine(packageFolder, "WAPT"));
            await System.IO.File.WriteAllTextAsync(System.IO.Path.Combine(packageFolder, "WAPT", "control"), "package: tis.package\nversion: 1.0.0\nname: TIS Package\ndescription: Application TIS\nfilename: installer.msi\n");
            await System.IO.File.WriteAllTextAsync(System.IO.Path.Combine(packageFolder, "installer.msi"), "binary-placeholder");

            var service = new PackageValidationService(
                inspector,
                new TestWaptCommandService(availabilitySuccess: true),
                new TestSettingsService(new AppSettings
                {
                    WaptExecutablePath = "wapt-get.exe",
                    EnableUpload = true,
                    AuditPackageArguments = "audit {packageId}",
                    UninstallPackageArguments = "remove {packageId}"
                }));

            var result = await service.ValidateAsync(packageFolder, includeWaptValidation: false);

            Assert.Equal(ReadinessVerdict.ReadyWithWarnings, result.Verdict);
            Assert.True(result.BuildPossible);
            Assert.True(result.UploadPossible);
            Assert.True(result.AuditPossible);
            Assert.True(result.UninstallPossible);
            Assert.Contains(result.Issues, issue => issue.Severity == "WARNING");
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
        private readonly bool _availabilitySuccess;

        public TestWaptCommandService(bool availabilitySuccess)
        {
            _availabilitySuccess = availabilitySuccess;
        }

        public Task<CommandExecutionResult> CheckWaptAvailabilityAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_availabilitySuccess ? CreateAvailableResult() : CreateSkippedAvailabilityResult());

        public Task<CommandExecutionResult> BuildPackageAsync(string packageFolder, WaptExecutionContext? executionContext = null, CancellationToken cancellationToken = default)
            => Task.FromResult(CreateResult(packageFolder));

        public Task<CommandExecutionResult> SignPackageAsync(string packageFolder, WaptExecutionContext? executionContext = null, CancellationToken cancellationToken = default)
            => Task.FromResult(CreateResult(packageFolder));

        public Task<CommandExecutionResult> UploadPackageAsync(string packageFolder, string? waptFilePath = null, WaptExecutionContext? executionContext = null, CancellationToken cancellationToken = default)
            => Task.FromResult(CreateResult(packageFolder));

        public Task<CommandExecutionResult> AuditPackageAsync(string packageFolder, string? packageId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(CreateResult(packageFolder));

        public Task<CommandExecutionResult> UninstallPackageAsync(string packageFolder, string? packageId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(CreateResult(packageFolder));

        public Task<CommandExecutionResult> ValidatePackageWithWaptAsync(string packageFolder, CancellationToken cancellationToken = default)
            => Task.FromResult(CreateResult(packageFolder));

        private static CommandExecutionResult CreateAvailableResult() => new()
        {
            FileName = "wapt-get.exe",
            ExitCode = 0,
            StandardOutput = "ok",
            Duration = System.TimeSpan.Zero,
            StartedAt = System.DateTimeOffset.Now
        };

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
