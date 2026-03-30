using System;
using System.IO;
using System.Linq;
using WaptStudio.Core.Utilities;
using Xunit;

namespace WaptStudio.Tests;

public sealed class WaptNamingTests : IDisposable
{
    private readonly string _root;

    public WaptNamingTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "WaptNamingTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public void BuildExpectedWaptFileName_UsesEnvironmentSegmentsWhenPresent()
    {
        var packageFolder = Path.Combine(_root, "cd48-waptstudio_2301_Windows_DEV-wapt");
        Directory.CreateDirectory(packageFolder);

        var result = WaptNaming.BuildExpectedWaptFileName(
            packageFolder,
            packageId: "cd48-waptstudio",
            version: "2301",
            targetOs: null,
            maturity: "DEV",
            architecture: null);

        Assert.Equal("cd48-waptstudio_2301_windows_DEV.wapt", result);
    }

    [Fact]
    public void BuildExpectedWaptFileName_DoesNotAppendAllArchitectureSuffix()
    {
        var packageFolder = Path.Combine(_root, "cd48-waptstudio_2501_Windows_DEV-wapt");
        Directory.CreateDirectory(packageFolder);

        var result = WaptNaming.BuildExpectedWaptFileName(
            packageFolder,
            packageId: "cd48-waptstudio",
            version: "2501",
            targetOs: "windows",
            maturity: "DEV",
            architecture: "all");

        Assert.Equal("cd48-waptstudio_2501_windows_DEV.wapt", result);
    }

    [Fact]
    public void SelectBestWaptCandidate_PrefersMatchingIdAndVersion()
    {
        var expected = Path.Combine(_root, "cd48-waptstudio_2301_windows_DEV.wapt");
        var newerButWrongVersion = Path.Combine(_root, "cd48-waptstudio_2501_windows_DEV.wapt");
        var unrelated = Path.Combine(_root, "other_9999.wapt");

        File.WriteAllText(expected, "2301");
        File.WriteAllText(newerButWrongVersion, "2501");
        File.WriteAllText(unrelated, "other");

        var result = WaptNaming.SelectBestWaptCandidate(
            Directory.EnumerateFiles(_root),
            expectedFileName: "cd48-waptstudio_2301_windows_DEV.wapt",
            packageId: "cd48-waptstudio",
            version: "2301");

        Assert.Equal(expected, result);
    }

    [Fact]
    public void SelectBestWaptCandidate_PrefersRealExpectedFileOverAllSuffixVariant()
    {
        var expected = Path.Combine(_root, "cd48-waptstudio_2501_windows_DEV.wapt");
        var withAllSuffix = Path.Combine(_root, "cd48-waptstudio_2501_windows_DEV_all.wapt");

        File.WriteAllText(expected, "expected");
        File.WriteAllText(withAllSuffix, "unexpected-all");

        var result = WaptNaming.SelectBestWaptCandidate(
            Directory.EnumerateFiles(_root),
            expectedFileName: "cd48-waptstudio_2501_windows_DEV.wapt",
            packageId: "cd48-waptstudio",
            version: "2501");

        Assert.Equal(expected, result);
    }

    [Fact]
    public void SelectBestWaptCandidate_UsesPackageAndVersionWhenExactMissing()
    {
        var best = Path.Combine(_root, "tis-app_2.0.0_windows_DEV.wapt");
        var other = Path.Combine(_root, "tis-app_1.0.0_windows_DEV.wapt");

        File.WriteAllText(best, "best");
        File.WriteAllText(other, "other");

        var result = WaptNaming.SelectBestWaptCandidate(
            Directory.EnumerateFiles(_root),
            expectedFileName: null,
            packageId: "tis-app",
            version: "2.0.0");

        Assert.Equal(best, result);
    }

    [Fact]
    public void SelectBestWaptCandidate_ReturnsNullWhenNoCoherentCandidate()
    {
        var unrelated = Path.Combine(_root, "something.wapt");
        File.WriteAllText(unrelated, "early");

        var result = WaptNaming.SelectBestWaptCandidate(
            Directory.EnumerateFiles(_root),
            expectedFileName: null,
            packageId: "cd48-waptstudio",
            version: "2301");

        Assert.Null(result);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, recursive: true);
            }
        }
        catch
        {
            // Best effort cleanup for test artifacts.
        }
    }
}
