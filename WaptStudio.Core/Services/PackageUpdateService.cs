using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WaptStudio.Core.Configuration;
using WaptStudio.Core.Models;
using WaptStudio.Core.Services.Interfaces;

namespace WaptStudio.Core.Services;

public sealed class PackageUpdateService : IPackageUpdateService
{
    private readonly IPackageInspectorService _packageInspectorService;
    private readonly ISettingsService _settingsService;

    public PackageUpdateService(IPackageInspectorService packageInspectorService, ISettingsService settingsService)
    {
        _packageInspectorService = packageInspectorService;
        _settingsService = settingsService;
    }

    public async Task<PackageUpdateResult> ReplaceInstallerAsync(
        PackageInfo packageInfo,
        string newInstallerFilePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(packageInfo);

        if (string.IsNullOrWhiteSpace(newInstallerFilePath) || !File.Exists(newInstallerFilePath))
        {
            throw new FileNotFoundException("Le nouveau fichier d'installation est introuvable.", newInstallerFilePath);
        }

        if (string.IsNullOrWhiteSpace(packageInfo.PackageFolder) || !Directory.Exists(packageInfo.PackageFolder))
        {
            throw new DirectoryNotFoundException("Le dossier du paquet est invalide.");
        }

        var settings = await _settingsService.LoadAsync(cancellationToken).ConfigureAwait(false);
        var extension = Path.GetExtension(newInstallerFilePath);
        if (!string.Equals(extension, ".msi", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(extension, ".exe", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Seuls les fichiers MSI et EXE sont autorises.");
        }

        AppPaths.EnsureCreated(settings);

        var backupDirectory = settings.CreateBackups
            ? Path.Combine(AppPaths.ResolveBackupsDirectory(settings), DateTime.Now.ToString("yyyyMMdd-HHmmss"))
            : null;

        if (backupDirectory is not null)
        {
            Directory.CreateDirectory(backupDirectory);
        }

        if (packageInfo.InstallerPath is not null && File.Exists(packageInfo.InstallerPath) && backupDirectory is not null)
        {
            File.Copy(packageInfo.InstallerPath, Path.Combine(backupDirectory, Path.GetFileName(packageInfo.InstallerPath)), overwrite: true);
        }

        if (packageInfo.SetupPyPath is not null && File.Exists(packageInfo.SetupPyPath) && backupDirectory is not null)
        {
            File.Copy(packageInfo.SetupPyPath, Path.Combine(backupDirectory, Path.GetFileName(packageInfo.SetupPyPath)), overwrite: true);
        }

        if (packageInfo.ControlFilePath is not null && File.Exists(packageInfo.ControlFilePath) && backupDirectory is not null)
        {
            File.Copy(packageInfo.ControlFilePath, Path.Combine(backupDirectory, Path.GetFileName(packageInfo.ControlFilePath)), overwrite: true);
        }

        var packageInstallerDirectory = packageInfo.InstallerPath is null
            ? packageInfo.PackageFolder
            : Path.GetDirectoryName(packageInfo.InstallerPath) ?? packageInfo.PackageFolder;
        var destinationInstallerPath = Path.Combine(packageInstallerDirectory, Path.GetFileName(newInstallerFilePath));

        File.Copy(newInstallerFilePath, destinationInstallerPath, overwrite: true);

        if (packageInfo.InstallerPath is not null
            && !string.Equals(Path.GetFullPath(packageInfo.InstallerPath), Path.GetFullPath(destinationInstallerPath), StringComparison.OrdinalIgnoreCase)
            && File.Exists(packageInfo.InstallerPath))
        {
            var replacedBackupPath = backupDirectory is null
                ? Path.Combine(packageInstallerDirectory, Path.GetFileName(packageInfo.InstallerPath) + ".bak")
                : Path.Combine(backupDirectory, Path.GetFileName(packageInfo.InstallerPath));

            if (!File.Exists(replacedBackupPath))
            {
                File.Copy(packageInfo.InstallerPath, replacedBackupPath, overwrite: true);
            }

            File.Delete(packageInfo.InstallerPath);
        }

        var result = new PackageUpdateResult
        {
            Success = true,
            Message = "Le fichier d'installation a ete remplace.",
            BackupDirectory = backupDirectory
        };
        result.UpdatedFiles.Add(destinationInstallerPath);

        if (packageInfo.SetupPyPath is not null && File.Exists(packageInfo.SetupPyPath))
        {
            var setupContent = await File.ReadAllTextAsync(packageInfo.SetupPyPath, cancellationToken).ConfigureAwait(false);
            var updatedContent = ReplaceInstallerReferences(setupContent, packageInfo.InstallerPath, destinationInstallerPath);
            updatedContent = TryUpdateVersion(updatedContent, newInstallerFilePath);

            if (!string.Equals(setupContent, updatedContent, StringComparison.Ordinal))
            {
                await File.WriteAllTextAsync(packageInfo.SetupPyPath, updatedContent, cancellationToken).ConfigureAwait(false);
                result.UpdatedFiles.Add(packageInfo.SetupPyPath);
            }
        }

        if (packageInfo.ControlFilePath is not null && File.Exists(packageInfo.ControlFilePath))
        {
            var controlContent = await File.ReadAllTextAsync(packageInfo.ControlFilePath, cancellationToken).ConfigureAwait(false);
            var updatedContent = ReplaceInstallerReferences(controlContent, packageInfo.InstallerPath, destinationInstallerPath);
            updatedContent = TryUpdateControlVersion(updatedContent, newInstallerFilePath);

            if (!string.Equals(controlContent, updatedContent, StringComparison.Ordinal))
            {
                await File.WriteAllTextAsync(packageInfo.ControlFilePath, updatedContent, cancellationToken).ConfigureAwait(false);
                result.UpdatedFiles.Add(packageInfo.ControlFilePath);
            }
        }

        result.UpdatedPackageInfo = await _packageInspectorService.AnalyzePackageAsync(packageInfo.PackageFolder, cancellationToken).ConfigureAwait(false);
        return result;
    }

    private static string ReplaceInstallerReferences(string content, string? previousInstallerPath, string newInstallerPath)
    {
        if (string.IsNullOrWhiteSpace(previousInstallerPath))
        {
            return content;
        }

        var oldName = Path.GetFileName(previousInstallerPath);
        var newName = Path.GetFileName(newInstallerPath);
        return content.Replace(oldName, newName, StringComparison.OrdinalIgnoreCase);
    }

    private static string TryUpdateVersion(string content, string installerPath)
    {
        var inferredVersion = InferVersionFromFileName(installerPath);
        if (string.IsNullOrWhiteSpace(inferredVersion))
        {
            return content;
        }

        return Regex.Replace(
            content,
            @"(?im)^(\s*version\s*=\s*['\"\"])(?<value>[^'\"\"]+)(['\"\"])",
            $"$1{inferredVersion}$3");
    }

    private static string TryUpdateControlVersion(string content, string installerPath)
    {
        var inferredVersion = InferVersionFromFileName(installerPath);
        if (string.IsNullOrWhiteSpace(inferredVersion))
        {
            return content;
        }

        return Regex.Replace(
            content,
            @"(?im)^(\s*version\s*[:=]\s*)(?<value>.+)$",
            $"$1{inferredVersion}");
    }

    private static string? InferVersionFromFileName(string installerPath)
    {
        var fileName = Path.GetFileNameWithoutExtension(installerPath);
        var match = Regex.Match(fileName, @"(?<!\d)(\d+(?:[._-]\d+)+)");
        return match.Success
            ? match.Groups[1].Value.Replace('_', '.').Replace('-', '.')
            : null;
    }
}
