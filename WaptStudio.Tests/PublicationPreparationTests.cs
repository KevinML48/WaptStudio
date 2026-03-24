using System;
using System.IO;
using WaptStudio.Core.Models;
using WaptStudio.Core.Utilities;
using Xunit;

namespace WaptStudio.Tests;

public sealed class PublicationPreparationTests : IDisposable
{
    private readonly string _rootDirectory;

    public PublicationPreparationTests()
    {
        _rootDirectory = Path.Combine(Path.GetTempPath(), "WaptStudio.Tests.Publication", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_rootDirectory);
    }

    [Fact]
    public void Evaluate_ReturnsConsoleReadyWhenRealWaptExists()
    {
        var packageFolder = Path.Combine(_rootDirectory, "cd48-tool_1.0.0_windows_DEV-wapt");
        Directory.CreateDirectory(packageFolder);
        var expectedWaptPath = Path.Combine(_rootDirectory, "cd48-tool_1.0.0_windows_DEV.wapt");
        File.WriteAllText(expectedWaptPath, "artifact");

        var result = PublicationPreparation.Evaluate(
            packageFolder,
            new PackageInfo
            {
                PackageFolder = packageFolder,
                PackageName = "cd48-tool",
                Version = "1.0.0",
                Maturity = "DEV",
                ExpectedWaptFileName = Path.GetFileName(expectedWaptPath)
            },
            new ValidationResult
            {
                Verdict = ReadinessVerdict.ReadyForBuildUpload,
                BuildPossible = true,
                UploadPossible = false
            },
            new AppSettings
            {
                EnableUpload = false,
                PreferWaptConsolePublish = true
            });

        Assert.True(result.PackageReady);
        Assert.True(result.HasRealWaptFile);
        Assert.True(result.CanPrepareForConsolePublish);
        Assert.Equal(expectedWaptPath, result.WaptFilePath);
        Assert.Equal("cd48-tool", result.PackageId);
        Assert.Equal("1.0.0", result.Version);
        Assert.Equal("DEV", result.Maturity);
        Assert.Equal(PublicationMode.WaptConsole, result.RecommendedMode);
        Assert.Equal("PackagePreparedForConsolePublish", PublicationPreparation.GetPreparationHistoryAction(result));
        Assert.Equal("ConsolePublishRecommended", PublicationPreparation.GetRecommendationHistoryAction(result));
    }

    [Fact]
    public void Evaluate_ReturnsNotReadyWhenNoRealWaptExists()
    {
        var packageFolder = Path.Combine(_rootDirectory, "cd48-tool_1.0.0_windows_DEV-wapt");
        Directory.CreateDirectory(packageFolder);

        var result = PublicationPreparation.Evaluate(
            packageFolder,
            new PackageInfo
            {
                PackageFolder = packageFolder,
                PackageName = "cd48-tool",
                Version = "1.0.0",
                Maturity = "DEV",
                ExpectedWaptFileName = "cd48-tool_1.0.0_windows_DEV.wapt"
            },
            new ValidationResult
            {
                Verdict = ReadinessVerdict.ReadyWithWarnings,
                BuildPossible = true,
                UploadPossible = false
            },
            new AppSettings());

        Assert.True(result.PackageReady);
        Assert.False(result.HasRealWaptFile);
        Assert.False(result.CanPrepareForConsolePublish);
        Assert.Null(result.WaptFilePath);
        Assert.Equal("PackageNotReadyForPublish", PublicationPreparation.GetPreparationHistoryAction(result));
    }

    [Fact]
    public void Evaluate_CanRecommendDirectUploadWhenEnvironmentChoosesIt()
    {
        var packageFolder = Path.Combine(_rootDirectory, "cd48-tool_1.0.0_windows_DEV-wapt");
        Directory.CreateDirectory(packageFolder);
        var waptPath = Path.Combine(_rootDirectory, "cd48-tool_1.0.0_windows_DEV.wapt");
        File.WriteAllText(waptPath, "artifact");

        var result = PublicationPreparation.Evaluate(
            packageFolder,
            new PackageInfo
            {
                PackageFolder = packageFolder,
                PackageName = "cd48-tool",
                Version = "1.0.0",
                Maturity = "DEV",
                ExpectedWaptFileName = Path.GetFileName(waptPath)
            },
            new ValidationResult
            {
                Verdict = ReadinessVerdict.ReadyForBuildUpload,
                BuildPossible = true,
                UploadPossible = true
            },
            new AppSettings
            {
                EnableUpload = true,
                PreferWaptConsolePublish = false
            });

        Assert.True(result.DirectUploadAvailable);
        Assert.Equal(PublicationMode.DirectUpload, result.RecommendedMode);
        Assert.Equal("DirectUploadRecommended", PublicationPreparation.GetRecommendationHistoryAction(result));
        Assert.Equal("DirectUploadSucceeded", PublicationPreparation.GetDirectUploadHistoryAction(true));
        Assert.Equal("DirectUploadFailed", PublicationPreparation.GetDirectUploadHistoryAction(false));
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootDirectory))
        {
            Directory.Delete(_rootDirectory, recursive: true);
        }
    }
}