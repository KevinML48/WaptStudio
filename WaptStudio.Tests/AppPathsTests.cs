using System;
using System.IO;
using WaptStudio.Core.Configuration;
using WaptStudio.Core.Models;
using Xunit;

namespace WaptStudio.Tests;

public sealed class AppPathsTests
{
    [Fact]
    public void BaseDirectory_UsesOverrideEnvironmentVariableWhenPresent()
    {
        var expectedPath = Path.Combine(Path.GetTempPath(), "WaptStudio-Portable-Test");
        var previousValue = Environment.GetEnvironmentVariable(AppPaths.BaseDirectoryOverrideEnvironmentVariable);

        try
        {
            Environment.SetEnvironmentVariable(AppPaths.BaseDirectoryOverrideEnvironmentVariable, expectedPath);

            Assert.Equal(Path.GetFullPath(expectedPath), AppPaths.BaseDirectory);
            Assert.StartsWith(AppPaths.BaseDirectory, AppPaths.CacheDirectory, StringComparison.OrdinalIgnoreCase);
            Assert.StartsWith(AppPaths.BaseDirectory, AppPaths.ConfigDirectory, StringComparison.OrdinalIgnoreCase);
            Assert.StartsWith(AppPaths.BaseDirectory, AppPaths.DataDirectory, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Environment.SetEnvironmentVariable(AppPaths.BaseDirectoryOverrideEnvironmentVariable, previousValue);
        }
    }

    [Fact]
    public void ResolveCacheDirectory_FallsBackToPortableDefault()
    {
        var settings = new AppSettings();

        Assert.Equal(AppPaths.CacheDirectory, AppPaths.ResolveCacheDirectory(settings));
    }
}