using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
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
    private static readonly Color AccentColor = Color.FromArgb(42, 102, 178);
    private static readonly Color SurfaceColor = Color.FromArgb(246, 248, 252);
    private static readonly Color PanelColor = Color.White;
    private static readonly Color BorderColor = Color.FromArgb(214, 221, 231);
    private static readonly Color ReadyColor = Color.FromArgb(44, 135, 81);
    private static readonly Color WarningColor = Color.FromArgb(217, 130, 24);
    private static readonly Color BlockedColor = Color.FromArgb(186, 61, 57);
    private static readonly Color InfoColor = Color.FromArgb(86, 102, 128);

    private readonly AppRuntime _runtime;
    private readonly TextBox _catalogRootFolderTextBox = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
    private readonly TextBox _searchTextBox = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
    private readonly ComboBox _categoryFilterComboBox = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly CheckBox _recursiveScanCheckBox = new() { Text = "Scan recursif", AutoSize = true };
    private readonly NumericUpDown _semiRecursiveDepthInput = new() { Minimum = 0, Maximum = 10, Value = 2, Width = 60 };
    private readonly Button _browseCatalogButton = new() { Text = "Parcourir", AutoSize = true };
    private readonly Button _scanCatalogButton = new() { Text = "Scanner", AutoSize = true };
    private readonly Button _settingsButton = new() { Text = "Parametres", AutoSize = true };
    private readonly DataGridView _catalogGrid = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AutoGenerateColumns = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, BackgroundColor = PanelColor, BorderStyle = BorderStyle.None, RowHeadersVisible = false };
    private readonly RichTextBox _packageDetailsTextBox = new() { Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = PanelColor };
    private readonly RichTextBox _readinessTextBox = new() { Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = PanelColor };
    private readonly RichTextBox _logsTextBox = new() { Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = PanelColor };
    private readonly DataGridView _historyGrid = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AutoGenerateColumns = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, BackgroundColor = PanelColor, BorderStyle = BorderStyle.None, RowHeadersVisible = false };
    private readonly Label _catalogSummaryLabel = new() { AutoSize = true, Text = "Aucun paquet charge", ForeColor = InfoColor };
    private readonly Label _selectedPackageLabel = new() { AutoSize = true, Text = "Aucun paquet selectionne", Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold) };
    private readonly Label _selectedPackageMetaLabel = new() { AutoSize = true, Text = "Chargez un catalogue de paquets CD48.", ForeColor = InfoColor };
    private readonly Label _readinessBadgeLabel = new() { AutoSize = true, Text = "BLOQUE", Padding = new Padding(12, 6, 12, 6), Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold) };
    private readonly Label _actionResultValueLabel = new() { AutoSize = true, Text = "Aucune action" };
    private readonly Label _waptStatusValueLabel = new() { AutoSize = true, Text = "Inconnu" };
    private readonly Button _analyzeButton = new() { Text = "Analyser", AutoSize = true };
    private readonly Button _replaceInstallerButton = new() { Text = "Remplacer MSI/EXE", AutoSize = true };
    private readonly Button _validateButton = new() { Text = "Valider readiness", AutoSize = true };
    private readonly Button _buildButton = new() { Text = "Build", AutoSize = true };
    private readonly Button _signButton = new() { Text = "Sign", AutoSize = true };
    private readonly Button _uploadButton = new() { Text = "Upload", AutoSize = true };
    private readonly Button _buildAndUploadButton = new() { Text = "Build + Upload", AutoSize = true };
    private readonly Button _auditButton = new() { Text = "Audit", AutoSize = true };
    private readonly Button _uninstallButton = new() { Text = "Uninstall", AutoSize = true };
    private readonly Button _restoreBackupButton = new() { Text = "Restaurer derniere sauvegarde", AutoSize = true };
    private readonly Button _openBackupFolderButton = new() { Text = "Ouvrir dossier backups", AutoSize = true };
    private readonly Button _manualWorkflowButton = new() { Text = "Workflow manuel", AutoSize = true };
    private readonly Button _saveReportButton = new() { Text = "Exporter rapport", AutoSize = true };
    private readonly Button _historyDetailsButton = new() { Text = "Details historique", AutoSize = true };

    private readonly BindingSource _catalogBindingSource = new();
    private readonly BindingSource _historyBindingSource = new();
    private readonly System.Windows.Forms.Timer _uiPulseTimer = new() { Interval = 450 };

    private AppSettings _settings = new();
    private IReadOnlyList<PackageCatalogItem> _catalogItems = Array.Empty<PackageCatalogItem>();
    private IReadOnlyList<HistoryEntry> _historyEntries = Array.Empty<HistoryEntry>();
    private PackageCatalogItem? _selectedCatalogItem;
    private ValidationResult? _currentValidationResult;
    private string? _lastPreparedManualActionType;
    private string? _lastPreparedManualCommand;
    private string? _lastPreparedManualPackageFolder;
    private string? _lastKnownWaptFilePath;
    private string _lastActionResult = "Aucune action";
    private string _currentSortColumn = nameof(PackageCatalogItem.LastModifiedUtc);
    private bool _sortAscending;
    private bool _pulseState;

    public MainForm(AppRuntime runtime)
    {
        _runtime = runtime;

        Text = "WaptStudio - Inventaire CD48";
        Width = 1720;
        Height = 1020;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = SurfaceColor;
        Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);

        InitializeComponent();
        WireEvents();
    }

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);
        _settings = await _runtime.LoadSettingsAsync().ConfigureAwait(true);
        BindSettingsToForm();
        await RefreshWaptStatusAsync().ConfigureAwait(true);
        await LoadHistoryAsync().ConfigureAwait(true);

        if (!string.IsNullOrWhiteSpace(_settings.CatalogRootFolder) && Directory.Exists(_settings.CatalogRootFolder))
        {
            await ScanCatalogAsync().ConfigureAwait(true);
        }

        AppendLog("WaptStudio est pret pour l'inventaire des paquets CD48.");
        _uiPulseTimer.Start();
    }

    private void InitializeComponent()
    {
        _categoryFilterComboBox.Items.AddRange(["Tous", "MSI", "EXE", "AUTRES"]);
        _categoryFilterComboBox.SelectedIndex = 0;

        ConfigureCatalogGrid();
        ConfigureHistoryGrid();
        StyleActionButtons();
        UpdateReadinessBadge(ReadinessVerdict.Blocked, "Aucun verdict");

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(16, 14, 16, 16),
            BackColor = SurfaceColor
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        root.Controls.Add(BuildHeaderPanel(), 0, 0);
        root.Controls.Add(BuildToolbarPanel(), 0, 1);
        root.Controls.Add(BuildStatusStripPanel(), 0, 2);
        root.Controls.Add(BuildMainArea(), 0, 3);

        Controls.Add(root);
    }

    private Control BuildHeaderPanel()
    {
        var panel = CreateCardPanel();
        panel.Padding = new Padding(18, 16, 18, 16);

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 6, RowCount = 2, BackColor = PanelColor };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var titleLabel = new Label
        {
            Text = "Inventaire paquets CD48",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold),
            ForeColor = Color.FromArgb(34, 45, 68),
            Margin = new Padding(0, 0, 0, 8)
        };

        var subtitleLabel = new Label
        {
            Text = "Chargez un dossier racine, filtrez les paquets MSI / EXE / AUTRES, puis pilotez readiness, remplacement, build, upload, uninstall et audit.",
            AutoSize = true,
            ForeColor = InfoColor,
            Margin = new Padding(0, 0, 0, 10)
        };

        layout.Controls.Add(titleLabel, 0, 0);
        layout.SetColumnSpan(titleLabel, 6);
        layout.Controls.Add(subtitleLabel, 0, 1);
        layout.SetColumnSpan(subtitleLabel, 6);

        var inputs = new TableLayoutPanel { Dock = DockStyle.Bottom, ColumnCount = 9, AutoSize = true, BackColor = PanelColor, Margin = new Padding(0, 16, 0, 0) };
        inputs.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        inputs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        inputs.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        inputs.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        inputs.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        inputs.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        inputs.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        inputs.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        inputs.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        inputs.Controls.Add(new Label { Text = "Racine CD48", AutoSize = true, Padding = new Padding(0, 8, 8, 0), ForeColor = InfoColor }, 0, 0);
        inputs.Controls.Add(_catalogRootFolderTextBox, 1, 0);
        inputs.Controls.Add(_browseCatalogButton, 2, 0);
        inputs.Controls.Add(_recursiveScanCheckBox, 3, 0);
        inputs.Controls.Add(new Label { Text = "Profondeur", AutoSize = true, Padding = new Padding(12, 8, 6, 0), ForeColor = InfoColor }, 4, 0);
        inputs.Controls.Add(_semiRecursiveDepthInput, 5, 0);
        inputs.Controls.Add(_scanCatalogButton, 6, 0);
        inputs.Controls.Add(_settingsButton, 7, 0);
        inputs.Controls.Add(_catalogSummaryLabel, 8, 0);

        panel.Controls.Add(layout);
        panel.Controls.Add(inputs);
        return panel;
    }

    private Control BuildToolbarPanel()
    {
        var panel = CreateCardPanel();
        panel.Padding = new Padding(18, 14, 18, 14);

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, BackColor = PanelColor };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var filterRow = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true, BackColor = PanelColor };
        filterRow.Controls.Add(new Label { Text = "Recherche", AutoSize = true, Padding = new Padding(0, 8, 8, 0), ForeColor = InfoColor });
        _searchTextBox.Width = 220;
        filterRow.Controls.Add(_searchTextBox);
        filterRow.Controls.Add(new Label { Text = "Categorie", AutoSize = true, Padding = new Padding(14, 8, 8, 0), ForeColor = InfoColor });
        _categoryFilterComboBox.Width = 120;
        filterRow.Controls.Add(_categoryFilterComboBox);
        filterRow.Controls.Add(new Label { Text = "Selection", AutoSize = true, Padding = new Padding(14, 8, 8, 0), ForeColor = InfoColor });
        filterRow.Controls.Add(_selectedPackageLabel);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, WrapContents = true, BackColor = PanelColor };
        actions.Controls.AddRange([
            _analyzeButton,
            _replaceInstallerButton,
            _validateButton,
            _buildButton,
            _signButton,
            _uploadButton,
            _buildAndUploadButton,
            _auditButton,
            _uninstallButton,
            _restoreBackupButton,
            _openBackupFolderButton,
            _manualWorkflowButton,
            _saveReportButton,
            _historyDetailsButton
        ]);

        root.Controls.Add(filterRow, 0, 0);
        root.Controls.Add(new Panel { Width = 12, BackColor = PanelColor }, 1, 0);
        root.Controls.Add(actions, 2, 0);
        panel.Controls.Add(root);
        return panel;
    }

    private Control BuildStatusStripPanel()
    {
        var panel = CreateCardPanel();
        panel.Padding = new Padding(18, 12, 18, 12);

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 6, BackColor = PanelColor };
        for (var index = 0; index < 6; index++)
        {
            layout.ColumnStyles.Add(new ColumnStyle(index % 2 == 0 ? SizeType.AutoSize : SizeType.Percent, index % 2 == 0 ? 0 : 50));
        }

        layout.Controls.Add(new Label { Text = "Paquet", AutoSize = true, Padding = new Padding(0, 7, 10, 0), ForeColor = InfoColor }, 0, 0);
        layout.Controls.Add(_selectedPackageMetaLabel, 1, 0);
        layout.Controls.Add(new Label { Text = "Readiness", AutoSize = true, Padding = new Padding(24, 7, 10, 0), ForeColor = InfoColor }, 2, 0);
        layout.Controls.Add(_readinessBadgeLabel, 3, 0);
        layout.Controls.Add(new Label { Text = "WAPT / Action", AutoSize = true, Padding = new Padding(24, 7, 10, 0), ForeColor = InfoColor }, 4, 0);

        var actionMetaPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, BackColor = PanelColor };
        actionMetaPanel.Controls.Add(_waptStatusValueLabel);
        actionMetaPanel.Controls.Add(new Label { Text = "|", AutoSize = true, Padding = new Padding(8, 0, 8, 0), ForeColor = BorderColor });
        actionMetaPanel.Controls.Add(_actionResultValueLabel);
        layout.Controls.Add(actionMetaPanel, 5, 0);

        panel.Controls.Add(layout);
        return panel;
    }

    private Control BuildMainArea()
    {
        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, SplitterDistance = 760, BackColor = SurfaceColor };
        split.Panel1.Padding = new Padding(0, 6, 8, 0);
        split.Panel2.Padding = new Padding(8, 6, 0, 0);

        split.Panel1.Controls.Add(BuildCatalogArea());
        split.Panel2.Controls.Add(BuildDetailsArea());
        return split;
    }

    private Control BuildCatalogArea()
    {
        var card = CreateCardPanel();
        card.Padding = new Padding(14);

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = PanelColor };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(CreateSectionHeader("Liste des paquets", "Inventaire triable et filtrable des paquets WAPT detectes."), 0, 0);
        layout.Controls.Add(_catalogGrid, 0, 1);

        card.Controls.Add(layout);
        return card;
    }

    private Control BuildDetailsArea()
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, BackColor = SurfaceColor };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 33));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 24));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 19));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 24));

        layout.Controls.Add(CreateSectionCard("Detail paquet", "Vue detaillee du paquet CD48 selectionne.", _packageDetailsTextBox), 0, 0);
        layout.Controls.Add(CreateSectionCard("Verdict readiness", "Explication claire des blocages, avertissements et actions possibles.", _readinessTextBox), 0, 1);
        layout.Controls.Add(CreateSectionCard("Journal", "Historique immediat des actions et des resultats WAPT.", _logsTextBox), 0, 2);
        layout.Controls.Add(CreateSectionCard("Historique local", "Actions tracees avec statut, message, readiness et chemin .wapt.", _historyGrid), 0, 3);
        return layout;
    }

    private void WireEvents()
    {
        _browseCatalogButton.Click += BrowseCatalogRootFolder;
        _scanCatalogButton.Click += async (_, _) => await ScanCatalogAsync().ConfigureAwait(true);
        _settingsButton.Click += async (_, _) => await OpenSettingsAsync().ConfigureAwait(true);
        _searchTextBox.TextChanged += (_, _) => RefreshCatalogGrid();
        _categoryFilterComboBox.SelectedIndexChanged += (_, _) => RefreshCatalogGrid();
        _recursiveScanCheckBox.CheckedChanged += (_, _) => _semiRecursiveDepthInput.Enabled = !_recursiveScanCheckBox.Checked;
        _catalogGrid.SelectionChanged += async (_, _) => await HandleSelectedCatalogItemChangedAsync().ConfigureAwait(true);
        _catalogGrid.ColumnHeaderMouseClick += (_, eventArgs) => ApplyCatalogSort(_catalogGrid.Columns[eventArgs.ColumnIndex].DataPropertyName);
        _historyGrid.CellDoubleClick += async (_, _) => await ShowSelectedHistoryDetailsAsync().ConfigureAwait(true);
        _historyDetailsButton.Click += async (_, _) => await ShowSelectedHistoryDetailsAsync().ConfigureAwait(true);
        _analyzeButton.Click += async (_, _) => await AnalyzeSelectedPackageAsync().ConfigureAwait(true);
        _replaceInstallerButton.Click += async (_, _) => await ReplaceInstallerAsync().ConfigureAwait(true);
        _validateButton.Click += async (_, _) => await ValidateSelectedPackageAsync(includeWaptValidation: true).ConfigureAwait(true);
        _buildButton.Click += async (_, _) => await ExecuteBuildAsync().ConfigureAwait(true);
        _signButton.Click += async (_, _) => await ExecuteSignAsync().ConfigureAwait(true);
        _uploadButton.Click += async (_, _) => await ExecuteUploadAsync().ConfigureAwait(true);
        _buildAndUploadButton.Click += async (_, _) => await ExecuteBuildAndUploadAsync().ConfigureAwait(true);
        _auditButton.Click += async (_, _) => await ExecuteAuditAsync().ConfigureAwait(true);
        _uninstallButton.Click += async (_, _) => await ExecuteUninstallAsync().ConfigureAwait(true);
        _restoreBackupButton.Click += async (_, _) => await RestoreLatestBackupAsync().ConfigureAwait(true);
        _openBackupFolderButton.Click += (_, _) => OpenFolder(AppPaths.ResolveBackupsDirectory(_settings));
        _manualWorkflowButton.Click += async (_, _) => await ShowManualWorkflowAsync().ConfigureAwait(true);
        _saveReportButton.Click += async (_, _) => await SaveReportAsync().ConfigureAwait(true);
        _uiPulseTimer.Tick += (_, _) => PulseSelectionAccent();
    }

    private async Task OpenSettingsAsync()
    {
        var settings = await _runtime.SettingsService.LoadAsync().ConfigureAwait(true);
        using var form = new SettingsForm(settings);
        if (form.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _settings = form.Settings;
        await _runtime.SettingsService.SaveAsync(_settings).ConfigureAwait(true);
        BindSettingsToForm();
        await RefreshWaptStatusAsync().ConfigureAwait(true);
        AppendLog("Parametres mis a jour.");
    }

    private void BindSettingsToForm()
    {
        _catalogRootFolderTextBox.Text = _settings.CatalogRootFolder ?? _settings.DefaultPackageFolder ?? string.Empty;
        _recursiveScanCheckBox.Checked = _settings.CatalogScanRecursively;
        _semiRecursiveDepthInput.Value = Math.Clamp(_settings.CatalogSemiRecursiveDepth, 0, 10);
        _semiRecursiveDepthInput.Enabled = !_recursiveScanCheckBox.Checked;
    }

    private async Task ScanCatalogAsync()
    {
        try
        {
            var rootFolder = _catalogRootFolderTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(rootFolder) || !Directory.Exists(rootFolder))
            {
                throw new DirectoryNotFoundException("Selectionnez un dossier racine CD48 valide.");
            }

            AppendLog($"Scan catalogue: {rootFolder}");
            _settings.CatalogRootFolder = rootFolder;
            _settings.CatalogScanRecursively = _recursiveScanCheckBox.Checked;
            _settings.CatalogSemiRecursiveDepth = (int)_semiRecursiveDepthInput.Value;
            await _runtime.SettingsService.SaveAsync(_settings).ConfigureAwait(true);

            _catalogItems = await _runtime.PackageCatalogService
                .ScanAsync(rootFolder, _recursiveScanCheckBox.Checked, (int)_semiRecursiveDepthInput.Value)
                .ConfigureAwait(true);

            ApplyCatalogSort(_currentSortColumn, preserveDirection: true);
            _catalogSummaryLabel.Text = $"{_catalogItems.Count} paquet(s) detecte(s)";
            SetActionResult("Inventaire charge.", InfoColor);
            AppendLog($"Catalogue charge: {_catalogItems.Count} paquet(s) CD48 detecte(s).");
        }
        catch (Exception ex)
        {
            await HandleUiOperationErrorAsync("Scan catalogue impossible.", ex).ConfigureAwait(true);
        }
    }

    private void RefreshCatalogGrid()
    {
        IEnumerable<PackageCatalogItem> query = _catalogItems;

        var search = _searchTextBox.Text.Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(item =>
                item.PackageId.Contains(search, StringComparison.OrdinalIgnoreCase)
                || item.VisibleName.Contains(search, StringComparison.OrdinalIgnoreCase)
                || item.Version.Contains(search, StringComparison.OrdinalIgnoreCase)
                || item.PackageFolder.Contains(search, StringComparison.OrdinalIgnoreCase)
                || item.Maturity.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        query = _categoryFilterComboBox.SelectedItem?.ToString() switch
        {
            "MSI" => query.Where(item => item.Category == PackageCategory.Msi),
            "EXE" => query.Where(item => item.Category == PackageCategory.Exe),
            "AUTRES" => query.Where(item => item.Category == PackageCategory.Other),
            _ => query
        };

        query = SortCatalogItems(query, _currentSortColumn, _sortAscending);
        _catalogBindingSource.DataSource = new BindingList<PackageCatalogItem>(query.ToList());
        _catalogGrid.DataSource = _catalogBindingSource;

        if (_catalogGrid.Rows.Count > 0)
        {
            _catalogGrid.Rows[0].Selected = true;
        }
        else
        {
            _selectedCatalogItem = null;
            RenderSelectedPackage(null, null);
        }

        _catalogSummaryLabel.Text = $"{_catalogGrid.Rows.Count} paquet(s) affiche(s)";
    }

    private void ApplyCatalogSort(string? columnName, bool preserveDirection = false)
    {
        if (string.IsNullOrWhiteSpace(columnName))
        {
            columnName = nameof(PackageCatalogItem.LastModifiedUtc);
        }

        if (!preserveDirection)
        {
            _sortAscending = _currentSortColumn == columnName ? !_sortAscending : columnName != nameof(PackageCatalogItem.LastModifiedUtc);
        }

        _currentSortColumn = columnName;
        RefreshCatalogGrid();
    }

    private static IEnumerable<PackageCatalogItem> SortCatalogItems(IEnumerable<PackageCatalogItem> source, string columnName, bool ascending)
    {
        return columnName switch
        {
            nameof(PackageCatalogItem.PackageId) => ascending ? source.OrderBy(item => item.PackageId, StringComparer.OrdinalIgnoreCase) : source.OrderByDescending(item => item.PackageId, StringComparer.OrdinalIgnoreCase),
            nameof(PackageCatalogItem.VisibleName) => ascending ? source.OrderBy(item => item.VisibleName, StringComparer.OrdinalIgnoreCase) : source.OrderByDescending(item => item.VisibleName, StringComparer.OrdinalIgnoreCase),
            nameof(PackageCatalogItem.Version) => ascending ? source.OrderBy(item => item.Version, StringComparer.OrdinalIgnoreCase) : source.OrderByDescending(item => item.Version, StringComparer.OrdinalIgnoreCase),
            nameof(PackageCatalogItem.Maturity) => ascending ? source.OrderBy(item => item.Maturity, StringComparer.OrdinalIgnoreCase) : source.OrderByDescending(item => item.Maturity, StringComparer.OrdinalIgnoreCase),
            nameof(PackageCatalogItem.ReadinessLabel) => ascending ? source.OrderBy(item => item.ReadinessVerdict) : source.OrderByDescending(item => item.ReadinessVerdict),
            nameof(PackageCatalogItem.PackageFolder) => ascending ? source.OrderBy(item => item.PackageFolder, StringComparer.OrdinalIgnoreCase) : source.OrderByDescending(item => item.PackageFolder, StringComparer.OrdinalIgnoreCase),
            _ => ascending ? source.OrderBy(item => item.LastModifiedUtc) : source.OrderByDescending(item => item.LastModifiedUtc)
        };
    }

    private async Task HandleSelectedCatalogItemChangedAsync()
    {
        if (_catalogGrid.CurrentRow?.DataBoundItem is not PackageCatalogItem item)
        {
            return;
        }

        _selectedCatalogItem = item;
        _lastKnownWaptFilePath = ResolveExpectedWaptFilePath(item.PackageInfo.PackageFolder, allowExpectedPathWhenMissing: false);

        if (_currentValidationResult is null || !_currentValidationResult.Issues.Any() || !string.Equals(item.PackageFolder, _selectedCatalogItem?.PackageFolder, StringComparison.OrdinalIgnoreCase))
        {
            _currentValidationResult = await _runtime.PackageValidationService.ValidateAsync(item.PackageFolder, item.PackageInfo, includeWaptValidation: false).ConfigureAwait(true);
        }

        RenderSelectedPackage(item, _currentValidationResult);
    }

    private void RenderSelectedPackage(PackageCatalogItem? item, ValidationResult? validationResult)
    {
        if (item is null)
        {
            _selectedPackageLabel.Text = "Aucun paquet selectionne";
            _selectedPackageMetaLabel.Text = "Chargez un catalogue puis selectionnez un paquet.";
            _packageDetailsTextBox.Text = string.Empty;
            _readinessTextBox.Text = string.Empty;
            UpdateReadinessBadge(ReadinessVerdict.Blocked, "Aucun verdict");
            return;
        }

        _selectedPackageLabel.Text = item.PackageId;
        _selectedPackageMetaLabel.Text = $"{item.VisibleName} | v{(string.IsNullOrWhiteSpace(item.Version) ? "N/A" : item.Version)} | {item.Category.ToString().ToUpperInvariant()} | {item.Maturity}";
        _packageDetailsTextBox.Text = BuildPackageDetailsText(item);
        _readinessTextBox.Text = BuildReadinessText(validationResult);
        UpdateReadinessBadge(validationResult?.Verdict ?? item.ReadinessVerdict, validationResult?.VerdictLabel ?? item.ReadinessLabel);
    }

    private async Task AnalyzeSelectedPackageAsync()
    {
        try
        {
            var item = RequireSelectedPackage();
            item.PackageInfo = await _runtime.PackageInspectorService.AnalyzePackageAsync(item.PackageFolder).ConfigureAwait(true);
            _currentValidationResult = await _runtime.PackageValidationService.ValidateAsync(item.PackageFolder, item.PackageInfo, includeWaptValidation: false).ConfigureAwait(true);
            ReplaceCatalogItem(item, _currentValidationResult);
            RenderSelectedPackage(item, _currentValidationResult);
            AppendLog($"Analyse metier terminee pour {item.PackageId}.");
            SetActionResult("Analyse terminee.", InfoColor);
            await RegisterHistoryAsync("Analyze", true, item.PackageFolder, item.PackageId, "Analyse du paquet terminee.", null, item.PackageInfo.Version, item.PackageInfo.Version, null, _currentValidationResult.VerdictLabel).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            await HandleUiOperationErrorAsync("Analyse impossible.", ex).ConfigureAwait(true);
        }
    }

    private async Task ValidateSelectedPackageAsync(bool includeWaptValidation)
    {
        try
        {
            var item = RequireSelectedPackage();
            _currentValidationResult = await _runtime.PackageValidationService.ValidateAsync(item.PackageFolder, item.PackageInfo, includeWaptValidation).ConfigureAwait(true);
            ReplaceCatalogItem(item, _currentValidationResult);
            RenderSelectedPackage(item, _currentValidationResult);
            AppendLog(_currentValidationResult.Summary);
            SetActionResult(_currentValidationResult.VerdictLabel, GetVerdictColor(_currentValidationResult.Verdict));
            await RegisterHistoryAsync("Validate", _currentValidationResult.Verdict != ReadinessVerdict.Blocked, item.PackageFolder, item.PackageId, _currentValidationResult.Summary, _currentValidationResult.CommandResult, item.PackageInfo.Version, item.PackageInfo.Version, _lastKnownWaptFilePath, _currentValidationResult.VerdictLabel).ConfigureAwait(true);
            await RefreshWaptStatusAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            await HandleUiOperationErrorAsync("Validation readiness impossible.", ex).ConfigureAwait(true);
        }
    }

    private async Task ReplaceInstallerAsync()
    {
        try
        {
            var item = RequireSelectedPackage();
            using var dialog = new OpenFileDialog
            {
                Filter = "Installateurs (*.msi;*.exe)|*.msi;*.exe",
                Title = "Selectionner un nouveau MSI ou EXE"
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            var previousVersion = item.PackageInfo.Version;
            var plan = await _runtime.PackageUpdateService.PreviewReplacementAsync(item.PackageInfo, dialog.FileName).ConfigureAwait(true);
            using (var previewForm = new PackageSynchronizationPreviewForm(plan))
            {
                if (previewForm.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }
            }

            var result = await _runtime.PackageUpdateService.ReplaceInstallerAsync(item.PackageInfo, dialog.FileName).ConfigureAwait(true);
            var updatedInfo = result.UpdatedPackageInfo ?? await _runtime.PackageInspectorService.AnalyzePackageAsync(result.UpdatedPackageFolder ?? item.PackageFolder).ConfigureAwait(true);
            var updatedValidation = await _runtime.PackageValidationService.ValidateAsync(updatedInfo.PackageFolder, updatedInfo, includeWaptValidation: false).ConfigureAwait(true);

            var updatedItem = new PackageCatalogItem
            {
                PackageId = updatedInfo.PackageName ?? item.PackageId,
                VisibleName = updatedInfo.VisibleName ?? item.VisibleName,
                Version = updatedInfo.Version ?? item.Version,
                Category = updatedInfo.Category,
                Maturity = updatedInfo.Maturity,
                ReadinessVerdict = updatedValidation.Verdict,
                ReadinessLabel = updatedValidation.VerdictLabel,
                LastModifiedUtc = updatedInfo.LastModifiedUtc,
                PackageFolder = updatedInfo.PackageFolder,
                PrimaryInstallerName = Path.GetFileName(updatedInfo.InstallerPath ?? string.Empty),
                PackageInfo = updatedInfo
            };

            ReplaceCatalogItem(item, updatedValidation, updatedItem);
            _selectedCatalogItem = updatedItem;
            _currentValidationResult = updatedValidation;
            RenderSelectedPackage(updatedItem, updatedValidation);

            AppendLog(result.Message);
            foreach (var line in result.ChangeSummaryLines)
            {
                AppendLog(line);
            }

            if (!string.IsNullOrWhiteSpace(result.BackupDirectory))
            {
                AppendLog($"Sauvegarde creee: {result.BackupDirectory}");
            }

            SetActionResult(result.Success ? "Remplacement applique." : "Remplacement incomplet.", result.Success ? ReadyColor : WarningColor);
            await RegisterHistoryAsync("ReplaceInstaller", result.Success, updatedInfo.PackageFolder, updatedInfo.PackageName, result.Message, null, previousVersion, updatedInfo.Version, updatedInfo.ExpectedWaptFileName is null ? null : Path.Combine(updatedInfo.PackageFolder, updatedInfo.ExpectedWaptFileName), updatedValidation.VerdictLabel).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            await HandleUiOperationErrorAsync("Remplacement de l'installeur impossible.", ex).ConfigureAwait(true);
        }
    }

    private async Task<bool> ExecuteBuildAsync(WaptExecutionContext? providedContext = null)
    {
        try
        {
            var item = RequireSelectedPackage();
            var ownsContext = providedContext is null;
            var executionContext = providedContext;

            try
            {
                if (!_settings.DryRunEnabled && executionContext is null)
                {
                    executionContext = PromptForCredentials(
                        "Build WAPT assiste",
                        "Saisissez le mot de passe du certificat pour tenter le build assiste. Si WAPT ne supporte pas l'automatisation non interactive, WaptStudio preparera un workflow manuel securise.",
                        requireCertificatePassword: true,
                        requireAdminCredentials: false);

                    if (executionContext is null)
                    {
                        return false;
                    }
                }

                var result = await _runtime.WaptCommandService.BuildPackageAsync(item.PackageFolder, executionContext).ConfigureAwait(true);
                var outcome = await HandleActionResultAsync("Build", item, result, executionContext?.GetSensitiveValues(), null).ConfigureAwait(true);
                _lastKnownWaptFilePath = outcome.ArtifactPath ?? ResolveExpectedWaptFilePath(item.PackageFolder, allowExpectedPathWhenMissing: result.IsDryRun);
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
            await HandleUiOperationErrorAsync("Build impossible.", ex).ConfigureAwait(true);
            return false;
        }
    }

    private async Task<bool> ExecuteSignAsync(WaptExecutionContext? providedContext = null)
    {
        try
        {
            var item = RequireSelectedPackage();
            var ownsContext = providedContext is null;
            var executionContext = providedContext;

            try
            {
                if (!_settings.DryRunEnabled && executionContext is null)
                {
                    executionContext = PromptForCredentials(
                        "Signature WAPT assistee",
                        "Saisissez le mot de passe du certificat pour tenter la signature assistee. Si WAPT refuse l'automatisation non interactive, WaptStudio basculera vers un workflow manuel.",
                        requireCertificatePassword: true,
                        requireAdminCredentials: false);

                    if (executionContext is null)
                    {
                        return false;
                    }
                }

                var result = await _runtime.WaptCommandService.SignPackageAsync(item.PackageFolder, executionContext).ConfigureAwait(true);
                var outcome = await HandleActionResultAsync("Sign", item, result, executionContext?.GetSensitiveValues(), null).ConfigureAwait(true);
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
            await HandleUiOperationErrorAsync("Sign impossible.", ex).ConfigureAwait(true);
            return false;
        }
    }

    private async Task<bool> ExecuteUploadAsync(string? explicitWaptFilePath = null, WaptExecutionContext? providedContext = null)
    {
        try
        {
            var item = RequireSelectedPackage();
            var waptFilePath = ResolveUploadWaptFilePath(item.PackageFolder, explicitWaptFilePath, allowExpectedPathWhenMissing: _settings.DryRunEnabled);
            if (string.IsNullOrWhiteSpace(waptFilePath))
            {
                MessageBox.Show(this, "Aucun fichier .wapt n'a pu etre determine pour l'upload.", "Upload", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            var ownsContext = providedContext is null;
            var executionContext = providedContext;
            try
            {
                if (!_settings.DryRunEnabled && executionContext is null)
                {
                    executionContext = PromptForCredentials(
                        "Upload WAPT authentifie",
                        "Saisissez l'identifiant administrateur WAPT et le mot de passe associe pour tenter l'upload assiste. Si cela n'est pas fiable, un workflow manuel sera prepare.",
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

                var result = await _runtime.WaptCommandService.UploadPackageAsync(item.PackageFolder, waptFilePath, executionContext).ConfigureAwait(true);
                var outcome = await HandleActionResultAsync("Upload", item, result, executionContext?.GetSensitiveValues(), waptFilePath).ConfigureAwait(true);
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
            await HandleUiOperationErrorAsync("Upload impossible.", ex).ConfigureAwait(true);
            return false;
        }
    }

    private async Task ExecuteBuildAndUploadAsync()
    {
        try
        {
            WaptExecutionContext? executionContext = null;
            try
            {
                if (!_settings.DryRunEnabled)
                {
                    executionContext = PromptForCredentials(
                        "Build + Upload",
                        "Saisissez le mot de passe certificat et les identifiants d'upload. Les secrets ne sont jamais stockes et seront purges apres l'action.",
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

                var item = RequireSelectedPackage();
                var uploadTarget = ResolveUploadWaptFilePath(item.PackageFolder, _lastKnownWaptFilePath, allowExpectedPathWhenMissing: _settings.DryRunEnabled);
                if (string.IsNullOrWhiteSpace(uploadTarget))
                {
                    MessageBox.Show(this, "Build termine mais aucun fichier .wapt n'a pu etre determine pour l'upload.", "Build + Upload", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

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

    private async Task ExecuteAuditAsync()
    {
        try
        {
            var item = RequireSelectedPackage();
            var result = await _runtime.WaptCommandService.AuditPackageAsync(item.PackageFolder, item.PackageId).ConfigureAwait(true);
            await HandleNonInteractiveActionAsync("Audit", item, result, null).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            await HandleUiOperationErrorAsync("Audit impossible.", ex).ConfigureAwait(true);
        }
    }

    private async Task ExecuteUninstallAsync()
    {
        try
        {
            var item = RequireSelectedPackage();
            var result = await _runtime.WaptCommandService.UninstallPackageAsync(item.PackageFolder, item.PackageId).ConfigureAwait(true);
            await HandleNonInteractiveActionAsync("Uninstall", item, result, null).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            await HandleUiOperationErrorAsync("Uninstall impossible.", ex).ConfigureAwait(true);
        }
    }

    private async Task HandleNonInteractiveActionAsync(string actionType, PackageCatalogItem item, CommandExecutionResult result, string? artifactPath)
    {
        AppendCommandResult(result);
        var message = BuildActionResultMessage(actionType, result);
        SetActionResult(message, ResolveResultColor(result));
        await RegisterHistoryAsync(actionType, result.IsSuccess || result.IsDryRun, item.PackageFolder, item.PackageId, message, result, item.PackageInfo.Version, item.PackageInfo.Version, artifactPath, _currentValidationResult?.VerdictLabel).ConfigureAwait(true);
        await RefreshWaptStatusAsync().ConfigureAwait(true);
    }

    private async Task<ActionHandlingOutcome> HandleActionResultAsync(string actionType, PackageCatalogItem item, CommandExecutionResult result, IReadOnlyList<string>? sensitiveValues, string? artifactPath)
    {
        var sanitizedResult = SensitiveDataSanitizer.SanitizeCommandResult(result, sensitiveValues);
        AppendCommandResult(sanitizedResult);
        var actionMessage = SensitiveDataSanitizer.SanitizeText(BuildActionResultMessage(actionType, sanitizedResult), sensitiveValues);
        SetActionResult(actionMessage, ResolveResultColor(sanitizedResult));

        if (sanitizedResult.RequiresExternalManualWorkflow)
        {
            return await PrepareAndRunManualWorkflowAsync(actionType, item, sanitizedResult, actionMessage).ConfigureAwait(true);
        }

        await RegisterHistoryAsync(BuildHistoryActionType(actionType, sanitizedResult), sanitizedResult.IsSuccess || sanitizedResult.IsDryRun, item.PackageFolder, item.PackageId, actionMessage, sanitizedResult, item.PackageInfo.Version, item.PackageInfo.Version, artifactPath, _currentValidationResult?.VerdictLabel).ConfigureAwait(true);
        await RefreshWaptStatusAsync().ConfigureAwait(true);

        if (sanitizedResult.ManualFallbackRecommended && SupportsManualWorkflow(actionType))
        {
            return await PrepareAndRunManualWorkflowAsync(actionType, item, sanitizedResult, actionMessage).ConfigureAwait(true);
        }

        return new ActionHandlingOutcome(sanitizedResult.IsSuccess || sanitizedResult.IsDryRun, artifactPath);
    }

    private async Task<ActionHandlingOutcome> PrepareAndRunManualWorkflowAsync(string actionType, PackageCatalogItem item, CommandExecutionResult result, string actionMessage)
    {
        _lastPreparedManualActionType = actionType;
        _lastPreparedManualCommand = result.ExecutedCommand;
        _lastPreparedManualPackageFolder = item.PackageFolder;

        var preparedMessage = result.ManualFallbackRecommended
            ? $"{GetManualHistoryLabel(actionType)}: execution assistee non fiable ou echouee, fallback manuel prepare."
            : $"{GetManualHistoryLabel(actionType)} manuel prepare. {actionMessage}";

        var preparedResult = new CommandExecutionResult
        {
            FileName = result.FileName,
            Arguments = result.Arguments,
            ExecutedCommand = result.ExecutedCommand,
            WorkingDirectory = item.PackageFolder,
            ExitCode = result.ExitCode,
            TimedOut = result.TimedOut,
            IsSkipped = true,
            RequiresExternalManualWorkflow = true,
            StandardError = preparedMessage,
            StartedAt = DateTimeOffset.Now,
            Duration = result.Duration
        };

        await RegisterHistoryAsync($"{actionType}ManualPrepared", false, item.PackageFolder, item.PackageId, preparedMessage, preparedResult, item.PackageInfo.Version, item.PackageInfo.Version, null, _currentValidationResult?.VerdictLabel).ConfigureAwait(true);
        var manualOutcome = await ShowManualWorkflowAsync(actionType, result.ExecutedCommand, item.PackageFolder).ConfigureAwait(true);
        await RefreshWaptStatusAsync().ConfigureAwait(true);
        return new ActionHandlingOutcome(manualOutcome.Confirmed, manualOutcome.ArtifactPath);
    }

    private async Task RestoreLatestBackupAsync()
    {
        try
        {
            var item = RequireSelectedPackage();
            var restoreResult = await _runtime.BackupRestoreService.RestoreLatestBackupAsync(item.PackageInfo).ConfigureAwait(true);
            AppendLog(restoreResult.Message);
            foreach (var file in restoreResult.RestoredFiles)
            {
                AppendLog($"Restaure: {file}");
            }

            if (restoreResult.Success)
            {
                var refreshedPackage = await _runtime.PackageInspectorService.AnalyzePackageAsync(item.PackageFolder).ConfigureAwait(true);
                _currentValidationResult = await _runtime.PackageValidationService.ValidateAsync(refreshedPackage.PackageFolder, refreshedPackage, includeWaptValidation: false).ConfigureAwait(true);
                var updatedItem = new PackageCatalogItem
                {
                    PackageId = refreshedPackage.PackageName ?? item.PackageId,
                    VisibleName = refreshedPackage.VisibleName ?? item.VisibleName,
                    Version = refreshedPackage.Version ?? item.Version,
                    Category = refreshedPackage.Category,
                    Maturity = refreshedPackage.Maturity,
                    ReadinessVerdict = _currentValidationResult.Verdict,
                    ReadinessLabel = _currentValidationResult.VerdictLabel,
                    LastModifiedUtc = refreshedPackage.LastModifiedUtc,
                    PackageFolder = refreshedPackage.PackageFolder,
                    PrimaryInstallerName = Path.GetFileName(refreshedPackage.InstallerPath ?? string.Empty),
                    PackageInfo = refreshedPackage
                };

                ReplaceCatalogItem(item, _currentValidationResult, updatedItem);
                _selectedCatalogItem = updatedItem;
                RenderSelectedPackage(updatedItem, _currentValidationResult);
            }

            SetActionResult(restoreResult.Message, restoreResult.Success ? ReadyColor : WarningColor);
            await RegisterHistoryAsync("RestoreBackup", restoreResult.Success, item.PackageFolder, item.PackageId, restoreResult.Message, null, item.PackageInfo.Version, item.PackageInfo.Version, null, _currentValidationResult?.VerdictLabel).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            await HandleUiOperationErrorAsync("Restauration impossible.", ex).ConfigureAwait(true);
        }
    }

    private async Task SaveReportAsync()
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "Rapport texte (*.txt)|*.txt",
            FileName = $"WaptStudio-cd48-{DateTime.Now:yyyyMMdd-HHmmss}.txt"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var builder = new StringBuilder();
        builder.AppendLine("WaptStudio - Rapport paquet CD48");
        builder.AppendLine($"Date: {DateTimeOffset.Now:O}");
        builder.AppendLine($"WAPT: {_waptStatusValueLabel.Text}");
        builder.AppendLine($"Derniere action: {_lastActionResult}");
        builder.AppendLine();
        builder.AppendLine("DETAIL PAQUET");
        builder.AppendLine(_packageDetailsTextBox.Text);
        builder.AppendLine();
        builder.AppendLine("READINESS");
        builder.AppendLine(_readinessTextBox.Text);
        builder.AppendLine();
        builder.AppendLine("LOGS");
        builder.AppendLine(_logsTextBox.Text);
        builder.AppendLine();
        builder.AppendLine("HISTORIQUE RECENT");

        foreach (var entry in _historyEntries.Take(30))
        {
            builder.AppendLine($"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss}] {entry.ActionType} | OK={entry.Success} | Readiness={entry.ReadinessVerdict ?? "N/A"} | .wapt={entry.WaptArtifactPath ?? "N/A"} | {entry.Message}");
        }

        await File.WriteAllTextAsync(dialog.FileName, builder.ToString()).ConfigureAwait(true);
        AppendLog($"Rapport exporte: {dialog.FileName}");
    }

    private async Task ShowSelectedHistoryDetailsAsync()
    {
        if (_historyGrid.CurrentRow?.DataBoundItem is not HistoryEntry entry)
        {
            MessageBox.Show(this, "Selectionnez une entree d'historique.", "Historique", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var fullEntry = await _runtime.HistoryService.GetEntryByIdAsync(entry.Id).ConfigureAwait(true) ?? entry;
        using var form = new HistoryDetailsForm(fullEntry);
        form.ShowDialog(this);
    }

    private async Task<ManualWorkflowOutcome> ShowManualWorkflowAsync(string? actionType = null, string? preparedCommand = null, string? packageFolder = null)
    {
        var resolvedActionType = !string.IsNullOrWhiteSpace(actionType) ? actionType : _lastPreparedManualActionType;
        var resolvedPackageFolder = !string.IsNullOrWhiteSpace(packageFolder) ? packageFolder : _lastPreparedManualPackageFolder;
        var resolvedCommand = !string.IsNullOrWhiteSpace(preparedCommand) ? preparedCommand : _lastPreparedManualCommand;

        if (string.IsNullOrWhiteSpace(resolvedActionType) || !SupportsManualWorkflow(resolvedActionType))
        {
            MessageBox.Show(this, "Aucune action manuelle preparee n'est disponible.", "Workflow manuel", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return new ManualWorkflowOutcome(false, null);
        }

        if (string.IsNullOrWhiteSpace(resolvedPackageFolder) || !Directory.Exists(resolvedPackageFolder))
        {
            MessageBox.Show(this, "Le dossier du paquet n'est plus disponible.", "Workflow manuel", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return new ManualWorkflowOutcome(false, null);
        }

        using var form = new ManualBuildWorkflowForm(
            GetManualWorkflowName(resolvedActionType),
            resolvedPackageFolder,
            resolvedCommand,
            GetManualInstructionText(resolvedActionType),
            GetManualArtifactLabel(resolvedActionType),
            GetManualSelectArtifactButtonText(resolvedActionType));

        if (form.ShowDialog(this) != DialogResult.OK || !form.ManualActionConfirmed)
        {
            return new ManualWorkflowOutcome(false, null);
        }

        var confirmationMessage = string.IsNullOrWhiteSpace(form.GeneratedPackagePath)
            ? $"{GetManualHistoryLabel(resolvedActionType)} manuel confirme."
            : $"{GetManualHistoryLabel(resolvedActionType)} manuel confirme avec artifact: {form.GeneratedPackagePath}";

        var result = new CommandExecutionResult
        {
            ExecutedCommand = resolvedCommand ?? string.Empty,
            WorkingDirectory = resolvedPackageFolder,
            ExitCode = 0,
            IsSkipped = true,
            StandardOutput = form.GeneratedPackagePath,
            StartedAt = DateTimeOffset.Now,
            Duration = TimeSpan.Zero
        };

        AppendLog(confirmationMessage);
        await RegisterHistoryAsync($"{resolvedActionType}ManualConfirmed", true, resolvedPackageFolder, _selectedCatalogItem?.PackageId, confirmationMessage, result, _selectedCatalogItem?.PackageInfo.Version, _selectedCatalogItem?.PackageInfo.Version, form.GeneratedPackagePath, _currentValidationResult?.VerdictLabel).ConfigureAwait(true);
        return new ManualWorkflowOutcome(true, form.GeneratedPackagePath);
    }

    private async Task RefreshWaptStatusAsync()
    {
        try
        {
            _settings = await _runtime.SettingsService.LoadAsync().ConfigureAwait(true);
            var result = await _runtime.WaptCommandService.CheckWaptAvailabilityAsync().ConfigureAwait(true);
            _waptStatusValueLabel.Text = result.IsSuccess
                ? result.IsDryRun ? "WAPT: dry-run" : "WAPT: disponible"
                : IsConfiguredWaptPathAvailable(_settings.WaptExecutablePath) ? "WAPT: configure mais indisponible" : "WAPT: indisponible";
            _waptStatusValueLabel.ForeColor = ResolveResultColor(result);
        }
        catch
        {
            _waptStatusValueLabel.Text = "WAPT: indisponible";
            _waptStatusValueLabel.ForeColor = BlockedColor;
        }
    }

    private async Task RegisterHistoryAsync(string actionType, bool success, string packageFolder, string? packageName, string message, CommandExecutionResult? commandResult, string? versionBefore, string? versionAfter, string? waptArtifactPath, string? readinessVerdict)
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
            VersionAfter = versionAfter,
            WaptArtifactPath = waptArtifactPath,
            ReadinessVerdict = readinessVerdict
        }).ConfigureAwait(true);

        await LoadHistoryAsync().ConfigureAwait(true);
    }

    private async Task LoadHistoryAsync()
    {
        _historyEntries = await _runtime.HistoryService.GetRecentEntriesAsync().ConfigureAwait(true);
        _historyBindingSource.DataSource = _historyEntries.ToList();
        _historyGrid.DataSource = _historyBindingSource;
    }

    private void ConfigureCatalogGrid()
    {
        _catalogGrid.DefaultCellStyle.Font = new Font("Segoe UI", 9F);
        _catalogGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
        _catalogGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(239, 243, 249);
        _catalogGrid.EnableHeadersVisualStyles = false;
        _catalogGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _catalogGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(PackageCatalogItem.PackageId), HeaderText = "Package ID", FillWeight = 18 });
        _catalogGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(PackageCatalogItem.VisibleName), HeaderText = "Nom", FillWeight = 19 });
        _catalogGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(PackageCatalogItem.Version), HeaderText = "Version", FillWeight = 10 });
        _catalogGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(PackageCatalogItem.Category), HeaderText = "Type", FillWeight = 8 });
        _catalogGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(PackageCatalogItem.Maturity), HeaderText = "Maturite", FillWeight = 9 });
        _catalogGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(PackageCatalogItem.ReadinessLabel), HeaderText = "Readiness", FillWeight = 16 });
        _catalogGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(PackageCatalogItem.LastModifiedUtc), HeaderText = "Derniere modif", FillWeight = 12, DefaultCellStyle = new DataGridViewCellStyle { Format = "g" } });
        _catalogGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(PackageCatalogItem.PackageFolder), HeaderText = "Dossier", FillWeight = 25 });
        _catalogGrid.CellFormatting += (_, eventArgs) =>
        {
            if (_catalogGrid.Columns[eventArgs.ColumnIndex].DataPropertyName == nameof(PackageCatalogItem.LastModifiedUtc) && eventArgs.Value is DateTime date)
            {
                eventArgs.Value = date.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
                eventArgs.FormattingApplied = true;
            }
        };
    }

    private void ConfigureHistoryGrid()
    {
        _historyGrid.DefaultCellStyle.Font = new Font("Segoe UI", 8.75F);
        _historyGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
        _historyGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(239, 243, 249);
        _historyGrid.EnableHeadersVisualStyles = false;
        _historyGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _historyGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(HistoryEntry.Timestamp), HeaderText = "Date", FillWeight = 14, DefaultCellStyle = new DataGridViewCellStyle { Format = "g" } });
        _historyGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(HistoryEntry.ActionType), HeaderText = "Action", FillWeight = 10 });
        _historyGrid.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = nameof(HistoryEntry.Success), HeaderText = "OK", FillWeight = 5 });
        _historyGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(HistoryEntry.ReadinessVerdict), HeaderText = "Readiness", FillWeight = 12 });
        _historyGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(HistoryEntry.WaptArtifactPath), HeaderText = ".wapt", FillWeight = 18 });
        _historyGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(HistoryEntry.Message), HeaderText = "Message", FillWeight = 41 });
    }

    private void ReplaceCatalogItem(PackageCatalogItem originalItem, ValidationResult validationResult, PackageCatalogItem? replacement = null)
    {
        var updatedItem = replacement ?? new PackageCatalogItem
        {
            PackageId = originalItem.PackageInfo.PackageName ?? originalItem.PackageId,
            VisibleName = originalItem.PackageInfo.VisibleName ?? originalItem.VisibleName,
            Version = originalItem.PackageInfo.Version ?? originalItem.Version,
            Category = originalItem.PackageInfo.Category,
            Maturity = originalItem.PackageInfo.Maturity,
            ReadinessVerdict = validationResult.Verdict,
            ReadinessLabel = validationResult.VerdictLabel,
            LastModifiedUtc = originalItem.PackageInfo.LastModifiedUtc,
            PackageFolder = originalItem.PackageInfo.PackageFolder,
            PrimaryInstallerName = Path.GetFileName(originalItem.PackageInfo.InstallerPath ?? string.Empty),
            PackageInfo = originalItem.PackageInfo
        };

        var items = _catalogItems.ToList();
        var index = items.FindIndex(item => string.Equals(item.PackageFolder, originalItem.PackageFolder, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
        {
            items[index] = updatedItem;
            _catalogItems = items;
            RefreshCatalogGrid();
        }
    }

    private void AppendCommandResult(CommandExecutionResult result)
    {
        AppendLog(result.Summary);
        if (!string.IsNullOrWhiteSpace(result.ExecutedCommand))
        {
            AppendLog((result.IsDryRun || result.IsConfigurationBlocked ? "Commande preparee: " : "Commande: ") + result.ExecutedCommand);
        }

        if (result.RequiresExternalManualWorkflow)
        {
            AppendLog("Action manuelle requise dans un terminal externe.");
        }
        else if (result.ManualFallbackRecommended)
        {
            AppendLog("L'execution assistee n'est pas fiable. Un fallback manuel est propose.");
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
        SetActionResult($"Erreur: {message}", BlockedColor);
        AppendLog($"ERREUR: {message} {exception.Message}");
        await _runtime.LogService.LogErrorAsync(message, exception).ConfigureAwait(true);
        MessageBox.Show(this, exception.Message, "Erreur WaptStudio", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private void AppendLog(string message)
    {
        _logsTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        _logsTextBox.SelectionStart = _logsTextBox.TextLength;
        _logsTextBox.ScrollToCaret();
    }

    private void SetActionResult(string value, Color? color = null)
    {
        _lastActionResult = value;
        _actionResultValueLabel.Text = value;
        _actionResultValueLabel.ForeColor = color ?? InfoColor;
    }

    private void UpdateReadinessBadge(ReadinessVerdict verdict, string label)
    {
        _readinessBadgeLabel.Text = string.IsNullOrWhiteSpace(label) ? "Aucun verdict" : label;
        _readinessBadgeLabel.ForeColor = Color.White;
        _readinessBadgeLabel.BackColor = GetVerdictColor(verdict);
    }

    private static Color ResolveResultColor(CommandExecutionResult result)
    {
        if (result.IsDryRun)
        {
            return AccentColor;
        }

        if (result.RequiresExternalManualWorkflow || result.ManualFallbackRecommended)
        {
            return WarningColor;
        }

        if (result.IsConfigurationBlocked)
        {
            return WarningColor;
        }

        return result.IsSuccess ? ReadyColor : BlockedColor;
    }

    private static string BuildActionResultMessage(string actionType, CommandExecutionResult result)
    {
        if (result.IsDryRun)
        {
            return $"{actionType}: succes simule.";
        }

        if (result.RequiresExternalManualWorkflow)
        {
            return $"{actionType}: action manuelle requise.";
        }

        if (result.ManualFallbackRecommended)
        {
            return $"{actionType}: fallback manuel recommande.";
        }

        if (result.IsConfigurationBlocked)
        {
            return $"{actionType}: action bloquee. {result.Summary}";
        }

        if (result.WasInteractiveExecutionAttempted && result.IsSuccess)
        {
            return $"{actionType}: succes reel via execution assistee.";
        }

        return result.IsSuccess ? $"{actionType}: succes reel." : $"{actionType}: erreur reelle. {result.Summary}";
    }

    private static string BuildHistoryActionType(string actionType, CommandExecutionResult result)
        => result.IsDryRun ? $"{actionType}DryRun" : result.WasInteractiveExecutionAttempted ? $"{actionType}InteractiveExecuted" : actionType;

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

    private static string GetManualInstructionText(string actionType)
        => string.Equals(actionType, "Upload", StringComparison.OrdinalIgnoreCase)
            ? "Cette action peut demander une authentification WAPT interactive. Copiez la commande, ouvrez un terminal dans le dossier du paquet, saisissez les secrets uniquement quand WAPT les demande, puis revenez rattacher le resultat manuel a l'historique."
            : "Cette action peut demander un secret interactif. Copiez la commande, ouvrez un terminal dans le dossier du paquet, saisissez les secrets uniquement quand WAPT les demande, puis revenez rattacher le resultat manuel a l'historique.";

    private static string GetManualArtifactLabel(string actionType)
        => string.Equals(actionType, "Upload", StringComparison.OrdinalIgnoreCase)
            ? "Chemin du .wapt uploade"
            : "Chemin du .wapt associe";

    private static string GetManualSelectArtifactButtonText(string actionType)
        => string.Equals(actionType, "Upload", StringComparison.OrdinalIgnoreCase)
            ? "Selectionner le .wapt uploade"
            : "Selectionner le .wapt associe";

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

        var expected = ResolveExpectedWaptFilePath(packageFolder, allowExpectedPathWhenMissing);
        if (!string.IsNullOrWhiteSpace(expected))
        {
            return expected;
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
        var item = _selectedCatalogItem;
        var expectedName = item?.PackageInfo.ExpectedWaptFileName;
        var candidate = Directory.Exists(packageFolder)
            ? Directory.EnumerateFiles(packageFolder, "*.wapt", SearchOption.AllDirectories).OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault()
            : null;

        if (!string.IsNullOrWhiteSpace(expectedName))
        {
            var expectedExisting = Directory.Exists(packageFolder)
                ? Directory.EnumerateFiles(packageFolder, expectedName, SearchOption.AllDirectories).FirstOrDefault()
                : null;
            if (!string.IsNullOrWhiteSpace(expectedExisting))
            {
                return expectedExisting;
            }

            if (allowExpectedPathWhenMissing)
            {
                return Path.Combine(packageFolder, expectedName);
            }
        }

        return candidate;
    }

    private PackageCatalogItem RequireSelectedPackage()
        => _selectedCatalogItem ?? throw new InvalidOperationException("Selectionnez d'abord un paquet dans l'inventaire.");

    private void BrowseCatalogRootFolder(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Selectionner le dossier racine des paquets CD48",
            InitialDirectory = Directory.Exists(_catalogRootFolderTextBox.Text) ? _catalogRootFolderTextBox.Text : Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _catalogRootFolderTextBox.Text = dialog.SelectedPath;
        }
    }

    private static bool IsConfiguredWaptPathAvailable(string path)
        => !string.IsNullOrWhiteSpace(path) && Path.IsPathRooted(path) && File.Exists(path);

    private static string BuildPackageDetailsText(PackageCatalogItem item)
    {
        var package = item.PackageInfo;
        var builder = new StringBuilder();
        builder.AppendLine($"Package ID: {item.PackageId}");
        builder.AppendLine($"Nom visible: {item.VisibleName}");
        builder.AppendLine($"Version: {(string.IsNullOrWhiteSpace(item.Version) ? "Non detectee" : item.Version)}");
        builder.AppendLine($"Type: {item.Category.ToString().ToUpperInvariant()}");
        builder.AppendLine($"Maturite: {item.Maturity}");
        builder.AppendLine($"Derniere modification: {item.LastModifiedUtc.ToLocalTime():yyyy-MM-dd HH:mm}");
        builder.AppendLine($"Dossier: {item.PackageFolder}");
        builder.AppendLine($"setup.py: {package.SetupPyPath ?? "Absent"}");
        builder.AppendLine($"control: {package.ControlFilePath ?? "Absent"}");
        builder.AppendLine($"Installeur principal: {package.InstallerPath ?? "Absent"}");
        builder.AppendLine($"Installeur reference: {package.ReferencedInstallerName ?? "Non detecte"}");
        builder.AppendLine($".wapt attendu: {package.ExpectedWaptFileName ?? "Non calculable"}");

        if (package.DetectedExecutables.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Installateurs detectes:");
            foreach (var executable in package.DetectedExecutables)
            {
                builder.AppendLine($"- {Path.GetFileName(executable)}");
            }
        }

        if (package.Warnings.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Avertissements inspection:");
            foreach (var warning in package.Warnings)
            {
                builder.AppendLine($"- {warning}");
            }
        }

        return builder.ToString();
    }

    private static string BuildReadinessText(ValidationResult? validationResult)
    {
        if (validationResult is null)
        {
            return "Aucun readiness calcule pour le paquet selectionne.";
        }

        var builder = new StringBuilder();
        builder.AppendLine(validationResult.VerdictLabel);
        builder.AppendLine(validationResult.Summary);
        builder.AppendLine();
        builder.AppendLine($"Build possible: {(validationResult.BuildPossible ? "Oui" : "Non")}");
        builder.AppendLine($"Upload possible: {(validationResult.UploadPossible ? "Oui" : "Non")}");
        builder.AppendLine($"Audit possible: {(validationResult.AuditPossible ? "Oui" : "Non")}");
        builder.AppendLine($"Uninstall possible: {(validationResult.UninstallPossible ? "Oui" : "Non")}");

        if (validationResult.Issues.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Detail:");
            foreach (var issue in validationResult.Issues)
            {
                builder.AppendLine($"- [{issue.Severity}] {issue.Message}");
            }
        }

        return builder.ToString();
    }

    private static Color GetVerdictColor(ReadinessVerdict verdict)
        => verdict switch
        {
            ReadinessVerdict.ReadyForBuildUpload => ReadyColor,
            ReadinessVerdict.ReadyWithWarnings => WarningColor,
            _ => BlockedColor
        };

    private void PulseSelectionAccent()
    {
        _pulseState = !_pulseState;
        _catalogGrid.GridColor = _pulseState ? Color.FromArgb(230, 235, 243) : BorderColor;
    }

    private static Panel CreateCardPanel()
        => new()
        {
            Dock = DockStyle.Fill,
            BackColor = PanelColor,
            Margin = new Padding(0, 0, 0, 10),
            BorderStyle = BorderStyle.FixedSingle
        };

    private static Control CreateSectionCard(string title, string subtitle, Control content)
    {
        var card = CreateCardPanel();
        card.Padding = new Padding(14);

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = PanelColor };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(CreateSectionHeader(title, subtitle), 0, 0);
        layout.Controls.Add(content, 0, 1);
        card.Controls.Add(layout);
        return card;
    }

    private static Control CreateSectionHeader(string title, string subtitle)
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 1, AutoSize = true, BackColor = PanelColor, Margin = new Padding(0, 0, 0, 10) };
        panel.Controls.Add(new Label { Text = title, AutoSize = true, Font = new Font("Segoe UI Semibold", 11.5F, FontStyle.Bold), ForeColor = Color.FromArgb(33, 45, 69), Margin = new Padding(0, 0, 0, 2) }, 0, 0);
        panel.Controls.Add(new Label { Text = subtitle, AutoSize = true, ForeColor = InfoColor }, 0, 1);
        return panel;
    }

    private void StyleActionButtons()
    {
        foreach (var button in new[] { _scanCatalogButton, _analyzeButton, _replaceInstallerButton, _validateButton, _buildButton, _signButton, _uploadButton, _buildAndUploadButton, _auditButton, _uninstallButton, _restoreBackupButton, _openBackupFolderButton, _manualWorkflowButton, _saveReportButton, _historyDetailsButton, _settingsButton, _browseCatalogButton })
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = BorderColor;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(239, 244, 251);
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(227, 236, 248);
            button.BackColor = button == _scanCatalogButton || button == _validateButton || button == _buildButton || button == _uploadButton ? AccentColor : PanelColor;
            button.ForeColor = button.BackColor == AccentColor ? Color.White : Color.FromArgb(44, 56, 76);
            button.Padding = new Padding(10, 6, 10, 6);
            button.Margin = new Padding(0, 0, 8, 8);
        }
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

    private sealed record ManualWorkflowOutcome(bool Confirmed, string? ArtifactPath);

    private sealed record ActionHandlingOutcome(bool Completed, string? ArtifactPath);
}