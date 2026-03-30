using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace WaptStudio.Tests;

public sealed class SourcePortabilityAuditTests
{
    [Fact]
    public void SourceFiles_DoNotContainDeveloperSpecificWindowsProfilePaths()
    {
        var repositoryRoot = FindRepositoryRoot();
        var windowsProfileRootFragment = "C:" + Path.DirectorySeparatorChar + "Users" + Path.DirectorySeparatorChar;
        var currentUserName = TryExtractWorkspaceUserName(repositoryRoot);
        var sourceFiles = Directory.EnumerateFiles(repositoryRoot, "*.*", SearchOption.AllDirectories)
            .Where(ShouldAuditFile)
            .ToList();

        var offenders = new List<string>();
        foreach (var filePath in sourceFiles)
        {
            var content = File.ReadAllText(filePath);
            if (content.Contains(windowsProfileRootFragment, StringComparison.OrdinalIgnoreCase)
                || (!string.IsNullOrWhiteSpace(currentUserName)
                    && content.Contains(currentUserName, StringComparison.OrdinalIgnoreCase)))
            {
                offenders.Add(Path.GetRelativePath(repositoryRoot, filePath));
            }
        }

        Assert.True(offenders.Count == 0, "Developer-specific paths found in: " + string.Join(", ", offenders));
    }

    private static string FindRepositoryRoot()
    {
        var currentDirectory = new DirectoryInfo(AppContext.BaseDirectory);
        while (currentDirectory is not null)
        {
            var solutionPath = Path.Combine(currentDirectory.FullName, "WaptStudio.sln");
            if (File.Exists(solutionPath))
            {
                return currentDirectory.FullName;
            }

            currentDirectory = currentDirectory.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }

    private static string? TryExtractWorkspaceUserName(string repositoryRoot)
    {
        var pathParts = repositoryRoot.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        for (var index = 0; index < pathParts.Length - 1; index++)
        {
            if (string.Equals(pathParts[index], "Users", StringComparison.OrdinalIgnoreCase))
            {
                return pathParts[index + 1];
            }
        }

        return null;
    }

    private static bool ShouldAuditFile(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        var extension = Path.GetExtension(filePath);
        if (!new[] { ".cs", ".csproj", ".props", ".ps1", ".md", ".sln" }.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        var segments = filePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (segments.Any(segment => string.Equals(segment, "bin", StringComparison.OrdinalIgnoreCase)
            || string.Equals(segment, "obj", StringComparison.OrdinalIgnoreCase)
            || string.Equals(segment, ".git", StringComparison.OrdinalIgnoreCase)
            || string.Equals(segment, ".venv", StringComparison.OrdinalIgnoreCase)
            || string.Equals(segment, "venv", StringComparison.OrdinalIgnoreCase)
            || string.Equals(segment, "artifacts", StringComparison.OrdinalIgnoreCase)
            || string.Equals(segment, "dist", StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        return !string.Equals(fileName, "project.assets.json", StringComparison.OrdinalIgnoreCase);
    }
}