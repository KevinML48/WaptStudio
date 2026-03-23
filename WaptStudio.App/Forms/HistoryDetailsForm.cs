using System.Text;
using System.Windows.Forms;
using WaptStudio.Core.Models;

namespace WaptStudio.App.Forms;

public sealed class HistoryDetailsForm : Form
{
    public HistoryDetailsForm(HistoryEntry entry)
    {
        Text = $"Historique #{entry.Id}";
        Width = 900;
        Height = 700;
        StartPosition = FormStartPosition.CenterParent;

        var textBox = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            Text = BuildContent(entry)
        };

        Controls.Add(textBox);
    }

    private static string BuildContent(HistoryEntry entry)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Id: {entry.Id}");
        builder.AppendLine($"Date: {entry.Timestamp:O}");
        builder.AppendLine($"Action: {entry.ActionType}");
        builder.AppendLine($"Paquet: {entry.PackageName ?? "N/A"}");
        builder.AppendLine($"Dossier: {entry.PackageFolder}");
        builder.AppendLine($"Succes: {entry.Success}");
        builder.AppendLine($"Utilisateur Windows: {entry.WindowsUser}");
        builder.AppendLine($"Version avant: {entry.VersionBefore ?? "N/A"}");
        builder.AppendLine($"Version apres: {entry.VersionAfter ?? "N/A"}");
        builder.AppendLine($"Verdict readiness: {entry.ReadinessVerdict ?? "N/A"}");
        builder.AppendLine($"Chemin .wapt: {entry.WaptArtifactPath ?? "N/A"}");
        builder.AppendLine($"Duree (ms): {entry.DurationMilliseconds}");
        builder.AppendLine($"Exit code: {entry.ExitCode}");
        builder.AppendLine($"Message: {entry.Message}");
        builder.AppendLine();
        builder.AppendLine("Commande executee:");
        builder.AppendLine(entry.ExecutedCommand ?? "N/A");
        builder.AppendLine();
        builder.AppendLine("STDOUT:");
        builder.AppendLine(entry.StandardOutput ?? string.Empty);
        builder.AppendLine();
        builder.AppendLine("STDERR:");
        builder.AppendLine(entry.StandardError ?? string.Empty);
        return builder.ToString();
    }
}