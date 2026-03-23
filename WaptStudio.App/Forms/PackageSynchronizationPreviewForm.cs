using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using WaptStudio.Core.Models;

namespace WaptStudio.App.Forms;

public sealed class PackageSynchronizationPreviewForm : Form
{
    public PackageSynchronizationPreviewForm(PackageSynchronizationPlan plan)
    {
        Text = "Previsualisation synchronisation WAPT";
        Width = 1080;
        Height = 760;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(246, 248, 252);
        Font = new Font("Segoe UI", 9.5F);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(18)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var summaryTextBox = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            BackColor = Color.White,
            Text = BuildPreview(plan)
        };

        var container = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(16)
        };
        container.Controls.Add(summaryTextBox);

        var applyButton = new Button { Text = "Appliquer", AutoSize = true };
        applyButton.FlatStyle = FlatStyle.Flat;
        applyButton.BackColor = Color.FromArgb(42, 102, 178);
        applyButton.ForeColor = Color.White;
        applyButton.Click += (_, _) =>
        {
            DialogResult = DialogResult.OK;
            Close();
        };

        var cancelButton = new Button { Text = "Annuler", AutoSize = true };
        cancelButton.FlatStyle = FlatStyle.Flat;
        cancelButton.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(12)
        };
        buttons.Controls.Add(cancelButton);
        buttons.Controls.Add(applyButton);

        root.Controls.Add(container, 0, 0);
        root.Controls.Add(buttons, 0, 1);
        Controls.Add(root);
    }

    private static string BuildPreview(PackageSynchronizationPlan plan)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Plan de synchronisation avant application");
        builder.AppendLine();
        builder.AppendLine($"Installeur: {plan.CurrentInstallerName ?? "N/A"} -> {plan.TargetInstallerName ?? "N/A"}");
        builder.AppendLine($"Type: {plan.CurrentInstallerType ?? "N/A"} -> {plan.TargetInstallerType ?? "N/A"}");
        builder.AppendLine($"Version: {plan.CurrentVersion ?? "N/A"} -> {plan.TargetVersion ?? "N/A"}");
        builder.AppendLine($"Name: {plan.CurrentVisibleName ?? "N/A"} -> {plan.TargetVisibleName ?? "N/A"}");
        builder.AppendLine($"Description: {plan.CurrentDescription ?? "N/A"} -> {plan.TargetDescription ?? "N/A"}");
        builder.AppendLine($"Description FR: {plan.CurrentDescriptionFr ?? "N/A"} -> {plan.TargetDescriptionFr ?? "N/A"}");
        builder.AppendLine($"Dossier: {plan.CurrentPackageFolder} -> {plan.TargetPackageFolder ?? plan.CurrentPackageFolder}");
        builder.AppendLine($"Nom .wapt attendu: {plan.ExpectedWaptFileName ?? "N/A"}");
        builder.AppendLine();
        builder.AppendLine("Resume des synchronisations:");
        foreach (var line in plan.SummaryLines.Distinct())
        {
            builder.AppendLine($"- {line}");
        }

        if (plan.FilesDeleted.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Fichiers supprimes:");
            foreach (var deleted in plan.FilesDeleted.Distinct())
            {
                builder.AppendLine($"- {deleted}");
            }
        }

        if (plan.FilesModified.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Fichiers modifies:");
            foreach (var modified in plan.FilesModified.Distinct())
            {
                builder.AppendLine($"- {modified}");
            }
        }

        builder.AppendLine();
        builder.AppendLine($"Sauvegarde auto: {(plan.BackupWillBeCreated ? "Oui" : "Non")}");
        if (!string.IsNullOrWhiteSpace(plan.BackupDirectory))
        {
            builder.AppendLine($"Dossier de sauvegarde: {plan.BackupDirectory}");
        }

        if (!string.IsNullOrWhiteSpace(plan.ExpectedWaptFileNameNote))
        {
            builder.AppendLine($"Note .wapt: {plan.ExpectedWaptFileNameNote}");
        }

        if (plan.Warnings.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Avertissements:");
            foreach (var warning in plan.Warnings.Distinct())
            {
                builder.AppendLine($"- {warning}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("Aucune suppression ne sera faite sans sauvegarde prealable. L'application mettra a jour setup.py et control uniquement sur les motifs reconnus et juges fiables.");
        return builder.ToString();
    }
}