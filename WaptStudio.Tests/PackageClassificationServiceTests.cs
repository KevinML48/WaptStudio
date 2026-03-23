using WaptStudio.Core.Models;
using WaptStudio.Core.Services;
using Xunit;

namespace WaptStudio.Tests;

public sealed class PackageClassificationServiceTests
{
    [Fact]
    public void Classify_ReturnsMsi_WhenSetupContainsInstallMsiCall()
    {
        var service = new PackageClassificationService();
        var info = new PackageInfo { InstallerPath = @"C:\Packages\app.exe" };

        var result = service.Classify(info, "install_msi_if_needed('app.msi')");

        Assert.Equal(PackageCategory.Msi, result);
    }

    [Fact]
    public void Classify_ReturnsExe_WhenSetupContainsInstallExeCall()
    {
        var service = new PackageClassificationService();
        var info = new PackageInfo { InstallerPath = @"C:\Packages\app.msi" };

        var result = service.Classify(info, "print('x')\ninstall_exe_if_needed('app.exe')");

        Assert.Equal(PackageCategory.Exe, result);
    }

    [Fact]
    public void Classify_ReturnsOther_WhenNoReliableSignalExists()
    {
        var service = new PackageClassificationService();
        var info = new PackageInfo { ReferencedInstallerName = "archive.zip" };

        var result = service.Classify(info, "print('noop')");

        Assert.Equal(PackageCategory.Other, result);
    }
}