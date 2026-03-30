using System;
using System.Linq;
using WaptStudio.Core.Models;
using WaptStudio.Core.Utilities;
using Xunit;

namespace WaptStudio.Tests;

public sealed class ActionReadinessEvaluatorTests
{
    [Fact]
    public void Evaluate_MarksBuildAsValidated_AndAuditUninstallAsNotVerifiedByDefault()
    {
        var packageInfo = CreatePackageInfo();
        var validationResult = new ValidationResult
        {
            Verdict = ReadinessVerdict.ReadyForBuildUpload,
            BuildPossible = true,
            UploadPossible = true,
            AuditPossible = true,
            UninstallPossible = true
        };
        var settings = new AppSettings
        {
            EnableUpload = true,
            AuditPackageArguments = "audit {packageId}",
            UninstallPackageArguments = "remove {packageId}"
        };

        var states = ActionReadinessEvaluator.Evaluate(packageInfo, validationResult, settings, packageFolder: packageInfo.PackageFolder);

        Assert.Equal(ActionReadinessStatus.Validated, states.Single(state => state.ActionKey == "build").Status);
        Assert.Equal(ActionReadinessStatus.Configured, states.Single(state => state.ActionKey == "upload").Status);
        Assert.Equal(ActionReadinessStatus.NotVerified, states.Single(state => state.ActionKey == "audit").Status);
        Assert.Equal(ActionReadinessStatus.NotVerified, states.Single(state => state.ActionKey == "uninstall").Status);
    }

    [Fact]
    public void Evaluate_MarksDirectUploadAsNotConfigured_WhenFeatureIsDisabled()
    {
        var packageInfo = CreatePackageInfo();
        var validationResult = new ValidationResult
        {
            Verdict = ReadinessVerdict.ReadyForBuildUpload,
            BuildPossible = true,
            UploadPossible = false
        };
        var settings = new AppSettings { EnableUpload = false };

        var states = ActionReadinessEvaluator.Evaluate(packageInfo, validationResult, settings, packageFolder: packageInfo.PackageFolder);

        Assert.Equal(ActionReadinessStatus.NotConfigured, states.Single(state => state.ActionKey == "upload").Status);
    }

    [Fact]
    public void Evaluate_MarksAuditAndUninstallAsTested_WhenSuccessfulHistoryExists()
    {
        var packageInfo = CreatePackageInfo();
        var validationResult = new ValidationResult
        {
            Verdict = ReadinessVerdict.ReadyWithWarnings,
            BuildPossible = true,
            AuditPossible = true,
            UninstallPossible = true
        };
        var settings = new AppSettings
        {
            AuditPackageArguments = "audit {packageId}",
            UninstallPackageArguments = "remove {packageId}"
        };
        var history = new[]
        {
            new HistoryEntry { ActionType = "Audit", Success = true, PackageFolder = packageInfo.PackageFolder, Timestamp = DateTimeOffset.UtcNow.AddMinutes(-5) },
            new HistoryEntry { ActionType = "Uninstall", Success = true, PackageFolder = packageInfo.PackageFolder, Timestamp = DateTimeOffset.UtcNow }
        };

        var states = ActionReadinessEvaluator.Evaluate(packageInfo, validationResult, settings, history, packageInfo.PackageFolder);

        Assert.Equal(ActionReadinessStatus.Tested, states.Single(state => state.ActionKey == "audit").Status);
        Assert.Equal(ActionReadinessStatus.Tested, states.Single(state => state.ActionKey == "uninstall").Status);
    }

    [Fact]
    public void Evaluate_MarksBuildAsTested_WhenSuccessfulBuildHistoryExists()
    {
        var packageInfo = CreatePackageInfo();
        var validationResult = new ValidationResult
        {
            Verdict = ReadinessVerdict.ReadyForBuildUpload,
            BuildPossible = true
        };
        var history = new[]
        {
            new HistoryEntry { ActionType = "Build", Success = true, PackageFolder = packageInfo.PackageFolder, Timestamp = DateTimeOffset.UtcNow }
        };

        var states = ActionReadinessEvaluator.Evaluate(packageInfo, validationResult, new AppSettings(), history, packageInfo.PackageFolder);

        Assert.Equal(ActionReadinessStatus.Tested, states.Single(state => state.ActionKey == "build").Status);
    }

    private static PackageInfo CreatePackageInfo()
        => new()
        {
            PackageFolder = @"C:\Packages\tis.package",
            PackageName = "tis.package",
            Version = "1.0.0",
            ExpectedWaptFileName = "tis.package_1.0.0_all.wapt"
        };
}