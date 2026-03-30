namespace WaptStudio.Core.Models;

public sealed class PublicationPreparationResult
{
    public string PackageFolder { get; init; } = string.Empty;

    public string? PackageId { get; init; }

    public string? Version { get; init; }

    public string? Maturity { get; init; }

    public string? WaptFilePath { get; init; }

    public bool PackageReady { get; init; }

    public bool HasRealWaptFile { get; init; }

    public bool DirectUploadAvailable { get; init; }

    public PublicationMode RecommendedMode { get; init; }

    public string StatusMessage { get; init; } = string.Empty;

    public string RecommendationMessage { get; init; } = string.Empty;

    public bool CanPrepareForConsolePublish => PackageReady && HasRealWaptFile;

    public bool CanPrepareDirectUpload => CanPrepareForConsolePublish && DirectUploadAvailable;
}