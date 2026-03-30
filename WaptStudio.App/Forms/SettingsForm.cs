using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using WaptStudio.Core.Configuration;
using WaptStudio.Core.Models;

namespace WaptStudio.App.Forms;

public sealed class SettingsForm : Form
{
    private static readonly Color SurfaceColor = Color.FromArgb(243, 245, 249);
    private static readonly Color CardColor = Color.White;
    private static readonly Color AccentColor = Color.FromArgb(32, 76, 178);
    private static readonly Color BorderColor = Color.FromArgb(220, 227, 238);
    private static readonly Color HeadingColor = Color.FromArgb(16, 24, 40);
    private static readonly Color InfoColor = Color.FromArgb(83, 96, 120);

    private readonly TextBox _catalogRootFolderTextBox = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
    private readonly CheckBox _catalogRecursiveCheckBox = new() { Text = "Inclure tous les sous-dossiers", AutoSize = true };
    private readonly NumericUpDown _catalogDepthInput = new() { Dock = DockStyle.Left, Minimum = 0, Maximum = 20, Width = 140 };
    private readonly TextBox _waptPathTextBox = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
    private readonly NumericUpDown _timeoutInput = new() { Dock = DockStyle.Left, Minimum = 5, Maximum = 7200, Width = 140 };
    private readonly CheckBox _dryRunCheckBox = new() { Text = "Simuler les actions", AutoSize = true };
    private readonly CheckBox _backupCheckBox = new() { Text = "Creer une sauvegarde avant operation sensible", AutoSize = true };
    private readonly CheckBox _signingCheckBox = new() { Text = "Activer la signature automatique", AutoSize = true };
    private readonly CheckBox _uploadCheckBox = new() { Text = "Autoriser l'upload du paquet", AutoSize = true };
    private readonly CheckBox _preferConsolePublishCheckBox = new() { Text = "Preferer WAPT Console pour publier", AutoSize = true };
    private readonly CheckBox _overwriteUploadCheckBox = new() { Text = "Autoriser le remplacement d'un paquet deja present", AutoSize = true };
    private readonly TextBox _availabilityArgsTextBox = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
    private readonly TextBox _validateArgsTextBox = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
    private readonly TextBox _buildArgsTextBox = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
    private readonly TextBox _signArgsTextBox = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
    private readonly TextBox _uploadArgsTextBox = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
    private readonly TextBox _auditArgsTextBox = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
    private readonly TextBox _uninstallArgsTextBox = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
    private readonly TextBox _logsDirectoryTextBox = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
    private readonly TextBox _cacheDirectoryTextBox = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
    private readonly TextBox _backupsDirectoryTextBox = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
    private readonly TextBox _signingKeyTextBox = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
    private readonly TextBox _uploadRepositoryTextBox = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
    private readonly TextBox _defaultPackageFolderTextBox = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };

    public SettingsForm(AppSettings settings)
    {
        Settings = Clone(settings);

        Text = "Parametres WaptStudio";
        Width = 1180;
        Height = 920;
        MinimumSize = new Size(1040, 820);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = SurfaceColor;
        Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);

        InitializeComponent();
        Bind(Settings);
    }

    public AppSettings Settings { get; private set; }

    private void InitializeComponent()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = SurfaceColor,
            Padding = new Padding(18)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var scrollPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = SurfaceColor,
            Padding = new Padding(0, 0, 8, 0)
        };

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            BackColor = SurfaceColor
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        content.Controls.Add(CreateIntroCard(), 0, 0);
        content.Controls.Add(BuildCatalogSection(), 0, 1);
        content.Controls.Add(BuildExecutionSection(), 0, 2);
        content.Controls.Add(BuildPublicationSection(), 0, 3);
        content.Controls.Add(BuildAdvancedCommandsSection(), 0, 4);
        content.Controls.Add(BuildDirectoriesSection(), 0, 5);

        scrollPanel.Controls.Add(content);
        root.Controls.Add(scrollPanel, 0, 0);
        root.Controls.Add(BuildFooter(), 0, 1);

        Controls.Add(root);
    }

    private Control CreateIntroCard()
    {
        var card = CreateSectionCard("Parametres WaptStudio", "Reglez le catalogue, WAPT, la publication et les dossiers sans avoir a deviner a quoi sert chaque champ.");
        card.Controls.Add(new Label
        {
            AutoSize = true,
            MaximumSize = new Size(980, 0),
            ForeColor = InfoColor,
            Text = $"Le catalogue principal charge automatiquement tout {Settings.CatalogRootFolder ?? @"C:\waptdev"}. Les reglages de scan ci-dessous restent disponibles pour les cas avances. Placeholders disponibles dans les commandes: {{packageFolder}}, {{waptFilePath}}, {{packageId}}, {{signingKeyPath}}, {{uploadRepositoryUrl}}, {{repositoryOption}}, {{overwriteFlag}}. Donnees locales: {AppPaths.BaseDirectory}."
        });
        return card;
    }

    private Control BuildCatalogSection()
    {
        var card = CreateSectionCard("Catalogue", "Le point de depart de WaptStudio doit rester simple: voir tous les paquets presents dans le dossier racine.");
        var layout = CreateSectionLayout();

        AddBrowseField(layout, "Racine catalogue paquets", "Dossier principal dans lequel WaptStudio recherche vos paquets.", _catalogRootFolderTextBox, BrowseCatalogRootFolder);
        AddToggleField(layout, "Scan recursif", "Inclut les sous-dossiers dans la recherche. Le chargement principal du catalogue reste de toute facon complet pour afficher tout le contenu du dossier choisi.", _catalogRecursiveCheckBox);
        AddValueField(layout, "Profondeur semi-recursive", "Nombre maximal de niveaux de sous-dossiers explores. Ce reglage ne sert qu'aux usages avances si vous limitez volontairement certains parcours.", _catalogDepthInput);
        AddBrowseField(layout, "Dossier paquet par defaut", "Dossier propose automatiquement quand vous ouvrez ou preparez un paquet hors catalogue principal.", _defaultPackageFolderTextBox, BrowseDefaultPackageFolder);

        card.Controls.Add(layout);
        return card;
    }

    private Control BuildExecutionSection()
    {
        var card = CreateSectionCard("Execution WAPT", "Tout ce qui permet a WaptStudio d'executer ses commandes de lecture, build et signature.");
        var layout = CreateSectionLayout();

        AddBrowseField(layout, "Chemin WAPT", "Chemin vers wapt-get.exe utilise pour les commandes WAPT.", _waptPathTextBox, BrowseWaptExecutable);
        AddValueField(layout, "Timeout global", "Duree maximale d'execution d'une commande avant arret.", _timeoutInput);
        AddToggleField(layout, "Dry-run", "Permet de simuler les actions sans modifier reellement les fichiers.", _dryRunCheckBox);
        AddToggleField(layout, "Activer les sauvegardes", "Cree une sauvegarde avant les operations sensibles.", _backupCheckBox);
        AddToggleField(layout, "Activer la signature", "Signe automatiquement les paquets si la configuration le permet.", _signingCheckBox);
        AddBrowseField(layout, "Cle de signature", "Certificat utilise pour signer les paquets lorsque la signature est activee.", _signingKeyTextBox, BrowseSigningKey);

        card.Controls.Add(layout);
        return card;
    }

    private Control BuildPublicationSection()
    {
        var card = CreateSectionCard("Publication", "Reglages utiles pour maitriser la mise en ligne du paquet sans afficher d'options inutiles dans l'ecran principal.");
        var layout = CreateSectionLayout();

        AddToggleField(layout, "Activer l'upload", "Autorise l'envoi du paquet vers le depot.", _uploadCheckBox);
        AddToggleField(layout, "Preferer la publication via WAPT Console", "Utilise en priorite WAPT Console pour publier.", _preferConsolePublishCheckBox);
        AddToggleField(layout, "Autoriser l'ecrasement a l'upload", "Permet de remplacer un paquet deja present sur le depot.", _overwriteUploadCheckBox);
        AddValueField(layout, "Repository upload", "Adresse du depot utilisee pour l'upload direct lorsque ce mode est active.", _uploadRepositoryTextBox);

        card.Controls.Add(layout);
        return card;
    }

    private Control BuildAdvancedCommandsSection()
    {
        var card = CreateSectionCard("Commandes avancees", "Reglages techniques reserves aux cas ou vous devez ajuster les commandes executees par WaptStudio.");
        var layout = CreateSectionLayout();

        AddValueField(layout, "Arguments test WAPT", "Commande de verification simple utilisee pour confirmer que WAPT est accessible.", _availabilityArgsTextBox);
        AddValueField(layout, "Arguments validation", "Commande WAPT utilisee pour relire un paquet et controler sa coherence.", _validateArgsTextBox);
        AddValueField(layout, "Arguments build", "Commande utilisee pour construire le fichier .wapt.", _buildArgsTextBox);
        AddValueField(layout, "Arguments sign", "Commande utilisee pour signer un paquet deja construit.", _signArgsTextBox);
        AddValueField(layout, "Arguments upload", "Commande utilisee pour envoyer un fichier .wapt vers le depot.", _uploadArgsTextBox);
        AddValueField(layout, "Arguments audit", "Commande d'audit poste conservee uniquement pour les usages avances hors ecran principal.", _auditArgsTextBox);
        AddValueField(layout, "Arguments uninstall", "Commande de desinstallation poste conservee uniquement pour les usages avances hors ecran principal.", _uninstallArgsTextBox);

        card.Controls.Add(layout);
        return card;
    }

    private Control BuildDirectoriesSection()
    {
        var card = CreateSectionCard("Dossiers de travail", "Emplacements utilises pour les journaux, le cache et les sauvegardes.");
        var layout = CreateSectionLayout();

        AddBrowseField(layout, "Dossier logs", "Emplacement des journaux techniques de WaptStudio.", _logsDirectoryTextBox, BrowseLogsDirectory);
        AddBrowseField(layout, "Dossier cache", "Emplacement des fichiers temporaires utilises pendant les analyses et les operations.", _cacheDirectoryTextBox, BrowseCacheDirectory);
        AddBrowseField(layout, "Dossier backups", "Emplacement des sauvegardes creees avant les operations sensibles.", _backupsDirectoryTextBox, BrowseBackupsDirectory);

        card.Controls.Add(layout);
        return card;
    }

    private Control BuildFooter()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 14, 0, 0),
            BackColor = SurfaceColor,
            AutoSize = true
        };

        var saveButton = CreatePrimaryButton("Enregistrer");
        saveButton.Click += SaveSettings;

        var cancelButton = CreateSecondaryButton("Annuler");
        cancelButton.DialogResult = DialogResult.Cancel;

        panel.Controls.Add(saveButton);
        panel.Controls.Add(cancelButton);

        AcceptButton = saveButton;
        CancelButton = cancelButton;
        return panel;
    }

    private void Bind(AppSettings settings)
    {
        _catalogRootFolderTextBox.Text = settings.CatalogRootFolder ?? @"C:\waptdev";
        _catalogRecursiveCheckBox.Checked = settings.CatalogScanRecursively;
        _catalogDepthInput.Value = settings.CatalogSemiRecursiveDepth;
        _waptPathTextBox.Text = settings.WaptExecutablePath;
        _timeoutInput.Value = settings.CommandTimeoutSeconds;
        _dryRunCheckBox.Checked = settings.DryRunEnabled;
        _backupCheckBox.Checked = settings.CreateBackups;
        _signingCheckBox.Checked = settings.EnableSigning;
        _uploadCheckBox.Checked = settings.EnableUpload;
        _preferConsolePublishCheckBox.Checked = settings.PreferWaptConsolePublish;
        _overwriteUploadCheckBox.Checked = settings.UploadOverwriteExisting;
        _availabilityArgsTextBox.Text = settings.AvailabilityArguments;
        _validateArgsTextBox.Text = settings.ValidatePackageArguments;
        _buildArgsTextBox.Text = settings.BuildPackageArguments;
        _signArgsTextBox.Text = settings.SignPackageArguments;
        _uploadArgsTextBox.Text = settings.UploadPackageArguments;
        _auditArgsTextBox.Text = settings.AuditPackageArguments;
        _uninstallArgsTextBox.Text = settings.UninstallPackageArguments;
        _logsDirectoryTextBox.Text = settings.LogsDirectory ?? string.Empty;
        _cacheDirectoryTextBox.Text = settings.CacheDirectory ?? string.Empty;
        _backupsDirectoryTextBox.Text = settings.BackupsDirectory ?? string.Empty;
        _signingKeyTextBox.Text = settings.SigningKeyPath ?? string.Empty;
        _uploadRepositoryTextBox.Text = settings.UploadRepositoryUrl ?? string.Empty;
        _defaultPackageFolderTextBox.Text = settings.DefaultPackageFolder ?? string.Empty;
    }

    private void SaveSettings(object? sender, EventArgs e)
    {
        Settings = new AppSettings
        {
            WaptExecutablePath = string.IsNullOrWhiteSpace(_waptPathTextBox.Text) ? CommandExecutionResult.DefaultExecutableName : _waptPathTextBox.Text.Trim(),
            CatalogRootFolder = EmptyToNull(_catalogRootFolderTextBox.Text) ?? @"C:\waptdev",
            CatalogScanRecursively = _catalogRecursiveCheckBox.Checked,
            CatalogSemiRecursiveDepth = (int)_catalogDepthInput.Value,
            CommandTimeoutSeconds = (int)_timeoutInput.Value,
            DryRunEnabled = _dryRunCheckBox.Checked,
            CreateBackups = _backupCheckBox.Checked,
            EnableSigning = _signingCheckBox.Checked,
            EnableUpload = _uploadCheckBox.Checked,
            PreferWaptConsolePublish = _preferConsolePublishCheckBox.Checked,
            UploadOverwriteExisting = _overwriteUploadCheckBox.Checked,
            AvailabilityArguments = _availabilityArgsTextBox.Text.Trim(),
            ValidatePackageArguments = _validateArgsTextBox.Text.Trim(),
            BuildPackageArguments = _buildArgsTextBox.Text.Trim(),
            SignPackageArguments = _signArgsTextBox.Text.Trim(),
            UploadPackageArguments = _uploadArgsTextBox.Text.Trim(),
            AuditPackageArguments = _auditArgsTextBox.Text.Trim(),
            UninstallPackageArguments = _uninstallArgsTextBox.Text.Trim(),
            LogsDirectory = EmptyToNull(_logsDirectoryTextBox.Text),
            CacheDirectory = EmptyToNull(_cacheDirectoryTextBox.Text),
            BackupsDirectory = EmptyToNull(_backupsDirectoryTextBox.Text),
            SigningKeyPath = EmptyToNull(_signingKeyTextBox.Text),
            UploadRepositoryUrl = EmptyToNull(_uploadRepositoryTextBox.Text),
            DefaultPackageFolder = EmptyToNull(_defaultPackageFolderTextBox.Text),
            HasCompletedFirstRunExperience = Settings.HasCompletedFirstRunExperience
        };

        DialogResult = DialogResult.OK;
        Close();
    }

    private void BrowseWaptExecutable(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Executables (*.exe)|*.exe|Tous les fichiers (*.*)|*.*",
            Title = "Selectionner l'executable WAPT"
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _waptPathTextBox.Text = dialog.FileName;
        }
    }

    private void BrowseLogsDirectory(object? sender, EventArgs e) => BrowseFolder(_logsDirectoryTextBox, "Selectionner le dossier de logs");

    private void BrowseCacheDirectory(object? sender, EventArgs e) => BrowseFolder(_cacheDirectoryTextBox, "Selectionner le dossier de cache");

    private void BrowseBackupsDirectory(object? sender, EventArgs e) => BrowseFolder(_backupsDirectoryTextBox, "Selectionner le dossier de sauvegardes");

    private void BrowseCatalogRootFolder(object? sender, EventArgs e) => BrowseFolder(_catalogRootFolderTextBox, "Selectionner le dossier racine des paquets");

    private void BrowseDefaultPackageFolder(object? sender, EventArgs e) => BrowseFolder(_defaultPackageFolderTextBox, "Selectionner le dossier paquet par defaut");

    private void BrowseSigningKey(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Certificats WAPT (*.p12;*.pem)|*.p12;*.pem|Tous les fichiers (*.*)|*.*",
            Title = "Selectionner le certificat de signature WAPT"
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _signingKeyTextBox.Text = dialog.FileName;
        }
    }

    private void BrowseFolder(TextBox target, string title)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = title,
            InitialDirectory = Directory.Exists(target.Text) ? target.Text : Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            target.Text = dialog.SelectedPath;
        }
    }

    private static TableLayoutPanel CreateSectionLayout()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            BackColor = CardColor,
            Margin = new Padding(0)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        return layout;
    }

    private Panel CreateSectionCard(string title, string subtitle)
    {
        var card = new Panel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = CardColor,
            Padding = new Padding(24),
            Margin = new Padding(0, 0, 0, 14)
        };

        card.Paint += (_, e) =>
        {
            using var pen = new Pen(BorderColor, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };

        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            BackColor = CardColor,
            Margin = new Padding(0, 0, 0, 14)
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.Controls.Add(new Label { Text = title, AutoSize = true, Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold), ForeColor = HeadingColor, Margin = new Padding(0, 0, 0, 4) }, 0, 0);
        header.Controls.Add(new Label { Text = subtitle, AutoSize = true, MaximumSize = new Size(980, 0), ForeColor = InfoColor, Margin = new Padding(0) }, 0, 1);

        card.Controls.Add(header);
        return card;
    }

    private void AddValueField(TableLayoutPanel layout, string label, string help, Control control)
    {
        layout.Controls.Add(CreateFieldBlock(label, help, control, null));
    }

    private void AddToggleField(TableLayoutPanel layout, string label, string help, CheckBox checkBox)
    {
        layout.Controls.Add(CreateFieldBlock(label, help, checkBox, null));
    }

    private void AddBrowseField(TableLayoutPanel layout, string label, string help, Control control, EventHandler browseHandler)
    {
        layout.Controls.Add(CreateFieldBlock(label, help, control, browseHandler));
    }

    private Control CreateFieldBlock(string label, string help, Control control, EventHandler? browseHandler)
    {
        var block = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            BackColor = CardColor,
            Margin = new Padding(0, 0, 0, 18)
        };
        block.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        block.Controls.Add(new Label
        {
            Text = label,
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
            ForeColor = HeadingColor,
            Margin = new Padding(0, 0, 0, 4)
        }, 0, 0);

        block.Controls.Add(new Label
        {
            Text = help,
            AutoSize = true,
            MaximumSize = new Size(980, 0),
            ForeColor = InfoColor,
            Margin = new Padding(0, 0, 0, 8)
        }, 0, 1);

        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = browseHandler is null ? 1 : 2,
            BackColor = CardColor,
            Margin = new Padding(0)
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        if (browseHandler is not null)
        {
            row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        }

        if (control is TextBox textBox)
        {
            textBox.MinimumSize = new Size(0, 34);
            textBox.Margin = new Padding(0);
        }
        else if (control is NumericUpDown numericUpDown)
        {
            numericUpDown.MinimumSize = new Size(140, 34);
            numericUpDown.Margin = new Padding(0);
        }
        else if (control is CheckBox checkBox)
        {
            checkBox.Margin = new Padding(0, 4, 0, 0);
            checkBox.ForeColor = HeadingColor;
        }

        row.Controls.Add(control, 0, 0);

        if (browseHandler is not null)
        {
            var button = CreateSecondaryButton("Parcourir");
            button.Margin = new Padding(10, 0, 0, 0);
            button.Click += browseHandler;
            row.Controls.Add(button, 1, 0);
        }

        block.Controls.Add(row, 0, 2);
        return block;
    }

    private static Button CreatePrimaryButton(string text)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlatStyle = FlatStyle.Flat,
            BackColor = AccentColor,
            ForeColor = Color.White,
            Padding = new Padding(18, 10, 18, 10),
            MinimumSize = new Size(150, 46),
            Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
            UseVisualStyleBackColor = false
        };
        button.FlatAppearance.BorderSize = 0;
        return button;
    }

    private static Button CreateSecondaryButton(string text)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlatStyle = FlatStyle.Flat,
            BackColor = CardColor,
            ForeColor = HeadingColor,
            Padding = new Padding(16, 10, 16, 10),
            MinimumSize = new Size(140, 44),
            Font = new Font("Segoe UI", 9.75F),
            UseVisualStyleBackColor = false
        };
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = BorderColor;
        return button;
    }

    private static string? EmptyToNull(string value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static AppSettings Clone(AppSettings source) => new()
    {
        CatalogRootFolder = source.CatalogRootFolder,
        CatalogScanRecursively = source.CatalogScanRecursively,
        CatalogSemiRecursiveDepth = source.CatalogSemiRecursiveDepth,
        WaptExecutablePath = source.WaptExecutablePath,
        CommandTimeoutSeconds = source.CommandTimeoutSeconds,
        AvailabilityArguments = source.AvailabilityArguments,
        ValidatePackageArguments = source.ValidatePackageArguments,
        BuildPackageArguments = source.BuildPackageArguments,
        SignPackageArguments = source.SignPackageArguments,
        UploadPackageArguments = source.UploadPackageArguments,
        AuditPackageArguments = source.AuditPackageArguments,
        UninstallPackageArguments = source.UninstallPackageArguments,
        DryRunEnabled = source.DryRunEnabled,
        CreateBackups = source.CreateBackups,
        LogsDirectory = source.LogsDirectory,
        CacheDirectory = source.CacheDirectory,
        BackupsDirectory = source.BackupsDirectory,
        EnableSigning = source.EnableSigning,
        EnableUpload = source.EnableUpload,
        PreferWaptConsolePublish = source.PreferWaptConsolePublish,
        UploadOverwriteExisting = source.UploadOverwriteExisting,
        SigningKeyPath = source.SigningKeyPath,
        UploadRepositoryUrl = source.UploadRepositoryUrl,
        DefaultPackageFolder = source.DefaultPackageFolder,
        HasCompletedFirstRunExperience = source.HasCompletedFirstRunExperience
    };
}
