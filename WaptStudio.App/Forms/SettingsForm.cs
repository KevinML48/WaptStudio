using System;
using System.IO;
using System.Windows.Forms;
using WaptStudio.Core.Models;

namespace WaptStudio.App.Forms;

public sealed class SettingsForm : Form
{
    private readonly TextBox _waptPathTextBox = new() { Dock = DockStyle.Fill };
    private readonly NumericUpDown _timeoutInput = new() { Dock = DockStyle.Fill, Minimum = 5, Maximum = 7200 };
    private readonly CheckBox _dryRunCheckBox = new() { Text = "Activer le dry-run", AutoSize = true };
    private readonly CheckBox _backupCheckBox = new() { Text = "Activer les sauvegardes", AutoSize = true };
    private readonly CheckBox _signingCheckBox = new() { Text = "Activer la signature", AutoSize = true };
    private readonly CheckBox _uploadCheckBox = new() { Text = "Activer l'upload", AutoSize = true };
    private readonly CheckBox _overwriteUploadCheckBox = new() { Text = "Autoriser l'ecrasement a l'upload", AutoSize = true };
    private readonly TextBox _availabilityArgsTextBox = new() { Dock = DockStyle.Fill };
    private readonly TextBox _validateArgsTextBox = new() { Dock = DockStyle.Fill };
    private readonly TextBox _buildArgsTextBox = new() { Dock = DockStyle.Fill };
    private readonly TextBox _signArgsTextBox = new() { Dock = DockStyle.Fill };
    private readonly TextBox _uploadArgsTextBox = new() { Dock = DockStyle.Fill };
    private readonly TextBox _logsDirectoryTextBox = new() { Dock = DockStyle.Fill };
    private readonly TextBox _backupsDirectoryTextBox = new() { Dock = DockStyle.Fill };
    private readonly TextBox _signingKeyTextBox = new() { Dock = DockStyle.Fill };
    private readonly TextBox _uploadRepositoryTextBox = new() { Dock = DockStyle.Fill };
    private readonly TextBox _defaultPackageFolderTextBox = new() { Dock = DockStyle.Fill };

    public SettingsForm(AppSettings settings)
    {
        Settings = Clone(settings);

        Text = "Parametres WaptStudio";
        Width = 980;
        Height = 700;
        StartPosition = FormStartPosition.CenterParent;

        InitializeComponent();
        Bind(Settings);
    }

    public AppSettings Settings { get; private set; }

    private void InitializeComponent()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            AutoScroll = true,
            Padding = new Padding(12)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var row = 0;
        AddBrowseRow(root, row++, "Chemin WAPT", _waptPathTextBox, BrowseWaptExecutable);
        AddSimpleRow(root, row++, "Timeout global (secondes)", _timeoutInput);
        AddSimpleRow(root, row++, "Dry-run", _dryRunCheckBox);
        AddSimpleRow(root, row++, "Backups", _backupCheckBox);
        AddSimpleRow(root, row++, "Signature", _signingCheckBox);
        AddSimpleRow(root, row++, "Upload", _uploadCheckBox);
        AddSimpleRow(root, row++, "Ecrasement upload", _overwriteUploadCheckBox);
        AddSimpleRow(root, row++, "Arguments test WAPT", _availabilityArgsTextBox);
        AddSimpleRow(root, row++, "Arguments validation", _validateArgsTextBox);
        AddSimpleRow(root, row++, "Arguments build", _buildArgsTextBox);
        AddSimpleRow(root, row++, "Arguments sign", _signArgsTextBox);
        AddSimpleRow(root, row++, "Arguments upload", _uploadArgsTextBox);
        AddBrowseRow(root, row++, "Dossier logs", _logsDirectoryTextBox, BrowseLogsDirectory);
        AddBrowseRow(root, row++, "Dossier backups", _backupsDirectoryTextBox, BrowseBackupsDirectory);
        AddBrowseRow(root, row++, "Cle de signature", _signingKeyTextBox, BrowseSigningKey);
        AddSimpleRow(root, row++, "Repository upload", _uploadRepositoryTextBox);
        AddBrowseRow(root, row++, "Dossier paquet par defaut", _defaultPackageFolderTextBox, BrowseDefaultPackageFolder);

