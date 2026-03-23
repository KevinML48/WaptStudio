using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WaptStudio.Core.Configuration;
using WaptStudio.Core.Models;
using WaptStudio.Core.Services.Interfaces;

namespace WaptStudio.Core.Services;

public sealed class PackageValidationService : IPackageValidationService
{
    private readonly IPackageInspectorService _packageInspectorService;
    private readonly ISettingsService _settingsService;
    private readonly IWaptCommandService _waptCommandService;

    public PackageValidationService(IPackageInspectorService packageInspectorService, IWaptCommandService waptCommandService, ISettingsService settingsService)
    {
        _packageInspectorService = packageInspectorService;
        _waptCommandService = waptCommandService;
        _settingsService = settingsService;
    }

    public async Task<ValidationResult> ValidateAsync(string packageFolder, PackageInfo? packageInfo = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(packageFolder) || !Directory.Exists(packageFolder))
        {
            throw new DirectoryNotFoundException("Le dossier du paquet est invalide.");
        }

        packageInfo ??= await _packageInspectorService.AnalyzePackageAsync(packageFolder, cancellationToken).ConfigureAwait(false);
        var settings = await _settingsService.LoadAsync(cancellationToken).ConfigureAwait(false);

        var result = new ValidationResult();

        result.AddOk("Dossier paquet accessible.");

        if (!packageInfo.HasSetupPy)
        {
            result.AddError("Le fichier setup.py est obligatoire.");
        }
        else
        {
            result.AddOk("setup.py present.");
        }

        if (!packageInfo.HasControlFile)
        {
            result.AddError("Le fichier control est obligatoire pour un flux WAPT exploitable.");
        }
        else
        {
            result.AddOk("control present.");
        }

        if (!packageInfo.HasInstaller)
        {
            result.AddError("Aucun fichier MSI/EXE n'est associe au paquet.");
        }
        else
        {
            result.AddOk($"Installeur detecte: {Path.GetFileName(packageInfo.InstallerPath)}");
        }

        if (!string.IsNullOrWhiteSpace(packageInfo.ReferencedInstallerName))
        {
            var hasReferencedInstaller = packageInfo.DetectedExecutables.Any(path => string.Equals(Path.GetFileName(path), packageInfo.ReferencedInstallerName, StringComparison.OrdinalIgnoreCase));
            if (hasReferencedInstaller)
            {
                result.AddOk($"Installeur reference coherent: {packageInfo.ReferencedInstallerName}");
            }
            else
            {
                result.AddError($"Installeur reference introuvable dans le dossier: {packageInfo.ReferencedInstallerName}");
            }
        }
        else
        {
            result.AddWarning("Aucun nom d'installeur reference n'a ete detecte dans setup.py/control.");
        }

        if (string.IsNullOrWhiteSpace(packageInfo.PackageName))
        {
            result.AddWarning("Le nom du paquet n'a pas ete detecte.");
        }
        else
        {
            result.AddOk($"Nom du paquet detecte: {packageInfo.PackageName}");
        }

        if (string.IsNullOrWhiteSpace(packageInfo.Version))
        {
            result.AddWarning("La version du paquet n'a pas ete detectee.");
        }
        else
        {
            result.AddOk($"Version detectee: {packageInfo.Version}");
        }

        if (string.IsNullOrWhiteSpace(settings.WaptExecutablePath))
        {
            result.AddError("Le chemin WAPT n'est pas configure.");
        }

        var backupRoot = AppPaths.ResolveBackupsDirectory(settings);
        try
        {
            Directory.CreateDirectory(backupRoot);
            result.AddOk($"Dossier de sauvegarde accessible: {backupRoot}");
        }
        catch (Exception ex)
        {
            result.AddError($"Dossier de sauvegarde inaccessible: {ex.Message}");
        }

        if (CanWriteToDirectory(packageFolder, out var writeMessage))
        {
            result.AddOk(writeMessage);
        }
        else
        {
            result.AddError(writeMessage);
        }

        var availabilityResult = await _waptCommandService.CheckWaptAvailabilityAsync(cancellationToken).ConfigureAwait(false);
        if (availabilityResult.IsSuccess)
        {
            result.AddOk("WAPT disponible.");
        }
        else
        {
            result.AddError(string.IsNullOrWhiteSpace(availabilityResult.StandardError)
                ? "WAPT indisponible."
                : availabilityResult.StandardError);
        }

        var commandResult = await _waptCommandService.ValidatePackageWithWaptAsync(packageFolder, cancellationToken).ConfigureAwait(false);
        result.CommandResult = commandResult;

        if (commandResult.IsDryRun)
        {
            result.AddInfo(commandResult.StandardOutput);
        }
        else if (commandResult.IsSkipped)
        {
            result.AddWarning(commandResult.StandardError);
        }
        else if (!commandResult.IsSuccess)
        {
            result.AddError(commandResult.StandardError.Length > 0 ? commandResult.StandardError : commandResult.Summary);
        }
        else if (!string.IsNullOrWhiteSpace(commandResult.StandardOutput))
        {
            result.AddOk("Validation WAPT terminee avec succes.");
        }

        return result;
    }

    private static bool CanWriteToDirectory(string packageFolder, out string message)
    {
        var probeFile = Path.Combine(packageFolder, $".waptstudio-write-test-{Guid.NewGuid():N}.tmp");

        try
        {
            File.WriteAllText(probeFile, "waptstudio");
            File.Delete(probeFile);
            message = "Dossier paquet inscriptible.";
            return true;
        }
        catch (Exception ex)
        {
            message = $"Ecriture impossible dans le dossier paquet: {ex.Message}";
            return false;
        }
    }
}
