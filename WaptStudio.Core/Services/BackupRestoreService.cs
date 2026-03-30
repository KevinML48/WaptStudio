using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WaptStudio.Core.Configuration;
using WaptStudio.Core.Models;
using WaptStudio.Core.Services.Interfaces;

namespace WaptStudio.Core.Services;

public sealed class BackupRestoreService : IBackupRestoreService
{
    private const string ManifestFileName = "backup-manifest.json";
    private readonly ISettingsService _settingsService;

    public BackupRestoreService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public async Task<PackageBackupInfo> CreatePackageBackupAsync(PackageInfo packageInfo, string reason, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(packageInfo);

        if (string.IsNullOrWhiteSpace(packageInfo.PackageFolder) || !Directory.Exists(packageInfo.PackageFolder))
        {
            throw new DirectoryNotFoundException("Le dossier du paquet a sauvegarder est introuvable.");
        }

        var settings = await _settingsService.LoadAsync(cancellationToken).ConfigureAwait(false);
        AppPaths.EnsureCreated(settings);

        var packageKey = BuildPackageKey(packageInfo);
        var backupDirectory = Path.Combine(
            AppPaths.ResolveBackupsDirectory(settings),
            packageKey,
            DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss"));
        Directory.CreateDirectory(backupDirectory);

        var snapshotRoot = Path.Combine(backupDirectory, "snapshot");
        DirectoryCopy(packageInfo.PackageFolder, snapshotRoot, recursive: true);

        var info = new PackageBackupInfo
        {
            PackageKey = packageKey,
            SourcePackageFolder = packageInfo.PackageFolder,
            BackupDirectory = backupDirectory,
            CreatedAt = DateTimeOffset.Now,
            Reason = reason
        };

        info.Files.AddRange(Directory.EnumerateFiles(snapshotRoot, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(snapshotRoot, path))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase));

        var manifestPath = Path.Combine(backupDirectory, ManifestFileName);
        await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(info, new JsonSerializerOptions { WriteIndented = true }), cancellationToken).ConfigureAwait(false);
        return info;
    }

    public async Task<PackageBackupInfo?> GetLatestBackupAsync(PackageInfo packageInfo, CancellationToken cancellationToken = default)
    {
        var settings = await _settingsService.LoadAsync(cancellationToken).ConfigureAwait(false);
        var packageKey = BuildPackageKey(packageInfo);
        var packageBackupRoot = Path.Combine(AppPaths.ResolveBackupsDirectory(settings), packageKey);
        if (!Directory.Exists(packageBackupRoot))
        {
            return null;
        }

        var latestBackupDirectory = Directory.EnumerateDirectories(packageBackupRoot)
            .OrderByDescending(path => path, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(latestBackupDirectory))
        {
            return null;
        }

        return await ReadManifestAsync(latestBackupDirectory, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PackageRestoreResult> RestoreLatestBackupAsync(PackageInfo packageInfo, CancellationToken cancellationToken = default)
    {
        var latestBackup = await GetLatestBackupAsync(packageInfo, cancellationToken).ConfigureAwait(false);
        if (latestBackup is null)
        {
            return new PackageRestoreResult
            {
                Success = false,
                Message = "Aucune sauvegarde disponible pour ce paquet."
            };
        }

        var snapshotRoot = Path.Combine(latestBackup.BackupDirectory, "snapshot");
        if (!Directory.Exists(snapshotRoot))
        {
            return new PackageRestoreResult
            {
                Success = false,
                Message = "La sauvegarde selectionnee est incomplete.",
                RestoredFromBackupDirectory = latestBackup.BackupDirectory
            };
        }

        if (Directory.Exists(packageInfo.PackageFolder))
        {
            Directory.Delete(packageInfo.PackageFolder, recursive: true);
        }

        Directory.CreateDirectory(packageInfo.PackageFolder);
        DirectoryCopy(snapshotRoot, packageInfo.PackageFolder, recursive: true);

        var result = new PackageRestoreResult
        {
            Success = true,
            Message = "La derniere sauvegarde a ete restauree.",
            RestoredFromBackupDirectory = latestBackup.BackupDirectory
        };
        result.RestoredFiles.AddRange(Directory.EnumerateFiles(packageInfo.PackageFolder, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(packageInfo.PackageFolder, path))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase));
        return result;
    }

    private static string BuildPackageKey(PackageInfo packageInfo)
    {
        var rawValue = packageInfo.PackageName;
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            rawValue = Path.GetFileName(packageInfo.PackageFolder);
        }

        var invalidCharacters = Path.GetInvalidFileNameChars();
        return new string(rawValue.Select(ch => invalidCharacters.Contains(ch) ? '_' : ch).ToArray());
    }

    private static async Task<PackageBackupInfo?> ReadManifestAsync(string backupDirectory, CancellationToken cancellationToken)
    {
        var manifestPath = Path.Combine(backupDirectory, ManifestFileName);
        if (!File.Exists(manifestPath))
        {
            return null;
        }

        var content = await File.ReadAllTextAsync(manifestPath, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<PackageBackupInfo>(content);
    }

    private static void DirectoryCopy(string sourceDir, string destinationDir, bool recursive)
    {
        var directory = new DirectoryInfo(sourceDir);
        if (!directory.Exists)
        {
            throw new DirectoryNotFoundException($"Le repertoire source est introuvable: {sourceDir}");
        }

        Directory.CreateDirectory(destinationDir);

        foreach (var file in directory.GetFiles())
        {
            file.CopyTo(Path.Combine(destinationDir, file.Name), overwrite: true);
        }

        if (!recursive)
        {
            return;
        }

        foreach (var subDirectory in directory.GetDirectories())
        {
            DirectoryCopy(subDirectory.FullName, Path.Combine(destinationDir, subDirectory.Name), recursive: true);
        }
    }
}