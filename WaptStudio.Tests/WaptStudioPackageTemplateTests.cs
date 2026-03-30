using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Xunit;

namespace WaptStudio.Tests;

public sealed class WaptStudioPackageTemplateTests
{
    [Fact]
    public void WaptTemplate_ContainsExpectedFiles()
    {
        var repositoryRoot = FindRepositoryRoot();

        Assert.True(File.Exists(Path.Combine(repositoryRoot, "Build-WaptStudio-Package.ps1")));
        Assert.True(File.Exists(Path.Combine(repositoryRoot, "Test-WaptStudio-Package.ps1")));
        Assert.True(File.Exists(Path.Combine(repositoryRoot, "packaging", "wapt", "README.md")));
        Assert.True(File.Exists(Path.Combine(repositoryRoot, "packaging", "wapt", "cd48-waptstudio", "setup.py")));
        Assert.True(File.Exists(Path.Combine(repositoryRoot, "packaging", "wapt", "cd48-waptstudio", "WAPT", "control")));
        Assert.True(File.Exists(Path.Combine(repositoryRoot, "packaging", "wapt", "cd48-waptstudio", "README.md")));
    }

    [Fact]
    public void SetupPy_InstallsIntoProgramFilesAndPreservesUserData()
    {
        var repositoryRoot = FindRepositoryRoot();
        var setupPyPath = Path.Combine(repositoryRoot, "packaging", "wapt", "cd48-waptstudio", "setup.py");
        var content = File.ReadAllText(setupPyPath);

        Assert.Contains("ProgramFiles", content, StringComparison.Ordinal);
        Assert.Contains("CREATE_DESKTOP_SHORTCUT = False", content, StringComparison.Ordinal);
        Assert.Contains("def install():", content, StringComparison.Ordinal);
        Assert.Contains("def uninstall():", content, StringComparison.Ordinal);
        Assert.Contains("Start Menu", content, StringComparison.Ordinal);
        Assert.DoesNotContain("shutil.rmtree(os.environ.get('LOCALAPPDATA'", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ControlTemplate_ContainsExpectedMetadata()
    {
        var repositoryRoot = FindRepositoryRoot();
        var controlPath = Path.Combine(repositoryRoot, "packaging", "wapt", "cd48-waptstudio", "WAPT", "control");
        var content = File.ReadAllText(controlPath);

        Assert.Contains("package      : __PACKAGE_ID__", content, StringComparison.Ordinal);
        Assert.Contains("version      : __PACKAGE_VERSION__", content, StringComparison.Ordinal);
        Assert.Contains("architecture : x64", content, StringComparison.Ordinal);
        Assert.Contains("target_os    : windows", content, StringComparison.Ordinal);
        Assert.Contains("maturity     : PROD", content, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildScript_StagesPublishedPayloadIntoArtifactPackage()
    {
        var repositoryRoot = FindRepositoryRoot();
        var buildScriptPath = Path.Combine(repositoryRoot, "Build-WaptStudio-Package.ps1");
        var content = File.ReadAllText(buildScriptPath);

        Assert.Contains("Build-Release.ps1", content, StringComparison.Ordinal);
        Assert.Contains("dist\\$RuntimeIdentifier\\self-contained", content, StringComparison.Ordinal);
        Assert.Contains("sources\\app", content, StringComparison.Ordinal);
        Assert.Contains("build-package", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildScript_GeneratedControlContainsNonEmptyPackageMetadata()
    {
        var repositoryRoot = FindRepositoryRoot();
        var buildScriptPath = Path.Combine(repositoryRoot, "Build-WaptStudio-Package.ps1");
        var tempRoot = Path.Combine(Path.GetTempPath(), "WaptStudioPackageTests", Guid.NewGuid().ToString("N"));
        var publishDirectory = Path.Combine(tempRoot, "publish");
        var outputRoot = Path.Combine(tempRoot, "artifacts");

        Directory.CreateDirectory(publishDirectory);
        File.WriteAllText(Path.Combine(publishDirectory, "WaptStudio.App.exe"), "placeholder");
        File.WriteAllText(Path.Combine(publishDirectory, "WaptStudio.App.dll"), "placeholder");

        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{buildScriptPath}\" -SkipPublish -PublishOutputPath \"{publishDirectory}\" -OutputRoot \"{outputRoot}\" -PackageVersion \"9.9.9.9\"",
                WorkingDirectory = repositoryRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            });

            Assert.NotNull(process);
            Assert.True(process!.WaitForExit(60000), "Le script de build WAPT n'a pas termine dans le delai imparti.");

            var standardOutput = process.StandardOutput.ReadToEnd();
            var standardError = process.StandardError.ReadToEnd();
            Assert.True(process.ExitCode == 0, $"Le script de build WAPT a echoue.{Environment.NewLine}STDOUT:{Environment.NewLine}{standardOutput}{Environment.NewLine}STDERR:{Environment.NewLine}{standardError}");

            var stagingRoot = Path.Combine(outputRoot, "cd48-waptstudio");
            var controlPath = Path.Combine(stagingRoot, "WAPT", "control");
            var manifestPath = Path.Combine(stagingRoot, "package-build-manifest.json");

            Assert.True(File.Exists(controlPath));
            Assert.True(File.Exists(manifestPath));

            var controlBytes = File.ReadAllBytes(controlPath);
            Assert.False(HasUtf8Bom(controlBytes), "Le control WAPT ne doit pas etre ecrit avec un BOM UTF-8.");

            var controlMetadata = ParseControlMetadata(File.ReadAllText(controlPath));
            Assert.Equal("cd48-waptstudio", controlMetadata["package"]);
            Assert.Equal("9.9.9.9", controlMetadata["version"]);

            using var manifestDocument = JsonDocument.Parse(File.ReadAllText(manifestPath));
            Assert.Equal("cd48-waptstudio", manifestDocument.RootElement.GetProperty("packageId").GetString());
            Assert.Equal("9.9.9.9", manifestDocument.RootElement.GetProperty("packageVersion").GetString());
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    private static Dictionary<string, string> ParseControlMetadata(string content)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        using var reader = new StringReader(content);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            var separatorIndex = line.IndexOf(':');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = line[..separatorIndex].Trim();
            if (key.Length == 0 || key.Contains('#', StringComparison.Ordinal))
            {
                continue;
            }

            var value = line[(separatorIndex + 1)..].Trim();
            metadata[key] = value;
        }

        return metadata;
    }

    private static bool HasUtf8Bom(byte[] bytes)
    {
        return bytes.Length >= 3
            && bytes[0] == 0xEF
            && bytes[1] == 0xBB
            && bytes[2] == 0xBF;
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
}