namespace WaptStudio.Core.Models;

public sealed class AppSettings
{
    public string? CatalogRootFolder { get; set; }

    public bool CatalogScanRecursively { get; set; } = true;

    public int CatalogSemiRecursiveDepth { get; set; } = 2;

    public string WaptExecutablePath { get; set; } = "wapt-get.exe";

    public int CommandTimeoutSeconds { get; set; } = 300;

    public string AvailabilityArguments { get; set; } = "--version";

    public string ValidatePackageArguments { get; set; } = "show {packageFolder}";

    public string BuildPackageArguments { get; set; } = "build-package {packageFolder}";

    public string SignPackageArguments { get; set; } = "sign-package {packageFolder}";

    public string UploadPackageArguments { get; set; } = "upload-package {waptFilePath}";

    public string AuditPackageArguments { get; set; } = "audit {packageId}";

    public string UninstallPackageArguments { get; set; } = "remove {packageId}";

    public bool DryRunEnabled { get; set; }

    public bool CreateBackups { get; set; } = true;

    public string? LogsDirectory { get; set; }

    public string? BackupsDirectory { get; set; }

    public bool EnableSigning { get; set; } = true;

    public bool EnableUpload { get; set; } = false;

    public bool UploadOverwriteExisting { get; set; } = false;

    public string? SigningKeyPath { get; set; }

    public string? UploadRepositoryUrl { get; set; }

    public string? DefaultPackageFolder { get; set; }
}
