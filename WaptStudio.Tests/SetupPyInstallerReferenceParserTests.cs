using WaptStudio.Core.Utilities;
using Xunit;

namespace WaptStudio.Tests;

public sealed class SetupPyInstallerReferenceParserTests
{
    [Fact]
    public void ParseInstallReferences_RecognizesLiteralMsiArgument()
    {
        var content = "install_msi_if_needed('demo.msi')\n";

        var references = SetupPyInstallerReferenceParser.ParseInstallReferences(content);

        var reference = Assert.Single(references);
        Assert.Equal("msi", reference.FunctionKind);
        Assert.Equal("demo.msi", reference.ResolvedInstallerName);
        Assert.Null(reference.ConstantName);
    }

    [Fact]
    public void ParseInstallReferences_RecognizesLiteralExeArgument()
    {
        var content = "install_exe_if_needed(\"setup.exe\")\n";

        var references = SetupPyInstallerReferenceParser.ParseInstallReferences(content);

        var reference = Assert.Single(references);
        Assert.Equal("exe", reference.FunctionKind);
        Assert.Equal("setup.exe", reference.ResolvedInstallerName);
        Assert.Null(reference.ConstantName);
    }

    [Fact]
    public void ParseInstallReferences_ResolvesSimpleMsiConstantArgument()
    {
        var content = "MSI_NAME = \"pdf24-creator-11.1.0.msi\"\ninstall_msi_if_needed(MSI_NAME)\n";

        var references = SetupPyInstallerReferenceParser.ParseInstallReferences(content);

        var reference = Assert.Single(references);
        Assert.Equal("MSI_NAME", reference.ConstantName);
        Assert.Equal("pdf24-creator-11.1.0.msi", reference.ResolvedInstallerName);
    }

    [Fact]
    public void ParseInstallReferences_ResolvesSimpleExeConstantArgument()
    {
        var content = "EXE_NAME = 'setup.exe'\ninstall_exe_if_needed(EXE_NAME)\n";

        var references = SetupPyInstallerReferenceParser.ParseInstallReferences(content);

        var reference = Assert.Single(references);
        Assert.Equal("EXE_NAME", reference.ConstantName);
        Assert.Equal("setup.exe", reference.ResolvedInstallerName);
    }

    [Fact]
    public void UpdateInstallerReference_RewritesLiteralInstallerCall_Coherently()
    {
        var content = "install_msi_if_needed('demo-1.0.msi')\n";

        var updated = SetupPyInstallerReferenceParser.UpdateInstallerReference(content, "demo-1.0.msi", "demo-2.0.msi");

        Assert.Contains("install_msi_if_needed('demo-2.0.msi')", updated);
        Assert.True(SetupPyInstallerReferenceParser.HasCoherentInstallerReference(updated, "demo-2.0.msi"), updated.Replace("\r", "<CR>").Replace("\n", "<NL>"));
    }

    [Fact]
    public void UpdateInstallerReference_RewritesSimpleConstantInstallerValue_Coherently()
    {
        var content = "MSI_NAME = \"demo-1.0.msi\"\ninstall_msi_if_needed(MSI_NAME)\n";

        var updated = SetupPyInstallerReferenceParser.UpdateInstallerReference(content, "demo-1.0.msi", "demo-2.0.msi");

        Assert.Contains("MSI_NAME = \"demo-2.0.msi\"", updated);
        Assert.Contains("install_msi_if_needed(MSI_NAME)", updated);
        Assert.True(SetupPyInstallerReferenceParser.HasCoherentInstallerReference(updated, "demo-2.0.msi"), updated.Replace("\r", "<CR>").Replace("\n", "<NL>"));
    }
}