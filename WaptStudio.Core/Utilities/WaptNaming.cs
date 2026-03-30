using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WaptStudio.Core.Utilities;

public static class WaptNaming
{
    public static string BuildExpectedWaptFileName(
        string packageFolder,
        string packageId,
        string? version,
        string? targetOs,
        string? maturity,
        string? architecture)
    {
        if (string.IsNullOrWhiteSpace(packageId) || string.IsNullOrWhiteSpace(version))
        {
            return "Nom .wapt non inferable";
        }

        var normalizedOs = NormalizeLower(targetOs) ?? InferTargetOs(packageFolder);
        var maturitySource = string.IsNullOrWhiteSpace(maturity)
            || string.Equals(maturity, "Inconnue", StringComparison.OrdinalIgnoreCase)
            ? InferMaturity(packageFolder)
            : maturity;
        var normalizedMaturity = NormalizeMaturity(maturitySource);
        var normalizedArchitecture = NormalizeArchitecture(architecture);

        var segments = new List<string> { packageId.Trim(), version.Trim() };

        if (!string.IsNullOrWhiteSpace(normalizedOs))
        {
            segments.Add(normalizedOs);
        }

        if (!string.IsNullOrWhiteSpace(normalizedMaturity))
        {
            segments.Add(normalizedMaturity);
        }

        if (!string.IsNullOrWhiteSpace(normalizedArchitecture))
        {
            segments.Add(normalizedArchitecture);
        }

        var baseName = string.Join("_", segments);
        if (segments.Count <= 2)
        {
            var suffix = ExtractSuffixFromFolder(packageFolder, version);
            if (!string.IsNullOrWhiteSpace(suffix))
            {
                baseName += suffix;
            }
        }

        return $"{baseName}.wapt";
    }

    public static string? InferTargetOs(string packageFolder)
    {
        var folderName = Path.GetFileName(packageFolder?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) ?? string.Empty);
        var probe = folderName?.ToLowerInvariant() ?? string.Empty;
        if (probe.Contains("windows", StringComparison.Ordinal))
        {
            return "windows";
        }

        if (probe.Contains("linux", StringComparison.Ordinal))
        {
            return "linux";
        }

        return null;
    }

    private static string? InferMaturity(string packageFolder)
    {
        var folderName = Path.GetFileName(packageFolder?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) ?? string.Empty);
        var probe = (folderName ?? string.Empty).ToUpperInvariant();
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

        return null;
    }

    public static string? SelectBestWaptCandidate(IEnumerable<string> candidatePaths, string? expectedFileName, string? packageId, string? version)
    {
        var candidates = candidatePaths?
            .Where(File.Exists)
            .Select(path => new Candidate(path))
            .ToList() ?? new List<Candidate>();

        if (candidates.Count == 0)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(expectedFileName))
        {
            var exact = candidates.FirstOrDefault(candidate => candidate.IsNamed(expectedFileName));
            if (exact is not null)
            {
                return exact.Path;
            }
        }

        var best = candidates
            .Select(candidate => new
            {
                candidate.Path,
                Score = candidate.Score(expectedFileName, packageId, version),
                Time = candidate.LastWriteTimeUtc
            })
            .OrderByDescending(entry => entry.Score)
            .ThenByDescending(entry => entry.Time)
            .FirstOrDefault();

        return best is not null && best.Score > 0 ? best.Path : null;
    }

    private static string? ExtractSuffixFromFolder(string packageFolder, string version)
    {
        try
        {
            var folderName = Path.GetFileName(packageFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            if (string.IsNullOrWhiteSpace(folderName))
            {
                return null;
            }

            if (folderName.EndsWith("-wapt", StringComparison.OrdinalIgnoreCase))
            {
                folderName = folderName[..^5];
            }

            var versionIndex = folderName.IndexOf(version, StringComparison.OrdinalIgnoreCase);
            if (versionIndex < 0)
            {
                return null;
            }

            var suffixStart = versionIndex + version.Length;
            if (suffixStart >= folderName.Length || folderName[suffixStart] != '_')
            {
                return null;
            }

            return folderName[suffixStart..];
        }
        catch
        {
            return null;
        }
    }

    private static string? NormalizeLower(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();

    private static string? NormalizeMaturity(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return string.Equals(trimmed, "Inconnue", StringComparison.OrdinalIgnoreCase)
            ? null
            : trimmed.ToUpperInvariant();
    }

    private static string? NormalizeArchitecture(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (trimmed.Equals("x86", StringComparison.OrdinalIgnoreCase))
        {
            return "x86";
        }

        if (trimmed.Equals("x64", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("amd64", StringComparison.OrdinalIgnoreCase))
        {
            return "x64";
        }

        if (trimmed.Equals("arm64", StringComparison.OrdinalIgnoreCase))
        {
            return "arm64";
        }

        return trimmed.ToLowerInvariant();
    }

    private sealed record Candidate(string Path)
    {
        public string FileName { get; } = System.IO.Path.GetFileName(Path);

        public DateTime LastWriteTimeUtc => File.GetLastWriteTimeUtc(Path);

        public bool IsNamed(string name)
            => string.Equals(FileName, name, StringComparison.OrdinalIgnoreCase);

        public int Score(string? expected, string? packageId, string? version)
        {
            var score = 0;

            if (!string.IsNullOrWhiteSpace(packageId) && FileName.StartsWith(packageId, StringComparison.OrdinalIgnoreCase))
            {
                score += 50;
            }

            if (!string.IsNullOrWhiteSpace(version) && FileName.Contains(version, StringComparison.OrdinalIgnoreCase))
            {
                score += 30;
            }

            if (!string.IsNullOrWhiteSpace(expected) && FileName.Contains(expected, StringComparison.OrdinalIgnoreCase))
            {
                score += 10;
            }

            return score;
        }
    }
}
