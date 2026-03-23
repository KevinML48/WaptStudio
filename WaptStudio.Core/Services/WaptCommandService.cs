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
        return await ExecuteTemplateAsync(settings, settings.AvailabilityArguments, Environment.CurrentDirectory, cancellationToken).ConfigureAwait(false);
    }

    public async Task<CommandExecutionResult> ValidatePackageWithWaptAsync(string packageFolder, CancellationToken cancellationToken = default)
    {
        var settings = await _settingsService.LoadAsync(cancellationToken).ConfigureAwait(false);
        return await ExecuteTemplateAsync(settings, settings.ValidatePackageArguments, packageFolder, cancellationToken, packageFolder).ConfigureAwait(false);
    }

    public async Task<CommandExecutionResult> BuildPackageAsync(string packageFolder, CancellationToken cancellationToken = default)
    {
        var settings = await _settingsService.LoadAsync(cancellationToken).ConfigureAwait(false);
        return await ExecuteTemplateAsync(settings, settings.BuildPackageArguments, packageFolder, cancellationToken, packageFolder).ConfigureAwait(false);
    }

    public async Task<CommandExecutionResult> SignPackageAsync(string packageFolder, CancellationToken cancellationToken = default)
    {
        var settings = await _settingsService.LoadAsync(cancellationToken).ConfigureAwait(false);
        return await ExecuteTemplateAsync(settings, settings.SignPackageArguments, packageFolder, cancellationToken, packageFolder).ConfigureAwait(false);
    }

    public async Task<CommandExecutionResult> UploadPackageAsync(string packageFolder, CancellationToken cancellationToken = default)
    {
        var settings = await _settingsService.LoadAsync(cancellationToken).ConfigureAwait(false);
        return await ExecuteTemplateAsync(settings, settings.UploadPackageArguments, packageFolder, cancellationToken, packageFolder).ConfigureAwait(false);
    }

    private async Task<CommandExecutionResult> ExecuteTemplateAsync(
        AppSettings settings,
        string template,
        string workingDirectory,
        CancellationToken cancellationToken,
        string? packageFolder = null)
    {
        if (string.IsNullOrWhiteSpace(settings.WaptExecutablePath))
        {
            return CreateSkippedResult(settings.WaptExecutablePath, string.Empty, workingDirectory, "Le chemin vers l'executable WAPT n'est pas configure.");
        }

        var executablePath = settings.WaptExecutablePath;
        if (Path.IsPathRooted(executablePath) && !File.Exists(executablePath))
        {
            return CreateSkippedResult(executablePath, string.Empty, workingDirectory, $"Executable WAPT introuvable: {executablePath}");
        }

        if (string.IsNullOrWhiteSpace(template))
        {
            return CreateSkippedResult(executablePath, string.Empty, workingDirectory, "Aucun argument de commande WAPT n'est configure.");
        }

        if (!settings.EnableUpload && string.Equals(template, settings.UploadPackageArguments, StringComparison.Ordinal))
        {
            return CreateSkippedResult(executablePath, template, workingDirectory, "L'upload est desactive dans la configuration locale.");
        }

        if (!settings.EnableSigning && string.Equals(template, settings.SignPackageArguments, StringComparison.Ordinal))
        {
            return CreateSkippedResult(executablePath, template, workingDirectory, "La signature est desactivee dans la configuration locale.");
        }

        var arguments = BuildArguments(template, settings, packageFolder, out var buildError);
        if (!string.IsNullOrWhiteSpace(buildError))
        {
            return CreateSkippedResult(executablePath, template, workingDirectory, buildError);
        }

        if (settings.DryRunEnabled)
        {
            return new CommandExecutionResult
            {
                FileName = executablePath,
                Arguments = arguments,
                ExecutedCommand = $"{executablePath} {arguments}".Trim(),
                WorkingDirectory = workingDirectory,
                ExitCode = 0,
                TimedOut = false,
                IsDryRun = true,
                StandardOutput = $"Dry-run: {executablePath} {arguments}".Trim(),
                StartedAt = DateTimeOffset.Now,
                Duration = TimeSpan.Zero
            };
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
        else if (template.Contains("{signingKeyPath}", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(settings.SigningKeyPath))
        {
            buildError = "La cle de signature est requise pour cette commande WAPT.";
        }

        return Regex.Replace(arguments, @"\s{2,}", " ").Trim();
    }

    private static CommandExecutionResult CreateSkippedResult(string executablePath, string arguments, string workingDirectory, string message) => new()
    {
        FileName = string.IsNullOrWhiteSpace(executablePath) ? "wapt-get.exe" : executablePath,
        Arguments = arguments,
        ExecutedCommand = $"{executablePath} {arguments}".Trim(),
        WorkingDirectory = workingDirectory,
        ExitCode = 1,
        TimedOut = false,
        IsSkipped = true,
        StandardError = message,
        StartedAt = DateTimeOffset.Now,
        Duration = TimeSpan.Zero
    };

    private static string Quote(string value) => $"\"{value}\"";
}