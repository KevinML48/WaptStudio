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
        Width = 980;
        Height = 640;
        StartPosition = FormStartPosition.CenterParent;

        var summaryTextBox = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            Text = BuildPreview(plan)
        };

        var applyButton = new Button { Text = "Appliquer", AutoSize = true };
        applyButton.Click += (_, _) =>
        {
            DialogResult = DialogResult.OK;
            Close();
        };

        var cancelButton = new Button { Text = "Annuler", AutoSize = true };
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

        Controls.Add(summaryTextBox);
        Controls.Add(buttons);
    }

    private static string BuildPreview(PackageSynchronizationPlan plan)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Plan de synchronisation WAPT apres remplacement MSI");
        builder.AppendLine();
        foreach (var line in plan.SummaryLines)
        {
            builder.AppendLine($"- {line}");
        }

        builder.AppendLine();
        builder.AppendLine($"Nom .wapt attendu: {plan.ExpectedWaptFileName}");
        if (!string.IsNullOrWhiteSpace(plan.ExpectedWaptFileNameNote))
        {
            builder.AppendLine($"Note: {plan.ExpectedWaptFileNameNote}");
        }

        if (plan.Warnings.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Points a verifier:");
            foreach (var warning in plan.Warnings.Distinct())
            {
                builder.AppendLine($"- {warning}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("Les modifications seront appliquees dans le dossier source, setup.py et control.");
        return builder.ToString();
    }
}