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
    private readonly IBackupRestoreService _backupRestoreService;

    public PackageUpdateService(IPackageInspectorService packageInspectorService, ISettingsService settingsService, IBackupRestoreService backupRestoreService)
    {
        _packageInspectorService = packageInspectorService;
        _settingsService = settingsService;
        _backupRestoreService = backupRestoreService;
    }

    public async Task<PackageUpdateResult> ReplaceInstallerAsync(
        PackageInfo packageInfo,
        string newInstallerFilePath,
        CancellationToken cancellationToken = default)
    {
        var synchronizationPlan = await PreviewReplacementAsync(packageInfo, newInstallerFilePath, cancellationToken).ConfigureAwait(false);

        var settings = await _settingsService.LoadAsync(cancellationToken).ConfigureAwait(false);
        var extension = Path.GetExtension(newInstallerFilePath);
        if (!string.Equals(extension, ".msi", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(extension, ".exe", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Seuls les fichiers MSI et EXE sont autorises.");
        }

        AppPaths.EnsureCreated(settings);
        var backup = await _backupRestoreService.CreatePackageBackupAsync(packageInfo, "replace-installer", cancellationToken).ConfigureAwait(false);
        var backupDirectory = backup.BackupDirectory;
        synchronizationPlan.BackupWillBeCreated = true;
        synchronizationPlan.BackupDirectory = backupDirectory;

        var packageInstallerDirectory = packageInfo.InstallerPath is null
            ? packageInfo.PackageFolder
            : Path.GetDirectoryName(packageInfo.InstallerPath) ?? packageInfo.PackageFolder;
        var destinationInstallerPath = Path.Combine(packageInstallerDirectory, Path.GetFileName(newInstallerFilePath));
        var previousInstallerName = synchronizationPlan.CurrentInstallerName;
        var newInstallerName = synchronizationPlan.TargetInstallerName ?? Path.GetFileName(destinationInstallerPath);
        var previousVersion = synchronizationPlan.CurrentVersion;
        var newVersion = synchronizationPlan.TargetVersion;

        File.Copy(newInstallerFilePath, destinationInstallerPath, overwrite: true);

        if (packageInfo.InstallerPath is not null
            && !string.Equals(Path.GetFullPath(packageInfo.InstallerPath), Path.GetFullPath(destinationInstallerPath), StringComparison.OrdinalIgnoreCase)
            && File.Exists(packageInfo.InstallerPath))
        {
            File.Delete(packageInfo.InstallerPath);
        }

        var result = new PackageUpdateResult
        {
            Success = true,
            Message = $"Le fichier d'installation et les metadonnees WAPT ont ete synchronises. Nom .wapt attendu: {synchronizationPlan.ExpectedWaptFileName}",
            BackupDirectory = backupDirectory,
            PreviousPackageFolder = packageInfo.PackageFolder,
            UpdatedPackageFolder = packageInfo.PackageFolder,
            SynchronizationPlan = synchronizationPlan
        };
        result.UpdatedFiles.Add(destinationInstallerPath);
        result.ChangeSummaryLines.AddRange(synchronizationPlan.SummaryLines);

        if (packageInfo.SetupPyPath is not null && File.Exists(packageInfo.SetupPyPath))
        {
            var setupContent = await File.ReadAllTextAsync(packageInfo.SetupPyPath, cancellationToken).ConfigureAwait(false);
            var updatedContent = UpdateSetupPyInstallerReference(setupContent, previousInstallerName, newInstallerName);
            updatedContent = TryUpdateSetupPyVersion(updatedContent, synchronizationPlan.TargetVersion);
            updatedContent = TryUpdateSetupPyInformationalLines(updatedContent, previousVersion, newVersion, previousInstallerName, newInstallerName);

            if (!string.Equals(setupContent, updatedContent, StringComparison.Ordinal))
            {
                await File.WriteAllTextAsync(packageInfo.SetupPyPath, updatedContent, cancellationToken).ConfigureAwait(false);
                result.UpdatedFiles.Add(packageInfo.SetupPyPath);
            }
        }

        if (packageInfo.ControlFilePath is not null && File.Exists(packageInfo.ControlFilePath))
        {
            var controlContent = await File.ReadAllTextAsync(packageInfo.ControlFilePath, cancellationToken).ConfigureAwait(false);
            var updatedContent = SynchronizeControlMetadata(controlContent, synchronizationPlan);

            if (!string.Equals(controlContent, updatedContent, StringComparison.Ordinal))
            {
                await File.WriteAllTextAsync(packageInfo.ControlFilePath, updatedContent, cancellationToken).ConfigureAwait(false);
                result.UpdatedFiles.Add(packageInfo.ControlFilePath);
            }
        }

        var finalPackageFolder = ApplyPackageFolderRename(synchronizationPlan, result);
        if (!string.Equals(finalPackageFolder, packageInfo.PackageFolder, StringComparison.OrdinalIgnoreCase))
        {
            RemapUpdatedFilePaths(result.UpdatedFiles, packageInfo.PackageFolder, finalPackageFolder);
        }

        result.UpdatedPackageInfo = await _packageInspectorService.AnalyzePackageAsync(finalPackageFolder, cancellationToken).ConfigureAwait(false);
        result.UpdatedPackageFolder = finalPackageFolder;
        return result;
    }

    public Task<PackageSynchronizationPlan> PreviewReplacementAsync(
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

        var plan = BuildSynchronizationPlan(packageInfo, newInstallerFilePath);
        return Task.FromResult(plan);
    }

    private static string UpdateSetupPyInstallerReference(string content, string? previousInstallerName, string newInstallerName)
    {
        if (string.IsNullOrWhiteSpace(previousInstallerName))
        {
            return content;
        }

        return Regex.Replace(
            content,
            @"(?im)(install_(?:msi|exe)_if_needed\(\s*['""])(?<value>[^'""]+)(['""])",
            match => string.Equals(match.Groups["value"].Value, previousInstallerName, StringComparison.OrdinalIgnoreCase)
                ? match.Groups[1].Value + newInstallerName + match.Groups[3].Value
                : match.Value);
    }

    private static string TryUpdateSetupPyVersion(string content, string? inferredVersion)
    {
        if (string.IsNullOrWhiteSpace(inferredVersion))
        {
            return content;
        }

        return Regex.Replace(
            content,
            @"(?im)^(\s*version\s*=\s*['""])(?<value>[^'""]+)(['""])",
            "${1}" + inferredVersion + "${3}");
    }

    private static string TryUpdateControlVersion(string content, string? inferredVersion)
    {
        if (string.IsNullOrWhiteSpace(inferredVersion))
        {
            return content;
        }

        return Regex.Replace(
            content,
            @"(?im)^(\s*version\s*[:=]\s*)(?<value>.+)$",
            "${1}" + inferredVersion);
    }

    private static string SynchronizeControlMetadata(string content, PackageSynchronizationPlan plan)
    {
        content = TryUpdateControlVersion(content, plan.TargetVersion);
        content = UpdateControlFieldValue(content, "filename", plan.TargetInstallerName);
        content = UpdateControlFieldValue(content, "name", plan.TargetVisibleName);
        content = UpdateControlFieldValue(content, "description", plan.TargetDescription);
        content = UpdateControlFieldValue(content, "description_fr", plan.TargetDescriptionFr);
        content = UpdateDescriptionVariants(content, plan);
        return content;
    }

    private static string UpdateDescriptionVariants(string content, PackageSynchronizationPlan plan)
    {
        return Regex.Replace(
            content,
            @"(?im)^(\s*(description_[a-z0-9_]+)\s*[:=]\s*)(?<value>.+)$",
            match =>
            {
                var fieldName = match.Groups[2].Value;
                if (string.Equals(fieldName, "description_fr", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(fieldName, "description", StringComparison.OrdinalIgnoreCase))
                {
                    return match.Value;
                }

                var updatedValue = ReplaceVisibleMetadataText(match.Groups["value"].Value.Trim(), plan.CurrentVersion, plan.TargetVersion, plan.CurrentInstallerName, plan.TargetInstallerName ?? string.Empty);
                return match.Groups[1].Value + updatedValue;
            });
    }

    private static string UpdateControlFieldValue(string content, string fieldName, string? targetValue)
    {
        if (string.IsNullOrWhiteSpace(targetValue))
        {
            return content;
        }

        return Regex.Replace(content, $@"(?im)^(\s*{Regex.Escape(fieldName)}\s*[:=]\s*)(?<value>.+)$", "${1}" + targetValue);
    }

    private static string ReplaceVisibleMetadataText(string value, string? previousVersion, string? newVersion, string? previousInstallerName, string newInstallerName)
    {
        var updatedValue = value;

        if (!string.IsNullOrWhiteSpace(previousInstallerName) && !string.IsNullOrWhiteSpace(newInstallerName))
        {
            updatedValue = updatedValue.Replace(previousInstallerName, newInstallerName, StringComparison.OrdinalIgnoreCase);
        }

        if (!string.IsNullOrWhiteSpace(previousVersion) && !string.IsNullOrWhiteSpace(newVersion))
        {
            updatedValue = updatedValue.Replace(previousVersion, newVersion, StringComparison.OrdinalIgnoreCase);
        }

        return updatedValue;
    }

    private static string TryUpdateSetupPyInformationalLines(string content, string? previousVersion, string? newVersion, string? previousInstallerName, string newInstallerName)
    {
        var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        for (var index = 0; index < lines.Length; index++)
        {
            if (!IsSetupPyInformationalLine(lines[index]))
            {
                continue;
            }

            var updatedLine = ReplaceVisibleMetadataText(lines[index], previousVersion, newVersion, previousInstallerName, newInstallerName);
            lines[index] = updatedLine;
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static bool IsSetupPyInformationalLine(string line)
    {
        var trimmed = line.TrimStart();
        return trimmed.StartsWith("print(", StringComparison.Ordinal)
            || trimmed.StartsWith("logger.", StringComparison.Ordinal)
            || trimmed.StartsWith("logging.", StringComparison.Ordinal)
            || trimmed.StartsWith("log(", StringComparison.Ordinal);
    }

    private static string ApplyPackageFolderRename(PackageSynchronizationPlan plan, PackageUpdateResult result)
    {
        if (!plan.PackageFolderRenamePlanned)
        {
            return plan.CurrentPackageFolder;
        }

        result.SuggestedPackageFolder = plan.TargetPackageFolder;
        if (!plan.PackageFolderRenamePossible)
        {
            result.Message += " Renommage du dossier non applique car le nom cible existe deja.";
            return plan.CurrentPackageFolder;
        }

        Directory.Move(plan.CurrentPackageFolder, plan.TargetPackageFolder);
        result.PackageFolderRenamed = true;
        return plan.TargetPackageFolder;
    }

    private static void RemapUpdatedFilePaths(System.Collections.Generic.List<string> updatedFiles, string previousPackageFolder, string updatedPackageFolder)
    {
        for (var index = 0; index < updatedFiles.Count; index++)
        {
            var current = updatedFiles[index];
            if (!current.StartsWith(previousPackageFolder, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            updatedFiles[index] = updatedPackageFolder + current[previousPackageFolder.Length..];
        }
    }

    private static void AddChangeSummary(System.Collections.Generic.List<string> changeSummaryLines, string label, string? previousValue, string? updatedValue)
    {
        if (string.IsNullOrWhiteSpace(previousValue) && string.IsNullOrWhiteSpace(updatedValue))
        {
            return;
        }

        changeSummaryLines.Add($"{label}: {previousValue ?? "N/A"} -> {updatedValue ?? "N/A"}");
    }

    private static PackageSynchronizationPlan BuildSynchronizationPlan(PackageInfo packageInfo, string newInstallerFilePath)
    {
        var targetVersion = InferVersionFromFileName(newInstallerFilePath) ?? packageInfo.Version;
        var currentInstallerName = packageInfo.InstallerPath is null ? packageInfo.ReferencedInstallerName : Path.GetFileName(packageInfo.InstallerPath);
        var targetInstallerName = Path.GetFileName(newInstallerFilePath);
        var packageId = packageInfo.PackageName ?? string.Empty;
        var targetVisibleName = BuildTargetVisibleName(packageInfo, targetVersion, currentInstallerName, targetInstallerName);
        var targetDescription = BuildTargetDescription(packageInfo.Description, packageInfo.VisibleName, targetVisibleName, packageInfo.Version, targetVersion, currentInstallerName, targetInstallerName);
        var targetDescriptionFr = BuildTargetDescription(packageInfo.DescriptionFr, packageInfo.VisibleName, targetVisibleName, packageInfo.Version, targetVersion, currentInstallerName, targetInstallerName);
        var targetPackageFolder = BuildTargetPackageFolder(packageInfo.PackageFolder, packageInfo.Version, targetVersion, out var renamePlanned, out var renamePossible);

        var plan = new PackageSynchronizationPlan
        {
            PackageId = packageId,
            CurrentVersion = packageInfo.Version,
            TargetVersion = targetVersion,
            CurrentInstallerName = currentInstallerName,
            TargetInstallerName = targetInstallerName,
            CurrentInstallerType = packageInfo.InstallerType,
            TargetInstallerType = Path.GetExtension(targetInstallerName).TrimStart('.').ToUpperInvariant(),
            CurrentVisibleName = packageInfo.VisibleName,
            TargetVisibleName = targetVisibleName,
            CurrentDescription = packageInfo.Description,
            TargetDescription = targetDescription,
            CurrentDescriptionFr = packageInfo.DescriptionFr,
            TargetDescriptionFr = targetDescriptionFr,
            CurrentPackageFolder = packageInfo.PackageFolder,
            TargetPackageFolder = targetPackageFolder,
            PackageFolderRenamePlanned = renamePlanned,
            PackageFolderRenamePossible = renamePossible,
            ExpectedWaptFileName = BuildExpectedWaptFileName(packageId, targetVersion),
            ExpectedWaptFileNameNote = "Nom calcule a partir du package id conserve et de la version cible.",
            BackupWillBeCreated = true
        };

        AddChangeSummary(plan.SummaryLines, "Package", packageId, packageId);
        AddChangeSummary(plan.SummaryLines, "Version", plan.CurrentVersion, plan.TargetVersion);
        AddChangeSummary(plan.SummaryLines, "Type", plan.CurrentInstallerType, plan.TargetInstallerType);
        AddChangeSummary(plan.SummaryLines, "MSI/EXE", plan.CurrentInstallerName, plan.TargetInstallerName);
        AddChangeSummary(plan.SummaryLines, "Nom", plan.CurrentVisibleName, plan.TargetVisibleName);
        AddChangeSummary(plan.SummaryLines, "Description", plan.CurrentDescription, plan.TargetDescription);
        AddChangeSummary(plan.SummaryLines, "Description FR", plan.CurrentDescriptionFr, plan.TargetDescriptionFr);
        AddChangeSummary(plan.SummaryLines, "Dossier", plan.CurrentPackageFolder, plan.TargetPackageFolder);
        AddChangeSummary(plan.SummaryLines, ".wapt attendu", null, plan.ExpectedWaptFileName);
        if (!string.IsNullOrWhiteSpace(plan.CurrentInstallerName))
        {
            plan.FilesDeleted.Add(plan.CurrentInstallerName);
        }

        plan.FilesModified.Add("setup.py");
        plan.FilesModified.Add("control");

        if (string.IsNullOrWhiteSpace(targetVersion))
        {
            plan.Warnings.Add("Nouvelle version non inferable de facon fiable: conservation de la version existante.");
        }

        if (string.IsNullOrWhiteSpace(packageInfo.VisibleName))
        {
            plan.Warnings.Add("Nom visible absent dans control: generation a partir du package id et de la version cible.");
        }

        if (string.IsNullOrWhiteSpace(packageInfo.Description))
        {
            plan.Warnings.Add("Description absente dans control: aucune description nouvelle n'a ete inventee.");
        }

        if (renamePlanned && !renamePossible)
        {
            plan.Warnings.Add("Renommage du dossier cible impossible car un dossier du meme nom existe deja.");
        }

        return plan;
    }

    private static string BuildTargetVisibleName(PackageInfo packageInfo, string? targetVersion, string? currentInstallerName, string targetInstallerName)
    {
        if (!string.IsNullOrWhiteSpace(packageInfo.VisibleName))
        {
            return ReplaceVisibleMetadataText(packageInfo.VisibleName, packageInfo.Version, targetVersion, currentInstallerName, targetInstallerName);
        }

        var readablePackageId = string.IsNullOrWhiteSpace(packageInfo.PackageName)
            ? "Package"
            : string.Join(" ", packageInfo.PackageName.Split(new[] { '.', '-', '_' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(segment => char.ToUpperInvariant(segment[0]) + segment[1..]));

        return string.IsNullOrWhiteSpace(targetVersion) ? readablePackageId : $"{readablePackageId} {targetVersion}";
    }

    private static string? BuildTargetDescription(string? currentDescription, string? currentVisibleName, string targetVisibleName, string? currentVersion, string? targetVersion, string? currentInstallerName, string targetInstallerName)
    {
        if (string.IsNullOrWhiteSpace(currentDescription))
        {
            return null;
        }

        return ReplaceVisibleMetadataText(currentDescription, currentVersion, targetVersion, currentInstallerName, targetInstallerName);
    }

    private static string BuildTargetPackageFolder(string currentPackageFolder, string? currentVersion, string? targetVersion, out bool renamePlanned, out bool renamePossible)
    {
        renamePlanned = false;
        renamePossible = false;

        if (string.IsNullOrWhiteSpace(currentVersion) || string.IsNullOrWhiteSpace(targetVersion))
        {
            return currentPackageFolder;
        }

        var folderName = Path.GetFileName(currentPackageFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (string.IsNullOrWhiteSpace(folderName) || !folderName.Contains(currentVersion, StringComparison.OrdinalIgnoreCase))
        {
            return currentPackageFolder;
        }

        var targetFolderName = folderName.Replace(currentVersion, targetVersion, StringComparison.OrdinalIgnoreCase);
        if (string.Equals(folderName, targetFolderName, StringComparison.Ordinal))
        {
            return currentPackageFolder;
        }

        var parentDirectory = Path.GetDirectoryName(currentPackageFolder);
        if (string.IsNullOrWhiteSpace(parentDirectory))
        {
            return currentPackageFolder;
        }

        renamePlanned = true;
        var targetPackageFolder = Path.Combine(parentDirectory, targetFolderName);
        renamePossible = !Directory.Exists(targetPackageFolder);
        return targetPackageFolder;
    }

    private static string BuildExpectedWaptFileName(string packageId, string? targetVersion)
    {
        if (string.IsNullOrWhiteSpace(packageId) || string.IsNullOrWhiteSpace(targetVersion))
        {
            return "Nom .wapt non inferable";
        }

        return $"{packageId}_{targetVersion}.wapt";
    }

    private static string? InferVersionFromFileName(string installerPath)
    {
        var fileName = Path.GetFileNameWithoutExtension(installerPath);
        var match = Regex.Match(fileName, @"(?<!\d)(\d+(?:[._-]\d+)+)");
        if (match.Success)
        {
            return match.Groups[1].Value.Replace('_', '.').Replace('-', '.');
        }

        var compactMatch = Regex.Match(fileName, @"(?<!\d)(\d{4,})(?!\d)");
        return compactMatch.Success ? compactMatch.Groups[1].Value : null;
    }
}
