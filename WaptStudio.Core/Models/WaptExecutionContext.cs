using System.Collections.Generic;

namespace WaptStudio.Core.Models;

public sealed class WaptExecutionContext
{
    public string? CertificatePassword { get; set; }

    public string? AdminUser { get; set; }

    public string? AdminPassword { get; set; }

    public string? WaptFilePath { get; set; }

    public bool HasCertificatePassword => !string.IsNullOrWhiteSpace(CertificatePassword);

    public bool HasAdminCredentials => !string.IsNullOrWhiteSpace(AdminUser) && !string.IsNullOrWhiteSpace(AdminPassword);

    public IReadOnlyList<string> GetSensitiveValues()
    {
        var values = new List<string>();

        if (!string.IsNullOrWhiteSpace(CertificatePassword))
        {
            values.Add(CertificatePassword);
        }

        if (!string.IsNullOrWhiteSpace(AdminUser))
        {
            values.Add(AdminUser);
        }

        if (!string.IsNullOrWhiteSpace(AdminPassword))
        {
            values.Add(AdminPassword);
        }

        return values;
    }

    public void Clear()
    {
        CertificatePassword = null;
        AdminUser = null;
        AdminPassword = null;
        WaptFilePath = null;
    }
}