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

        if (executables.Count > 0)
        {
            packageInfo.InstallerPath = executables[0];
            packageInfo.InstallerType = Path.GetExtension(executables[0]).TrimStart('.').ToUpperInvariant();
        }
        else
        {
            packageInfo.Warnings.Add("Aucun fichier MSI ou EXE n'a ete detecte.");
        }

        if (packageInfo.ControlFilePath is not null)
        {
            var controlContent = await File.ReadAllTextAsync(packageInfo.ControlFilePath, cancellationToken).ConfigureAwait(false);
            packageInfo.PackageName ??= ExtractControlValue(controlContent, "package");
            packageInfo.Version ??= ExtractControlValue(controlContent, "version");
            packageInfo.ReferencedInstallerName ??= ExtractControlValue(controlContent, "filename") ?? ExtractReferencedInstaller(controlContent);
        }

        if (packageInfo.SetupPyPath is not null)
        {
            var setupPyContent = await File.ReadAllTextAsync(packageInfo.SetupPyPath, cancellationToken).ConfigureAwait(false);
            packageInfo.PackageName ??= ExtractSetupPyValue(setupPyContent, "package");
            packageInfo.Version ??= ExtractSetupPyValue(setupPyContent, "version");
            packageInfo.ReferencedInstallerName ??= ExtractSetupPyValue(setupPyContent, "installer") ?? ExtractReferencedInstaller(setupPyContent);

            if (packageInfo.InstallerPath is null)
            {
                var referencedInstaller = packageInfo.ReferencedInstallerName;
                if (!string.IsNullOrWhiteSpace(referencedInstaller))
                {
                    var candidatePath = Path.Combine(packageFolder, referencedInstaller);
                    if (File.Exists(candidatePath))
                    {
                        packageInfo.InstallerPath = candidatePath;
                        packageInfo.InstallerType = Path.GetExtension(candidatePath).TrimStart('.').ToUpperInvariant();
                    }
                }
            }
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

        return packageInfo;
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
            $@"(?im)^\s*{Regex.Escape(key)}\s*=\s*['\"\"](?<value>[^'\"\"]+)['\"\"]");

        return match.Success ? match.Groups["value"].Value.Trim() : null;
    }

    private static string? ExtractReferencedInstaller(string content)
    {
        var match = Regex.Match(content, @"(?im)(?<installer>[^'\""\r\n]+\.(msi|exe))");
        return match.Success ? match.Groups["installer"].Value : null;
    }

}

