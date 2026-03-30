using System;
using System.Drawing;
using System.Windows.Forms;
using WaptStudio.Core.Models;
using WaptStudio.Core.Utilities;

namespace WaptStudio.App.Forms;

public sealed class PackageVersionStrategyForm : Form
{
    private static readonly Color HeadingColor = Color.FromArgb(15, 23, 42);
    private static readonly Color InfoColor = Color.FromArgb(82, 96, 120);
    private static readonly Color MutedColor = Color.FromArgb(148, 163, 184);
    private static readonly Color StrategyPanelColor = Color.FromArgb(248, 250, 253);
    private static readonly Color StrategySelectedColor = Color.FromArgb(218, 234, 255);
    private static readonly Color ExplicitFieldInactiveColor = Color.FromArgb(243, 245, 249);

    private readonly string? _currentVersion;
    private readonly string? _suggestedVersion;
    private readonly RadioButton _keepCurrentRadioButton = new() { AutoSize = true, Text = "Conserver la version actuelle" };
    private readonly RadioButton _incrementRevisionRadioButton = new() { AutoSize = true, Text = "Incrementer la revision du paquet" };
    private readonly RadioButton _setExplicitVersionRadioButton = new() { AutoSize = true, Text = "Definir une nouvelle version" };
    private readonly RadioButton _updateCurrentFolderRadioButton = new() { AutoSize = true, Text = "Mettre a jour dans le dossier actuel" };
    private readonly RadioButton _createVersionedFolderCloneRadioButton = new() { AutoSize = true, Text = "Creer un nouveau dossier versionne en conservant le contenu existant" };
    private readonly TextBox _explicitVersionTextBox = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, Enabled = false };
    private readonly Label _keepCurrentDescriptionLabel = new();
    private readonly Label _incrementRevisionDescriptionLabel = new();
    private readonly Label _setExplicitVersionDescriptionLabel = new();
    private readonly Label _explicitVersionTitleLabel = new();
    private readonly Label _explicitVersionHintLabel = new();
    private readonly Label _updateCurrentFolderDescriptionLabel = new();
    private readonly Label _createVersionedFolderCloneDescriptionLabel = new();
    private readonly TableLayoutPanel _explicitVersionLayout = new() { Dock = DockStyle.Top, ColumnCount = 1, AutoSize = true, BackColor = ExplicitFieldInactiveColor, Padding = new Padding(14) };

    public PackageVersionStrategyForm(PackageInfo packageInfo, string installerFileName, string? suggestedVersion)
    {
        _currentVersion = packageInfo.Version;
        _suggestedVersion = suggestedVersion;

        Text = "Gestion de version WAPT";
        Width = 760;
        Height = 560;
        MinimumSize = new Size(700, 520);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(243, 245, 249);
        Font = new Font("Segoe UI", 9.5F);

        InitializeComponent(packageInfo, installerFileName);
    }

    public PackageVersionSelection Selection { get; private set; } = new();

    private void InitializeComponent(PackageInfo packageInfo, string installerFileName)
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(20),
            BackColor = BackColor
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var header = CreateCard();
        var headerLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, AutoSize = true, BackColor = Color.White };
        headerLayout.Controls.Add(new Label
        {
            Text = "Choisissez comment mettre a jour la version WAPT",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
            ForeColor = HeadingColor,
            Margin = new Padding(0, 0, 0, 6)
        }, 0, 0);

        var alertText = PackageVersioning.IsSuggestedProductVersionDifferent(_currentVersion, _suggestedVersion)
            ? "Le nouvel installateur semble correspondre a une autre version du logiciel. Choisissez explicitement comment mettre a jour la version WAPT."
            : "Le remplacement de l'installeur est pret. Choisissez explicitement comment gerer la version WAPT avant application.";

        headerLayout.Controls.Add(new Label
        {
            Text = alertText,
            AutoSize = true,
            MaximumSize = new Size(660, 0),
            ForeColor = InfoColor
        }, 0, 1);
        header.Controls.Add(headerLayout);

        var content = CreateCard();
        var contentLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, AutoSize = true, BackColor = Color.White };
        contentLayout.Controls.Add(CreateReadOnlyField("Paquet", packageInfo.PackageName ?? "Non detecte"), 0, 0);
        contentLayout.Controls.Add(CreateReadOnlyField("Installeur actuel", packageInfo.ReferencedInstallerName ?? packageInfo.InstallerPath ?? "Non detecte"), 0, 1);
        contentLayout.Controls.Add(CreateReadOnlyField("Nouvel installeur", installerFileName), 0, 2);
        contentLayout.Controls.Add(CreateReadOnlyField("Version actuelle", _currentVersion ?? "Non detectee"), 0, 3);
        contentLayout.Controls.Add(CreateReadOnlyField("Version suggeree", _suggestedVersion ?? "Aucune suggestion fiable"), 0, 4);

        var strategyPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 1,
            AutoSize = true,
            BackColor = StrategyPanelColor,
            Padding = new Padding(16),
            Margin = new Padding(0, 12, 0, 12)
        };
        AddStrategyOption(strategyPanel, 0, _keepCurrentRadioButton, _keepCurrentDescriptionLabel, BuildKeepCurrentText(), PackageVersionStrategy.KeepCurrentVersion);
        AddStrategyOption(strategyPanel, 2, _incrementRevisionRadioButton, _incrementRevisionDescriptionLabel, BuildIncrementText(), PackageVersionStrategy.IncrementPackageRevision);
        AddStrategyOption(strategyPanel, 4, _setExplicitVersionRadioButton, _setExplicitVersionDescriptionLabel, "Saisissez explicitement la version produit ou produit-revision a utiliser pour synchroniser le paquet.", PackageVersionStrategy.SetExplicitVersion);
        contentLayout.Controls.Add(strategyPanel, 0, 5);

        var folderModePanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 1,
            AutoSize = true,
            BackColor = StrategyPanelColor,
            Padding = new Padding(16),
            Margin = new Padding(0, 0, 0, 12)
        };
        folderModePanel.Controls.Add(new Label
        {
            Text = "Choisissez ensuite comment traiter le dossier du paquet si la version cible conduit a un nouveau nom de dossier.",
            AutoSize = true,
            MaximumSize = new Size(640, 0),
            ForeColor = InfoColor,
            Margin = new Padding(0, 0, 0, 8)
        }, 0, 0);
        AddFolderModeOption(folderModePanel, 1, _updateCurrentFolderRadioButton, _updateCurrentFolderDescriptionLabel, "Le paquet courant reste le dossier de travail. Seules les metadonnees et l'installeur sont mis a jour sur place.", PackageFolderUpdateMode.UpdateCurrentFolder);
        AddFolderModeOption(folderModePanel, 3, _createVersionedFolderCloneRadioButton, _createVersionedFolderCloneDescriptionLabel, "Si un nouveau dossier versionne est necessaire, WaptStudio clone d'abord tout le paquet existant puis ne modifie que les elements cibles.", PackageFolderUpdateMode.CreateVersionedFolderClone);
        contentLayout.Controls.Add(folderModePanel, 0, 6);

        _explicitVersionTitleLabel.Text = "Version cible si vous choisissez 'Definir une nouvelle version'";
        _explicitVersionTitleLabel.AutoSize = true;
        _explicitVersionTitleLabel.ForeColor = InfoColor;
        _explicitVersionTitleLabel.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
        _explicitVersionTitleLabel.Margin = new Padding(0, 0, 0, 6);

        _explicitVersionHintLabel.Text = "Exemples: 11.0.0, 11.0.0-1, 24.09.00.0. Si la version actuelle utilise deja une revision, une saisie '11.0.0' sera normalisee en '11.0.0-1'.";
        _explicitVersionHintLabel.AutoSize = true;
        _explicitVersionHintLabel.MaximumSize = new Size(640, 0);
        _explicitVersionHintLabel.ForeColor = InfoColor;
        _explicitVersionHintLabel.Margin = new Padding(0, 8, 0, 0);

        _explicitVersionLayout.Controls.Add(_explicitVersionTitleLabel, 0, 0);
        _explicitVersionLayout.Controls.Add(_explicitVersionTextBox, 0, 1);
        _explicitVersionLayout.Controls.Add(_explicitVersionHintLabel, 0, 2);
        contentLayout.Controls.Add(_explicitVersionLayout, 0, 7);
        content.Controls.Add(contentLayout);

        _keepCurrentRadioButton.Checked = true;
        _createVersionedFolderCloneRadioButton.Checked = true;

        if (!string.IsNullOrWhiteSpace(_suggestedVersion))
        {
            _explicitVersionTextBox.Text = PackageVersioning.HasPackageRevision(_currentVersion)
                ? $"{_suggestedVersion}-1"
                : _suggestedVersion;
        }

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, AutoSize = true, BackColor = BackColor };
        var confirmButton = new Button { Text = "Continuer", AutoSize = true };
        confirmButton.Click += (_, _) => ConfirmSelection();
        var cancelButton = new Button { Text = "Annuler", AutoSize = true };
        cancelButton.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };
        buttons.Controls.Add(confirmButton);
        buttons.Controls.Add(cancelButton);

        root.Controls.Add(header, 0, 0);
        root.Controls.Add(content, 0, 1);
        root.Controls.Add(buttons, 0, 2);
        Controls.Add(root);

        UpdateInputState();
    }

    private void UpdateInputState()
    {
        var explicitVersionSelected = _setExplicitVersionRadioButton.Checked;

        _explicitVersionTextBox.Enabled = explicitVersionSelected;
        _explicitVersionTextBox.BackColor = explicitVersionSelected ? Color.White : ExplicitFieldInactiveColor;
        _explicitVersionTextBox.ForeColor = explicitVersionSelected ? HeadingColor : MutedColor;

        _explicitVersionLayout.BackColor = explicitVersionSelected ? StrategySelectedColor : ExplicitFieldInactiveColor;
        _explicitVersionTitleLabel.ForeColor = explicitVersionSelected ? HeadingColor : InfoColor;
        _explicitVersionHintLabel.ForeColor = explicitVersionSelected ? InfoColor : MutedColor;

        ApplyStrategyVisualState(_keepCurrentRadioButton, _keepCurrentDescriptionLabel);
        ApplyStrategyVisualState(_incrementRevisionRadioButton, _incrementRevisionDescriptionLabel);
        ApplyStrategyVisualState(_setExplicitVersionRadioButton, _setExplicitVersionDescriptionLabel);
        ApplyStrategyVisualState(_updateCurrentFolderRadioButton, _updateCurrentFolderDescriptionLabel);
        ApplyStrategyVisualState(_createVersionedFolderCloneRadioButton, _createVersionedFolderCloneDescriptionLabel);
    }

    private void ConfirmSelection()
    {
        if (_keepCurrentRadioButton.Checked)
        {
            if (string.IsNullOrWhiteSpace(_currentVersion))
            {
                MessageBox.Show(this, "Aucune version actuelle n'est disponible. Utilisez plutot 'Definir une nouvelle version'.", "Gestion de version WAPT", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Selection = new PackageVersionSelection { Strategy = PackageVersionStrategy.KeepCurrentVersion };
        }
        else if (_incrementRevisionRadioButton.Checked)
        {
            if (!PackageVersioning.TryIncrementPackageRevision(_currentVersion, out _, out var errorMessage))
            {
                MessageBox.Show(this, errorMessage, "Gestion de version WAPT", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Selection = new PackageVersionSelection { Strategy = PackageVersionStrategy.IncrementPackageRevision };
        }
        else
        {
            if (!PackageVersioning.TryNormalizeExplicitVersion(_explicitVersionTextBox.Text, _currentVersion, out var normalizedVersion, out var errorMessage))
            {
                MessageBox.Show(this, errorMessage, "Gestion de version WAPT", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Selection = new PackageVersionSelection
            {
                Strategy = PackageVersionStrategy.SetExplicitVersion,
                ExplicitVersion = normalizedVersion
            };
        }

        Selection = new PackageVersionSelection
        {
            Strategy = Selection.Strategy,
            ExplicitVersion = Selection.ExplicitVersion,
            FolderUpdateMode = _createVersionedFolderCloneRadioButton.Checked
                ? PackageFolderUpdateMode.CreateVersionedFolderClone
                : PackageFolderUpdateMode.UpdateCurrentFolder
        };

        DialogResult = DialogResult.OK;
        Close();
    }

    private string BuildKeepCurrentText()
        => string.IsNullOrWhiteSpace(_currentVersion)
            ? "Aucune version actuelle detectee pour ce paquet."
            : $"La version reste {(_currentVersion ?? "non detectee")}. A utiliser si vous remplacez seulement l'installeur sans vouloir changer la version WAPT.";

    private string BuildIncrementText()
    {
        if (PackageVersioning.TryIncrementPackageRevision(_currentVersion, out var nextVersion, out _))
        {
            return $"Conserve la partie produit et increment uniquement la revision du paquet. Exemple ici: {_currentVersion} -> {nextVersion}.";
        }

        return "Cette option demande une version actuelle lisible comme '11.0.0' ou '11.0.0-1'. Si ce n'est pas le cas, utilisez plutot 'Definir une nouvelle version'.";
    }

    private static Control CreateReadOnlyField(string label, string value)
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 1, AutoSize = true, BackColor = Color.White, Margin = new Padding(0, 0, 0, 10) };
        layout.Controls.Add(new Label { Text = label, AutoSize = true, ForeColor = InfoColor, Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold), Margin = new Padding(0, 0, 0, 4) }, 0, 0);
        layout.Controls.Add(new Label { Text = value, AutoSize = true, ForeColor = HeadingColor, Font = new Font("Segoe UI", 10F) }, 0, 1);
        return layout;
    }

    private void AddStrategyOption(TableLayoutPanel strategyPanel, int rowIndex, RadioButton radioButton, Label descriptionLabel, string description, PackageVersionStrategy strategy)
    {
        radioButton.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
        radioButton.UseVisualStyleBackColor = false;
        radioButton.BackColor = Color.Transparent;
        radioButton.ForeColor = HeadingColor;
        radioButton.Margin = new Padding(0, rowIndex == 0 ? 0 : 10, 0, 0);
        radioButton.CheckedChanged += (_, _) => UpdateInputState();

        descriptionLabel.Text = description;
        descriptionLabel.AutoSize = true;
        descriptionLabel.MaximumSize = new Size(620, 0);
        descriptionLabel.ForeColor = InfoColor;
        descriptionLabel.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
        descriptionLabel.Margin = new Padding(24, 4, 0, 0);
        descriptionLabel.Cursor = Cursors.Hand;
        descriptionLabel.Click += (_, _) => SelectStrategy(strategy);

        strategyPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        strategyPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        strategyPanel.Controls.Add(radioButton, 0, rowIndex);
        strategyPanel.Controls.Add(descriptionLabel, 0, rowIndex + 1);
    }

    private void AddFolderModeOption(TableLayoutPanel panel, int rowIndex, RadioButton radioButton, Label descriptionLabel, string description, PackageFolderUpdateMode mode)
    {
        radioButton.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
        radioButton.UseVisualStyleBackColor = false;
        radioButton.BackColor = Color.Transparent;
        radioButton.ForeColor = HeadingColor;
        radioButton.Margin = new Padding(0, rowIndex == 1 ? 0 : 10, 0, 0);
        radioButton.CheckedChanged += (_, _) => UpdateInputState();

        descriptionLabel.Text = description;
        descriptionLabel.AutoSize = true;
        descriptionLabel.MaximumSize = new Size(620, 0);
        descriptionLabel.ForeColor = InfoColor;
        descriptionLabel.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
        descriptionLabel.Margin = new Padding(24, 4, 0, 0);
        descriptionLabel.Cursor = Cursors.Hand;
        descriptionLabel.Click += (_, _) => SelectFolderMode(mode);

        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.Controls.Add(radioButton, 0, rowIndex);
        panel.Controls.Add(descriptionLabel, 0, rowIndex + 1);
    }

    private void SelectStrategy(PackageVersionStrategy strategy)
    {
        switch (strategy)
        {
            case PackageVersionStrategy.KeepCurrentVersion:
                _keepCurrentRadioButton.Checked = true;
                break;
            case PackageVersionStrategy.IncrementPackageRevision:
                _incrementRevisionRadioButton.Checked = true;
                break;
            case PackageVersionStrategy.SetExplicitVersion:
                _setExplicitVersionRadioButton.Checked = true;
                break;
        }

        UpdateInputState();
    }

    private void SelectFolderMode(PackageFolderUpdateMode mode)
    {
        switch (mode)
        {
            case PackageFolderUpdateMode.CreateVersionedFolderClone:
                _createVersionedFolderCloneRadioButton.Checked = true;
                break;
            default:
                _updateCurrentFolderRadioButton.Checked = true;
                break;
        }

        UpdateInputState();
    }

    private static void ApplyStrategyVisualState(RadioButton radioButton, Label descriptionLabel)
    {
        var selected = radioButton.Checked;
        radioButton.ForeColor = selected ? Color.FromArgb(17, 94, 89) : HeadingColor;
        descriptionLabel.ForeColor = selected ? Color.FromArgb(51, 65, 85) : InfoColor;
        descriptionLabel.BackColor = selected ? StrategySelectedColor : StrategyPanelColor;
    }

    private static Panel CreateCard()
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(20), Margin = new Padding(0, 0, 0, 12) };
        panel.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(214, 223, 235), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
        };
        return panel;
    }
}