using System;
using System.Text;
using WaptStudio.Core.Configuration;
using WaptStudio.Core.Models;

namespace WaptStudio.Core.Utilities;

public static class WaptEnvironmentReportBuilder
{
    public static string Build(AppSettings settings, WaptEnvironmentInfo environmentInfo)
    {
        var builder = new StringBuilder();
        builder.AppendLine("WaptStudio - Diagnostic environnement local");
        builder.AppendLine(new string('=', 44));
        builder.AppendLine();
        builder.AppendLine("Stockage local utilisateur");
        builder.AppendLine($"- Base locale          : {environmentInfo.BaseDirectory}");
        builder.AppendLine($"- Configuration        : {environmentInfo.ConfigDirectory}");
        builder.AppendLine($"- Fichier settings     : {environmentInfo.SettingsFilePath}");
        builder.AppendLine($"- Donnees              : {environmentInfo.DataDirectory}");
        builder.AppendLine($"- Historique SQLite    : {environmentInfo.HistoryDatabasePath}");
        builder.AppendLine($"- Cache                : {environmentInfo.CacheDirectory}");
        builder.AppendLine($"- Logs                 : {environmentInfo.LogsDirectory}");
        builder.AppendLine($"- Backups              : {environmentInfo.BackupsDirectory}");
        builder.AppendLine();
        builder.AppendLine("Integration WAPT");
        builder.AppendLine($"- Chemin configure     : {environmentInfo.ConfiguredExecutablePath ?? "auto"}");
        builder.AppendLine($"- Chemin effectif      : {environmentInfo.EffectiveExecutablePath ?? "non detecte"}");
        builder.AppendLine($"- Source detection     : {TranslateDetectionSource(environmentInfo.ExecutableDetectionSource)}");
        builder.AppendLine($"- WAPT disponible      : {(environmentInfo.IsWaptExecutableAvailable ? "oui" : "non")}");
        builder.AppendLine();
        builder.AppendLine("Signature / publication");
        builder.AppendLine($"- Signature active     : {(settings.EnableSigning ? "oui" : "non")}");
        builder.AppendLine($"- Cle configuree       : {environmentInfo.SigningKeyPath ?? "non renseignee"}");
        builder.AppendLine($"- Cle accessible       : {(environmentInfo.IsSigningKeyAvailable ? "oui" : "non")}");
        builder.AppendLine($"- Upload active        : {(settings.EnableUpload ? "oui" : "non")}");
        builder.AppendLine($"- Depot d'upload       : {settings.UploadRepositoryUrl ?? "non renseigne"}");
        builder.AppendLine();
        builder.AppendLine("Ce que WaptStudio detecte automatiquement");
        builder.AppendLine("- l'emplacement de wapt-get.exe via le PATH, WAPT_HOME/WAPT_ROOT et les emplacements Windows courants");
        builder.AppendLine("- les dossiers locaux utilisateur sous %LOCALAPPDATA%\\WaptStudio ou via la variable WAPTSTUDIO_HOME");
        builder.AppendLine();
        builder.AppendLine("Ce qui reste manuel");
        builder.AppendLine("- le certificat de signature personnel");
        builder.AppendLine("- l'URL du depot d'upload et les droits associes");
        builder.AppendLine("- le dossier catalogue a inventorier");
        builder.AppendLine();

        if (!environmentInfo.IsWaptExecutableAvailable)
        {
            builder.AppendLine("Actions recommandees");
            builder.AppendLine("- installer WAPT sur ce poste ou ajouter wapt-get.exe au PATH");
            builder.AppendLine("- sinon renseigner explicitement le champ 'Chemin WAPT' dans les parametres");
            builder.AppendLine();
        }

        if (environmentInfo.CheckedExecutablePaths.Count > 0)
        {
            builder.AppendLine("Emplacements verifies");
            foreach (var checkedPath in environmentInfo.CheckedExecutablePaths)
            {
                builder.AppendLine($"- {checkedPath}");
            }
            builder.AppendLine();
        }

        builder.AppendLine($"Variable optionnelle   : {AppPaths.BaseDirectoryOverrideEnvironmentVariable}");
        builder.AppendLine("Utilisez-la uniquement si vous devez forcer un autre emplacement de stockage local.");
        return builder.ToString().TrimEnd();
    }

    private static string TranslateDetectionSource(string detectionSource)
        => detectionSource switch
        {
            "configuration" => "configuration utilisateur",
            "path" => "PATH Windows",
            "common-location" => "emplacement WAPT standard",
            _ => "non detecte"
        };
}