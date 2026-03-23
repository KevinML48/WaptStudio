using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WaptStudio.Core.Models;
using WaptStudio.Core.Services;
using WaptStudio.Core.Services.Interfaces;
using WaptStudio.Core.Utilities;
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
    public async Task BuildPackageAsync_PreparesManualWorkflowWhenCertificatePasswordIsMissingOutsideDryRun()
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
        Assert.True(result.RequiresExternalManualWorkflow);
        Assert.False(result.IsConfigurationBlocked);
        Assert.False(commandExecutionService.WasCalled);
        Assert.Contains("Build interactif", result.StandardError);
        Assert.Contains("build-package", result.ExecutedCommand);
    }

    [Fact]
    public async Task BuildPackageAsync_UsesAssistedExecutionWhenCertificatePasswordIsProvided()
    {
        var commandExecutionService = new TrackingCommandExecutionService();
        var settingsService = new TestSettingsService(new AppSettings
        {
            WaptExecutablePath = "wapt-get.exe",
            BuildPackageArguments = "build-package {packageFolder}",
            DryRunEnabled = false
        });
        var service = new WaptCommandService(commandExecutionService, settingsService);

        var result = await service.BuildPackageAsync(@"C:\Packages\Sample", new WaptExecutionContext { CertificatePassword = "secret-cert" });

        Assert.True(commandExecutionService.WasCalled);
        Assert.True(result.WasInteractiveExecutionAttempted);
        Assert.True(result.IsSuccess);
        Assert.Contains("secret-cert", commandExecutionService.LastOptions?.SensitiveValuesToRedact ?? []);
        Assert.Contains("secret-cert", commandExecutionService.LastOptions?.StandardInputText ?? string.Empty);
    }

    [Fact]
    public async Task SignPackageAsync_UsesDryRunWithoutExecutingProcess_WhenKeyIsMissing()
    {
        var commandExecutionService = new TrackingCommandExecutionService();
        var settingsService = new TestSettingsService(new AppSettings
        {
            WaptExecutablePath = @"C:\WAPT\wapt-get.exe",
            SignPackageArguments = "sign-package {packageFolder}",
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
        Assert.DoesNotContain("--private-key", result.ExecutedCommand);
    }

    [Fact]
    public async Task UploadPackageAsync_UsesDryRunWithoutExecutingProcess()
    {
        var commandExecutionService = new TrackingCommandExecutionService();
        var settingsService = new TestSettingsService(new AppSettings
        {
            WaptExecutablePath = @"C:\WAPT\wapt-get.exe",
            UploadPackageArguments = "upload-package {waptFilePath}",
            EnableUpload = true,
            DryRunEnabled = true
        });
        var service = new WaptCommandService(commandExecutionService, settingsService);

        var result = await service.UploadPackageAsync(packageFolder: @"C:\Packages\Sample", waptFilePath: @"C:\Packages\Sample\sample.wapt");

        Assert.True(result.IsDryRun);
        Assert.True(result.IsSuccess);
        Assert.False(result.IsConfigurationBlocked);
        Assert.False(commandExecutionService.WasCalled);
        Assert.Contains("upload-package", result.ExecutedCommand);
        Assert.Contains("sample.wapt", result.ExecutedCommand);
    }

    [Fact]
    public async Task SignPackageAsync_BlocksWhenSigningKeyIsMissingOutsideDryRun()
    {
        var commandExecutionService = new TrackingCommandExecutionService();
        var settingsService = new TestSettingsService(new AppSettings
        {
            WaptExecutablePath = @"C:\WAPT\wapt-get.exe",
            SignPackageArguments = "sign-package {packageFolder}",
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
    public async Task SignPackageAsync_NormalizesDeprecatedPrivateKeySyntaxAndUsesAssistedExecutionWhenPasswordIsProvided()
    {
        var certificatePath = CreateTempCertificateFile(".p12");

        try
        {
            var commandExecutionService = new TrackingCommandExecutionService();
            var settingsService = new TestSettingsService(new AppSettings
            {
                WaptExecutablePath = "wapt-get.exe",
                SignPackageArguments = "sign-package --private-key {signingKeyPath} {packageFolder}",
                EnableSigning = true,
                SigningKeyPath = certificatePath,
                DryRunEnabled = false
            });
            var service = new WaptCommandService(commandExecutionService, settingsService);

            var result = await service.SignPackageAsync(@"C:\waptdev\sample", new WaptExecutionContext { CertificatePassword = "secret-cert" });

            Assert.True(result.WasInteractiveExecutionAttempted);
            Assert.True(result.IsSuccess);
            Assert.False(result.IsConfigurationBlocked);
            Assert.True(commandExecutionService.WasCalled);
            Assert.Contains("sign-package", result.ExecutedCommand);
            Assert.Contains("C:\\waptdev\\sample", result.ExecutedCommand.Replace("\"", string.Empty));
            Assert.DoesNotContain("--private-key", result.ExecutedCommand);
            Assert.Contains("secret-cert", commandExecutionService.LastOptions?.StandardInputText ?? string.Empty);
        }
        finally
        {
            File.Delete(certificatePath);
        }
    }

    [Fact]
    public async Task SignPackageAsync_BlocksWhenSigningKeyUsesCrtExtensionOutsideDryRun()
    {
        var certificatePath = CreateTempCertificateFile(".crt");

        try
        {
            var commandExecutionService = new TrackingCommandExecutionService();
            var settingsService = new TestSettingsService(new AppSettings
            {
                WaptExecutablePath = @"C:\WAPT\wapt-get.exe",
                SignPackageArguments = "sign-package {packageFolder}",
                EnableSigning = true,
                SigningKeyPath = certificatePath,
                DryRunEnabled = false
            });
            var service = new WaptCommandService(commandExecutionService, settingsService);

            var result = await service.SignPackageAsync(@"C:\Packages\Sample");

            Assert.False(result.IsDryRun);
            Assert.False(result.IsSuccess);
            Assert.True(result.IsConfigurationBlocked);
            Assert.False(commandExecutionService.WasCalled);
            Assert.Equal("Le certificat .crt seul n'est pas accepte pour la signature WAPT. Utilisez un fichier .p12 ou .pem.", result.StandardError);
        }
        finally
        {
            File.Delete(certificatePath);
        }
    }

    [Fact]
    public async Task SignPackageAsync_AllowsPemForManualWorkflowOutsideDryRun()
    {
        var certificatePath = CreateTempCertificateFile(".pem");

        try
        {
            var commandExecutionService = new TrackingCommandExecutionService();
            var settingsService = new TestSettingsService(new AppSettings
            {
                WaptExecutablePath = @"C:\WAPT\wapt-get.exe",
                SignPackageArguments = "sign-package {packageFolder}",
                EnableSigning = true,
                SigningKeyPath = certificatePath,
                DryRunEnabled = false
            });
            var service = new WaptCommandService(commandExecutionService, settingsService);

            var result = await service.SignPackageAsync(@"C:\Packages\Sample");

            Assert.False(result.IsDryRun);
            Assert.False(result.IsSuccess);
            Assert.True(result.RequiresExternalManualWorkflow);
            Assert.False(result.IsConfigurationBlocked);
            Assert.False(commandExecutionService.WasCalled);
        }
        finally
        {
            File.Delete(certificatePath);
        }
    }

    [Fact]
    public async Task UploadPackageAsync_BlocksWhenUploadIsDisabledOutsideDryRun()
    {
        var commandExecutionService = new TrackingCommandExecutionService();
        var settingsService = new TestSettingsService(new AppSettings
        {
            WaptExecutablePath = @"C:\WAPT\wapt-get.exe",
            UploadPackageArguments = "upload-package {waptFilePath}",
            EnableUpload = false,
            DryRunEnabled = false
        });
        var service = new WaptCommandService(commandExecutionService, settingsService);

        var result = await service.UploadPackageAsync(packageFolder: @"C:\Packages\Sample", waptFilePath: @"C:\Packages\Sample\sample.wapt");

        Assert.False(result.IsDryRun);
        Assert.False(result.IsSuccess);
        Assert.True(result.IsConfigurationBlocked);
        Assert.False(commandExecutionService.WasCalled);
        Assert.Equal("Upload desactive dans la configuration.", result.StandardError);
    }

    [Fact]
    public async Task UploadPackageAsync_PreparesManualWorkflowWhenAdminCredentialsAreMissingOutsideDryRun()
    {
        var commandExecutionService = new TrackingCommandExecutionService();
        var settingsService = new TestSettingsService(new AppSettings
        {
            WaptExecutablePath = "wapt-get.exe",
            UploadPackageArguments = "upload-package {waptFilePath}",
            EnableUpload = true,
            DryRunEnabled = false
        });
        var service = new WaptCommandService(commandExecutionService, settingsService);

        var result = await service.UploadPackageAsync(packageFolder: @"C:\Packages\Sample", waptFilePath: @"C:\Packages\Sample\sample.wapt");

        Assert.False(result.IsDryRun);
        Assert.False(result.IsSuccess);
        Assert.True(result.RequiresExternalManualWorkflow);
        Assert.False(commandExecutionService.WasCalled);
        Assert.Contains("Upload authentifie", result.StandardError);
    }

    [Fact]
    public async Task UploadPackageAsync_UsesAssistedExecutionWithoutRepositoryWhenCredentialsAreProvided()
    {
        var commandExecutionService = new TrackingCommandExecutionService();
        var settingsService = new TestSettingsService(new AppSettings
        {
            WaptExecutablePath = "wapt-get.exe",
            UploadPackageArguments = "upload-package {waptFilePath}",
            EnableUpload = true,
            DryRunEnabled = false
        });
        var service = new WaptCommandService(commandExecutionService, settingsService);

        var result = await service.UploadPackageAsync(
            packageFolder: @"C:\Packages\Sample",
            waptFilePath: @"C:\Packages\Sample\sample.wapt",
            executionContext: new WaptExecutionContext { AdminUser = "admin-user", AdminPassword = "admin-password" });

        Assert.True(commandExecutionService.WasCalled);
        Assert.True(result.WasInteractiveExecutionAttempted);
        Assert.True(result.IsSuccess);
        Assert.DoesNotContain("repository", result.ExecutedCommand, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("admin-user", commandExecutionService.LastOptions?.StandardInputText ?? string.Empty);
        Assert.Contains("admin-password", commandExecutionService.LastOptions?.StandardInputText ?? string.Empty);
    }

    [Fact]
    public void AppSettings_DoesNotExposePersistentSecretProperties()
    {
        var propertyNames = typeof(AppSettings).GetProperties().Select(property => property.Name).ToArray();

        Assert.DoesNotContain("AdminUser", propertyNames);
        Assert.DoesNotContain("AdminPassword", propertyNames);
        Assert.DoesNotContain("CertificatePassword", propertyNames);
    }

    [Fact]
    public void SensitiveDataSanitizer_RedactsSecretsFromExecutionResults()
    {
        var result = new CommandExecutionResult
        {
            ExecutedCommand = "upload-package sample.wapt",
            StandardOutput = "user admin-user authenticated",
            StandardError = "password admin-password rejected"
        };

        var sanitized = SensitiveDataSanitizer.SanitizeCommandResult(result, ["admin-user", "admin-password"]);

        Assert.DoesNotContain("admin-user", sanitized.StandardOutput);
        Assert.DoesNotContain("admin-password", sanitized.StandardError);
        Assert.Contains("[REDACTED]", sanitized.StandardOutput);
        Assert.Contains("[REDACTED]", sanitized.StandardError);
    }

    private sealed class TrackingCommandExecutionService : ICommandExecutionService
    {
        public bool WasCalled { get; private set; }

        public CommandExecutionOptions? LastOptions { get; private set; }

        public Task<CommandExecutionResult> ExecuteAsync(string fileName, string arguments, string workingDirectory, int timeoutSeconds, CommandExecutionOptions? options = null, CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            LastOptions = options;
            return Task.FromResult(new CommandExecutionResult
            {
                FileName = fileName,
                Arguments = arguments,
                ExecutedCommand = $"{fileName} {arguments}".Trim(),
                WorkingDirectory = workingDirectory,
                ExitCode = 0,
                StandardOutput = "ok",
                StandardError = string.Empty,
                StartedAt = DateTimeOffset.Now,
                Duration = TimeSpan.Zero
            });
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

    private static string CreateTempCertificateFile(string extension)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"waptstudio-sign-test-{Guid.NewGuid():N}{extension}");
        File.WriteAllText(tempPath, "test-certificate");
        return tempPath;
    }
}
