using System;
using System.Collections.Generic;
using System.IO;
using WaptStudio.Core.Models;
using WaptStudio.Core.Utilities;
using Xunit;

namespace WaptStudio.Tests;

public sealed class WaptEnvironmentDetectorTests
{
    [Fact]
    public void Inspect_ResolvesAbsoluteConfiguredExecutable()
    {
        var configuredExecutable = @"C:\Program Files (x86)\wapt\wapt-get.exe";
        var settings = new AppSettings { WaptExecutablePath = configuredExecutable };

        var environment = WaptEnvironmentDetector.Inspect(
            settings,
            fileExists: path => string.Equals(path, configuredExecutable, StringComparison.OrdinalIgnoreCase),
            getEnvironmentVariable: _ => null);

        Assert.True(environment.IsWaptExecutableAvailable);
        Assert.Equal(configuredExecutable, environment.EffectiveExecutablePath);
        Assert.Equal("configuration", environment.ExecutableDetectionSource);
    }

    [Fact]
    public void Inspect_ResolvesExecutableFromPathWhenDefaultNameIsConfigured()
    {
        var pathExecutable = @"C:\Tools\Wapt\wapt-get.exe";
        var settings = new AppSettings { WaptExecutablePath = "wapt-get.exe" };
        var environmentVariables = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["PATH"] = @"C:\Tools\Wapt;C:\Windows\System32"
        };

        var environment = WaptEnvironmentDetector.Inspect(
            settings,
            fileExists: path => string.Equals(path, pathExecutable, StringComparison.OrdinalIgnoreCase),
            getEnvironmentVariable: name => environmentVariables.TryGetValue(name, out var value) ? value : null);

        Assert.True(environment.IsWaptExecutableAvailable);
        Assert.Equal(pathExecutable, environment.EffectiveExecutablePath);
        Assert.Equal("path", environment.ExecutableDetectionSource);
    }

    [Fact]
    public void Inspect_ResolvesExecutableFromCommonLocationWhenNotOnPath()
    {
        var commonLocationExecutable = Path.Combine(@"C:\Program Files", "wapt", "wapt-get.exe");
        var settings = new AppSettings { WaptExecutablePath = "wapt-get.exe" };
        var environmentVariables = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["PATH"] = @"C:\Windows\System32",
            ["ProgramFiles"] = @"C:\Program Files"
        };

        var environment = WaptEnvironmentDetector.Inspect(
            settings,
            fileExists: path => string.Equals(path, commonLocationExecutable, StringComparison.OrdinalIgnoreCase),
            getEnvironmentVariable: name => environmentVariables.TryGetValue(name, out var value) ? value : null);

        Assert.True(environment.IsWaptExecutableAvailable);
        Assert.Equal(commonLocationExecutable, environment.EffectiveExecutablePath);
        Assert.Equal("common-location", environment.ExecutableDetectionSource);
    }
}