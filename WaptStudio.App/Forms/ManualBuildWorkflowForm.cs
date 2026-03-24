using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace WaptStudio.App.Forms;

public sealed class ManualBuildWorkflowForm : Form
{
    private static readonly Color SurfaceColor = Color.FromArgb(243, 245, 249);
    private static readonly Color PanelColor = Color.White;
    private static readonly Color BorderColor = Color.FromArgb(228, 233, 240);
    private static readonly Color AccentColor = Color.FromArgb(37, 99, 186);
    private static readonly Color InfoColor = Color.FromArgb(100, 116, 139);
    private static readonly Color HeadingColor = Color.FromArgb(15, 23, 42);

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
        BackColor = SurfaceColor;
        Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);

        var titleLabel = new Label
        {
            AutoSize = true,
            Text = $"Workflow manuel {workflowName}",
            Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
            ForeColor = HeadingColor,
            Margin = new Padding(0, 0, 0, 6)
        };

        var instructionsLabel = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(920, 0),
            Text = instructionsText,
            ForeColor = InfoColor,
            Margin = new Padding(0, 0, 0, 10)
        };

        var reassuranceLabel = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(920, 0),
            Text = "Suivez simplement les etapes ci-dessous. WaptStudio prepare la commande et vous laisse confirmer le resultat une fois l'action terminee dans PowerShell.",
            ForeColor = InfoColor,
            Margin = new Padding(0, 0, 0, 8)
        };

        _commandTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            Height = 100,
            ScrollBars = ScrollBars.Vertical,
            BorderStyle = BorderStyle.FixedSingle,
            Text = preparedCommand ?? string.Empty
        };

        var packageFolderTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BorderStyle = BorderStyle.FixedSingle,
            Text = _packageFolder
        };

        _generatedPackageTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BorderStyle = BorderStyle.FixedSingle,
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

        foreach (var button in new[] { copyButton, openPowerShellButton, selectPackageButton, confirmButton, closeButton })
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = BorderColor;
            button.FlatAppearance.BorderSize = button == confirmButton ? 0 : 1;
            button.Padding = new Padding(14, 8, 14, 8);
            button.BackColor = button == confirmButton ? AccentColor : PanelColor;
            button.ForeColor = button == confirmButton ? Color.White : HeadingColor;
            button.Font = new Font("Segoe UI", 9.5F, button == confirmButton ? FontStyle.Bold : FontStyle.Regular);
            button.Margin = new Padding(0, 0, 8, 8);
        }

        var buttonsFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = true,
            Padding = new Padding(0, 8, 0, 0),
            BackColor = PanelColor
        };
        buttonsFlow.Controls.Add(copyButton);
        buttonsFlow.Controls.Add(openPowerShellButton);
        buttonsFlow.Controls.Add(selectPackageButton);
        buttonsFlow.Controls.Add(confirmButton);
        buttonsFlow.Controls.Add(closeButton);

        var stepsPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            BackColor = PanelColor,
            Margin = new Padding(0, 0, 0, 12)
        };
        stepsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        stepsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        stepsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));
        stepsPanel.Controls.Add(CreateStepCard("1. Copier", "Recopiez la commande preparee par WaptStudio."), 0, 0);
        stepsPanel.Controls.Add(CreateStepCard("2. Executer", "Ouvrez PowerShell dans le dossier du paquet et laissez WAPT demander les secrets si necessaire."), 1, 0);
        stepsPanel.Controls.Add(CreateStepCard("3. Confirmer", "Rattachez le .wapt obtenu puis confirmez la reussite manuelle."), 2, 0);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 8,
            Padding = new Padding(16),
            BackColor = SurfaceColor
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 38));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 62));

        var headerCard = CreateCard();
        var headerLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, BackColor = PanelColor };
        headerLayout.Controls.Add(titleLabel, 0, 0);
        headerLayout.Controls.Add(instructionsLabel, 0, 1);
        headerLayout.Controls.Add(reassuranceLabel, 0, 2);
        headerLayout.Controls.Add(stepsPanel, 0, 3);
        headerCard.Controls.Add(headerLayout);

        var detailsCard = CreateCard();
        var detailsLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6, BackColor = PanelColor };
        detailsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        detailsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
        detailsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        detailsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        detailsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        detailsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        detailsLayout.Controls.Add(CreateFieldLabel("Commande WAPT a executer manuellement"), 0, 0);
        detailsLayout.Controls.Add(_commandTextBox, 0, 1);
        detailsLayout.Controls.Add(CreateFieldLabel("Dossier du paquet"), 0, 2);
        detailsLayout.Controls.Add(packageFolderTextBox, 0, 3);
        detailsLayout.Controls.Add(CreateFieldLabel(artifactLabel), 0, 4);
        detailsLayout.Controls.Add(_generatedPackageTextBox, 0, 5);
        detailsCard.Controls.Add(detailsLayout);

        var actionsCard = CreateCard();
        actionsCard.Controls.Add(buttonsFlow);

        layout.Controls.Add(headerCard, 0, 0);
        layout.Controls.Add(detailsCard, 0, 1);
        layout.Controls.Add(actionsCard, 0, 2);

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

    private static Panel CreateCard()
        => new()
        {
            Dock = DockStyle.Fill,
            BackColor = PanelColor,
            BorderStyle = BorderStyle.None,
            Padding = new Padding(18),
            Margin = new Padding(0, 0, 0, 10)
        };

    private static Label CreateFieldLabel(string text)
        => new()
        {
            Text = text,
            AutoSize = true,
            Padding = new Padding(0, 0, 0, 4),
            Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
            ForeColor = HeadingColor
        };

    private static Control CreateStepCard(string title, string description)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(248, 250, 252),
            BorderStyle = BorderStyle.None,
            Padding = new Padding(14),
            Margin = new Padding(0, 0, 10, 0),
            MinimumSize = new Size(0, 86)
        };

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, BackColor = panel.BackColor };
        layout.Controls.Add(new Label { Text = title, AutoSize = true, Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold), ForeColor = HeadingColor }, 0, 0);
        layout.Controls.Add(new Label { Text = description, AutoSize = true, MaximumSize = new Size(240, 0), ForeColor = InfoColor, Margin = new Padding(0, 4, 0, 0) }, 0, 1);
        panel.Controls.Add(layout);
        return panel;
    }
}