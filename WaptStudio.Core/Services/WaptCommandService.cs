using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WaptStudio.Core.Models;
using WaptStudio.Core.Services.Interfaces;

namespace WaptStudio.Core.Services;

public sealed class WaptCommandService : IWaptCommandService
{
    private enum WaptActionType
    {
        Availability,
        Validate,
        Build,
        Sign,
        Upload,
        Audit,
        Uninstall
    }

    private readonly ICommandExecutionService _commandExecutionService;
    private readonly ISettingsService _settingsService;

    public WaptCommandService(ICommandExecutionService commandExecutionService, ISettingsService settingsService)
    {
        _commandExecutionService = commandExecutionService;
        _settingsService = settingsService;
    }

    public async Task<CommandExecutionResult> CheckWaptAvailabilityAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _settingsService.LoadAsync(cancellationToken).ConfigureAwait(false);
        return await ExecuteTemplateAsync(settings, WaptActionType.Availability, settings.AvailabilityArguments, Environment.CurrentDirectory, cancellationToken).ConfigureAwait(false);
    }

    public async Task<CommandExecutionResult> ValidatePackageWithWaptAsync(string packageFolder, CancellationToken cancellationToken = default)
    {
        var settings = await _settingsService.LoadAsync(cancellationToken).ConfigureAwait(false);
        return await ExecuteTemplateAsync(settings, WaptActionType.Validate, settings.ValidatePackageArguments, packageFolder, cancellationToken, packageFolder).ConfigureAwait(false);
    }

    public async Task<CommandExecutionResult> BuildPackageAsync(string packageFolder, WaptExecutionContext? executionContext = null, CancellationToken cancellationToken = default)
    {
        var settings = await _settingsService.LoadAsync(cancellationToken).ConfigureAwait(false);
        return await ExecuteTemplateAsync(settings, WaptActionType.Build, settings.BuildPackageArguments, packageFolder, cancellationToken, packageFolder, null, executionContext).ConfigureAwait(false);
    }

    public async Task<CommandExecutionResult> SignPackageAsync(string packageFolder, WaptExecutionContext? executionContext = null, CancellationToken cancellationToken = default)
    {
        var settings = await _settingsService.LoadAsync(cancellationToken).ConfigureAwait(false);
        return await ExecuteTemplateAsync(settings, WaptActionType.Sign, settings.SignPackageArguments, packageFolder, cancellationToken, packageFolder, null, executionContext).ConfigureAwait(false);
    }

    public async Task<CommandExecutionResult> UploadPackageAsync(string packageFolder, string? waptFilePath = null, WaptExecutionContext? executionContext = null, CancellationToken cancellationToken = default)
    {
        var settings = await _settingsService.LoadAsync(cancellationToken).ConfigureAwait(false);
        return await ExecuteTemplateAsync(settings, WaptActionType.Upload, settings.UploadPackageArguments, packageFolder, cancellationToken, packageFolder, waptFilePath, executionContext).ConfigureAwait(false);
    }

    public async Task<CommandExecutionResult> AuditPackageAsync(string packageFolder, string? packageId = null, CancellationToken cancellationToken = default)
    {
        var settings = await _settingsService.LoadAsync(cancellationToken).ConfigureAwait(false);
        return await ExecuteTemplateAsync(settings, WaptActionType.Audit, settings.AuditPackageArguments, packageFolder, cancellationToken, packageFolder, null, null, packageId).ConfigureAwait(false);
    }

    public async Task<CommandExecutionResult> UninstallPackageAsync(string packageFolder, string? packageId = null, CancellationToken cancellationToken = default)
    {
        var settings = await _settingsService.LoadAsync(cancellationToken).ConfigureAwait(false);
        return await ExecuteTemplateAsync(settings, WaptActionType.Uninstall, settings.UninstallPackageArguments, packageFolder, cancellationToken, packageFolder, null, null, packageId).ConfigureAwait(false);
    }

    private async Task<CommandExecutionResult> ExecuteTemplateAsync(
        AppSettings settings,
        WaptActionType actionType,
        string template,
        string workingDirectory,
        CancellationToken cancellationToken,
        string? packageFolder = null,
        string? waptFilePath = null,
        WaptExecutionContext? executionContext = null,
        string? packageId = null)
    {
        var executablePath = string.IsNullOrWhiteSpace(settings.WaptExecutablePath)
            ? CommandExecutionResult.DefaultExecutableName
            : settings.WaptExecutablePath;
        var effectiveTemplate = NormalizeTemplate(actionType, template);

        if (string.IsNullOrWhiteSpace(effectiveTemplate))
        {
            return CreateBlockedResult(executablePath, string.Empty, workingDirectory, "Aucun argument de commande WAPT n'est configure.");
        }

        var resolvedWaptFilePath = executionContext?.WaptFilePath ?? waptFilePath;
        var arguments = BuildArguments(effectiveTemplate, settings, packageFolder, resolvedWaptFilePath, packageId, out var buildError);
        var executedCommand = BuildExecutedCommand(executablePath, arguments);

        if (settings.DryRunEnabled)
        {
            return CreateDryRunResult(executablePath, arguments, executedCommand, workingDirectory);
        }

        var preconditionFailure = ValidateRealExecutionPreconditions(actionType, settings, effectiveTemplate, packageFolder, resolvedWaptFilePath, executionContext, buildError);
        if (!string.IsNullOrWhiteSpace(preconditionFailure))
        {
            return CreateBlockedResult(executablePath, arguments, workingDirectory, preconditionFailure, executedCommand);
        }

        if (RequiresManualWorkflow(actionType, effectiveTemplate, executionContext))
        {
            return CreateManualWorkflowResult(
                executablePath,
                arguments,
                workingDirectory,
                CreateManualWorkflowMessage(actionType),
                executedCommand);
        }

        if (!string.IsNullOrWhiteSpace(buildError))
        {
            return CreateBlockedResult(executablePath, arguments, workingDirectory, buildError, executedCommand);
        }

        if (Path.IsPathRooted(executablePath) && !File.Exists(executablePath))
        {
            return CreateBlockedResult(executablePath, arguments, workingDirectory, $"Executable WAPT introuvable: {executablePath}", executedCommand);
        }

        var result = await _commandExecutionService.ExecuteAsync(
            executablePath,
            arguments,
            workingDirectory,
            settings.CommandTimeoutSeconds,
            BuildExecutionOptions(actionType, executionContext),
            cancellationToken).ConfigureAwait(false);

        return FinalizeExecutionResult(result, actionType);
    }

    private static string BuildArguments(string template, AppSettings settings, string? packageFolder, string? waptFilePath, string? packageId, out string? buildError)
    {
        buildError = null;
        var replacements = new Dictionary<string, string>
        {
            ["packageFolder"] = packageFolder is null ? string.Empty : Quote(packageFolder),
            ["waptFilePath"] = string.IsNullOrWhiteSpace(waptFilePath) ? string.Empty : Quote(waptFilePath),
            ["packageId"] = string.IsNullOrWhiteSpace(packageId) ? string.Empty : packageId,
            ["signingKeyPath"] = string.IsNullOrWhiteSpace(settings.SigningKeyPath) ? string.Empty : Quote(settings.SigningKeyPath),
            ["uploadRepositoryUrl"] = string.IsNullOrWhiteSpace(settings.UploadRepositoryUrl) ? string.Empty : Quote(settings.UploadRepositoryUrl),
            ["repositoryOption"] = string.IsNullOrWhiteSpace(settings.UploadRepositoryUrl) ? string.Empty : $"--repository {Quote(settings.UploadRepositoryUrl)}",
            ["overwriteFlag"] = settings.UploadOverwriteExisting ? "--overwrite" : string.Empty
        };

        var arguments = template;
        foreach (var replacement in replacements)
        {
            arguments = arguments.Replace($"{{{replacement.Key}}}", replacement.Value, StringComparison.OrdinalIgnoreCase);
        }

        if (template.Contains("{packageFolder}", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(packageFolder))
        {
            buildError = "Le dossier du paquet est requis pour cette commande WAPT.";
        }

        if (template.Contains("{waptFilePath}", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(waptFilePath))
        {
            buildError = "Le fichier .wapt est requis pour cette commande WAPT.";
        }

        if (template.Contains("{packageId}", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(packageId))
        {
            buildError = "Le package id est requis pour cette commande WAPT.";
        }

        return Regex.Replace(arguments, @"\s{2,}", " ").Trim();
    }

    private static string NormalizeTemplate(WaptActionType actionType, string template)
    {
        if (actionType != WaptActionType.Sign || string.IsNullOrWhiteSpace(template))
        {
            if (actionType == WaptActionType.Upload && !string.IsNullOrWhiteSpace(template) && template.Contains("upload-package", StringComparison.OrdinalIgnoreCase))
            {
                var normalizedUploadTemplate = template
                    .Replace("{packageFolder}", "{waptFilePath}", StringComparison.OrdinalIgnoreCase)
                    .Replace("{repositoryOption}", string.Empty, StringComparison.OrdinalIgnoreCase)
                    .Replace("{overwriteFlag}", string.Empty, StringComparison.OrdinalIgnoreCase);

                return Regex.Replace(normalizedUploadTemplate, @"\s{2,}", " ").Trim();
            }

            return template;
        }

        if (!template.Contains("sign-package", StringComparison.OrdinalIgnoreCase))
        {
            return template;
        }

        var normalizedTemplate = Regex.Replace(
            template,
            @"\s*--private-key\s+(?:\{signingKeyPath\}|""[^""]+""|\S+)",
            string.Empty,
            RegexOptions.IgnoreCase);

        return string.IsNullOrWhiteSpace(normalizedTemplate)
            ? "sign-package {packageFolder}"
            : Regex.Replace(normalizedTemplate, @"\s{2,}", " ").Trim();
    }

    private static string? ValidateRealExecutionPreconditions(WaptActionType actionType, AppSettings settings, string template, string? packageFolder, string? waptFilePath, WaptExecutionContext? executionContext, string? buildError)
    {
        if (string.IsNullOrWhiteSpace(settings.WaptExecutablePath))
        {
            return "Le chemin vers l'executable WAPT n'est pas configure.";
        }

        if (string.IsNullOrWhiteSpace(template))
        {
            return "Aucun argument de commande WAPT n'est configure.";
        }

        if (!string.IsNullOrWhiteSpace(buildError))
        {
            return buildError;
        }

        if (actionType == WaptActionType.Sign)
        {
            if (!settings.EnableSigning)
            {
                return "Signature desactivee dans la configuration.";
            }

            if (string.IsNullOrWhiteSpace(settings.SigningKeyPath))
            {
                return "Cle de signature non renseignee.";
            }

            var signingKeyValidationFailure = ValidateSigningKeyPath(settings.SigningKeyPath);
            if (!string.IsNullOrWhiteSpace(signingKeyValidationFailure))
            {
                return signingKeyValidationFailure;
            }

            if (Path.IsPathRooted(settings.SigningKeyPath) && !File.Exists(settings.SigningKeyPath))
            {
                return "Cle de signature introuvable.";
            }
        }

        if (actionType == WaptActionType.Upload)
        {
            if (!settings.EnableUpload)
            {
                return "Upload desactive dans la configuration.";
            }

            var repositoryRequired = template.Contains("{uploadRepositoryUrl}", StringComparison.OrdinalIgnoreCase)
                || template.Contains("{repositoryOption}", StringComparison.OrdinalIgnoreCase);

            if (repositoryRequired && string.IsNullOrWhiteSpace(settings.UploadRepositoryUrl))
            {
                return "Repository d'upload non renseigne.";
            }

            if (string.IsNullOrWhiteSpace(packageFolder))
            {
                return "Parametres d'upload incomplets.";
            }

            if (string.IsNullOrWhiteSpace(waptFilePath))
            {
                return "Fichier .wapt non renseigne pour l'upload.";
            }
        }

        if ((actionType == WaptActionType.Audit || actionType == WaptActionType.Uninstall) && string.IsNullOrWhiteSpace(template))
        {
            return "Commande WAPT non configuree pour cette action.";
        }

        if ((actionType == WaptActionType.Build || actionType == WaptActionType.Sign) && RequiresInteractiveInput(actionType, template) && executionContext?.HasCertificatePassword != true)
        {
            return null;
        }

        if (actionType == WaptActionType.Upload && executionContext?.HasAdminCredentials != true)
        {
            return null;
        }

        return null;
    }

    private static string? ValidateSigningKeyPath(string signingKeyPath)
    {
        var extension = Path.GetExtension(signingKeyPath)?.ToLowerInvariant();

        return extension switch
        {
            ".p12" => null,
            ".pem" => null,
            ".crt" => "Le certificat .crt seul n'est pas accepte pour la signature WAPT. Utilisez un fichier .p12 ou .pem.",
            _ => "Format de cle/certificat non supporte pour la signature WAPT. Utilisez un fichier .p12 ou .pem."
        };
    }

    private static bool RequiresInteractiveInput(WaptActionType actionType, string template)
        => (actionType == WaptActionType.Build && template.Contains("build-package", StringComparison.OrdinalIgnoreCase))
            || (actionType == WaptActionType.Sign && template.Contains("sign-package", StringComparison.OrdinalIgnoreCase))
            || (actionType == WaptActionType.Upload && template.Contains("upload-package", StringComparison.OrdinalIgnoreCase));

    private static bool RequiresManualWorkflow(WaptActionType actionType, string template, WaptExecutionContext? executionContext)
    {
        if (!RequiresInteractiveInput(actionType, template))
        {
            return false;
        }

        return actionType switch
        {
            WaptActionType.Build => executionContext?.HasCertificatePassword != true,
            WaptActionType.Sign => executionContext?.HasCertificatePassword != true,
            WaptActionType.Upload => executionContext?.HasAdminCredentials != true,
            _ => false
        };
    }

    private static string CreateManualWorkflowMessage(WaptActionType actionType)
        => actionType switch
        {
            WaptActionType.Build => "Build interactif a preparer dans un terminal externe si l'execution assistee n'est pas disponible.",
            WaptActionType.Sign => "Signature interactive a preparer dans un terminal externe si l'execution assistee n'est pas disponible.",
            WaptActionType.Upload => "Upload authentifie a preparer dans un terminal externe si l'execution assistee n'est pas disponible.",
            _ => "Interaction utilisateur requise pour terminer la commande."
        };

    private static CommandExecutionOptions? BuildExecutionOptions(WaptActionType actionType, WaptExecutionContext? executionContext)
    {
        if (executionContext is null)
        {
            return null;
        }

        var standardInputText = actionType switch
        {
            WaptActionType.Build when executionContext.HasCertificatePassword => executionContext.CertificatePassword + Environment.NewLine,
            WaptActionType.Sign when executionContext.HasCertificatePassword => executionContext.CertificatePassword + Environment.NewLine,
            WaptActionType.Upload when executionContext.HasAdminCredentials => executionContext.AdminUser + Environment.NewLine + executionContext.AdminPassword + Environment.NewLine,
            _ => null
        };

        if (string.IsNullOrWhiteSpace(standardInputText))
        {
            return null;
        }

        return new CommandExecutionOptions
        {
            StandardInputText = standardInputText,
            SensitiveValuesToRedact = executionContext.GetSensitiveValues()
        };
    }

    private static CommandExecutionResult FinalizeExecutionResult(CommandExecutionResult result, WaptActionType actionType)
    {
        if (!RequiresInteractiveInput(actionType, result.Arguments))
        {
            return result;
        }

        var interactiveResult = new CommandExecutionResult
        {
            FileName = result.FileName,
            Arguments = result.Arguments,
            ExecutedCommand = result.ExecutedCommand,
            WorkingDirectory = result.WorkingDirectory,
            ExitCode = result.ExitCode,
            TimedOut = result.TimedOut,
            IsDryRun = result.IsDryRun,
            IsSkipped = result.IsSkipped,
            IsConfigurationBlocked = result.IsConfigurationBlocked,
            WasInteractiveExecutionAttempted = true,
            ManualFallbackRecommended = !result.IsSuccess,
            StandardOutput = result.StandardOutput,
            StandardError = !result.IsSuccess && string.IsNullOrWhiteSpace(result.StandardError)
                ? CreateAutomationFailureMessage(actionType)
                : AppendFallbackRecommendation(result.StandardError, actionType, result.IsSuccess),
            StartedAt = result.StartedAt,
            Duration = result.Duration
        };

        return interactiveResult;
    }

    private static string CreateAutomationFailureMessage(WaptActionType actionType)
        => actionType switch
        {
            WaptActionType.Build => "Le build assiste n'a pas abouti. Utilisez le workflow manuel si WAPT refuse l'automatisation non interactive.",
            WaptActionType.Sign => "La signature assistee n'a pas abouti. Utilisez le workflow manuel si WAPT refuse l'automatisation non interactive.",
            WaptActionType.Upload => "L'upload assiste n'a pas abouti. Utilisez le workflow manuel si WAPT refuse l'automatisation non interactive.",
            _ => "L'execution assistee n'a pas abouti."
        };

    private static string AppendFallbackRecommendation(string error, WaptActionType actionType, bool isSuccess)
    {
        if (isSuccess)
        {
            return error;
        }

        var recommendation = CreateAutomationFailureMessage(actionType);
        if (string.IsNullOrWhiteSpace(error))
        {
            return recommendation;
        }

        return error.Contains(recommendation, StringComparison.OrdinalIgnoreCase)
            ? error
            : error + Environment.NewLine + recommendation;
    }

    private static string BuildExecutedCommand(string executablePath, string arguments) => $"{executablePath} {arguments}".Trim();

    private static CommandExecutionResult CreateDryRunResult(string executablePath, string arguments, string executedCommand, string workingDirectory) => new()
    {
        FileName = executablePath,
        Arguments = arguments,
        ExecutedCommand = executedCommand,
        WorkingDirectory = workingDirectory,
        ExitCode = 0,
        TimedOut = false,
        IsDryRun = true,
        StandardOutput = $"Dry-run: {executedCommand}",
        StartedAt = DateTimeOffset.Now,
        Duration = TimeSpan.Zero
    };

    private static CommandExecutionResult CreateBlockedResult(string executablePath, string arguments, string workingDirectory, string message, string? executedCommand = null) => new()
    {
        FileName = string.IsNullOrWhiteSpace(executablePath) ? CommandExecutionResult.DefaultExecutableName : executablePath,
        Arguments = arguments,
        ExecutedCommand = string.IsNullOrWhiteSpace(executedCommand) ? BuildExecutedCommand(executablePath, arguments) : executedCommand,
        WorkingDirectory = workingDirectory,
        ExitCode = 0,
        TimedOut = false,
        IsSkipped = true,
        IsConfigurationBlocked = true,
        StandardError = message,
        StartedAt = DateTimeOffset.Now,
        Duration = TimeSpan.Zero
    };

    private static CommandExecutionResult CreateManualWorkflowResult(string executablePath, string arguments, string workingDirectory, string message, string? executedCommand = null) => new()
    {
        FileName = string.IsNullOrWhiteSpace(executablePath) ? CommandExecutionResult.DefaultExecutableName : executablePath,
        Arguments = arguments,
        ExecutedCommand = string.IsNullOrWhiteSpace(executedCommand) ? BuildExecutedCommand(executablePath, arguments) : executedCommand,
        WorkingDirectory = workingDirectory,
        ExitCode = 0,
        TimedOut = false,
        IsSkipped = true,
        RequiresExternalManualWorkflow = true,
        StandardError = message,
        StartedAt = DateTimeOffset.Now,
        Duration = TimeSpan.Zero
    };

    private static string Quote(string value) => $"\"{value}\"";
}