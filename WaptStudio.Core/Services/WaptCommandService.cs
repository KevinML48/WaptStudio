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
        Upload
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

    public async Task<CommandExecutionResult> BuildPackageAsync(string packageFolder, CancellationToken cancellationToken = default)
    {
        var settings = await _settingsService.LoadAsync(cancellationToken).ConfigureAwait(false);
        return await ExecuteTemplateAsync(settings, WaptActionType.Build, settings.BuildPackageArguments, packageFolder, cancellationToken, packageFolder).ConfigureAwait(false);
    }

    public async Task<CommandExecutionResult> SignPackageAsync(string packageFolder, CancellationToken cancellationToken = default)
    {
        var settings = await _settingsService.LoadAsync(cancellationToken).ConfigureAwait(false);
        return await ExecuteTemplateAsync(settings, WaptActionType.Sign, settings.SignPackageArguments, packageFolder, cancellationToken, packageFolder).ConfigureAwait(false);
    }

    public async Task<CommandExecutionResult> UploadPackageAsync(string packageFolder, CancellationToken cancellationToken = default)
    {
        var settings = await _settingsService.LoadAsync(cancellationToken).ConfigureAwait(false);
        return await ExecuteTemplateAsync(settings, WaptActionType.Upload, settings.UploadPackageArguments, packageFolder, cancellationToken, packageFolder).ConfigureAwait(false);
    }

    private async Task<CommandExecutionResult> ExecuteTemplateAsync(
        AppSettings settings,
        WaptActionType actionType,
        string template,
        string workingDirectory,
        CancellationToken cancellationToken,
        string? packageFolder = null)
    {
        var executablePath = string.IsNullOrWhiteSpace(settings.WaptExecutablePath)
            ? CommandExecutionResult.DefaultExecutableName
            : settings.WaptExecutablePath;

        if (string.IsNullOrWhiteSpace(template))
        {
            return CreateBlockedResult(executablePath, string.Empty, workingDirectory, "Aucun argument de commande WAPT n'est configure.");
        }

        var arguments = BuildArguments(template, settings, packageFolder, out var buildError);
        var executedCommand = BuildExecutedCommand(executablePath, arguments);

        if (settings.DryRunEnabled)
        {
            return CreateDryRunResult(executablePath, arguments, executedCommand, workingDirectory);
        }

        var preconditionFailure = ValidateRealExecutionPreconditions(actionType, settings, template, packageFolder, buildError);
        if (!string.IsNullOrWhiteSpace(preconditionFailure))
        {
            return CreateBlockedResult(executablePath, arguments, workingDirectory, preconditionFailure, executedCommand);
        }

        if (RequiresInteractiveBuildConsole(actionType, template))
        {
            return CreateInteractiveResult(
                executablePath,
                arguments,
                workingDirectory,
                "Build reel interactif non supporte dans l'interface. Lancez la commande manuellement dans un terminal pour saisir le mot de passe du certificat.",
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

        return await _commandExecutionService.ExecuteAsync(
            executablePath,
            arguments,
            workingDirectory,
            settings.CommandTimeoutSeconds,
            cancellationToken).ConfigureAwait(false);
    }

    private static string BuildArguments(string template, AppSettings settings, string? packageFolder, out string? buildError)
    {
        buildError = null;
        var replacements = new Dictionary<string, string>
        {
            ["packageFolder"] = packageFolder is null ? string.Empty : Quote(packageFolder),
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

        return Regex.Replace(arguments, @"\s{2,}", " ").Trim();
    }

    private static string? ValidateRealExecutionPreconditions(WaptActionType actionType, AppSettings settings, string template, string? packageFolder, string? buildError)
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
        }

        return null;
    }

    private static bool RequiresInteractiveBuildConsole(WaptActionType actionType, string template)
        => actionType == WaptActionType.Build
            && template.Contains("build-package", StringComparison.OrdinalIgnoreCase);

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

    private static CommandExecutionResult CreateInteractiveResult(string executablePath, string arguments, string workingDirectory, string message, string? executedCommand = null) => new()
    {
        FileName = string.IsNullOrWhiteSpace(executablePath) ? CommandExecutionResult.DefaultExecutableName : executablePath,
        Arguments = arguments,
        ExecutedCommand = string.IsNullOrWhiteSpace(executedCommand) ? BuildExecutedCommand(executablePath, arguments) : executedCommand,
        WorkingDirectory = workingDirectory,
        ExitCode = 0,
        TimedOut = false,
        IsSkipped = true,
        RequiresUserInteraction = true,
        StandardError = message,
        StartedAt = DateTimeOffset.Now,
        Duration = TimeSpan.Zero
    };

    private static string Quote(string value) => $"\"{value}\"";
}