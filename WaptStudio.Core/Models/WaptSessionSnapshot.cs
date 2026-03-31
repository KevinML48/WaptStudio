namespace WaptStudio.Core.Models;

public sealed record WaptSessionSnapshot(
    bool HasCertificatePassword,
    bool HasServerCredentials)
{
    public bool HasAnySecrets => HasCertificatePassword || HasServerCredentials;
}