using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WaptStudio.Core.Configuration;
using WaptStudio.Core.Models;
using WaptStudio.Core.Utilities;
using WaptStudio.Core.Services.Interfaces;

namespace WaptStudio.Core.Services;

public sealed class PackageUpdateService : IPackageUpdateService
{
    private static readonly HashSet<string> SkippedCloneDirectoryNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git",
        ".vs",
        "__pycache__",
        ".waptstudio-backups"
    };

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
        PackageVersionSelection? versionSelection = null,
        CancellationToken cancellationToken = default)
    {
        var synchronizationPlan = await PreviewReplacementAsync(packageInfo, newInstallerFilePath, versionSelection, cancellationToken).ConfigureAwait(false);

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

        var result = new PackageUpdateResult
        {
            Success = true,
            Message = $"Le fichier d'installation et les metadonnees WAPT ont ete synchronises. Nom .wapt attendu: {synchronizationPlan.ExpectedWaptFileName}",
            BackupDirectory = backupDirectory,
            PreviousPackageFolder = packageInfo.PackageFolder,
            UpdatedPackageFolder = packageInfo.PackageFolder,
            SynchronizationPlan = synchronizationPlan
        };

        var workingPackageFolder = PrepareWorkingPackageFolder(packageInfo, synchronizationPlan, result);
        result.UpdatedPackageFolder = workingPackageFolder;

        var packageInstallerDirectory = ResolveWorkingInstallerDirectory(packageInfo, workingPackageFolder);
        var destinationInstallerPath = Path.Combine(packageInstallerDirectory, Path.GetFileName(newInstallerFilePath));
        var previousInstallerName = synchronizationPlan.CurrentInstallerName;
        var newInstallerName = synchronizationPlan.TargetInstallerName ?? Path.GetFileName(destinationInstallerPath);
        var previousVersion = synchronizationPlan.CurrentVersion;
        var newVersion = synchronizationPlan.TargetVersion;

        File.Copy(newInstallerFilePath, destinationInstallerPath, overwrite: true);

        var workingInstallerPath = MapPathToWorkingPackageFolder(packageInfo.InstallerPath, packageInfo.PackageFolder, workingPackageFolder);
        if (workingInstallerPath is not null
            && !string.Equals(Path.GetFullPath(workingInstallerPath), Path.GetFullPath(destinationInstallerPath), StringComparison.OrdinalIgnoreCase)
            && File.Exists(workingInstallerPath))
        {
            File.Delete(workingInstallerPath);
        }

        result.UpdatedFiles.Add(destinationInstallerPath);
        result.ChangeSummaryLines.AddRange(synchronizationPlan.SummaryLines);

        var workingSetupPyPath = MapPathToWorkingPackageFolder(packageInfo.SetupPyPath, packageInfo.PackageFolder, workingPackageFolder);
        if (workingSetupPyPath is not null && File.Exists(workingSetupPyPath))
        {
            var setupContent = await File.ReadAllTextAsync(workingSetupPyPath, cancellationToken).ConfigureAwait(false);
            var updatedContent = UpdateSetupPyInstallerReference(setupContent, previousInstallerName, newInstallerName);
            updatedContent = TryUpdateSetupPyVersion(updatedContent, synchronizationPlan.TargetVersion);
            updatedContent = TryUpdateSetupPyInformationalLines(updatedContent, previousVersion, newVersion, previousInstallerName, newInstallerName);

            if (!string.Equals(setupContent, updatedContent, StringComparison.Ordinal))
            {
                await File.WriteAllTextAsync(workingSetupPyPath, updatedContent, cancellationToken).ConfigureAwait(false);
                result.UpdatedFiles.Add(workingSetupPyPath);
            }

            var validatedContent = await File.ReadAllTextAsync(workingSetupPyPath, cancellationToken).ConfigureAwait(false);
            EnsureSetupPyInstallerReferenceIsCoherent(validatedContent, newInstallerName);
        }

        var workingControlFilePath = MapPathToWorkingPackageFolder(packageInfo.ControlFilePath, packageInfo.PackageFolder, workingPackageFolder);
        if (workingControlFilePath is not null && File.Exists(workingControlFilePath))
        {
            var controlContent = await File.ReadAllTextAsync(workingControlFilePath, cancellationToken).ConfigureAwait(false);
            var updatedContent = SynchronizeControlMetadata(controlContent, synchronizationPlan);
            EnsureControlPackageIsUnchanged(controlContent, updatedContent);

            if (!string.Equals(controlContent, updatedContent, StringComparison.Ordinal))
            {
                await File.WriteAllTextAsync(workingControlFilePath, updatedContent, cancellationToken).ConfigureAwait(false);
                result.UpdatedFiles.Add(workingControlFilePath);
            }
        }

        var finalPackageFolder = ApplyPackageFolderRename(synchronizationPlan, result, workingPackageFolder);
        if (!string.Equals(finalPackageFolder, workingPackageFolder, StringComparison.OrdinalIgnoreCase))
        {
            RemapUpdatedFilePaths(result.UpdatedFiles, workingPackageFolder, finalPackageFolder);
        }

        result.UpdatedPackageInfo = await _packageInspectorService.AnalyzePackageAsync(finalPackageFolder, cancellationToken).ConfigureAwait(false);
        result.UpdatedPackageFolder = finalPackageFolder;
        return result;
    }

    public Task<PackageSynchronizationPlan> PreviewReplacementAsync(
        PackageInfo packageInfo,
        string newInstallerFilePath,
        PackageVersionSelection? versionSelection = null,
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

        var plan = BuildSynchronizationPlan(packageInfo, newInstallerFilePath, versionSelection);
        return Task.FromResult(plan);
    }

    private static string UpdateSetupPyInstallerReference(string content, string? previousInstallerName, string newInstallerName)
        => SetupPyInstallerReferenceParser.UpdateInstallerReference(content, previousInstallerName, newInstallerName);

    private static void EnsureSetupPyInstallerReferenceIsCoherent(string content, string newInstallerName)
    {
        var newExtension = Path.GetExtension(newInstallerName).TrimStart('.').ToLowerInvariant();
        var expectedFunction = newExtension == "exe" ? "install_exe_if_needed" : "install_msi_if_needed";
        var references = SetupPyInstallerReferenceParser.ParseInstallReferences(content);
        if (references.Count == 0)
        {
            throw new InvalidOperationException($"setup.py ne reference plus d'appel {expectedFunction} apres remplacement.");
        }

        if (!SetupPyInstallerReferenceParser.HasCoherentInstallerReference(content, newInstallerName))
        {
            throw new InvalidOperationException("setup.py ne reference pas exactement le nouvel installeur apres remplacement.");
        }
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

    private static void EnsureControlPackageIsUnchanged(string originalContent, string updatedContent)
    {
        var originalPackage = ExtractControlFieldValue(originalContent, "package");
        var updatedPackage = ExtractControlFieldValue(updatedContent, "package");
        if (!string.Equals(originalPackage, updatedPackage, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Le champ control/package ne peut pas etre modifie automatiquement.");
        }
    }

    private static string? ExtractControlFieldValue(string content, string fieldName)
    {
        var match = Regex.Match(content, $@"(?im)^\s*{Regex.Escape(fieldName)}\s*[:=]\s*(?<value>.+)$");
        return match.Success ? match.Groups["value"].Value.Trim() : null;
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

    private static string ApplyPackageFolderRename(PackageSynchronizationPlan plan, PackageUpdateResult result, string workingPackageFolder)
    {
        if (plan.PackageFolderClonePlanned)
        {
            return workingPackageFolder;
        }

        if (!plan.PackageFolderRenamePlanned)
        {
            return workingPackageFolder;
        }

        result.SuggestedPackageFolder = plan.TargetPackageFolder;
        if (!plan.PackageFolderRenamePossible)
        {
            result.Message += " Renommage du dossier non applique car le nom cible existe deja.";
            return plan.CurrentPackageFolder;
        }

        Directory.Move(workingPackageFolder, plan.TargetPackageFolder);
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

    private static PackageSynchronizationPlan BuildSynchronizationPlan(PackageInfo packageInfo, string newInstallerFilePath, PackageVersionSelection? versionSelection)
    {
        var inferredInstallerVersion = PackageVersioning.InferVersionFromInstallerFileName(newInstallerFilePath);
        var targetVersion = ResolveTargetVersion(packageInfo, inferredInstallerVersion, versionSelection);
        var currentInstallerName = packageInfo.InstallerPath is null ? packageInfo.ReferencedInstallerName : Path.GetFileName(packageInfo.InstallerPath);
        var targetInstallerName = Path.GetFileName(newInstallerFilePath);
        var packageId = packageInfo.PackageName ?? string.Empty;
        var targetVisibleName = BuildTargetVisibleName(packageInfo, targetVersion, currentInstallerName, targetInstallerName);
        var targetDescription = BuildTargetDescription(packageInfo.Description, packageInfo.VisibleName, targetVisibleName, packageInfo.Version, targetVersion, currentInstallerName, targetInstallerName);
        var targetDescriptionFr = BuildTargetDescription(packageInfo.DescriptionFr, packageInfo.VisibleName, targetVisibleName, packageInfo.Version, targetVersion, currentInstallerName, targetInstallerName);
        var suggestedVersionedPackageFolder = BuildTargetPackageFolder(packageInfo.PackageFolder, packageId, packageInfo.Version, targetVersion, out var renameCandidatePlanned, out var renameCandidatePossible);
        var folderUpdateMode = ResolveFolderUpdateMode(packageInfo.PackageFolder, suggestedVersionedPackageFolder, packageInfo.Version, targetVersion, versionSelection?.FolderUpdateMode);
        var versionChanges = !string.IsNullOrWhiteSpace(packageInfo.Version)
            && !string.IsNullOrWhiteSpace(targetVersion)
            && !string.Equals(packageInfo.Version, targetVersion, StringComparison.OrdinalIgnoreCase);
        var clonePlanned = folderUpdateMode == PackageFolderUpdateMode.CreateVersionedFolderClone
            && !string.Equals(packageInfo.PackageFolder, suggestedVersionedPackageFolder, StringComparison.OrdinalIgnoreCase)
            && versionChanges;
        var clonePossible = !clonePlanned || !Directory.Exists(suggestedVersionedPackageFolder);
        var renamePlanned = renameCandidatePlanned
            && (!versionChanges || folderUpdateMode != PackageFolderUpdateMode.UpdateCurrentFolder)
            && !clonePlanned;
        var renamePossible = !renamePlanned || renameCandidatePossible;
        var targetPackageFolder = clonePlanned || renamePlanned
            ? suggestedVersionedPackageFolder
            : packageInfo.PackageFolder;

        var expectedVersion = targetVersion ?? packageInfo.Version;
        var plan = new PackageSynchronizationPlan
        {
            PackageId = packageId,
            VersionStrategy = versionSelection?.Strategy ?? PackageVersionStrategy.KeepCurrentVersion,
            VersionStrategyLabel = PackageVersioning.DescribeStrategy(versionSelection?.Strategy ?? PackageVersionStrategy.KeepCurrentVersion),
            FolderUpdateMode = folderUpdateMode,
            FolderUpdateModeLabel = DescribeFolderUpdateMode(folderUpdateMode),
            SuggestedVersion = inferredInstallerVersion,
            SuggestedVersionedPackageFolder = string.Equals(packageInfo.PackageFolder, suggestedVersionedPackageFolder, StringComparison.OrdinalIgnoreCase)
                ? null
                : suggestedVersionedPackageFolder,
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
            PackageFolderClonePlanned = clonePlanned,
            PackageFolderClonePossible = clonePossible,
            ExpectedWaptFileName = WaptNaming.BuildExpectedWaptFileName(
                targetPackageFolder,
                packageId,
                expectedVersion,
                packageInfo.TargetOs,
                packageInfo.Maturity,
                packageInfo.Architecture),
            ExpectedWaptFileNameNote = "Nom calcule a partir du package id, de la version cible et du contexte (OS/maturite).",
            BackupWillBeCreated = true
        };

        AddChangeSummary(plan.SummaryLines, "Package", packageId, packageId);
        AddChangeSummary(plan.SummaryLines, "Strategie de version", null, plan.VersionStrategyLabel);
        AddChangeSummary(plan.SummaryLines, "Mode dossier", null, plan.FolderUpdateModeLabel);
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

        if (!string.IsNullOrWhiteSpace(packageInfo.Version)
            && !string.IsNullOrWhiteSpace(inferredInstallerVersion)
            && !string.Equals(packageInfo.Version, inferredInstallerVersion, StringComparison.OrdinalIgnoreCase))
        {
            if (versionSelection is null || versionSelection.Strategy != PackageVersionStrategy.SetExplicitVersion)
            {
                plan.Warnings.Add($"Le nouvel installeur suggere {inferredInstallerVersion}, mais la version WAPT cible reste {targetVersion ?? packageInfo.Version}. Aucun changement silencieux n'est applique.");
            }
        }

        if (versionSelection?.Strategy == PackageVersionStrategy.SetExplicitVersion && !string.IsNullOrWhiteSpace(versionSelection.ExplicitVersion))
        {
            plan.Warnings.Add($"Version cible definie explicitement par l'utilisateur: {plan.TargetVersion}.");
        }

        if (string.IsNullOrWhiteSpace(packageInfo.VisibleName))
        {
            plan.Warnings.Add("Nom visible absent dans control: generation a partir du package id et de la version cible.");
        }

        if (string.IsNullOrWhiteSpace(packageInfo.Description))
        {
            plan.Warnings.Add("Description absente dans control: aucune description nouvelle n'a ete inventee.");
        }

        if (clonePlanned && !clonePossible)
        {
            plan.Warnings.Add("Creation du nouveau dossier impossible car le dossier cible existe deja.");
        }

        if (renamePlanned && !renamePossible && !clonePlanned)
        {
            plan.Warnings.Add("Renommage du dossier cible impossible car un dossier du meme nom existe deja.");
        }

        if (clonePlanned)
        {
            plan.Warnings.Add("Le nouveau dossier sera cree a partir d'une copie complete du paquet existant, puis seules les metadonnees cibles seront mises a jour.");
        }

        return plan;
    }

    private static PackageFolderUpdateMode ResolveFolderUpdateMode(string currentPackageFolder, string suggestedVersionedPackageFolder, string? currentVersion, string? targetVersion, PackageFolderUpdateMode? requestedMode)
    {
        if (requestedMode.HasValue)
        {
            return requestedMode.Value;
        }

        var versionChanges = !string.IsNullOrWhiteSpace(currentVersion)
            && !string.IsNullOrWhiteSpace(targetVersion)
            && !string.Equals(currentVersion, targetVersion, StringComparison.OrdinalIgnoreCase);
        var suggestedFolderDiffers = !string.Equals(currentPackageFolder, suggestedVersionedPackageFolder, StringComparison.OrdinalIgnoreCase);

        return versionChanges && suggestedFolderDiffers
            ? PackageFolderUpdateMode.CreateVersionedFolderClone
            : PackageFolderUpdateMode.UpdateCurrentFolder;
    }

    private static string DescribeFolderUpdateMode(PackageFolderUpdateMode mode)
        => mode switch
        {
            PackageFolderUpdateMode.CreateVersionedFolderClone => "Creer un nouveau dossier versionne en conservant le contenu existant",
            _ => "Mettre a jour dans le dossier actuel"
        };

    private static string? ResolveTargetVersion(PackageInfo packageInfo, string? inferredInstallerVersion, PackageVersionSelection? versionSelection)
    {
        if (versionSelection is null)
        {
            return string.IsNullOrWhiteSpace(packageInfo.Version)
                ? inferredInstallerVersion
                : packageInfo.Version;
        }

        return versionSelection.Strategy switch
        {
            PackageVersionStrategy.KeepCurrentVersion => packageInfo.Version ?? throw new InvalidOperationException("Aucune version actuelle n'est disponible a conserver pour ce paquet."),
            PackageVersionStrategy.IncrementPackageRevision => ResolveIncrementedRevision(packageInfo.Version),
            PackageVersionStrategy.SetExplicitVersion => ResolveExplicitVersion(versionSelection.ExplicitVersion, packageInfo.Version),
            _ => packageInfo.Version
        };
    }

    private static string ResolveIncrementedRevision(string? currentVersion)
    {
        if (PackageVersioning.TryIncrementPackageRevision(currentVersion, out var normalizedVersion, out var errorMessage))
        {
            return normalizedVersion;
        }

        throw new InvalidOperationException(errorMessage);
    }

    private static string ResolveExplicitVersion(string? explicitVersion, string? currentVersion)
    {
        if (PackageVersioning.TryNormalizeExplicitVersion(explicitVersion, currentVersion, out var normalizedVersion, out var errorMessage))
        {
            return normalizedVersion;
        }

        throw new InvalidOperationException(errorMessage);
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

    private static string BuildTargetPackageFolder(string currentPackageFolder, string? packageId, string? currentVersion, string? targetVersion, out bool renamePlanned, out bool renamePossible)
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

        var versionIndex = folderName.IndexOf(currentVersion, StringComparison.OrdinalIgnoreCase);
        if (versionIndex <= 0)
        {
            return currentPackageFolder;
        }

        var prefixWithSeparator = folderName[..versionIndex];
        var versionSuffix = folderName[(versionIndex + currentVersion.Length)..];
        var versionSeparator = prefixWithSeparator[^1];
        if (versionSeparator is not '_' and not '-')
        {
            return currentPackageFolder;
        }

        var fallbackPrefix = prefixWithSeparator[..^1].TrimEnd('_', '-');
        var stablePrefix = BuildStablePackageFolderPrefix(packageId, fallbackPrefix);
        var targetFolderName = string.Concat(stablePrefix, versionSeparator, targetVersion, versionSuffix);
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

    private static string BuildStablePackageFolderPrefix(string? packageId, string fallbackPrefix)
    {
        if (string.IsNullOrWhiteSpace(packageId))
        {
            return fallbackPrefix;
        }

        var normalized = Regex.Replace(packageId.Trim(), @"[\s._]+", "-");
        normalized = Regex.Replace(normalized, @"-{2,}", "-");
        normalized = normalized.Trim('-');
        return string.IsNullOrWhiteSpace(normalized) ? fallbackPrefix : normalized;
    }

    private static string PrepareWorkingPackageFolder(PackageInfo packageInfo, PackageSynchronizationPlan plan, PackageUpdateResult result)
    {
        if (plan.PackageFolderClonePlanned)
        {
            result.SuggestedPackageFolder = plan.TargetPackageFolder;
            if (!plan.PackageFolderClonePossible)
            {
                throw new InvalidOperationException("Le dossier versionne cible existe deja. Impossible de cloner proprement le paquet sans ecraser un dossier existant.");
            }

            ClonePackageFolder(packageInfo.PackageFolder, plan.TargetPackageFolder);
            result.PackageFolderCloned = true;
            return plan.TargetPackageFolder;
        }

        return packageInfo.PackageFolder;
    }

    private static string ResolveWorkingInstallerDirectory(PackageInfo packageInfo, string workingPackageFolder)
    {
        if (packageInfo.InstallerPath is null)
        {
            return workingPackageFolder;
        }

        var sourceDirectory = Path.GetDirectoryName(packageInfo.InstallerPath) ?? packageInfo.PackageFolder;
        var relativeDirectory = Path.GetRelativePath(packageInfo.PackageFolder, sourceDirectory);
        return string.Equals(relativeDirectory, ".", StringComparison.Ordinal)
            ? workingPackageFolder
            : Path.Combine(workingPackageFolder, relativeDirectory);
    }

    private static string? MapPathToWorkingPackageFolder(string? originalPath, string sourcePackageFolder, string workingPackageFolder)
    {
        if (string.IsNullOrWhiteSpace(originalPath))
        {
            return null;
        }

        var relativePath = Path.GetRelativePath(sourcePackageFolder, originalPath);
        return string.Equals(relativePath, ".", StringComparison.Ordinal)
            ? workingPackageFolder
            : Path.Combine(workingPackageFolder, relativePath);
    }

    private static void ClonePackageFolder(string sourcePackageFolder, string targetPackageFolder)
    {
        Directory.CreateDirectory(targetPackageFolder);

        foreach (var sourceDirectory in Directory.EnumerateDirectories(sourcePackageFolder, "*", SearchOption.AllDirectories))
        {
            var relativeDirectory = Path.GetRelativePath(sourcePackageFolder, sourceDirectory);
            if (ShouldSkipCloneDirectory(relativeDirectory))
            {
                continue;
            }

            Directory.CreateDirectory(Path.Combine(targetPackageFolder, relativeDirectory));
        }

        foreach (var sourceFile in Directory.EnumerateFiles(sourcePackageFolder, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourcePackageFolder, sourceFile);
            if (ShouldSkipCloneFile(relativePath))
            {
                continue;
            }

            var targetFile = Path.Combine(targetPackageFolder, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);
            File.Copy(sourceFile, targetFile, overwrite: true);
        }
    }

    private static bool ShouldSkipCloneDirectory(string relativeDirectory)
    {
        var segments = relativeDirectory.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
        return segments.Any(segment => SkippedCloneDirectoryNames.Contains(segment));
    }

    private static bool ShouldSkipCloneFile(string relativePath)
    {
        var directory = Path.GetDirectoryName(relativePath);
        if (!string.IsNullOrWhiteSpace(directory) && ShouldSkipCloneDirectory(directory))
        {
            return true;
        }

        var fileName = Path.GetFileName(relativePath);
        if (fileName.EndsWith(".wapt", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return string.Equals(fileName, "Thumbs.db", StringComparison.OrdinalIgnoreCase);
    }

}
