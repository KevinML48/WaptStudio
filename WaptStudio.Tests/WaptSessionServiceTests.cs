using WaptStudio.Core.Models;
using WaptStudio.Core.Services;
using Xunit;

namespace WaptStudio.Tests;

public sealed class WaptSessionServiceTests
{
    [Fact]
    public void Session_IsEmptyByDefault()
    {
        using var service = new WaptSessionService();

        var snapshot = service.GetSnapshot();

        Assert.False(snapshot.HasCertificatePassword);
        Assert.False(snapshot.HasServerCredentials);
        Assert.False(snapshot.HasAnySecrets);
        Assert.Null(service.CreateExecutionContext(includeCertificatePassword: true, includeAdminCredentials: false));
        Assert.Null(service.CreateExecutionContext(includeCertificatePassword: false, includeAdminCredentials: true));
    }

    [Fact]
    public void Session_StoresAndReusesSecretsAcrossMultipleContexts()
    {
        using var service = new WaptSessionService();
        service.StoreFromExecutionContext(
            new WaptExecutionContext
            {
                CertificatePassword = "cert-secret",
                AdminUser = "server-user",
                AdminPassword = "server-password"
            },
            includeCertificatePassword: true,
            includeAdminCredentials: true);

        var buildContext = service.CreateExecutionContext(includeCertificatePassword: true, includeAdminCredentials: false);
        var uploadContext = service.CreateExecutionContext(includeCertificatePassword: false, includeAdminCredentials: true, waptFilePath: @"C:\Packages\sample.wapt");

        Assert.NotNull(buildContext);
        Assert.Equal("cert-secret", buildContext!.CertificatePassword);
        Assert.False(buildContext.HasAdminCredentials);

        Assert.NotNull(uploadContext);
        Assert.Equal("server-user", uploadContext!.AdminUser);
        Assert.Equal("server-password", uploadContext.AdminPassword);
        Assert.Equal(@"C:\Packages\sample.wapt", uploadContext.WaptFilePath);
    }

    [Fact]
    public void Session_ClearAll_RemovesSecrets()
    {
        using var service = new WaptSessionService();
        service.StoreCertificatePassword("cert-secret");
        service.StoreServerCredentials("server-user", "server-password");

        service.ClearAll();

        var snapshot = service.GetSnapshot();
        Assert.False(snapshot.HasAnySecrets);
        Assert.Null(service.CreateExecutionContext(includeCertificatePassword: true, includeAdminCredentials: false));
        Assert.Null(service.CreateExecutionContext(includeCertificatePassword: false, includeAdminCredentials: true));
    }

    [Fact]
    public void Session_Dispose_ClearsSecrets()
    {
        var service = new WaptSessionService();
        service.StoreCertificatePassword("cert-secret");
        service.StoreServerCredentials("server-user", "server-password");

        service.Dispose();

        var snapshot = service.GetSnapshot();
        Assert.False(snapshot.HasAnySecrets);
    }
}