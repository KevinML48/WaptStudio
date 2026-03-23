using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WaptStudio.Core.Models;
using WaptStudio.Core.Services.Interfaces;

namespace WaptStudio.Core.Services;

public sealed partial class PackageInspectorService : IPackageInspectorService
{
    private readonly IPackageClassificationService _packageClassificationService;

    public PackageInspectorService()
        : this(new PackageClassificationService())
    {
    }

    public PackageInspectorService(IPackageClassificationService packageClassificationService)
    {
        _packageClassificationService = packageClassificationService;
    }

    public async Task<PackageInfo> AnalyzePackageAsync(string packageFolder, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(packageFolder))
        {
            throw new ArgumentException("Le dossier du paquet est requis.", nameof(packageFolder));
        }

        if (!Directory.Exists(packageFolder))
        {
            throw new DirectoryNotFoundException($"Le dossier '{packageFolder}' est introuvable.");
        }

        var packageInfo = new PackageInfo { PackageFolder = packageFolder };

        packageInfo.SetupPyPath = FindFile(packageFolder, "setup.py");
        packageInfo.ControlFilePath = FindFile(packageFolder, "control");

        var executables = Directory.EnumerateFiles(packageFolder, "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".msi", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        packageInfo.DetectedExecutables.AddRange(executables);

        string? setupPyContent = null;

        if (packageInfo.ControlFilePath is not null)
        {
            var controlContent = await File.ReadAllTextAsync(packageInfo.ControlFilePath, cancellationToken).ConfigureAwait(false);
            packageInfo.PackageName ??= ExtractControlValue(controlContent, "package");
            packageInfo.VisibleName ??= ExtractControlValue(controlContent, "name");
            packageInfo.Description ??= ExtractControlValue(controlContent, "description");
            packageInfo.DescriptionFr ??= ExtractControlValue(controlContent, "description_fr");
            packageInfo.Version ??= ExtractControlValue(controlContent, "version");
            packageInfo.ReferencedInstallerName ??= ExtractControlValue(controlContent, "filename") ?? ExtractReferencedInstaller(controlContent);
        }

        if (packageInfo.SetupPyPath is not null)
        {
            setupPyContent = await File.ReadAllTextAsync(packageInfo.SetupPyPath, cancellationToken).ConfigureAwait(false);
            packageInfo.PackageName ??= ExtractSetupPyValue(setupPyContent, "package");
            packageInfo.Version ??= ExtractSetupPyValue(setupPyContent, "version");
            packageInfo.ReferencedInstallerName ??= ExtractSetupPyValue(setupPyContent, "installer") ?? ExtractReferencedInstallerFromSetupPy(setupPyContent);
        }

        packageInfo.InstallerPath = ResolvePrimaryInstallerPath(packageFolder, executables, packageInfo.ReferencedInstallerName);
        if (packageInfo.InstallerPath is not null)
        {
            packageInfo.InstallerType = Path.GetExtension(packageInfo.InstallerPath).TrimStart('.').ToUpperInvariant();
        }
        else
        {
            packageInfo.Warnings.Add("Aucun fichier MSI ou EXE n'a ete detecte.");
        }

        if (packageInfo.SetupPyPath is null)
        {
            packageInfo.Warnings.Add("Le fichier setup.py est absent.");
        }

        if (packageInfo.ControlFilePath is null)
        {
            packageInfo.Warnings.Add("Le fichier control est absent.");
        }

        if (!string.IsNullOrWhiteSpace(packageInfo.ReferencedInstallerName)
            && !packageInfo.DetectedExecutables.Any(path => string.Equals(Path.GetFileName(path), packageInfo.ReferencedInstallerName, StringComparison.OrdinalIgnoreCase)))
        {
            packageInfo.Warnings.Add($"Installeur reference non trouve: {packageInfo.ReferencedInstallerName}");
        }

        packageInfo.Category = _packageClassificationService.Classify(packageInfo, setupPyContent);
        packageInfo.Maturity = _packageClassificationService.DetectMaturity(packageFolder, packageInfo.PackageName);
        packageInfo.LastModifiedUtc = Directory.EnumerateFiles(packageFolder, "*", SearchOption.AllDirectories)
            .Select(File.GetLastWriteTimeUtc)
            .DefaultIfEmpty(Directory.GetLastWriteTimeUtc(packageFolder))
            .Max();
        packageInfo.ExpectedWaptFileName = string.IsNullOrWhiteSpace(packageInfo.PackageName) || string.IsNullOrWhiteSpace(packageInfo.Version)
            ? null
            : $"{packageInfo.PackageName}_{packageInfo.Version}.wapt";

        return packageInfo;
    }

    private static string? ResolvePrimaryInstallerPath(string packageFolder, System.Collections.Generic.IReadOnlyList<string> executables, string? referencedInstallerName)
    {
        if (!string.IsNullOrWhiteSpace(referencedInstallerName))
        {
            var referencedMatch = executables.FirstOrDefault(path => string.Equals(Path.GetFileName(path), referencedInstallerName, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(referencedMatch))
            {
                return referencedMatch;
            }

            var candidatePath = Path.Combine(packageFolder, referencedInstallerName);
            if (File.Exists(candidatePath))
            {
                return candidatePath;
            }
        }

        return executables.Count > 0 ? executables[0] : null;
    }

    private static string? FindFile(string packageFolder, string fileName)
        => Directory.EnumerateFiles(packageFolder, fileName, SearchOption.AllDirectories)
            .OrderBy(path => path.Length)
            .FirstOrDefault();

    private static string? ExtractControlValue(string content, string key)
    {
        foreach (var line in content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("#", StringComparison.Ordinal) || string.IsNullOrWhiteSpace(trimmed))
            {
                continue;
            }

            var separators = new[] { ':', '=' };
            var separatorIndex = trimmed.IndexOfAny(separators);
            if (separatorIndex <= 0)
            {
                continue;
            }

            var currentKey = trimmed[..separatorIndex].Trim();
            if (!string.Equals(currentKey, key, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return trimmed[(separatorIndex + 1)..].Trim();
        }

        return null;
    }

    private static string? ExtractSetupPyValue(string content, string key)
    {
        var match = Regex.Match(
            content,
            $@"(?im)^\s*{Regex.Escape(key)}\s*=\s*['""](?<value>[^'""]+)['""]");

        return match.Success ? match.Groups["value"].Value.Trim() : null;
    }

    private static string? ExtractReferencedInstaller(string content)
    {
        var match = Regex.Match(content, @"(?im)(?<installer>[^'""\r\n]+\.(msi|exe))");
        return match.Success ? match.Groups["installer"].Value : null;
    }

    private static string? ExtractReferencedInstallerFromSetupPy(string content)
    {
        var match = Regex.Match(
            content,
            @"(?im)^(?!\s*#).*?\binstall_(?:msi|exe)_if_needed\(\s*['""'](?<installer>[^'""\r\n]+\.(?:msi|exe))['""']");

        return match.Success ? match.Groups["installer"].Value.Trim() : null;
    }

}

