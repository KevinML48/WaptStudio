using System;
using System.IO;
using System.Windows.Forms;

namespace WaptStudio.App.Forms;

public sealed class ManualBuildWorkflowForm : Form
{
    private readonly string _packageFolder;
    private readonly TextBox _commandTextBox;
    private readonly TextBox _generatedPackageTextBox;

    public ManualBuildWorkflowForm(string workflowName, string packageFolder, string? preparedCommand, string instructionsText, string artifactLabel, string selectArtifactButtonText)
    {
        _packageFolder = packageFolder;
        GeneratedPackagePath = string.Empty;

        Text = $"Workflow {workflowName} manuel WAPT";
        Width = 980;
        Height = 520;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;

        var instructionsLabel = new Label
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            MaximumSize = new System.Drawing.Size(920, 0),
            Text = instructionsText
        };

        _commandTextBox = new TextBox
        {
            Dock = DockStyle.Top,
            Multiline = true,
            ReadOnly = true,
            Height = 90,
            ScrollBars = ScrollBars.Vertical,
            Text = preparedCommand ?? string.Empty
        };

        var packageFolderTextBox = new TextBox
        {
            Dock = DockStyle.Top,
            ReadOnly = true,
            Text = _packageFolder
        };

        _generatedPackageTextBox = new TextBox
        {
            Dock = DockStyle.Top,
            ReadOnly = true,
            Text = string.Empty
        };

        var copyButton = new Button { Text = "Copier la commande", AutoSize = true };
        copyButton.Click += (_, _) => CopyCommand();

        var openPowerShellButton = new Button { Text = "Ouvrir PowerShell ici", AutoSize = true };
        openPowerShellButton.Click += (_, _) => OpenPowerShellHere();

        var selectPackageButton = new Button { Text = selectArtifactButtonText, AutoSize = true };
        selectPackageButton.Click += (_, _) => SelectGeneratedPackage();

        var confirmButton = new Button { Text = "Marquer comme operation manuelle reussie", AutoSize = true };
        confirmButton.Click += (_, _) => ConfirmManualAction();

        var closeButton = new Button { Text = "Fermer", AutoSize = true };
        closeButton.Click += (_, _) => Close();

        var buttonsFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            WrapContents = true,
            Padding = new Padding(0, 8, 0, 0)
        };
        buttonsFlow.Controls.Add(copyButton);
        buttonsFlow.Controls.Add(openPowerShellButton);
        buttonsFlow.Controls.Add(selectPackageButton);
        buttonsFlow.Controls.Add(confirmButton);
        buttonsFlow.Controls.Add(closeButton);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 8,
            Padding = new Padding(12)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        layout.Controls.Add(instructionsLabel, 0, 0);
        layout.Controls.Add(new Label { Text = "Commande WAPT a executer manuellement", AutoSize = true, Padding = new Padding(0, 12, 0, 4) }, 0, 1);
        layout.Controls.Add(_commandTextBox, 0, 2);
        layout.Controls.Add(new Label { Text = "Dossier du paquet", AutoSize = true, Padding = new Padding(0, 12, 0, 4) }, 0, 3);
        layout.Controls.Add(packageFolderTextBox, 0, 4);
        layout.Controls.Add(new Label { Text = artifactLabel, AutoSize = true, Padding = new Padding(0, 12, 0, 4) }, 0, 5);
        layout.Controls.Add(_generatedPackageTextBox, 0, 6);
        layout.Controls.Add(buttonsFlow, 0, 7);

        Controls.Add(layout);
    }

    public string GeneratedPackagePath { get; private set; }

    public bool ManualActionConfirmed { get; private set; }

    private void CopyCommand()
    {
        if (string.IsNullOrWhiteSpace(_commandTextBox.Text))
        {
            MessageBox.Show(this, "Aucune commande a copier.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Clipboard.SetText(_commandTextBox.Text);
        MessageBox.Show(this, "Commande copiee dans le presse-papiers.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void OpenPowerShellHere()
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "powershell.exe",
            WorkingDirectory = _packageFolder,
            UseShellExecute = true
        });
    }

    private void SelectGeneratedPackage()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Paquets WAPT (*.wapt)|*.wapt|Tous les fichiers (*.*)|*.*",
            Title = "Selectionner le fichier .wapt associe",
            InitialDirectory = Directory.Exists(_packageFolder) ? _packageFolder : Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        GeneratedPackagePath = dialog.FileName;
        _generatedPackageTextBox.Text = dialog.FileName;
    }

    private void ConfirmManualAction()
    {
        ManualActionConfirmed = true;
        DialogResult = DialogResult.OK;
        Close();
    }
}