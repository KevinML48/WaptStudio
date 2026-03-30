using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WaptStudio.Core.Models;
using WaptStudio.Core.Services.Interfaces;

namespace WaptStudio.Core.Services;

public sealed class PackageCatalogService : IPackageCatalogService
{
    private readonly IPackageInspectorService _packageInspectorService;
    private readonly IPackageValidationService _packageValidationService;

    public PackageCatalogService(IPackageInspectorService packageInspectorService, IPackageValidationService packageValidationService)
    {
        _packageInspectorService = packageInspectorService;
        _packageValidationService = packageValidationService;
    }

    public async Task<IReadOnlyList<PackageCatalogItem>> ScanAsync(string rootFolder, bool recursive, int semiRecursiveDepth, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rootFolder) || !Directory.Exists(rootFolder))
        {
            throw new DirectoryNotFoundException("Le dossier racine des paquets est invalide.");
        }

        var packageFolders = DiscoverPackageFolders(rootFolder, recursive, semiRecursiveDepth)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var items = new List<PackageCatalogItem>(packageFolders.Count);
        foreach (var packageFolder in packageFolders)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var packageInfo = await _packageInspectorService.AnalyzePackageAsync(packageFolder, cancellationToken).ConfigureAwait(false);
            var readiness = await _packageValidationService.ValidateAsync(packageFolder, packageInfo, includeWaptValidation: false, cancellationToken).ConfigureAwait(false);

            items.Add(new PackageCatalogItem
            {
                PackageId = packageInfo.PackageName ?? Path.GetFileName(packageFolder),
                VisibleName = packageInfo.VisibleName ?? packageInfo.PackageName ?? Path.GetFileName(packageFolder),
                Version = packageInfo.Version ?? string.Empty,
                Category = packageInfo.Category,
                Maturity = packageInfo.Maturity,
                ReadinessVerdict = readiness.Verdict,
                ReadinessLabel = readiness.VerdictLabel,
                LastModifiedUtc = packageInfo.LastModifiedUtc,
                PackageFolder = packageFolder,
                PrimaryInstallerName = Path.GetFileName(packageInfo.InstallerPath ?? packageInfo.ReferencedInstallerName ?? string.Empty),
                PackageInfo = packageInfo
            });
        }

        return items;
    }

    private static IEnumerable<string> DiscoverPackageFolders(string rootFolder, bool recursive, int semiRecursiveDepth)
    {
        var maxDepth = recursive ? int.MaxValue : Math.Max(0, semiRecursiveDepth);
        var queue = new Queue<(string Path, int Depth)>();
        queue.Enqueue((rootFolder, 0));

        while (queue.Count > 0)
        {
            var (currentPath, depth) = queue.Dequeue();

            if (LooksLikePackageFolder(currentPath))
            {
                yield return currentPath;
                continue;
            }

            if (depth >= maxDepth)
            {
                continue;
            }

            IEnumerable<string> subDirectories;
            try
            {
                subDirectories = Directory.EnumerateDirectories(currentPath);
            }
            catch
            {
                continue;
            }

            foreach (var subDirectory in subDirectories)
            {
                queue.Enqueue((subDirectory, depth + 1));
            }
        }
    }

    private static bool LooksLikePackageFolder(string folderPath)
    {
        try
        {
            return File.Exists(Path.Combine(folderPath, "setup.py"))
                || File.Exists(Path.Combine(folderPath, "control"))
                || File.Exists(Path.Combine(folderPath, "WAPT", "control"));
        }
        catch
        {
            return false;
        }
    }
}