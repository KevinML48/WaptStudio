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

        var textBox = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            Text = diagnosticsText
        };

        Controls.Add(textBox);
    }
}
