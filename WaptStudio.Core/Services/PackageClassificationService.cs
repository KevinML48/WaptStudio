using System;
using System.IO;
using System.Text.RegularExpressions;
using WaptStudio.Core.Models;
using WaptStudio.Core.Services.Interfaces;

namespace WaptStudio.Core.Services;

public sealed class PackageClassificationService : IPackageClassificationService
{
    public PackageCategory Classify(PackageInfo packageInfo, string? setupPyContent = null)
    {
        ArgumentNullException.ThrowIfNull(packageInfo);

        var normalizedSetupPy = setupPyContent ?? string.Empty;
        if (ContainsInstallCall(normalizedSetupPy, "msi"))
        {
            return PackageCategory.Msi;
        }

        if (ContainsInstallCall(normalizedSetupPy, "exe"))
        {
            return PackageCategory.Exe;
        }

        var installerExtension = Path.GetExtension(packageInfo.InstallerPath ?? packageInfo.ReferencedInstallerName ?? string.Empty);
        if (string.Equals(installerExtension, ".msi", StringComparison.OrdinalIgnoreCase))
        {
            return PackageCategory.Msi;
        }

        if (string.Equals(installerExtension, ".exe", StringComparison.OrdinalIgnoreCase))
        {
            return PackageCategory.Exe;
        }

        return PackageCategory.Other;
    }

    public string DetectMaturity(string packageFolder, string? packageId = null)
    {
        var probe = string.Join(" ", packageFolder ?? string.Empty, packageId ?? string.Empty).ToUpperInvariant();
        if (probe.Contains("PROD", StringComparison.Ordinal))
        {
            return "PROD";
        }

        if (probe.Contains("PREPROD", StringComparison.Ordinal) || probe.Contains("RECETTE", StringComparison.Ordinal))
        {
            return "RECETTE";
        }

        if (probe.Contains("TEST", StringComparison.Ordinal) || probe.Contains("QUALIF", StringComparison.Ordinal))
        {
            return "TEST";
        }

        if (probe.Contains("DEV", StringComparison.Ordinal) || probe.Contains("LAB", StringComparison.Ordinal))
        {
            return "DEV";
        }

        return "Inconnue";
    }

    private static bool ContainsInstallCall(string setupPyContent, string installerKind)
        => Regex.IsMatch(
            setupPyContent,
            $@"(?im)^(?!\s*#).*?\binstall_{Regex.Escape(installerKind)}_if_needed\(",
            RegexOptions.CultureInvariant);
}