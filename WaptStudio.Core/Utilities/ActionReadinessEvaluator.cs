using System;
using System.Collections.Generic;
using System.Linq;
using WaptStudio.Core.Models;

namespace WaptStudio.Core.Utilities;

public static class ActionReadinessEvaluator
{
    public static IReadOnlyList<ActionReadinessState> Evaluate(
        PackageInfo packageInfo,
        ValidationResult validationResult,
        AppSettings settings,
        IEnumerable<HistoryEntry>? historyEntries = null,
        string? packageFolder = null)
    {
        ArgumentNullException.ThrowIfNull(packageInfo);
        ArgumentNullException.ThrowIfNull(validationResult);
        ArgumentNullException.ThrowIfNull(settings);

        var history = historyEntries?
            .Where(entry => string.IsNullOrWhiteSpace(packageFolder) || string.Equals(entry.PackageFolder, packageFolder, StringComparison.OrdinalIgnoreCase))
            .ToArray()
            ?? Array.Empty<HistoryEntry>();

        return new[]
        {
            EvaluateBuild(validationResult, history),
            EvaluateDirectUpload(packageInfo, validationResult, settings, history),
            EvaluateAudit(packageInfo, validationResult, settings, history),
            EvaluateUninstall(packageInfo, validationResult, settings, history)
        };
    }

    private static ActionReadinessState EvaluateBuild(ValidationResult validationResult, IReadOnlyCollection<HistoryEntry> history)
    {
        var lastBuild = FindLatest(history, "Build", "BuildAndPublishBuildSucceeded", "BuildManualConfirmed");

        if (!validationResult.BuildPossible)
        {
            return Create("build", "Construire le .wapt", ActionReadinessStatus.NotAvailable, "Le paquet n'est pas encore assez coherent pour lancer une construction fiable.");
        }

        if (lastBuild?.Success == true)
        {
            return Create("build", "Construire le .wapt", ActionReadinessStatus.Tested, "Une construction reussie a deja ete enregistree pour ce paquet dans WaptStudio.");
        }

        return validationResult.Verdict == ReadinessVerdict.ReadyForBuildUpload
            ? Create("build", "Construire le .wapt", ActionReadinessStatus.Validated, "Le paquet a ete relu et les preconditions de construction sont valides dans la configuration actuelle.")
            : Create("build", "Construire le .wapt", ActionReadinessStatus.Available, "La construction reste possible, mais des points d'attention doivent etre revus avant de considerer le paquet comme pleinement fiable.");
    }

    private static ActionReadinessState EvaluateDirectUpload(PackageInfo packageInfo, ValidationResult validationResult, AppSettings settings, IReadOnlyCollection<HistoryEntry> history)
    {
        var lastUpload = FindLatest(history, "DirectUploadSucceeded", "DirectUploadFailed", "Upload", "UploadManualConfirmed", "DirectUploadManualConfirmed");

        if (!validationResult.BuildPossible)
        {
            return Create("upload", "Upload direct", ActionReadinessStatus.NotAvailable, "L'upload direct reste indisponible tant que le paquet n'est pas assez propre pour etre construit correctement.");
        }

        if (!settings.EnableUpload)
        {
            return Create("upload", "Upload direct", ActionReadinessStatus.NotConfigured, "Le mode d'upload direct est desactive dans la configuration de WaptStudio.");
        }

        if (string.IsNullOrWhiteSpace(packageInfo.ExpectedWaptFileName))
        {
            return Create("upload", "Upload direct", ActionReadinessStatus.NotAvailable, "Le nom attendu du fichier .wapt n'est pas fiabilise, donc l'envoi direct n'est pas assez sur pour etre propose comme pret.");
        }

        if (lastUpload?.Success == true)
        {
            return Create("upload", "Upload direct", ActionReadinessStatus.Tested, "Un upload direct a deja abouti pour ce paquet ou ce workflow sur ce poste.");
        }

        if (lastUpload is not null && !lastUpload.Success)
        {
            return Create("upload", "Upload direct", ActionReadinessStatus.Configured, "La fonction est configuree, mais la derniere tentative connue n'a pas abouti. Verifiez l'acces serveur et l'authentification avant de la reutiliser.");
        }

        return Create("upload", "Upload direct", ActionReadinessStatus.Configured, "La fonction est configuree dans l'application, mais elle n'est pas encore validee par un envoi reel reussi sur cet environnement.");
    }

    private static ActionReadinessState EvaluateAudit(PackageInfo packageInfo, ValidationResult validationResult, AppSettings settings, IReadOnlyCollection<HistoryEntry> history)
    {
        var lastAudit = FindLatest(history, "Audit", "AuditManualConfirmed");

        if (!validationResult.BuildPossible || string.IsNullOrWhiteSpace(packageInfo.PackageName))
        {
            return Create("audit", "Verifier sur un poste", ActionReadinessStatus.NotAvailable, "Le paquet n'offre pas encore les informations minimales pour lancer un audit poste de facon fiable.");
        }

        if (string.IsNullOrWhiteSpace(settings.AuditPackageArguments))
        {
            return Create("audit", "Verifier sur un poste", ActionReadinessStatus.NotConfigured, "La commande d'audit poste n'est pas configuree dans WaptStudio.");
        }

        if (lastAudit?.Success == true)
        {
            return Create("audit", "Verifier sur un poste", ActionReadinessStatus.Tested, "Un audit reel a deja ete lance avec succes depuis WaptStudio pour ce paquet.");
        }

        return Create("audit", "Verifier sur un poste", ActionReadinessStatus.NotVerified, "La fonction est preparee, mais aucun test poste reussi n'a encore ete enregistre pour ce paquet.");
    }

    private static ActionReadinessState EvaluateUninstall(PackageInfo packageInfo, ValidationResult validationResult, AppSettings settings, IReadOnlyCollection<HistoryEntry> history)
    {
        var lastUninstall = FindLatest(history, "Uninstall", "UninstallManualConfirmed");

        if (!validationResult.BuildPossible || string.IsNullOrWhiteSpace(packageInfo.PackageName))
        {
            return Create("uninstall", "Desinstaller du poste", ActionReadinessStatus.NotAvailable, "La desinstallation poste n'est pas encore presentable comme disponible tant que le paquet reste incomplet ou bloque.");
        }

        if (string.IsNullOrWhiteSpace(settings.UninstallPackageArguments))
        {
            return Create("uninstall", "Desinstaller du poste", ActionReadinessStatus.NotConfigured, "La commande de desinstallation poste n'est pas configuree dans WaptStudio.");
        }

        if (lastUninstall?.Success == true)
        {
            return Create("uninstall", "Desinstaller du poste", ActionReadinessStatus.Tested, "Une desinstallation reelle a deja ete confirmee depuis WaptStudio pour ce paquet.");
        }

        return Create("uninstall", "Desinstaller du poste", ActionReadinessStatus.NotVerified, "La fonction est preparee, mais aucune desinstallation reelle reussie n'a encore ete enregistree pour ce paquet.");
    }

    private static HistoryEntry? FindLatest(IReadOnlyCollection<HistoryEntry> history, params string[] actionTypes)
        => history
            .Where(entry => actionTypes.Any(actionType => string.Equals(entry.ActionType, actionType, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(entry => entry.Timestamp)
            .FirstOrDefault();

    private static ActionReadinessState Create(string key, string label, ActionReadinessStatus status, string detail)
        => new()
        {
            ActionKey = key,
            ActionLabel = label,
            Status = status,
            Detail = detail
        };
}