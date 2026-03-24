using System.Drawing;
using System.Windows.Forms;

namespace WaptStudio.App.Forms;

public sealed class EnvironmentDiagnosticsForm : Form
{
    public EnvironmentDiagnosticsForm(string diagnosticsText)
    {
        Text = "Diagnostic environnement";
        Width = 900;
        Height = 650;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(243, 245, 249);
        Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 1,
            Padding = new Padding(20),
            BackColor = Color.FromArgb(243, 245, 249)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            BorderStyle = BorderStyle.None,
            Padding = new Padding(18)
        };

        var textBox = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            BackColor = Color.White,
            Font = new Font("Consolas", 9.5F, FontStyle.Regular),
            Text = diagnosticsText
        };

        card.Controls.Add(textBox);
        root.Controls.Add(card, 0, 0);
        Controls.Add(root);
    }
}
