using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace WaptStudio.Core.Utilities;

public static class SetupPyInstallerReferenceParser
{
    private static readonly Regex ConstantAssignmentRegex = new(
        @"^\s*(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*=\s*(?<quote>['""])(?<value>[^'""\r\n]+\.(?:msi|exe))\k<quote>\s*$",
        RegexOptions.Compiled);

    private static readonly Regex InstallCallRegex = new(
        @"\binstall_(?<fn>msi|exe)_if_needed\(\s*(?<arg>[^)\r\n]+?)\s*\)",
        RegexOptions.Compiled);

    private static readonly Regex InstallFunctionRegex = new(
        @"install_(?:msi|exe)_if_needed(?=\s*\()",
        RegexOptions.Compiled);

    public static string? ExtractReferencedInstallerName(string content)
    {
        foreach (var reference in ParseInstallReferences(content))
        {
            if (!string.IsNullOrWhiteSpace(reference.ResolvedInstallerName))
            {
                return reference.ResolvedInstallerName;
            }
        }

        return null;
    }

    public static IReadOnlyList<SetupPyInstallReference> ParseInstallReferences(string content)
    {
        var constants = ParseConstants(content);
        var references = new List<SetupPyInstallReference>();

        foreach (var line in EnumerateLines(content))
        {
            if (IsCommentLine(line))
            {
                continue;
            }

            var match = InstallCallRegex.Match(line);
            if (!match.Success)
            {
                continue;
            }

            var argumentToken = match.Groups["arg"].Value.Trim();
            var functionKind = match.Groups["fn"].Value.Trim();
            var constantName = IsQuoted(argumentToken) ? null : argumentToken;
            var resolvedInstallerName = ResolveArgument(argumentToken, constants);

            references.Add(new SetupPyInstallReference(
                functionKind,
                argumentToken,
                constantName,
                resolvedInstallerName));
        }

        return references;
    }

    public static string UpdateInstallerReference(string content, string? previousInstallerName, string newInstallerName)
    {
        var references = ParseInstallReferences(content);
        var updatedLines = new List<string>();
        var targetFunctionName = BuildInstallFunctionName(newInstallerName);

        foreach (var line in EnumerateLines(content))
        {
            var updatedLine = line;
            if (!IsCommentLine(line))
            {
                var installCallMatch = InstallCallRegex.Match(line);
                if (installCallMatch.Success)
                {
                    updatedLine = InstallFunctionRegex.Replace(updatedLine, targetFunctionName, 1);

                    var argumentToken = installCallMatch.Groups["arg"].Value.Trim();
                    if (IsQuoted(argumentToken))
                    {
                        var argumentGroup = installCallMatch.Groups["arg"];
                        var quote = argumentToken[0];
                        updatedLine = $"{updatedLine[..argumentGroup.Index]}{quote}{newInstallerName}{quote}{updatedLine[(argumentGroup.Index + argumentGroup.Length)..]}";
                    }
                }
            }

            updatedLines.Add(updatedLine);
        }

        var updatedContent = string.Join(Environment.NewLine, updatedLines);

        var constantsToReplace = references
            .Where(reference => !string.IsNullOrWhiteSpace(reference.ConstantName)
                && !string.IsNullOrWhiteSpace(reference.ResolvedInstallerName)
                && (string.IsNullOrWhiteSpace(previousInstallerName)
                    || string.Equals(reference.ResolvedInstallerName, previousInstallerName, StringComparison.OrdinalIgnoreCase)))
            .Select(reference => reference.ConstantName!)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (constantsToReplace.Count == 0)
        {
            return updatedContent;
        }

        var constantUpdatedLines = new List<string>();
        foreach (var line in EnumerateLines(updatedContent))
        {
            var rewrittenLine = line;
            var constantMatch = ConstantAssignmentRegex.Match(line);
            if (constantMatch.Success)
            {
                var constantName = constantMatch.Groups["name"].Value.Trim();
                if (constantsToReplace.Contains(constantName, StringComparer.Ordinal))
                {
                    rewrittenLine = $"{line[..constantMatch.Groups["value"].Index]}{newInstallerName}{line[(constantMatch.Groups["value"].Index + constantMatch.Groups["value"].Length)..]}";
                }
            }

            constantUpdatedLines.Add(rewrittenLine);
        }

        return string.Join(Environment.NewLine, constantUpdatedLines);
    }

    public static bool HasCoherentInstallerReference(string content, string newInstallerName)
    {
        var expectedKind = PathLikeExtension(newInstallerName);
        foreach (var reference in ParseInstallReferences(content))
        {
            if (!string.Equals(reference.FunctionKind, expectedKind, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (string.Equals(reference.ResolvedInstallerName, newInstallerName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static Dictionary<string, string> ParseConstants(string content)
    {
        var constants = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var line in EnumerateLines(content))
        {
            if (IsCommentLine(line))
            {
                continue;
            }

            var match = ConstantAssignmentRegex.Match(line);
            if (!match.Success)
            {
                continue;
            }

            constants[match.Groups["name"].Value.Trim()] = match.Groups["value"].Value.Trim();
        }

        return constants;
    }

    private static string? ResolveArgument(string argumentToken, IReadOnlyDictionary<string, string> constants)
    {
        if (IsQuoted(argumentToken))
        {
            return argumentToken[1..^1].Trim();
        }

        return constants.TryGetValue(argumentToken, out var resolvedValue)
            ? resolvedValue
            : null;
    }

    private static bool IsQuoted(string token)
        => token.Length >= 2
            && ((token[0] == '\'' && token[^1] == '\'')
                || (token[0] == '"' && token[^1] == '"'));

    private static string BuildInstallFunctionName(string installerName)
        => string.Equals(PathLikeExtension(installerName), "exe", StringComparison.OrdinalIgnoreCase)
            ? "install_exe_if_needed"
            : "install_msi_if_needed";

    private static IEnumerable<string> EnumerateLines(string content)
        => content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

    private static bool IsCommentLine(string line)
        => line.TrimStart().StartsWith("#", StringComparison.Ordinal);

    private static string PathLikeExtension(string installerName)
    {
        var extensionIndex = installerName.LastIndexOf('.');
        return extensionIndex >= 0 && extensionIndex < installerName.Length - 1
            ? installerName[(extensionIndex + 1)..]
            : string.Empty;
    }
}

public sealed record SetupPyInstallReference(
    string FunctionKind,
    string ArgumentToken,
    string? ConstantName,
    string? ResolvedInstallerName);