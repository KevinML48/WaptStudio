using System.Text;
using System.Drawing;
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
        BackColor = Color.FromArgb(243, 245, 249);
        Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(20),
            BackColor = Color.FromArgb(243, 245, 249)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var summaryCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            BorderStyle = BorderStyle.None,
            Padding = new Padding(18),
            Margin = new Padding(0, 0, 0, 10)
        };
        summaryCard.Controls.Add(new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            BackColor = Color.White,
            Text = BuildSummary(entry)
        });

        var tabControl = new TabControl { Dock = DockStyle.Fill };
        tabControl.TabPages.Add(CreateTab("Commande", entry.ExecutedCommand ?? "Aucune commande enregistree."));
        tabControl.TabPages.Add(CreateTab("Sortie standard", entry.StandardOutput ?? string.Empty));
        tabControl.TabPages.Add(CreateTab("Erreurs / alertes", entry.StandardError ?? string.Empty));
        tabControl.TabPages.Add(CreateTab("Fiche complete", BuildContent(entry)));

        root.Controls.Add(summaryCard, 0, 0);
        root.Controls.Add(tabControl, 0, 1);
        Controls.Add(root);
    }

    private static TabPage CreateTab(string title, string content)
    {
        var page = new TabPage(title) { BackColor = Color.White };
        page.Controls.Add(new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            BackColor = Color.White,
            Text = string.IsNullOrWhiteSpace(content) ? "Aucune information disponible." : content
        });
        return page;
    }

    private static string BuildSummary(HistoryEntry entry)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Action: {entry.ActionType}");
        builder.AppendLine($"Date: {entry.Timestamp:yyyy-MM-dd HH:mm:ss}");
        builder.AppendLine($"Paquet: {entry.PackageName ?? "N/A"}");
        builder.AppendLine($"Etat: {(entry.Success ? "Reussite" : "Echec ou action inachevee")}");
        builder.AppendLine($"Verdict preparation: {entry.ReadinessVerdict ?? "N/A"}");
        builder.AppendLine($"Version: {entry.VersionBefore ?? "N/A"} -> {entry.VersionAfter ?? "N/A"}");
        builder.AppendLine($".wapt associe: {entry.WaptArtifactPath ?? "N/A"}");
        builder.AppendLine($"Resume: {entry.Message}");
        builder.AppendLine();
        builder.AppendLine("Les details techniques restent disponibles dans les onglets ci-dessous.");
        return builder.ToString();
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