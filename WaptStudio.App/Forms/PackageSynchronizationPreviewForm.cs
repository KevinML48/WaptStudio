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
        Text = "Previsualisation du remplacement";
        Width = 1080;
        Height = 760;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(243, 245, 249);
        Font = new Font("Segoe UI", 9.5F);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(20)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Color.FromArgb(243, 245, 249)
        };
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 34));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 26));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 40));

        var titlePanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, BackColor = Color.FromArgb(243, 245, 249), Margin = new Padding(0, 0, 0, 10) };
        titlePanel.Controls.Add(new Label
        {
            Text = "Verifiez ce qui va changer avant application",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            Margin = new Padding(0, 0, 0, 4)
        }, 0, 0);
        titlePanel.Controls.Add(new Label
        {
            Text = "Le remplacement reste borne au paquet courant. Une sauvegarde est preparee avant toute suppression ou modification reconnue comme fiable.",
            AutoSize = true,
            ForeColor = Color.FromArgb(100, 116, 139),
            MaximumSize = new Size(980, 0)
        }, 0, 1);

        var beforeAfter = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = Color.FromArgb(243, 245, 249) };
        beforeAfter.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        beforeAfter.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        beforeAfter.Controls.Add(CreateTextCard("Avant", BuildBeforeText(plan)), 0, 0);
        beforeAfter.Controls.Add(CreateTextCard("Apres", BuildAfterText(plan)), 1, 0);

        var impact = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = Color.FromArgb(243, 245, 249) };
        impact.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        impact.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        impact.Controls.Add(CreateTextCard("Ce qui sera mis a jour", BuildImpactText(plan)), 0, 0);
        impact.Controls.Add(CreateTextCard("Sauvegarde et points de vigilance", BuildRiskText(plan)), 1, 0);

        var detailsTextBox = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            BackColor = Color.White,
            Text = BuildPreview(plan)
        };

        var detailsCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            BorderStyle = BorderStyle.None,
            Padding = new Padding(18)
        };
        var detailsLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = Color.White };
        detailsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        detailsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        detailsLayout.Controls.Add(new Label
        {
            Text = "Details techniques",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            Margin = new Padding(0, 0, 0, 8)
        }, 0, 0);
        detailsLayout.Controls.Add(detailsTextBox, 0, 1);
        detailsCard.Controls.Add(detailsLayout);

        content.Controls.Add(titlePanel, 0, 0);
        content.Controls.Add(beforeAfter, 0, 1);
        content.Controls.Add(impact, 0, 2);
        content.Controls.Add(detailsCard, 0, 3);

        var applyButton = new Button { Text = "Appliquer", AutoSize = true };
        applyButton.FlatStyle = FlatStyle.Flat;
        applyButton.BackColor = Color.FromArgb(37, 99, 186);
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

        root.Controls.Add(content, 0, 0);
        root.Controls.Add(buttons, 0, 1);
        Controls.Add(root);
    }

    private static Control CreateTextCard(string title, string content)
    {
        var textBox = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            BackColor = Color.White,
            Text = content
        };

        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            BorderStyle = BorderStyle.None,
            Padding = new Padding(18),
            Margin = new Padding(0, 0, 10, 10)
        };

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = Color.White };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(new Label
        {
            Text = title,
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            Margin = new Padding(0, 0, 0, 8)
        }, 0, 0);
        layout.Controls.Add(textBox, 0, 1);
        card.Controls.Add(layout);
        return card;
    }

    private static string BuildBeforeText(PackageSynchronizationPlan plan)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Paquet: {plan.PackageId}");
        builder.AppendLine($"Installeur actuel: {plan.CurrentInstallerName ?? "Non detecte"}");
        builder.AppendLine($"Type actuel: {plan.CurrentInstallerType ?? "Non detecte"}");
        builder.AppendLine($"Version actuelle: {plan.CurrentVersion ?? "Non detectee"}");
        builder.AppendLine($"Nom actuel: {plan.CurrentVisibleName ?? "Non detecte"}");
        builder.AppendLine($"Dossier actuel: {plan.CurrentPackageFolder}");
        return builder.ToString();
    }

    private static string BuildAfterText(PackageSynchronizationPlan plan)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Nouvel installeur: {plan.TargetInstallerName ?? "Non detecte"}");
        builder.AppendLine($"Nouveau type: {plan.TargetInstallerType ?? "Non detecte"}");
        builder.AppendLine($"Nouvelle version: {plan.TargetVersion ?? "Non detectee"}");
        builder.AppendLine($"Nom apres mise a jour: {plan.TargetVisibleName ?? "Non detecte"}");
        builder.AppendLine($"Dossier vise: {plan.TargetPackageFolder ?? plan.CurrentPackageFolder}");
        builder.AppendLine($"Nom .wapt attendu: {plan.ExpectedWaptFileName ?? "Non calculable"}");
        return builder.ToString();
    }

    private static string BuildImpactText(PackageSynchronizationPlan plan)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Resume des changements:");
        foreach (var line in plan.SummaryLines.Distinct())
        {
            builder.AppendLine($"- {line}");
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

        if (plan.FilesDeleted.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Fichiers retires:");
            foreach (var deleted in plan.FilesDeleted.Distinct())
            {
                builder.AppendLine($"- {deleted}");
            }
        }

        return builder.ToString();
    }

    private static string BuildRiskText(PackageSynchronizationPlan plan)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Sauvegarde automatique: {(plan.BackupWillBeCreated ? "Oui" : "Non")}");
        if (!string.IsNullOrWhiteSpace(plan.BackupDirectory))
        {
            builder.AppendLine($"Dossier de sauvegarde: {plan.BackupDirectory}");
        }

        if (!string.IsNullOrWhiteSpace(plan.ExpectedWaptFileNameNote))
        {
            builder.AppendLine();
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
        else
        {
            builder.AppendLine();
            builder.AppendLine("Aucun avertissement supplementaire detecte.");
        }

        return builder.ToString();
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