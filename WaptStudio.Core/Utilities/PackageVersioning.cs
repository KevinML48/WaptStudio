using System;
using System.IO;
using System.Text.RegularExpressions;
using WaptStudio.Core.Models;

namespace WaptStudio.Core.Utilities;

public static partial class PackageVersioning
{
    public static string DescribeStrategy(PackageVersionStrategy strategy)
        => strategy switch
        {
            PackageVersionStrategy.KeepCurrentVersion => "Conserver la version actuelle",
            PackageVersionStrategy.IncrementPackageRevision => "Incrementer la revision du paquet",
            PackageVersionStrategy.SetExplicitVersion => "Definir une nouvelle version",
            _ => "Strategie inconnue"
        };

    public static string? InferVersionFromInstallerFileName(string installerPath)
    {
        var fileName = Path.GetFileNameWithoutExtension(installerPath);
        var match = VersionFromFileNameRegex().Match(fileName);
        if (match.Success)
        {
            return match.Groups[1].Value.Replace('_', '.').Replace('-', '.');
        }

        var compactMatch = CompactVersionRegex().Match(fileName);
        return compactMatch.Success ? compactMatch.Groups[1].Value : null;
    }

    public static string? ExtractProductVersion(string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return null;
        }

        var match = StructuredVersionRegex().Match(version.Trim());
        return match.Success ? match.Groups["product"].Value : null;
    }

    public static bool HasPackageRevision(string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return false;
        }

        var match = StructuredVersionRegex().Match(version.Trim());
        return match.Success && match.Groups["revision"].Success;
    }

    public static bool IsSuggestedProductVersionDifferent(string? currentVersion, string? suggestedVersion)
    {
        var currentProduct = ExtractProductVersion(currentVersion) ?? currentVersion?.Trim();
        var suggestedProduct = ExtractProductVersion(suggestedVersion) ?? suggestedVersion?.Trim();

        return !string.IsNullOrWhiteSpace(currentProduct)
            && !string.IsNullOrWhiteSpace(suggestedProduct)
            && !string.Equals(currentProduct, suggestedProduct, StringComparison.OrdinalIgnoreCase);
    }

    public static bool TryIncrementPackageRevision(string? currentVersion, out string normalizedVersion, out string errorMessage)
    {
        normalizedVersion = string.Empty;
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(currentVersion))
        {
            errorMessage = "La version actuelle est absente. Impossible d'incrementer la revision du paquet.";
            return false;
        }

        var match = StructuredVersionRegex().Match(currentVersion.Trim());
        if (!match.Success)
        {
            errorMessage = "La version actuelle n'utilise pas un format pris en charge pour incrementer la revision. Utilisez plutot 'Definir une nouvelle version'.";
            return false;
        }

        var product = match.Groups["product"].Value;
        var revision = match.Groups["revision"].Success
            ? int.Parse(match.Groups["revision"].Value) + 1
            : 1;

        normalizedVersion = $"{product}-{revision}";
        return true;
    }

    public static bool TryNormalizeExplicitVersion(string? input, string? currentVersion, out string normalizedVersion, out string errorMessage)
    {
        normalizedVersion = string.Empty;
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(input))
        {
            errorMessage = "La nouvelle version ne peut pas etre vide.";
            return false;
        }

        var trimmed = input.Trim();
        var match = StructuredVersionRegex().Match(trimmed);
        if (!match.Success)
        {
            errorMessage = "Format de version invalide. Utilisez par exemple 11.0.0, 11.0.0-1 ou 24.09.00.0.";
            return false;
        }

        var product = match.Groups["product"].Value;
        if (match.Groups["revision"].Success)
        {
            normalizedVersion = $"{product}-{match.Groups["revision"].Value}";
            return true;
        }

        normalizedVersion = HasPackageRevision(currentVersion)
            ? $"{product}-1"
            : product;
        return true;
    }

    [GeneratedRegex(@"(?<!\d)(\d+(?:[._-]\d+)+)", RegexOptions.Compiled)]
    private static partial Regex VersionFromFileNameRegex();

    [GeneratedRegex(@"(?<!\d)(\d{4,})(?!\d)", RegexOptions.Compiled)]
    private static partial Regex CompactVersionRegex();

    [GeneratedRegex(@"^(?<product>\d+(?:\.\d+)*)(?:-(?<revision>\d+))?$", RegexOptions.Compiled)]
    private static partial Regex StructuredVersionRegex();
}