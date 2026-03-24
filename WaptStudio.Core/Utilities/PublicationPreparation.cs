using System;
using System.IO;
using System.Linq;
using WaptStudio.Core.Models;

namespace WaptStudio.Core.Utilities;

public static class PublicationPreparation
{
    public static PublicationPreparationResult Evaluate(
        string packageFolder,
        PackageInfo packageInfo,
        ValidationResult? validationResult,
        AppSettings settings,
        string? explicitWaptFilePath = null,
        bool allowExpectedPathWhenMissing = false)
    {
        ArgumentNullException.ThrowIfNull(packageInfo);
        ArgumentNullException.ThrowIfNull(settings);

        var waptFilePath = ResolveWaptFilePath(packageFolder, packageInfo, explicitWaptFilePath, allowExpectedPathWhenMissing);
        var hasRealWaptFile = !string.IsNullOrWhiteSpace(waptFilePath) && File.Exists(waptFilePath);
        var packageReady = validationResult is not null
            && validationResult.Verdict != ReadinessVerdict.Blocked
            && validationResult.BuildPossible;
        var directUploadAvailable = settings.EnableUpload && validationResult?.UploadPossible == true;
        var recommendedMode = directUploadAvailable && !settings.PreferWaptConsolePublish
            ? PublicationMode.DirectUpload
            : PublicationMode.WaptConsole;

        var statusMessage = BuildStatusMessage(packageReady, hasRealWaptFile, directUploadAvailable);
        var recommendationMessage = BuildRecommendationMessage(packageReady, hasRealWaptFile, recommendedMode, directUploadAvailable);

        return new PublicationPreparationResult
        {
            PackageFolder = packageFolder,
            PackageId = packageInfo.PackageName,
            Version = packageInfo.Version,
            Maturity = packageInfo.Maturity,
            WaptFilePath = hasRealWaptFile ? waptFilePath : null,
            PackageReady = packageReady,
            HasRealWaptFile = hasRealWaptFile,
            DirectUploadAvailable = directUploadAvailable,
            RecommendedMode = recommendedMode,
            StatusMessage = statusMessage,
            RecommendationMessage = recommendationMessage
        };
    }

    public static string GetPreparationHistoryAction(PublicationPreparationResult result)
        => result.CanPrepareForConsolePublish
            ? "PackagePreparedForConsolePublish"
            : "PackageNotReadyForPublish";

    public static string GetRecommendationHistoryAction(PublicationPreparationResult result)
        => result.RecommendedMode == PublicationMode.WaptConsole
            ? "ConsolePublishRecommended"
            : "DirectUploadRecommended";

    public static string GetDirectUploadHistoryAction(bool success)
        => success ? "DirectUploadSucceeded" : "DirectUploadFailed";

    private static string? ResolveWaptFilePath(string packageFolder, PackageInfo packageInfo, string? explicitWaptFilePath, bool allowExpectedPathWhenMissing)
    {
        if (!string.IsNullOrWhiteSpace(explicitWaptFilePath) && (File.Exists(explicitWaptFilePath) || allowExpectedPathWhenMissing))
        {
            return explicitWaptFilePath;
        }

        var parentDirectory = Path.GetDirectoryName(packageFolder);
        var searchDirectories = new[] { packageFolder, parentDirectory }
            .Where(directory => !string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
            .ToArray();

        var candidates = searchDirectories
            .SelectMany(directory => Directory.EnumerateFiles(directory!, "*.wapt", SearchOption.TopDirectoryOnly))
            .ToArray();

        var selected = WaptNaming.SelectBestWaptCandidate(candidates, packageInfo.ExpectedWaptFileName, packageInfo.PackageName, packageInfo.Version);
        if (!string.IsNullOrWhiteSpace(selected))
        {
            return selected;
        }

        if (!string.IsNullOrWhiteSpace(packageInfo.ExpectedWaptFileName) && allowExpectedPathWhenMissing)
        {
            return Path.Combine(packageFolder, packageInfo.ExpectedWaptFileName);
        }

        return null;
    }

    private static string BuildStatusMessage(bool packageReady, bool hasRealWaptFile, bool directUploadAvailable)
    {
        if (!packageReady)
        {
            return "Le paquet n'est pas pret a etre publie.";
        }

        if (!hasRealWaptFile)
        {
            return "Le paquet est valide, mais aucun vrai fichier .wapt n'a ete trouve.";
        }

        return directUploadAvailable
            ? "Le paquet est pret a etre publie."
            : "Le paquet est pret a etre publie via WAPT Console.";
    }

    private static string BuildRecommendationMessage(bool packageReady, bool hasRealWaptFile, PublicationMode recommendedMode, bool directUploadAvailable)
    {
        if (!packageReady)
        {
            return "Corrigez d'abord les blocages de readiness, puis relancez la verification avant toute publication.";
        }

        if (!hasRealWaptFile)
        {
            return "Construisez d'abord le vrai fichier .wapt, puis utilisez la publication via WAPT Console ou l'upload direct selon votre environnement.";
        }

        if (recommendedMode == PublicationMode.DirectUpload)
        {
            return "L'environnement autorise l'upload direct. Vous pouvez publier directement depuis WaptStudio si les identifiants serveur sont operationnels.";
        }

        return directUploadAvailable
            ? "Le paquet est pret a etre publie via WAPT Console. L'upload direct reste disponible uniquement pour les environnements qui maitrisent reellement l'authentification serveur."
            : "Le paquet est pret a etre publie via WAPT Console.";
    }
}