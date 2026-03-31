using System;
using WaptStudio.Core.Models;

namespace WaptStudio.Core.Services;

public sealed class WaptSessionService : IDisposable
{
    private char[]? _certificatePassword;
    private string? _adminUser;
    private char[]? _adminPassword;

    public bool HasCertificatePassword => _certificatePassword is { Length: > 0 };

    public bool HasServerCredentials => !string.IsNullOrWhiteSpace(_adminUser) && _adminPassword is { Length: > 0 };

    public WaptSessionSnapshot GetSnapshot()
        => new(HasCertificatePassword, HasServerCredentials);

    public void StoreFromExecutionContext(WaptExecutionContext executionContext, bool includeCertificatePassword, bool includeAdminCredentials)
    {
        ArgumentNullException.ThrowIfNull(executionContext);

        if (includeCertificatePassword && !string.IsNullOrWhiteSpace(executionContext.CertificatePassword))
        {
            StoreCertificatePassword(executionContext.CertificatePassword);
        }

        if (includeAdminCredentials && executionContext.HasAdminCredentials)
        {
            StoreServerCredentials(executionContext.AdminUser!, executionContext.AdminPassword!);
        }
    }

    public WaptExecutionContext? CreateExecutionContext(bool includeCertificatePassword, bool includeAdminCredentials, string? waptFilePath = null)
    {
        var context = new WaptExecutionContext
        {
            WaptFilePath = waptFilePath
        };

        if (includeCertificatePassword)
        {
            if (!HasCertificatePassword)
            {
                return null;
            }

            context.CertificatePassword = new string(_certificatePassword!);
        }

        if (includeAdminCredentials)
        {
            if (!HasServerCredentials)
            {
                context.Clear();
                return null;
            }

            context.AdminUser = _adminUser;
            context.AdminPassword = new string(_adminPassword!);
        }

        return context;
    }

    public void StoreCertificatePassword(string certificatePassword)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(certificatePassword);
        ReplaceSecret(ref _certificatePassword, certificatePassword);
    }

    public void StoreServerCredentials(string adminUser, string adminPassword)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(adminUser);
        ArgumentException.ThrowIfNullOrWhiteSpace(adminPassword);

        _adminUser = adminUser.Trim();
        ReplaceSecret(ref _adminPassword, adminPassword);
    }

    public void ClearCertificatePassword()
        => ClearSecret(ref _certificatePassword);

    public void ClearServerCredentials()
    {
        _adminUser = null;
        ClearSecret(ref _adminPassword);
    }

    public void ClearAll()
    {
        ClearCertificatePassword();
        ClearServerCredentials();
    }

    public void Dispose()
        => ClearAll();

    private static void ReplaceSecret(ref char[]? target, string value)
    {
        ClearSecret(ref target);
        target = value.ToCharArray();
    }

    private static void ClearSecret(ref char[]? value)
    {
        if (value is null)
        {
            return;
        }

        Array.Clear(value, 0, value.Length);
        value = null;
    }
}