        var helpLabel = new Label
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            Text = "Placeholders disponibles dans les arguments: {packageFolder}, {signingKeyPath}, {uploadRepositoryUrl}, {repositoryOption}, {overwriteFlag}"
        };
        root.Controls.Add(helpLabel, 0, row);
        root.SetColumnSpan(helpLabel, 3);

        var buttonsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(12)
        };

        var saveButton = new Button { Text = "Enregistrer", AutoSize = true };
        saveButton.Click += SaveSettings;
        var cancelButton = new Button { Text = "Annuler", AutoSize = true, DialogResult = DialogResult.Cancel };

        buttonsPanel.Controls.Add(saveButton);
        buttonsPanel.Controls.Add(cancelButton);

        Controls.Add(root);
        Controls.Add(buttonsPanel);
        AcceptButton = saveButton;
        CancelButton = cancelButton;
    }

    private void Bind(AppSettings settings)
    {
        _waptPathTextBox.Text = settings.WaptExecutablePath;
        _timeoutInput.Value = settings.CommandTimeoutSeconds;
        _dryRunCheckBox.Checked = settings.DryRunEnabled;
        _backupCheckBox.Checked = settings.CreateBackups;
        _signingCheckBox.Checked = settings.EnableSigning;
        _uploadCheckBox.Checked = settings.EnableUpload;
        _overwriteUploadCheckBox.Checked = settings.UploadOverwriteExisting;
        _availabilityArgsTextBox.Text = settings.AvailabilityArguments;
        _validateArgsTextBox.Text = settings.ValidatePackageArguments;
        _buildArgsTextBox.Text = settings.BuildPackageArguments;
        _signArgsTextBox.Text = settings.SignPackageArguments;
        _uploadArgsTextBox.Text = settings.UploadPackageArguments;
        _logsDirectoryTextBox.Text = settings.LogsDirectory ?? string.Empty;
        _backupsDirectoryTextBox.Text = settings.BackupsDirectory ?? string.Empty;
        _signingKeyTextBox.Text = settings.SigningKeyPath ?? string.Empty;
        _uploadRepositoryTextBox.Text = settings.UploadRepositoryUrl ?? string.Empty;
        _defaultPackageFolderTextBox.Text = settings.DefaultPackageFolder ?? string.Empty;
    }

    private void SaveSettings(object? sender, EventArgs e)
    {
        Settings = new AppSettings
        {
            WaptExecutablePath = _waptPathTextBox.Text.Trim(),
            CommandTimeoutSeconds = (int)_timeoutInput.Value,
            DryRunEnabled = _dryRunCheckBox.Checked,
            CreateBackups = _backupCheckBox.Checked,
            EnableSigning = _signingCheckBox.Checked,
            EnableUpload = _uploadCheckBox.Checked,
            UploadOverwriteExisting = _overwriteUploadCheckBox.Checked,
            AvailabilityArguments = _availabilityArgsTextBox.Text.Trim(),
            ValidatePackageArguments = _validateArgsTextBox.Text.Trim(),
            BuildPackageArguments = _buildArgsTextBox.Text.Trim(),
            SignPackageArguments = _signArgsTextBox.Text.Trim(),
            UploadPackageArguments = _uploadArgsTextBox.Text.Trim(),
            LogsDirectory = EmptyToNull(_logsDirectoryTextBox.Text),
            BackupsDirectory = EmptyToNull(_backupsDirectoryTextBox.Text),
            SigningKeyPath = EmptyToNull(_signingKeyTextBox.Text),
            UploadRepositoryUrl = EmptyToNull(_uploadRepositoryTextBox.Text),
            DefaultPackageFolder = EmptyToNull(_defaultPackageFolderTextBox.Text)
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

    private void BrowseBackupsDirectory(object? sender, EventArgs e) => BrowseFolder(_backupsDirectoryTextBox, "Selectionner le dossier de backups");

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

    private static string? EmptyToNull(string value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void AddSimpleRow(TableLayoutPanel layout, int rowIndex, string labelText, Control control)
    {
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(new Label { Text = labelText, AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(0, 6, 8, 0) }, 0, rowIndex);
        layout.Controls.Add(control, 1, rowIndex);
        layout.SetColumnSpan(control, 2);
    }

    private static void AddBrowseRow(TableLayoutPanel layout, int rowIndex, string labelText, Control control, EventHandler browseHandler)
    {
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(new Label { Text = labelText, AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(0, 6, 8, 0) }, 0, rowIndex);
        layout.Controls.Add(control, 1, rowIndex);

        var button = new Button { Text = "...", AutoSize = true };
        button.Click += browseHandler;
        layout.Controls.Add(button, 2, rowIndex);
    }

    private static AppSettings Clone(AppSettings source) => new()
    {
        WaptExecutablePath = source.WaptExecutablePath,
        CommandTimeoutSeconds = source.CommandTimeoutSeconds,
        AvailabilityArguments = source.AvailabilityArguments,
        ValidatePackageArguments = source.ValidatePackageArguments,
        BuildPackageArguments = source.BuildPackageArguments,
        SignPackageArguments = source.SignPackageArguments,
        UploadPackageArguments = source.UploadPackageArguments,
        DryRunEnabled = source.DryRunEnabled,
        CreateBackups = source.CreateBackups,
        LogsDirectory = source.LogsDirectory,
        BackupsDirectory = source.BackupsDirectory,
        EnableSigning = source.EnableSigning,
        EnableUpload = source.EnableUpload,
        UploadOverwriteExisting = source.UploadOverwriteExisting,
        SigningKeyPath = source.SigningKeyPath,
        UploadRepositoryUrl = source.UploadRepositoryUrl,
        DefaultPackageFolder = source.DefaultPackageFolder
    };
}
