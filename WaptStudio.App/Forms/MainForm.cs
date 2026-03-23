using System;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WaptStudio.App.Bootstrap;
using WaptStudio.Core.Configuration;
using WaptStudio.Core.Models;
using WaptStudio.Core.Utilities;

namespace WaptStudio.App.Forms;

public sealed class MainForm : Form
{
    private readonly AppRuntime _runtime;
    private readonly TextBox _packageFolderTextBox = new() { Dock = DockStyle.Fill };
    private readonly RichTextBox _packageInfoTextBox = new() { Dock = DockStyle.Fill, ReadOnly = true };
    private readonly RichTextBox _logsTextBox = new() { Dock = DockStyle.Fill, ReadOnly = true };
    private readonly DataGridView _historyGrid = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AutoGenerateColumns = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false };
    private readonly Label _packageNameValueLabel = new() { AutoSize = true, Text = "-" };
    private readonly Label _packageVersionValueLabel = new() { AutoSize = true, Text = "-" };
    private readonly Label _installerValueLabel = new() { AutoSize = true, Text = "-" };
    private readonly Label _waptStatusValueLabel = new() { AutoSize = true, Text = "Inconnu" };
    private readonly Label _actionResultValueLabel = new() { AutoSize = true, Text = "Aucune action" };
    private readonly Button _analyzeButton = new() { Text = "Analyser", AutoSize = true };
    private readonly Button _replaceButton = new() { Text = "Remplacer MSI", AutoSize = true };
    private readonly Button _validateButton = new() { Text = "Valider", AutoSize = true };
    private readonly Button _buildButton = new() { Text = "Construire", AutoSize = true };
    private readonly Button _buildAndUploadButton = new() { Text = "Build + Upload", AutoSize = true };
    private readonly Button _signButton = new() { Text = "Signer", AutoSize = true };
    private readonly Button _uploadButton = new() { Text = "Uploader", AutoSize = true };
    private readonly Button _testWaptButton = new() { Text = "Tester WAPT", AutoSize = true };
    private readonly Button _diagnosticEnvironmentButton = new() { Text = "Diagnostic environnement", AutoSize = true };
    private readonly Button _openPackageFolderButton = new() { Text = "Ouvrir dossier paquet", AutoSize = true };
    private readonly Button _openPowerShellHereButton = new() { Text = "Ouvrir PowerShell ici", AutoSize = true };
    private readonly Button _manualBuildButton = new() { Text = "Renseigner action manuelle", AutoSize = true };
    private readonly Button _openLogsFolderButton = new() { Text = "Ouvrir dossier logs", AutoSize = true };
    private readonly Button _saveReportButton = new() { Text = "Sauvegarder rapport", AutoSize = true };
    private readonly Button _historyDetailsButton = new() { Text = "Voir detail historique", AutoSize = true };
    private readonly Button _settingsButton = new() { Text = "Parametres", AutoSize = true };

    private PackageInfo? _currentPackage;
    private AppSettings _settings = new();
    private string _lastActionResult = "Aucune action";
    private IReadOnlyList<HistoryEntry> _historyEntries = Array.Empty<HistoryEntry>();
    private string? _lastPreparedManualActionType;
    private string? _lastPreparedManualCommand;
    private string? _lastPreparedManualPackageFolder;
    private string? _lastKnownWaptFilePath;

    public MainForm(AppRuntime runtime)
    {
        _runtime = runtime;

        Text = "WaptStudio";
        Width = 1520;
        Height = 960;
        StartPosition = FormStartPosition.CenterScreen;

        InitializeComponent();
        WireEvents();
    }

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);
        _settings = await _runtime.LoadSettingsAsync().ConfigureAwait(true);

        if (!string.IsNullOrWhiteSpace(_settings.DefaultPackageFolder) && Directory.Exists(_settings.DefaultPackageFolder))
        {
            _packageFolderTextBox.Text = _settings.DefaultPackageFolder;
        }

        await RefreshWaptStatusAsync().ConfigureAwait(true);
        await LoadHistoryAsync().ConfigureAwait(true);
        await ShowStartupDiagnosticsAsync().ConfigureAwait(true);
        AppendLog("WaptStudio est pret pour un premier test local.");
    }

    private void InitializeComponent()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(12)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var folderPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 4,
            AutoSize = true
        };
        folderPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        folderPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        folderPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        folderPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        folderPanel.Controls.Add(new Label { Text = "Dossier paquet", AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(0, 8, 8, 0) }, 0, 0);
        folderPanel.Controls.Add(_packageFolderTextBox, 1, 0);

        var browseButton = new Button { Text = "Parcourir...", AutoSize = true };
        browseButton.Click += BrowsePackageFolder;
        folderPanel.Controls.Add(browseButton, 2, 0);
        folderPanel.Controls.Add(_settingsButton, 3, 0);

        var primaryStatusPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 6,
            AutoSize = true,
            Padding = new Padding(0, 8, 0, 0)
        };
        primaryStatusPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        primaryStatusPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
        primaryStatusPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        primaryStatusPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
        primaryStatusPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        primaryStatusPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        AddStatusPair(primaryStatusPanel, 0, "Paquet", _packageNameValueLabel);
        AddStatusPair(primaryStatusPanel, 2, "Version", _packageVersionValueLabel);
        AddStatusPair(primaryStatusPanel, 4, "Installeur", _installerValueLabel);

        var secondaryStatusPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 4,
            AutoSize = true,
            Padding = new Padding(0, 4, 0, 8)
        };
        secondaryStatusPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        secondaryStatusPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        secondaryStatusPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        secondaryStatusPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 75));
        AddStatusPair(secondaryStatusPanel, 0, "WAPT", _waptStatusValueLabel);
        AddStatusPair(secondaryStatusPanel, 2, "Dernier resultat", _actionResultValueLabel);

        var actionsFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            WrapContents = true,
            Padding = new Padding(0, 6, 0, 10)
        };
        actionsFlow.Controls.AddRange(new Control[]
        {
            _analyzeButton,
            _replaceButton,
            _validateButton,
            _buildButton,
            _buildAndUploadButton,
            _signButton,
            _uploadButton,
            _testWaptButton,
            _diagnosticEnvironmentButton,
            _openPackageFolderButton,
            _openPowerShellHereButton,
            _manualBuildButton,
            _openLogsFolderButton,
            _saveReportButton,
            _historyDetailsButton
        });

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 670
        };

        var leftLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
        leftLayout.Controls.Add(CreateGroupBox("Paquet courant", _packageInfoTextBox), 0, 0);
        leftLayout.Controls.Add(CreateGroupBox("Journal en direct", _logsTextBox), 0, 1);

        ConfigureHistoryGrid();
        split.Panel1.Controls.Add(leftLayout);
        split.Panel2.Controls.Add(CreateGroupBox("Historique local", _historyGrid));

        root.Controls.Add(folderPanel, 0, 0);
        root.Controls.Add(primaryStatusPanel, 0, 1);
        root.Controls.Add(secondaryStatusPanel, 0, 2);
        root.Controls.Add(actionsFlow, 0, 3);
        root.Controls.Add(split, 0, 4);

        Controls.Add(root);
    }

    private void WireEvents()
    {
        _analyzeButton.Click += async (_, _) => await AnalyzePackageAsync().ConfigureAwait(true);
        _replaceButton.Click += async (_, _) => await ReplaceInstallerAsync().ConfigureAwait(true);
        _validateButton.Click += async (_, _) => await ValidatePackageAsync().ConfigureAwait(true);
        _buildButton.Click += async (_, _) => await ExecuteBuildAsync().ConfigureAwait(true);
        _buildAndUploadButton.Click += async (_, _) => await ExecuteBuildAndUploadAsync().ConfigureAwait(true);
        _signButton.Click += async (_, _) => await ExecuteSignAsync().ConfigureAwait(true);
        _uploadButton.Click += async (_, _) => await ExecuteUploadAsync().ConfigureAwait(true);
        _testWaptButton.Click += async (_, _) => await TestWaptAsync().ConfigureAwait(true);
        _diagnosticEnvironmentButton.Click += async (_, _) => await ShowEnvironmentDiagnosticsAsync().ConfigureAwait(true);
        _openPackageFolderButton.Click += (_, _) => OpenPackageFolder();
        _openPowerShellHereButton.Click += (_, _) => OpenPowerShellHere();
        _manualBuildButton.Click += async (_, _) => await ShowManualWorkflowAsync().ConfigureAwait(true);
        _openLogsFolderButton.Click += (_, _) => OpenFolder(AppPaths.ResolveLogsDirectory(_settings));
        _saveReportButton.Click += async (_, _) => await SaveReportAsync().ConfigureAwait(true);
        _historyDetailsButton.Click += async (_, _) => await ShowSelectedHistoryDetailsAsync().ConfigureAwait(true);
        _historyGrid.CellDoubleClick += async (_, _) => await ShowSelectedHistoryDetailsAsync().ConfigureAwait(true);
        _settingsButton.Click += OpenSettings;
    }

    private void ConfigureHistoryGrid()
    {
        _historyGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(HistoryEntry.Id), HeaderText = "Id", Width = 60 });
        _historyGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(HistoryEntry.Timestamp), HeaderText = "Date", Width = 160 });
        _historyGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(HistoryEntry.ActionType), HeaderText = "Action", Width = 110 });
        _historyGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(HistoryEntry.PackageName), HeaderText = "Paquet", Width = 140 });
        _historyGrid.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = nameof(HistoryEntry.Success), HeaderText = "OK", Width = 45 });
        _historyGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(HistoryEntry.DurationMilliseconds), HeaderText = "Duree (ms)", Width = 90 });
        _historyGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(HistoryEntry.Message), HeaderText = "Message", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
    }

    private async void BrowsePackageFolder(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Selectionner un dossier de paquet WAPT",
            InitialDirectory = Directory.Exists(_packageFolderTextBox.Text) ? _packageFolderTextBox.Text : Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _packageFolderTextBox.Text = dialog.SelectedPath;
        _settings.DefaultPackageFolder = dialog.SelectedPath;
        await _runtime.SettingsService.SaveAsync(_settings).ConfigureAwait(true);
    }

    private async void OpenSettings(object? sender, EventArgs e)
    {
        var settings = await _runtime.SettingsService.LoadAsync().ConfigureAwait(true);
        using var settingsForm = new SettingsForm(settings);
        if (settingsForm.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _settings = settingsForm.Settings;
        await _runtime.SettingsService.SaveAsync(_settings).ConfigureAwait(true);
        await RefreshWaptStatusAsync().ConfigureAwait(true);
        AppendLog("Configuration locale enregistree.");

        if (_currentPackage is not null)
        {
            DisplayPackageInfo(_currentPackage);
        }
    }

    private async Task AnalyzePackageAsync()
    {
        try
        {
            var packageFolder = GetPackageFolder();
            AppendLog($"Analyse du paquet: {packageFolder}");
            _currentPackage = await _runtime.PackageInspectorService.AnalyzePackageAsync(packageFolder).ConfigureAwait(true);
            DisplayPackageInfo(_currentPackage);
            SetActionResult("Analyse terminee.");
            await RegisterHistoryAsync("Analyze", true, packageFolder, _currentPackage.PackageName, "Analyse du paquet terminee.", null, null, _currentPackage.Version).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            await HandleUiOperationErrorAsync("Analyse impossible.", ex).ConfigureAwait(true);
        }
    }

    private async Task ReplaceInstallerAsync()
    {
        try
        {
            var packageFolder = GetPackageFolder();
            _currentPackage ??= await _runtime.PackageInspectorService.AnalyzePackageAsync(packageFolder).ConfigureAwait(true);
            var previousVersion = _currentPackage.Version;

            using var dialog = new OpenFileDialog
            {
                Filter = "Installers (*.msi;*.exe)|*.msi;*.exe",
                Title = "Selectionner un nouveau MSI ou EXE"
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            var synchronizationPlan = await _runtime.PackageUpdateService.PreviewReplacementAsync(_currentPackage, dialog.FileName).ConfigureAwait(true);
            using (var previewForm = new PackageSynchronizationPreviewForm(synchronizationPlan))
            {
                if (previewForm.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }
            }

            AppendLog($"Remplacement de l'installeur par {dialog.FileName}");
            var result = await _runtime.PackageUpdateService.ReplaceInstallerAsync(_currentPackage, dialog.FileName).ConfigureAwait(true);
            _currentPackage = result.UpdatedPackageInfo;

            if (_currentPackage is not null)
            {
                DisplayPackageInfo(_currentPackage);
                _packageFolderTextBox.Text = _currentPackage.PackageFolder;
                _settings.DefaultPackageFolder = _currentPackage.PackageFolder;
                await _runtime.SettingsService.SaveAsync(_settings).ConfigureAwait(true);
            }

            AppendLog(result.Message);
            foreach (var summaryLine in result.ChangeSummaryLines)
            {
                AppendLog(summaryLine);
            }
            if (!string.IsNullOrWhiteSpace(result.BackupDirectory))
            {
                AppendLog($"Sauvegarde creee dans {result.BackupDirectory}");
            }

            if (!string.IsNullOrWhiteSpace(result.SuggestedPackageFolder))
            {
                AppendLog(result.PackageFolderRenamed
                    ? $"Dossier racine renomme vers {result.SuggestedPackageFolder}"
                    : $"Nom de dossier attendu: {result.SuggestedPackageFolder}");
            }

            SetActionResult(result.Message);
            await RegisterHistoryAsync("ReplaceInstaller", result.Success, result.UpdatedPackageFolder ?? packageFolder, _currentPackage?.PackageName, result.Message, null, previousVersion, _currentPackage?.Version).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            await HandleUiOperationErrorAsync("Remplacement d'installeur impossible.", ex).ConfigureAwait(true);
        }
    }

    private async Task ValidatePackageAsync()
    {
        try
        {
            var packageFolder = GetPackageFolder();
            _currentPackage ??= await _runtime.PackageInspectorService.AnalyzePackageAsync(packageFolder).ConfigureAwait(true);
            AppendLog($"Validation du paquet: {packageFolder}");

            var result = await _runtime.PackageValidationService.ValidateAsync(packageFolder, _currentPackage).ConfigureAwait(true);
            foreach (var issue in result.Issues)
            {
                AppendLog($"[{issue.Severity}] {issue.Message}");
            }

            if (result.CommandResult is not null)
            {
                AppendCommandResult(result.CommandResult);
            }

            var message = result.IsValid ? "Validation terminee." : "Validation terminee avec erreurs.";
            SetActionResult(message);
            await RegisterHistoryAsync("Validate", result.IsValid, packageFolder, _currentPackage?.PackageName, message, result.CommandResult, _currentPackage?.Version, _currentPackage?.Version).ConfigureAwait(true);
            await RefreshWaptStatusAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            await HandleUiOperationErrorAsync("Validation impossible.", ex).ConfigureAwait(true);
        }
    }

    private async Task<bool> ExecuteBuildAsync(WaptExecutionContext? providedContext = null)
    {
        try
        {
            var packageFolder = GetPackageFolder();
            _currentPackage ??= await _runtime.PackageInspectorService.AnalyzePackageAsync(packageFolder).ConfigureAwait(true);
            AppendLog($"Execution Build sur {packageFolder}");

            var ownsContext = providedContext is null;
            var executionContext = providedContext;

            try
            {
                if (!_settings.DryRunEnabled && executionContext is null)
                {
                    executionContext = PromptForCredentials(
                        "Build WAPT assiste",
                        "Saisissez le mot de passe du certificat pour tenter le build assiste dans WaptStudio. Si WAPT refuse l'automatisation non interactive, un fallback manuel sera propose.",
                        requireCertificatePassword: true,
                        requireAdminCredentials: false);

                    if (executionContext is null)
                    {
                        return false;
                    }
                }

                var result = await _runtime.WaptCommandService.BuildPackageAsync(packageFolder, executionContext).ConfigureAwait(true);
                var outcome = await HandleActionResultAsync("Build", packageFolder, result, executionContext?.GetSensitiveValues()).ConfigureAwait(true);
                _lastKnownWaptFilePath = outcome.ArtifactPath ?? ResolveExpectedWaptFilePath(packageFolder, allowExpectedPathWhenMissing: result.IsDryRun);
                return outcome.Completed;
            }
            finally
            {
                if (ownsContext)
                {
                    executionContext?.Clear();
                }
            }
        }
        catch (Exception ex)
        {
            await HandleUiOperationErrorAsync("Operation Build impossible.", ex).ConfigureAwait(true);
            return false;
        }
    }

    private async Task<bool> ExecuteSignAsync(WaptExecutionContext? providedContext = null)
    {
        try
        {
            var packageFolder = GetPackageFolder();
            _currentPackage ??= await _runtime.PackageInspectorService.AnalyzePackageAsync(packageFolder).ConfigureAwait(true);
            AppendLog($"Execution Sign sur {packageFolder}");

            var ownsContext = providedContext is null;
            var executionContext = providedContext;

            try
            {
                if (!_settings.DryRunEnabled && executionContext is null)
                {
                    executionContext = PromptForCredentials(
                        "Signature WAPT assistee",
                        "Saisissez le mot de passe du certificat pour tenter la signature assistee dans WaptStudio. Si WAPT refuse l'automatisation non interactive, un fallback manuel sera propose.",
                        requireCertificatePassword: true,
                        requireAdminCredentials: false);

                    if (executionContext is null)
                    {
                        return false;
                    }
                }

                var result = await _runtime.WaptCommandService.SignPackageAsync(packageFolder, executionContext).ConfigureAwait(true);
                var outcome = await HandleActionResultAsync("Sign", packageFolder, result, executionContext?.GetSensitiveValues()).ConfigureAwait(true);
                return outcome.Completed;
            }
            finally
            {
                if (ownsContext)
                {
                    executionContext?.Clear();
                }
            }
        }
        catch (Exception ex)
        {
            await HandleUiOperationErrorAsync("Operation Sign impossible.", ex).ConfigureAwait(true);
            return false;
        }
    }

    private async Task<bool> ExecuteUploadAsync(string? explicitWaptFilePath = null, WaptExecutionContext? providedContext = null)
    {
        try
        {
            var packageFolder = GetPackageFolder();
            _currentPackage ??= await _runtime.PackageInspectorService.AnalyzePackageAsync(packageFolder).ConfigureAwait(true);
            var waptFilePath = ResolveUploadWaptFilePath(packageFolder, explicitWaptFilePath, allowExpectedPathWhenMissing: _settings.DryRunEnabled);

            if (string.IsNullOrWhiteSpace(waptFilePath))
            {
                MessageBox.Show(this, "Aucun fichier .wapt n'a pu etre determine pour l'upload.", "Upload WAPT", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            AppendLog($"Execution Upload sur {waptFilePath}");

            var ownsContext = providedContext is null;
            var executionContext = providedContext;

            try
            {
                if (!_settings.DryRunEnabled && executionContext is null)
                {
                    executionContext = PromptForCredentials(
                        "Upload WAPT authentifie",
                        "Saisissez l'identifiant administrateur WAPT et son mot de passe pour tenter l'upload assiste dans WaptStudio. Si WAPT refuse l'automatisation non interactive, un fallback manuel sera propose.",
                        requireCertificatePassword: false,
                        requireAdminCredentials: true);

                    if (executionContext is null)
                    {
                        return false;
                    }
                }

                if (executionContext is not null)
                {
                    executionContext.WaptFilePath = waptFilePath;
                }

                var result = await _runtime.WaptCommandService.UploadPackageAsync(packageFolder, waptFilePath, executionContext).ConfigureAwait(true);
                var outcome = await HandleActionResultAsync("Upload", packageFolder, result, executionContext?.GetSensitiveValues()).ConfigureAwait(true);
                return outcome.Completed;
            }
            finally
            {
                if (ownsContext)
                {
                    executionContext?.Clear();
                }
            }
        }
        catch (Exception ex)
        {
            await HandleUiOperationErrorAsync("Operation Upload impossible.", ex).ConfigureAwait(true);
            return false;
        }
    }

    private async Task ExecuteBuildAndUploadAsync()
    {
        try
        {
            var packageFolder = GetPackageFolder();
            _currentPackage ??= await _runtime.PackageInspectorService.AnalyzePackageAsync(packageFolder).ConfigureAwait(true);

            WaptExecutionContext? executionContext = null;

            try
            {
                if (!_settings.DryRunEnabled)
                {
                    executionContext = PromptForCredentials(
                        "Workflow Build + Upload",
                        "Saisissez les informations necessaires pour tenter un build assiste puis un upload authentifie dans WaptStudio. Si WAPT refuse l'automatisation non interactive, un fallback manuel sera propose pour chaque etape.",
                        requireCertificatePassword: true,
                        requireAdminCredentials: true);

                    if (executionContext is null)
                    {
                        return;
                    }
                }

                var buildCompleted = await ExecuteBuildAsync(executionContext).ConfigureAwait(true);
                if (!buildCompleted)
                {
                    return;
                }

                var uploadTarget = ResolveUploadWaptFilePath(packageFolder, _lastKnownWaptFilePath, allowExpectedPathWhenMissing: _settings.DryRunEnabled);
                if (string.IsNullOrWhiteSpace(uploadTarget))
                {
                    MessageBox.Show(this, "Build termine mais aucun fichier .wapt n'a pu etre determine pour l'upload.", "Build + Upload", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                AppendLog($"Enchainement vers Upload sur {uploadTarget}");
                await ExecuteUploadAsync(uploadTarget, executionContext).ConfigureAwait(true);
            }
            finally
            {
                executionContext?.Clear();
            }
        }
        catch (Exception ex)
        {
            await HandleUiOperationErrorAsync("Workflow Build + Upload impossible.", ex).ConfigureAwait(true);
        }
    }

    private async Task<ActionHandlingOutcome> HandleActionResultAsync(string actionType, string packageFolder, CommandExecutionResult result, IReadOnlyList<string>? sensitiveValues)
    {
        var sanitizedResult = SensitiveDataSanitizer.SanitizeCommandResult(result, sensitiveValues);
        AppendCommandResult(sanitizedResult);

        var actionMessage = SensitiveDataSanitizer.SanitizeText(BuildActionResultMessage(actionType, sanitizedResult), sensitiveValues);
        SetCommandActionResult(actionMessage, sanitizedResult);

        if (sanitizedResult.RequiresExternalManualWorkflow)
        {
            return await PrepareAndRunManualWorkflowAsync(actionType, packageFolder, sanitizedResult, actionMessage).ConfigureAwait(true);
        }

        var historyActionType = BuildHistoryActionType(actionType, sanitizedResult);
        await RegisterHistoryAsync(historyActionType, sanitizedResult.IsSuccess, packageFolder, _currentPackage?.PackageName, actionMessage, sanitizedResult, _currentPackage?.Version, _currentPackage?.Version).ConfigureAwait(true);
        await RefreshWaptStatusAsync().ConfigureAwait(true);

        if (sanitizedResult.ManualFallbackRecommended && SupportsManualWorkflow(actionType))
        {
            return await PrepareAndRunManualWorkflowAsync(actionType, packageFolder, sanitizedResult, actionMessage).ConfigureAwait(true);
        }

        return new ActionHandlingOutcome(sanitizedResult.IsSuccess || sanitizedResult.IsDryRun, null);
    }

    private async Task<ActionHandlingOutcome> PrepareAndRunManualWorkflowAsync(string actionType, string packageFolder, CommandExecutionResult result, string actionMessage)
    {
        _lastPreparedManualActionType = actionType;
        _lastPreparedManualCommand = result.ExecutedCommand;
        _lastPreparedManualPackageFolder = packageFolder;

        var preparedMessage = result.ManualFallbackRecommended
            ? $"{GetManualHistoryLabel(actionType)}: execution assistee non fiable ou echouee, fallback manuel prepare."
            : $"{GetManualHistoryLabel(actionType)} manuel prepare. {actionMessage}";

        var preparedResult = new CommandExecutionResult
        {
            FileName = result.FileName,
            Arguments = result.Arguments,
            ExecutedCommand = result.ExecutedCommand,
            WorkingDirectory = packageFolder,
            ExitCode = result.ExitCode,
            TimedOut = result.TimedOut,
            IsSkipped = true,
            RequiresExternalManualWorkflow = true,
            StandardError = preparedMessage,
            StartedAt = DateTimeOffset.Now,
            Duration = result.Duration
        };

        await RegisterHistoryAsync($"{actionType}ManualPrepared", false, packageFolder, _currentPackage?.PackageName, preparedMessage, preparedResult, _currentPackage?.Version, _currentPackage?.Version).ConfigureAwait(true);
        var manualOutcome = await ShowManualWorkflowAsync(actionType, result.ExecutedCommand, packageFolder).ConfigureAwait(true);
        await RefreshWaptStatusAsync().ConfigureAwait(true);
        return new ActionHandlingOutcome(manualOutcome.Confirmed, manualOutcome.ArtifactPath);
    }

    private async Task TestWaptAsync()
    {
        try
        {
            AppendLog("Test de disponibilite WAPT...");
            var result = await _runtime.WaptCommandService.CheckWaptAvailabilityAsync().ConfigureAwait(true);
            AppendCommandResult(result);
            var actionMessage = BuildActionResultMessage("Tester WAPT", result);
            SetCommandActionResult(actionMessage, result);
            await RegisterHistoryAsync("CheckWapt", result.IsSuccess, GetSafePackageFolder(), _currentPackage?.PackageName, actionMessage, result, _currentPackage?.Version, _currentPackage?.Version).ConfigureAwait(true);
            await RefreshWaptStatusAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            await HandleUiOperationErrorAsync("Test WAPT impossible.", ex).ConfigureAwait(true);
        }
    }

    private async Task RefreshWaptStatusAsync()
    {
        try
        {
            _settings = await _runtime.SettingsService.LoadAsync().ConfigureAwait(true);
            var result = await _runtime.WaptCommandService.CheckWaptAvailabilityAsync().ConfigureAwait(true);
            var pathExists = IsConfiguredWaptPathAvailable(_settings.WaptExecutablePath);
            _waptStatusValueLabel.Text = result.IsSuccess
                ? result.IsDryRun ? "Dry-run" : "Disponible"
                : pathExists ? "Configure mais indisponible" : "Indisponible";
        }
        catch
        {
            _waptStatusValueLabel.Text = "Indisponible";
        }
    }

    private async Task RegisterHistoryAsync(
        string actionType,
        bool success,
        string packageFolder,
        string? packageName,
        string message,
        CommandExecutionResult? commandResult,
        string? versionBefore,
        string? versionAfter)
    {
        await _runtime.HistoryService.AddEntryAsync(new HistoryEntry
        {
            ActionType = actionType,
            PackageFolder = packageFolder,
            PackageName = packageName,
            Success = success,
            Message = message,
            ExecutedCommand = commandResult?.ExecutedCommand,
            StandardOutput = commandResult?.StandardOutput,
            StandardError = commandResult?.StandardError,
            ExitCode = commandResult?.ExitCode ?? 0,
            DurationMilliseconds = (int)(commandResult?.Duration.TotalMilliseconds ?? 0),
            VersionBefore = versionBefore,
            VersionAfter = versionAfter
        }).ConfigureAwait(true);

        await LoadHistoryAsync().ConfigureAwait(true);
    }

    private async Task LoadHistoryAsync()
    {
        _historyEntries = await _runtime.HistoryService.GetRecentEntriesAsync().ConfigureAwait(true);
        _historyGrid.DataSource = _historyEntries.ToList();
    }

    private async Task ShowSelectedHistoryDetailsAsync()
    {
        if (_historyGrid.CurrentRow?.DataBoundItem is not HistoryEntry selected)
        {
            MessageBox.Show(this, "Selectionnez une entree d'historique.", "Historique", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var entry = await _runtime.HistoryService.GetEntryByIdAsync(selected.Id).ConfigureAwait(true) ?? selected;
        using var form = new HistoryDetailsForm(entry);
        form.ShowDialog(this);
    }

    private async Task<ManualWorkflowOutcome> ShowManualWorkflowAsync(string? actionType = null, string? preparedCommand = null, string? packageFolder = null)
    {
        var resolvedActionType = !string.IsNullOrWhiteSpace(actionType)
            ? actionType
            : _lastPreparedManualActionType;
        var resolvedPackageFolder = !string.IsNullOrWhiteSpace(packageFolder)
            ? packageFolder
            : _lastPreparedManualPackageFolder ?? GetSafePackageFolder();
        var resolvedCommand = !string.IsNullOrWhiteSpace(preparedCommand)
            ? preparedCommand
            : _lastPreparedManualCommand;

        if (string.IsNullOrWhiteSpace(resolvedActionType) || !SupportsManualWorkflow(resolvedActionType))
        {
            MessageBox.Show(this, "Aucune action manuelle preparee n'est disponible.", "Workflow manuel", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return new ManualWorkflowOutcome(false, null);
        }

        if (string.IsNullOrWhiteSpace(resolvedPackageFolder) || !Directory.Exists(resolvedPackageFolder))
        {
            MessageBox.Show(this, "Selectionnez d'abord un dossier de paquet valide.", "Workflow manuel", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return new ManualWorkflowOutcome(false, null);
        }

        using var form = new ManualBuildWorkflowForm(
            GetManualWorkflowName(resolvedActionType),
            resolvedPackageFolder,
            resolvedCommand,
            GetManualInstructionText(resolvedActionType),
            GetManualArtifactLabel(resolvedActionType),
            GetManualSelectArtifactButtonText(resolvedActionType));
        if (form.ShowDialog(this) != DialogResult.OK)
        {
            return new ManualWorkflowOutcome(false, null);
        }

        if (!form.ManualActionConfirmed)
        {
            return new ManualWorkflowOutcome(false, null);
        }

        _lastPreparedManualActionType = resolvedActionType;
        _lastPreparedManualCommand = resolvedCommand;
        _lastPreparedManualPackageFolder = resolvedPackageFolder;

        var generatedPackagePath = form.GeneratedPackagePath;
        var confirmationMessage = string.IsNullOrWhiteSpace(generatedPackagePath)
            ? $"{GetManualHistoryLabel(resolvedActionType)} manuel confirme par l'utilisateur."
            : $"{GetManualHistoryLabel(resolvedActionType)} manuel confirme. Artifact renseigne: {generatedPackagePath}";

        var confirmationResult = new CommandExecutionResult
        {
            ExecutedCommand = resolvedCommand ?? string.Empty,
            WorkingDirectory = resolvedPackageFolder,
            ExitCode = 0,
            TimedOut = false,
            IsSkipped = true,
            StandardOutput = string.IsNullOrWhiteSpace(generatedPackagePath) ? string.Empty : generatedPackagePath,
            StartedAt = DateTimeOffset.Now,
            Duration = TimeSpan.Zero
        };

        AppendLog(confirmationMessage);
        SetActionResult($"{GetManualHistoryLabel(resolvedActionType)} manuel rattache a l'historique.");
        await RegisterHistoryAsync($"{resolvedActionType}ManualConfirmed", true, resolvedPackageFolder, _currentPackage?.PackageName, confirmationMessage, confirmationResult, _currentPackage?.Version, _currentPackage?.Version).ConfigureAwait(true);
        return new ManualWorkflowOutcome(true, generatedPackagePath);
    }

    private static bool SupportsManualWorkflow(string actionType)
        => string.Equals(actionType, "Build", StringComparison.OrdinalIgnoreCase)
            || string.Equals(actionType, "Sign", StringComparison.OrdinalIgnoreCase)
            || string.Equals(actionType, "Upload", StringComparison.OrdinalIgnoreCase);

    private static string GetManualWorkflowName(string actionType)
        => string.Equals(actionType, "Sign", StringComparison.OrdinalIgnoreCase)
            ? "signature"
            : string.Equals(actionType, "Upload", StringComparison.OrdinalIgnoreCase)
                ? "upload"
            : "build";

    private static string GetManualHistoryLabel(string actionType)
        => string.Equals(actionType, "Sign", StringComparison.OrdinalIgnoreCase)
            ? "Signature"
            : string.Equals(actionType, "Upload", StringComparison.OrdinalIgnoreCase)
                ? "Upload"
            : "Build";

    private static string GetManualArtifactLabel(string actionType)
        => string.Equals(actionType, "Sign", StringComparison.OrdinalIgnoreCase)
            ? "Chemin du .wapt signe (optionnel si vous voulez tracer l'artifact final)"
            : string.Equals(actionType, "Upload", StringComparison.OrdinalIgnoreCase)
                ? "Chemin du .wapt uploade (optionnel si vous voulez tracer l'artifact)"
            : "Chemin du .wapt genere (optionnel mais recommande)";

    private static string GetManualInstructionText(string actionType)
        => string.Equals(actionType, "Upload", StringComparison.OrdinalIgnoreCase)
            ? "Cette action WAPT peut demander l'authentification administrateur du serveur. Copiez la commande ci-dessous, lancez-la dans un terminal, saisissez les identifiants uniquement quand WAPT les demande, puis revenez dans WaptStudio pour rattacher le resultat manuel a l'historique."
            : "Cette action WAPT peut demander le mot de passe du certificat. Copiez la commande ci-dessous, lancez-la dans un terminal, saisissez les secrets uniquement quand WAPT les demande, puis revenez dans WaptStudio pour rattacher le resultat manuel a l'historique.";

    private static string GetManualSelectArtifactButtonText(string actionType)
        => string.Equals(actionType, "Upload", StringComparison.OrdinalIgnoreCase)
            ? "Selectionner le .wapt uploade"
            : "Selectionner le .wapt associe";

    private static string BuildHistoryActionType(string actionType, CommandExecutionResult result)
    {
        if (result.IsDryRun)
        {
            return $"{actionType}DryRun";
        }

        if (result.WasInteractiveExecutionAttempted)
        {
            return $"{actionType}InteractiveExecuted";
        }

        return actionType;
    }

    private void DisplayPackageInfo(PackageInfo packageInfo)
    {
        _packageNameValueLabel.Text = packageInfo.PackageName ?? "Non detecte";
        _packageVersionValueLabel.Text = packageInfo.Version ?? "Non detectee";
        _installerValueLabel.Text = packageInfo.InstallerPath is null ? "Absent" : Path.GetFileName(packageInfo.InstallerPath);

        var builder = new StringBuilder();
        builder.AppendLine($"Dossier paquet: {packageInfo.PackageFolder}");
        builder.AppendLine($"Package: {packageInfo.PackageName ?? "Non detecte"}");
        builder.AppendLine($"Version: {packageInfo.Version ?? "Non detectee"}");
        builder.AppendLine($"setup.py: {packageInfo.SetupPyPath ?? "Absent"}");
        builder.AppendLine($"control: {packageInfo.ControlFilePath ?? "Absent"}");
        builder.AppendLine($"Installeur principal: {packageInfo.InstallerPath ?? "Absent"}");
        builder.AppendLine($"Type installeur: {packageInfo.InstallerType ?? "N/A"}");
        builder.AppendLine($"Installeur reference: {packageInfo.ReferencedInstallerName ?? "Non detecte"}");
        builder.AppendLine();
        builder.AppendLine("Installateurs trouves:");

        if (packageInfo.DetectedExecutables.Count == 0)
        {
            builder.AppendLine("- Aucun");
        }
        else
        {
            foreach (var executable in packageInfo.DetectedExecutables)
            {
                builder.AppendLine($"- {executable}");
            }
        }

        if (packageInfo.Warnings.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Warnings:");
            foreach (var warning in packageInfo.Warnings)
            {
                builder.AppendLine($"- {warning}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("Chemins importants:");
        builder.AppendLine($"- WAPT: {_settings.WaptExecutablePath}");
        builder.AppendLine($"- WAPT chemin existe: {(IsConfiguredWaptPathAvailable(_settings.WaptExecutablePath) ? "Oui" : "Non / PATH")}");
        builder.AppendLine($"- Logs: {AppPaths.ResolveLogsDirectory(_settings)}");
        builder.AppendLine($"- Backups: {AppPaths.ResolveBackupsDirectory(_settings)}");
        builder.AppendLine($"- SQLite: {(File.Exists(AppPaths.HistoryDatabasePath) ? "Initialisee" : "Non initialisee")}");
        builder.AppendLine($"- Dry-run: {(_settings.DryRunEnabled ? "Oui" : "Non")}");
        _packageInfoTextBox.Text = builder.ToString();
    }

    private async Task SaveReportAsync()
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "Rapport texte (*.txt)|*.txt",
            FileName = $"WaptStudio-rapport-{DateTime.Now:yyyyMMdd-HHmmss}.txt"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var builder = new StringBuilder();
        builder.AppendLine("WaptStudio - Rapport d'execution");
        builder.AppendLine($"Date: {DateTimeOffset.Now:O}");
        builder.AppendLine($"WAPT: {_waptStatusValueLabel.Text}");
        builder.AppendLine($"Dernier resultat: {_lastActionResult}");
        builder.AppendLine();
        builder.AppendLine("Paquet courant");
        builder.AppendLine(_packageInfoTextBox.Text);
        builder.AppendLine();
        builder.AppendLine("Logs");
        builder.AppendLine(_logsTextBox.Text);
        builder.AppendLine();
        builder.AppendLine("Historique recent");

        foreach (var entry in _historyEntries.Take(20))
        {
            builder.AppendLine($"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss}] #{entry.Id} {entry.ActionType} | OK={entry.Success} | {entry.Message}");
        }

        await File.WriteAllTextAsync(dialog.FileName, builder.ToString()).ConfigureAwait(true);
        AppendLog($"Rapport sauvegarde: {dialog.FileName}");
    }

    private void AppendCommandResult(CommandExecutionResult result)
    {
        AppendLog(result.Summary);
        if (result.IsDryRun)
        {
            AppendLog("Aucune execution reelle n'a ete lancee car le mode dry-run est actif.");
        }
        else if (result.RequiresExternalManualWorkflow)
        {
            AppendLog("Cette action necessite un terminal externe pour finaliser la commande WAPT.");
        }
        else if (result.ManualFallbackRecommended)
        {
            AppendLog("L'execution assistee n'a pas abouti de facon fiable. Un fallback manuel peut etre utilise.");
        }
        else if (result.IsConfigurationBlocked)
        {
            AppendLog("Action bloquee avant execution reelle en raison de la configuration locale.");
        }

        if (!string.IsNullOrWhiteSpace(result.ExecutedCommand))
        {
            AppendLog(result.IsDryRun || result.IsConfigurationBlocked
                ? $"Commande preparee: {result.ExecutedCommand}"
                : $"Commande: {result.ExecutedCommand}");
        }

        if (!string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            AppendLog(result.StandardOutput);
        }

        if (!string.IsNullOrWhiteSpace(result.StandardError))
        {
            AppendLog(result.StandardError);
        }
    }

    private async Task HandleUiOperationErrorAsync(string message, Exception exception)
    {
        SetActionResult($"Erreur: {message}");
        AppendLog($"ERREUR: {message} {exception.Message}");
        await _runtime.LogService.LogErrorAsync(message, exception).ConfigureAwait(true);
        MessageBox.Show(this, exception.Message, "Erreur WaptStudio", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private void AppendLog(string message)
    {
        _logsTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
    }

    private void SetActionResult(string value)
    {
        _lastActionResult = value;
        _actionResultValueLabel.Text = value;
        _actionResultValueLabel.ForeColor = SystemColors.ControlText;
    }

    private void SetCommandActionResult(string value, CommandExecutionResult result)
    {
        _lastActionResult = value;
        _actionResultValueLabel.Text = value;
        _actionResultValueLabel.ForeColor = result.IsDryRun
            ? Color.DarkBlue
            : result.RequiresUserInteraction
                ? Color.SaddleBrown
            : result.IsConfigurationBlocked
                ? Color.DarkOrange
                : result.IsSuccess
                    ? Color.DarkGreen
                    : Color.DarkRed;
    }

    private static string BuildActionResultMessage(string actionType, CommandExecutionResult result)
    {
        if (result.IsDryRun)
        {
            return $"{actionType}: succes simule en dry-run.";
        }

        if (result.RequiresExternalManualWorkflow)
        {
            return $"{actionType}: fallback manuel prepare. Lancez la commande dans un terminal externe.";
        }

        if (result.ManualFallbackRecommended)
        {
            return $"{actionType}: echec ou fiabilite insuffisante de l'execution assistee. Workflow manuel recommande.";
        }

        if (result.IsConfigurationBlocked)
        {
            return $"{actionType}: action bloquee. {result.Summary}";
        }

        if (result.WasInteractiveExecutionAttempted && result.IsSuccess)
        {
            return $"{actionType}: execution assistee reussie.";
        }

        if (result.IsSuccess)
        {
            return $"{actionType}: succes reel.";
        }

        return $"{actionType}: echec reel. {result.Summary}";
    }

    private async Task ShowStartupDiagnosticsAsync()
    {
        var diagnostics = await BuildEnvironmentDiagnosticsReportAsync().ConfigureAwait(true);
        AppendLog($"Version application: {diagnostics.AppVersion}");
        AppendLog($"Chemin WAPT configure: {diagnostics.WaptExecutablePath}");
        AppendLog($"Chemin WAPT disponible: {(diagnostics.WaptPathExists ? "Oui" : "Non / PATH")}");
        AppendLog($"Logs: {diagnostics.LogsDirectory}");
        AppendLog($"Backups: {diagnostics.BackupsDirectory}");
        AppendLog($"SQLite: {(diagnostics.SqliteAvailable ? "Initialisee" : "Non initialisee")}");
    }

    private async Task ShowEnvironmentDiagnosticsAsync()
    {
        var diagnostics = await BuildEnvironmentDiagnosticsReportAsync().ConfigureAwait(true);
        using var form = new EnvironmentDiagnosticsForm(BuildEnvironmentDiagnosticsText(diagnostics));
        form.ShowDialog(this);
    }

    private async Task<EnvironmentDiagnosticsSnapshot> BuildEnvironmentDiagnosticsReportAsync()
    {
        _settings = await _runtime.SettingsService.LoadAsync().ConfigureAwait(true);

        var logsDirectory = AppPaths.ResolveLogsDirectory(_settings);
        var backupsDirectory = AppPaths.ResolveBackupsDirectory(_settings);
        var sqlitePath = AppPaths.HistoryDatabasePath;
        var waptResult = await _runtime.WaptCommandService.CheckWaptAvailabilityAsync().ConfigureAwait(true);

        return new EnvironmentDiagnosticsSnapshot(
            AppVersion: GetApplicationVersion(),
            WaptExecutablePath: _settings.WaptExecutablePath,
            WaptPathExists: IsConfiguredWaptPathAvailable(_settings.WaptExecutablePath),
            WaptStatus: _waptStatusValueLabel.Text,
            LogsDirectory: logsDirectory,
            LogsDirectoryAvailable: Directory.Exists(logsDirectory),
            BackupsDirectory: backupsDirectory,
            BackupsDirectoryAvailable: Directory.Exists(backupsDirectory),
            SqlitePath: sqlitePath,
            SqliteAvailable: File.Exists(sqlitePath),
            WindowsUser: Environment.UserName,
            WaptResultSummary: waptResult.Summary,
            WaptCommand: waptResult.ExecutedCommand,
            IsDryRunEnabled: _settings.DryRunEnabled);
    }

    private string BuildEnvironmentDiagnosticsText(EnvironmentDiagnosticsSnapshot diagnostics)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Diagnostic environnement WaptStudio");
        builder.AppendLine();
        builder.AppendLine($"Version application: {diagnostics.AppVersion}");
        builder.AppendLine($"Utilisateur Windows: {diagnostics.WindowsUser}");
        builder.AppendLine($"Chemin executable WAPT: {diagnostics.WaptExecutablePath}");
        builder.AppendLine($"Chemin WAPT existe: {(diagnostics.WaptPathExists ? "Oui" : "Non / resolution via PATH")}");
        builder.AppendLine($"Statut WAPT: {diagnostics.WaptStatus}");
        builder.AppendLine($"Resultat test WAPT: {diagnostics.WaptResultSummary}");
        builder.AppendLine($"Commande test WAPT: {diagnostics.WaptCommand}");
        builder.AppendLine($"Dry-run actif: {(diagnostics.IsDryRunEnabled ? "Oui" : "Non")}");
        builder.AppendLine($"Dossier logs: {diagnostics.LogsDirectory}");
        builder.AppendLine($"Dossier logs disponible: {(diagnostics.LogsDirectoryAvailable ? "Oui" : "Non")}");
        builder.AppendLine($"Dossier backups: {diagnostics.BackupsDirectory}");
        builder.AppendLine($"Dossier backups disponible: {(diagnostics.BackupsDirectoryAvailable ? "Oui" : "Non")}");
        builder.AppendLine($"SQLite: {diagnostics.SqlitePath}");
        builder.AppendLine($"SQLite initialisee: {(diagnostics.SqliteAvailable ? "Oui" : "Non")}");
        return builder.ToString();
    }

    private static string GetApplicationVersion()
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        return assembly.GetName().Version?.ToString() ?? Application.ProductVersion ?? "Version inconnue";
    }

    private static bool IsConfiguredWaptPathAvailable(string waptExecutablePath)
    {
        return !string.IsNullOrWhiteSpace(waptExecutablePath)
            && Path.IsPathRooted(waptExecutablePath)
            && File.Exists(waptExecutablePath);
    }

    private WaptExecutionContext? PromptForCredentials(string title, string description, bool requireCertificatePassword, bool requireAdminCredentials)
    {
        using var form = new CredentialPromptForm(title, description, requireCertificatePassword, requireAdminCredentials, requireAdminCredentials);
        return form.ShowDialog(this) == DialogResult.OK ? form.ExecutionContext : null;
    }

    private string? ResolveUploadWaptFilePath(string packageFolder, string? explicitWaptFilePath, bool allowExpectedPathWhenMissing)
    {
        if (!string.IsNullOrWhiteSpace(explicitWaptFilePath) && (File.Exists(explicitWaptFilePath) || allowExpectedPathWhenMissing))
        {
            return explicitWaptFilePath;
        }

        var expectedWaptFilePath = ResolveExpectedWaptFilePath(packageFolder, allowExpectedPathWhenMissing);
        if (!string.IsNullOrWhiteSpace(expectedWaptFilePath) && (File.Exists(expectedWaptFilePath) || allowExpectedPathWhenMissing))
        {
            return expectedWaptFilePath;
        }

        using var dialog = new OpenFileDialog
        {
            Filter = "Paquets WAPT (*.wapt)|*.wapt|Tous les fichiers (*.*)|*.*",
            Title = "Selectionner le paquet .wapt a uploader",
            InitialDirectory = Directory.Exists(packageFolder) ? packageFolder : Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
        };

        return dialog.ShowDialog(this) == DialogResult.OK ? dialog.FileName : null;
    }

    private string? ResolveExpectedWaptFilePath(string packageFolder, bool allowExpectedPathWhenMissing)
    {
        var expectedFileName = BuildExpectedWaptFileName();
        var candidateFiles = Directory.EnumerateFiles(packageFolder, "*.wapt", SearchOption.AllDirectories)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .ToList();

        if (!string.IsNullOrWhiteSpace(expectedFileName))
        {
            var expectedMatch = candidateFiles.FirstOrDefault(path => string.Equals(Path.GetFileName(path), expectedFileName, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(expectedMatch))
            {
                return expectedMatch;
            }

            if (allowExpectedPathWhenMissing)
            {
                return Path.Combine(packageFolder, expectedFileName);
            }
        }

        return candidateFiles.FirstOrDefault();
    }

    private string? BuildExpectedWaptFileName()
    {
        if (string.IsNullOrWhiteSpace(_currentPackage?.PackageName) || string.IsNullOrWhiteSpace(_currentPackage?.Version))
        {
            return null;
        }

        return $"{_currentPackage.PackageName}_{_currentPackage.Version}.wapt";
    }

    private string GetPackageFolder()
    {
        var packageFolder = _packageFolderTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(packageFolder) || !Directory.Exists(packageFolder))
        {
            throw new DirectoryNotFoundException("Selectionnez un dossier de paquet valide.");
        }

        return packageFolder;
    }

    private string GetSafePackageFolder()
    {
        var packageFolder = _packageFolderTextBox.Text.Trim();
        return Directory.Exists(packageFolder) ? packageFolder : string.Empty;
    }

    private void OpenPackageFolder()
    {
        var packageFolder = GetSafePackageFolder();
        if (string.IsNullOrWhiteSpace(packageFolder))
        {
            MessageBox.Show(this, "Selectionnez d'abord un dossier de paquet valide.", "WaptStudio", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        OpenFolder(packageFolder);
    }

    private void OpenPowerShellHere()
    {
        var packageFolder = GetSafePackageFolder();
        if (string.IsNullOrWhiteSpace(packageFolder))
        {
            MessageBox.Show(this, "Selectionnez d'abord un dossier de paquet valide.", "WaptStudio", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = "powershell.exe",
            WorkingDirectory = packageFolder,
            UseShellExecute = true
        });
    }

    private static void OpenFolder(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            MessageBox.Show($"Dossier introuvable: {folderPath}", "WaptStudio", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"\"{folderPath}\"",
            UseShellExecute = true
        });
    }

    private static void AddStatusPair(TableLayoutPanel panel, int columnIndex, string labelText, Control valueControl)
    {
        panel.Controls.Add(new Label { Text = labelText, AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(0, 4, 8, 0) }, columnIndex, 0);
        panel.Controls.Add(valueControl, columnIndex + 1, 0);
    }

    private static GroupBox CreateGroupBox(string title, Control child)
    {
        var box = new GroupBox { Text = title, Dock = DockStyle.Fill, Padding = new Padding(10) };
        child.Dock = DockStyle.Fill;
        box.Controls.Add(child);
        return box;
    }

    private sealed record EnvironmentDiagnosticsSnapshot(
        string AppVersion,
        string WaptExecutablePath,
        bool WaptPathExists,
        string WaptStatus,
        string LogsDirectory,
        bool LogsDirectoryAvailable,
        string BackupsDirectory,
        bool BackupsDirectoryAvailable,
        string SqlitePath,
        bool SqliteAvailable,
        string WindowsUser,
        string WaptResultSummary,
        string WaptCommand,
        bool IsDryRunEnabled);

    private sealed record ManualWorkflowOutcome(bool Confirmed, string? ArtifactPath);

    private sealed record ActionHandlingOutcome(bool Completed, string? ArtifactPath);
}
