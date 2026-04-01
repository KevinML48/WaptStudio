using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
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
    private const string DefaultCatalogRootFolder = @"C:\waptdev";

    private enum MainPage
    {
        Catalogue,
        Package,
        Replacement,
        Publication,
        History,
        Settings
    }

    private static readonly Color AccentColor = Color.FromArgb(32, 76, 178);
    private static readonly Color AccentSoftColor = Color.FromArgb(232, 240, 255);
    private static readonly Color RecommendedColor = Color.FromArgb(18, 122, 86);
    private static readonly Color RecommendedSoftColor = Color.FromArgb(227, 248, 240);
    private static readonly Color WarningSoftColor = Color.FromArgb(255, 245, 226);
    private static readonly Color DangerSoftColor = Color.FromArgb(255, 234, 236);
    private static readonly Color SurfaceColor = Color.FromArgb(237, 241, 247);
    private static readonly Color PanelColor = Color.White;
    private static readonly Color PanelAltColor = Color.FromArgb(248, 250, 253);
    private static readonly Color BorderColor = Color.FromArgb(214, 223, 235);
    private static readonly Color ReadyColor = Color.FromArgb(22, 163, 74);
    private static readonly Color WarningColor = Color.FromArgb(234, 149, 17);
    private static readonly Color BlockedColor = Color.FromArgb(220, 53, 53);
    private static readonly Color InfoColor = Color.FromArgb(82, 96, 120);
    private static readonly Color SubtleColor = Color.FromArgb(120, 137, 163);
    private static readonly Color SurfaceDarkColor = Color.FromArgb(226, 232, 241);
    private static readonly Color HeadingColor = Color.FromArgb(9, 18, 35);
    private static readonly Color CardHoverColor = Color.FromArgb(246, 249, 253);

    private readonly AppRuntime _runtime;
    private readonly TextBox _catalogRootFolderTextBox = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
    private readonly TextBox _searchTextBox = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
    private readonly ComboBox _categoryFilterComboBox = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Button _browseCatalogButton = new() { Text = "Parcourir", AutoSize = true };
    private readonly Button _scanCatalogButton = new() { Text = "Charger les paquets", AutoSize = true };
    private readonly Button _settingsButton = new() { Text = "Parametres", AutoSize = true };
    private readonly Button _updateCertificateSessionButton = new() { Text = "Mettre a jour le certificat", AutoSize = true };
    private readonly Button _updateServerSessionButton = new() { Text = "Mettre a jour les identifiants serveur", AutoSize = true };
    private readonly Button _clearSessionSecretsButton = new() { Text = "Effacer les secrets de session", AutoSize = true };
    private readonly DataGridView _catalogGrid = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AutoGenerateColumns = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, BackgroundColor = PanelColor, BorderStyle = BorderStyle.None, RowHeadersVisible = false };
    private readonly RichTextBox _packageSummaryTextBox = new() { Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = PanelColor };
    private readonly RichTextBox _assistantTextBox = new() { Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = PanelColor };
    private readonly RichTextBox _packageDetailsTextBox = new() { Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = PanelColor };
    private readonly RichTextBox _readinessTextBox = new() { Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = PanelColor };
    private readonly RichTextBox _logsTextBox = new() { Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = PanelColor };
    private readonly RichTextBox _packageOverviewTextBox = new() { Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = PanelColor };
    private readonly RichTextBox _replacementOverviewTextBox = new() { Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = PanelColor };
    private readonly RichTextBox _publicationOverviewTextBox = new() { Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = PanelColor };
    private readonly RichTextBox _settingsOverviewTextBox = new() { Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = PanelColor };
    private readonly RichTextBox _sessionOverviewTextBox = new() { Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = PanelColor };
    private readonly DataGridView _historyGrid = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AutoGenerateColumns = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, BackgroundColor = PanelColor, BorderStyle = BorderStyle.None, RowHeadersVisible = false };
    private readonly TabControl _activityTabControl = new() { Dock = DockStyle.Fill };
    private readonly Panel _pageHostPanel = new() { Dock = DockStyle.Fill, BackColor = SurfaceColor };
    private readonly Label _catalogSummaryLabel = new() { AutoSize = true, Text = "Aucun paquet charge", ForeColor = InfoColor };
    private readonly Label _selectedPackageLabel = new() { AutoSize = true, Text = "Aucun paquet selectionne", Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold), ForeColor = HeadingColor };
    private readonly Label _selectedPackageMetaLabel = new() { AutoSize = true, Text = "Selectionnez un paquet dans le catalogue.", ForeColor = SubtleColor, Margin = new Padding(0, 6, 0, 0) };
    private readonly Label _statusPackageValueLabel = new() { AutoSize = true, Text = "Aucun paquet choisi", ForeColor = InfoColor };
    private readonly Label _navigationContextTitleLabel = new() { AutoSize = true, Text = "Paquet en cours", Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(61, 82, 125), Margin = new Padding(0, 0, 0, 8) };
    private readonly Label _navigationContextValueLabel = new() { AutoSize = true, Text = "Aucun paquet choisi", Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold), ForeColor = HeadingColor, MaximumSize = new Size(178, 0), Margin = new Padding(0, 0, 0, 4) };
    private readonly Label _navigationContextMetaLabel = new() { AutoSize = true, Text = "Selectionnez un paquet pour afficher son contexte.", Font = new Font("Segoe UI", 8.9F, FontStyle.Regular), ForeColor = Color.FromArgb(68, 86, 117), MaximumSize = new Size(178, 0), Margin = new Padding(0, 0, 0, 0) };
    private readonly Label _navigationContextHintLabel = new() { AutoSize = true, Text = "La fiche detaillee et les actions utiles sont a droite.", MaximumSize = new Size(178, 0), ForeColor = Color.FromArgb(96, 112, 143), Margin = new Padding(0, 10, 0, 0) };
    private readonly Label _assistantVerdictBadgeLabel = new() { AutoSize = true, Text = "SYNTHESE DU PAQUET", ForeColor = AccentColor, BackColor = AccentSoftColor, Padding = new Padding(16, 8, 16, 8), Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold) };
    private readonly Label _nextStepTitleLabel = new() { AutoSize = true, Text = "Actions disponibles", Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold), ForeColor = HeadingColor };
    private readonly Label _nextStepDescriptionLabel = new() { AutoSize = true, Text = "Chargez le catalogue puis selectionnez un paquet.", ForeColor = InfoColor, MaximumSize = new Size(600, 0), Font = new Font("Segoe UI", 10F, FontStyle.Regular) };
    private readonly Label _actionResultValueLabel = new() { AutoSize = true, Text = "Aucune action" };
    private readonly Label _waptStatusValueLabel = new() { AutoSize = true, Text = "Inconnu" };
    private readonly Button _analyzeButton = new() { Text = "Relire le paquet", AutoSize = true };
    private readonly Button _replaceInstallerButton = new() { Text = "Remplacer l'installeur", AutoSize = true };
    private readonly Button _validateButton = new() { Text = "Verifier le paquet", AutoSize = true };
    private readonly Button _buildButton = new() { Text = "Construire le .wapt", AutoSize = true };
    private readonly Button _signButton = new() { Text = "Signer le .wapt", AutoSize = true };
    private readonly Button _uploadButton = new() { Text = "Publier...", AutoSize = true };
    private readonly Button _buildAndUploadButton = new() { Text = "Construire puis publier...", AutoSize = true };
    private readonly Button _restoreBackupButton = new() { Text = "Revenir a la derniere sauvegarde", AutoSize = true };
    private readonly Button _openBackupFolderButton = new() { Text = "Ouvrir les sauvegardes", AutoSize = true };
    private readonly Button _saveReportButton = new() { Text = "Exporter un resume", AutoSize = true };
    private readonly Button _showAdvancedDetailsButton = new() { Text = "Voir plus de details", AutoSize = true };
    private readonly Button _openPackagePageButton = new() { Text = "Ouvrir la fiche paquet", AutoSize = true };
    private readonly Button _catalogPageButton = new() { Text = "Catalogue", Dock = DockStyle.Top, Height = 48, FlatStyle = FlatStyle.Flat };
    private readonly Button _packagePageButton = new() { Text = "Paquet", Dock = DockStyle.Top, Height = 48, FlatStyle = FlatStyle.Flat };
    private readonly Button _replacementPageButton = new() { Text = "Remplacement", Dock = DockStyle.Top, Height = 48, FlatStyle = FlatStyle.Flat };
    private readonly Button _publicationPageButton = new() { Text = "Publication", Dock = DockStyle.Top, Height = 48, FlatStyle = FlatStyle.Flat };
    private readonly Button _historyPageButton = new() { Text = "Historique", Dock = DockStyle.Top, Height = 48, FlatStyle = FlatStyle.Flat };
    private readonly Button _settingsPageButton = new() { Text = "Parametres", Dock = DockStyle.Top, Height = 48, FlatStyle = FlatStyle.Flat };

    private readonly BindingSource _catalogBindingSource = new();
    private readonly BindingSource _historyBindingSource = new();
    private readonly System.Windows.Forms.Timer _uiPulseTimer = new() { Interval = 450 };
    private readonly System.Windows.Forms.Timer _loadingAnimTimer = new() { Interval = 350 };
    private readonly Panel _assistantDecisionPanel = new() { Dock = DockStyle.Top, BackColor = AccentSoftColor, Padding = new Padding(20, 18, 20, 18), Margin = new Padding(0, 0, 0, 14), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
    private readonly Dictionary<MainPage, Control> _pageViews = new();
    private readonly Dictionary<MainPage, Button> _pageButtons = new();
    private int _loadingDotCount;

    private AppSettings _settings = new();
    private IReadOnlyList<PackageCatalogItem> _catalogItems = Array.Empty<PackageCatalogItem>();
    private IReadOnlyList<HistoryEntry> _historyEntries = Array.Empty<HistoryEntry>();
    private PackageCatalogItem? _selectedCatalogItem;
    private ValidationResult? _currentValidationResult;
    private string? _lastPreparedManualActionType;
    private string? _lastPreparedManualCommand;
    private string? _lastPreparedManualPackageFolder;
    private string? _lastKnownWaptFilePath;
    private string? _lastKnownWaptFilePackageFolder;
    private string _lastActionResult = "Aucune action";
    private string _currentSortColumn = nameof(PackageCatalogItem.LastModifiedUtc);
    private bool _sortAscending;
    private bool _pulseState;
    private MainPage _currentPage = MainPage.Catalogue;

    public MainForm(AppRuntime runtime)
    {
        _runtime = runtime;

        Text = "WaptStudio - Inventaire CD48";
        Width = 1720;
        Height = 1020;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = SurfaceColor;
        Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
        MinimumSize = new Size(1480, 900);

        InitializeComponent();
        WireEvents();
    }

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);
        _settings = await _runtime.LoadSettingsAsync().ConfigureAwait(true);
        BindSettingsToForm();

        await RefreshWaptStatusAsync().ConfigureAwait(true);
        await ShowFirstRunEnvironmentDiagnosticsAsync().ConfigureAwait(true);
        await LoadHistoryAsync().ConfigureAwait(true);

        if (!string.IsNullOrWhiteSpace(_settings.CatalogRootFolder) && Directory.Exists(_settings.CatalogRootFolder))
        {
            await ScanCatalogAsync().ConfigureAwait(true);
        }

        AppendLog("WaptStudio est pret pour l'inventaire des paquets CD48.");
        _uiPulseTimer.Start();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _runtime.WaptSessionService.Dispose();
        base.OnFormClosed(e);
    }

    private void InitializeComponent()
    {
        _categoryFilterComboBox.Items.AddRange(["Tous", "MSI", "EXE", "AUTRES"]);
        _categoryFilterComboBox.SelectedIndex = 0;

        _showAdvancedDetailsButton.FlatStyle = FlatStyle.Flat;
        _showAdvancedDetailsButton.FlatAppearance.BorderColor = BorderColor;
        _showAdvancedDetailsButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(243, 246, 251);
        _showAdvancedDetailsButton.BackColor = PanelColor;
        _showAdvancedDetailsButton.ForeColor = HeadingColor;
        _showAdvancedDetailsButton.Padding = new Padding(14, 8, 14, 8);
        _showAdvancedDetailsButton.Font = new Font("Segoe UI", 9.5F);
        StyleNavigationButtons();

        ConfigureCatalogGrid();
        ConfigureHistoryGrid();
        ConfigureOverviewTextBoxes();
        StyleActionButtons();

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(20, 16, 20, 16),
            BackColor = SurfaceColor
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        root.Controls.Add(BuildHeaderPanel(), 0, 0);
        root.Controls.Add(BuildStatusStripPanel(), 0, 1);
        root.Controls.Add(BuildMainArea(), 0, 2);

        Controls.Add(root);
    }

    private Control BuildHeaderPanel()
    {
        var panel = CreateCardPanel();
        panel.Padding = new Padding(28, 22, 28, 20);
        panel.AutoSize = true;

        var layout = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 1, RowCount = 1, AutoSize = true, BackColor = PanelColor };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var topRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, AutoSize = true, BackColor = PanelColor };
        topRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        topRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var titleBlock = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, AutoSize = true, BackColor = PanelColor };
        titleBlock.Controls.Add(new Label
        {
            Text = "WaptStudio",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 21F, FontStyle.Bold),
            ForeColor = HeadingColor,
            Margin = new Padding(0, 0, 0, 2)
        }, 0, 0);
        titleBlock.Controls.Add(new Label
        {
            Text = "Catalogue, fiche paquet, changement d'installeur, construction, signature et publication sans faux statut global.",
            AutoSize = true,
            Font = new Font("Segoe UI", 10F, FontStyle.Regular),
            ForeColor = InfoColor,
            MaximumSize = new Size(760, 0)
        }, 0, 1);

        var statusRow = new FlowLayoutPanel { AutoSize = true, WrapContents = false, BackColor = PanelAltColor, Margin = new Padding(0, 6, 0, 0), Padding = new Padding(14, 10, 14, 10) };
        statusRow.Controls.Add(_catalogSummaryLabel);
        statusRow.Controls.Add(new Label { Text = "  ·  ", AutoSize = true, ForeColor = SubtleColor, Padding = new Padding(2, 0, 2, 0) });
        statusRow.Controls.Add(_waptStatusValueLabel);

        topRow.Controls.Add(titleBlock, 0, 0);
        topRow.Controls.Add(statusRow, 1, 0);

        layout.Controls.Add(topRow, 0, 0);

        panel.Controls.Add(layout);
        return panel;
    }

    private Control BuildStatusStripPanel()
    {
        var panel = CreateCardPanel();
        panel.Padding = new Padding(26, 12, 26, 12);
        panel.BackColor = SurfaceDarkColor;
        panel.AutoSize = true;

        var layout = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 4, AutoSize = true, BackColor = SurfaceDarkColor };
        for (var index = 0; index < 4; index++)
        {
            layout.ColumnStyles.Add(new ColumnStyle(index % 2 == 0 ? SizeType.AutoSize : SizeType.Percent, index % 2 == 0 ? 0 : 50));
        }

        layout.Controls.Add(new Label { Text = "Paquet", AutoSize = true, Padding = new Padding(0, 7, 10, 0), ForeColor = SubtleColor, Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold) }, 0, 0);
        layout.Controls.Add(_statusPackageValueLabel, 1, 0);
        layout.Controls.Add(new Label { Text = "Derniere action", AutoSize = true, Padding = new Padding(26, 7, 10, 0), ForeColor = SubtleColor, Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold) }, 2, 0);
        layout.Controls.Add(_actionResultValueLabel, 3, 0);

        panel.Controls.Add(layout);
        return panel;
    }

    private Control BuildMainArea()
    {
        var shell = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = SurfaceColor };
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 256));
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        shell.Controls.Add(BuildNavigationRail(), 0, 0);
        shell.Controls.Add(BuildPageHost(), 1, 0);
        return shell;
    }

    private Control BuildNavigationRail()
    {
        _pageButtons.Clear();
        _pageButtons[MainPage.Catalogue] = _catalogPageButton;
        _pageButtons[MainPage.Package] = _packagePageButton;
        _pageButtons[MainPage.Replacement] = _replacementPageButton;
        _pageButtons[MainPage.Publication] = _publicationPageButton;
        _pageButtons[MainPage.History] = _historyPageButton;
        _pageButtons[MainPage.Settings] = _settingsPageButton;

        var rail = CreateCardPanel();
        rail.Padding = new Padding(18, 20, 18, 20);

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = PanelColor };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var brand = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 1, AutoSize = true, BackColor = PanelColor, Margin = new Padding(0, 0, 0, 18) };
        brand.Controls.Add(new Label { Text = "Navigation", AutoSize = true, Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold), ForeColor = HeadingColor }, 0, 0);
        brand.Controls.Add(new Label { Text = "Le catalogue reste central, puis la fiche et les actions utiles prennent le relais.", AutoSize = true, MaximumSize = new Size(180, 0), ForeColor = InfoColor, Margin = new Padding(0, 6, 0, 0) }, 0, 1);

        var buttonsPanel = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 1, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, BackColor = PanelColor, Margin = new Padding(0) };
        buttonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        buttonsPanel.RowStyles.Clear();
        buttonsPanel.Controls.Add(_catalogPageButton, 0, 0);
        buttonsPanel.Controls.Add(_packagePageButton, 0, 1);
        buttonsPanel.Controls.Add(_replacementPageButton, 0, 2);
        buttonsPanel.Controls.Add(_publicationPageButton, 0, 3);
        buttonsPanel.Controls.Add(_historyPageButton, 0, 4);
        buttonsPanel.Controls.Add(_settingsPageButton, 0, 5);

        var contextCard = new Panel { Dock = DockStyle.Top, BackColor = Color.FromArgb(236, 243, 255), Padding = new Padding(16, 14, 16, 14), Margin = new Padding(0, 18, 0, 0), MinimumSize = new Size(0, 152), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        contextCard.Paint += (_, e) =>
        {
            using var shadowPen = new Pen(Color.FromArgb(204, 219, 245), 2);
            using var pen = new Pen(Color.FromArgb(126, 157, 214), 1);
            e.Graphics.DrawRectangle(shadowPen, 1, 1, contextCard.Width - 3, contextCard.Height - 3);
            e.Graphics.DrawRectangle(pen, 0, 0, contextCard.Width - 1, contextCard.Height - 1);
            using var accentBrush = new SolidBrush(Color.FromArgb(32, 76, 178));
            e.Graphics.FillRectangle(accentBrush, 0, 0, 6, contextCard.Height);
        };
        var contextLayout = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 1, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, BackColor = contextCard.BackColor, Margin = new Padding(0) };
        contextLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        contextLayout.Controls.Add(_navigationContextTitleLabel, 0, 0);
        contextLayout.Controls.Add(_navigationContextValueLabel, 0, 1);
        contextLayout.Controls.Add(_navigationContextMetaLabel, 0, 2);
        contextLayout.Controls.Add(_navigationContextHintLabel, 0, 3);
        contextCard.Controls.Add(contextLayout);

        layout.Controls.Add(brand, 0, 0);
        layout.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = PanelColor, Controls = { buttonsPanel } }, 0, 1);
        layout.Controls.Add(contextCard, 0, 2);
        rail.Controls.Add(layout);
        return rail;
    }

    private Control BuildPageHost()
    {
        _pageViews.Clear();
        _pageHostPanel.Controls.Clear();

        _pageViews[MainPage.Catalogue] = BuildCataloguePage();
        _pageViews[MainPage.Package] = BuildPackagePage();
        _pageViews[MainPage.Replacement] = BuildReplacementPage();
        _pageViews[MainPage.Publication] = BuildPublicationPage();
        _pageViews[MainPage.History] = BuildHistoryPage();
        _pageViews[MainPage.Settings] = BuildSettingsPage();

        foreach (var page in _pageViews.Values)
        {
            page.Dock = DockStyle.Fill;
            page.Visible = false;
            _pageHostPanel.Controls.Add(page);
        }

        ShowPage(_currentPage);
        return _pageHostPanel;
    }

    private Control BuildCataloguePage()
    {
        var page = CreatePageCanvas();

        var inventoryLauncherCard = CreateCatalogueLauncherCard();
        inventoryLauncherCard.Margin = new Padding(0, 0, 0, 18);

        var catalogueTableCard = BuildCatalogArea();
        catalogueTableCard.Margin = new Padding(0, 0, 0, 8);

        page.Controls.Add(catalogueTableCard);
        page.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 14, BackColor = SurfaceColor });
        page.Controls.Add(inventoryLauncherCard);
        return page;
    }

    private Control CreateCatalogueLauncherCard()
    {
        var controls = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 6, AutoSize = true, BackColor = PanelColor, Margin = new Padding(0, 0, 0, 12) };
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        controls.Controls.Add(new Label { Text = "Catalogue paquets", AutoSize = true, Padding = new Padding(0, 10, 10, 0), ForeColor = InfoColor, Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold) }, 0, 0);
        controls.Controls.Add(_catalogRootFolderTextBox, 1, 0);
        controls.Controls.Add(_browseCatalogButton, 2, 0);
        controls.Controls.Add(_scanCatalogButton, 3, 0);
        controls.Controls.Add(_openPackagePageButton, 4, 0);
        controls.Controls.Add(_settingsButton, 5, 0);

        return CreatePageSectionCard("Inventaire", "Le chargement principal parcourt automatiquement tout le dossier choisi, y compris les sous-dossiers.", controls);
    }

    private Control BuildPackagePage()
    {
        var page = CreatePageCanvas();

        var grid = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 2, AutoSize = true, BackColor = SurfaceColor };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        grid.Controls.Add(BuildPackageSummaryCard(), 0, 0);
        grid.Controls.Add(BuildAssistantCard(), 1, 0);

        page.Controls.Add(CreatePageSectionCard("Fiche paquet", "Une lecture rapide du paquet, de son installateur et des actions utiles.", CreateInfoPanel(_packageOverviewTextBox, 150)));
        page.Controls.Add(grid);
        return page;
    }

    private Control BuildReplacementPage()
    {
        var page = CreatePageCanvas();

        var actions = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = true, BackColor = PanelColor };
        actions.Controls.AddRange(new Control[] { _replaceInstallerButton, _restoreBackupButton });

        page.Controls.Add(CreatePageSectionCard("Changement d'installeur", "Relisez le paquet, changez l'installeur, puis revenez a la derniere sauvegarde si necessaire.", CreateInfoPanel(_replacementOverviewTextBox, 150)));
        page.Controls.Add(CreatePageSectionCard("Actions de remplacement", "Remplacez l'installeur avec apercu, puis restaurez une sauvegarde si necessaire.", actions));
        return page;
    }

    private Control BuildPublicationPage()
    {
        var page = CreatePageCanvas();

        var stepGrid = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 3, AutoSize = true, BackColor = SurfaceColor };
        stepGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));
        stepGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        stepGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));

        stepGrid.Controls.Add(CreateWorkflowStepCard("1. Relire", "Actualisez l'analyse technique du paquet avant de construire.", _analyzeButton, _validateButton), 0, 0);
        stepGrid.Controls.Add(CreateWorkflowStepCard("2. Construire", "Generez puis signez le .wapt quand le paquet est pret.", _buildButton, _signButton), 1, 0);
        stepGrid.Controls.Add(CreateWorkflowStepCard("3. Publier", "Preparez une publication via WAPT Console ou utilisez l'upload direct si votre environnement le permet vraiment.", _uploadButton, _buildAndUploadButton), 2, 0);

        page.Controls.Add(CreatePageSectionCard("Publication", "Le parcours reste simple: relire, construire, signer puis publier.", CreateInfoPanel(_publicationOverviewTextBox, 145)));
        page.Controls.Add(stepGrid);
        return page;
    }

    private Control BuildHistoryPage()
    {
        var page = CreatePageCanvas();
        page.Controls.Add(BuildActivityArea());
        return page;
    }

    private Control BuildSettingsPage()
    {
        var page = CreatePageCanvas();

        var actions = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = true, BackColor = PanelColor };
        actions.Controls.AddRange(new Control[] { _settingsButton, _saveReportButton, _showAdvancedDetailsButton });

        page.Controls.Add(CreatePageSectionCard("Environnement", "Retrouvez ici le dossier catalogue, l'etat WAPT et les reglages transverses utiles.", CreateInfoPanel(_settingsOverviewTextBox, 118)));
        page.Controls.Add(BuildSessionManagementCard());
        page.Controls.Add(CreatePageSectionCard("Outils et configuration", "Les reglages avances restent ici pour alleger l'ecran principal.", actions));
        return page;
    }

    private Control BuildSessionManagementCard()
    {
        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 1,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = PanelColor
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        content.Controls.Add(CreateInfoPanel(_sessionOverviewTextBox, 118), 0, 0);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = true, BackColor = PanelColor, Margin = new Padding(0, 12, 0, 0) };
        actions.Controls.AddRange(new Control[] { _updateCertificateSessionButton, _updateServerSessionButton, _clearSessionSecretsButton });
        content.Controls.Add(actions, 0, 1);

        return CreatePageSectionCard("Session WAPT", "Session en memoire pour le certificat et les identifiants serveur, effacee a la fermeture de WaptStudio.", content);
    }

    private static Control CreateWorkflowStepCard(string title, string description, params Button[] buttons)
    {
        var actions = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = true, BackColor = PanelColor };
        actions.Controls.AddRange(buttons);
        return CreateStackedSectionCard(title, description, actions);
    }

    private static Panel CreateInfoPanel(Control content, int minimumHeight)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.FromArgb(250, 252, 255),
            Padding = new Padding(14, 12, 14, 12),
            MinimumSize = new Size(0, minimumHeight)
        };
        panel.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(226, 232, 241), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
        };
        content.BackColor = panel.BackColor;
        content.Dock = DockStyle.Top;
        panel.Controls.Add(content);
        return panel;
    }

    private void ConfigureOverviewTextBoxes()
    {
        foreach (var textBox in new[] { _packageOverviewTextBox, _replacementOverviewTextBox, _publicationOverviewTextBox, _settingsOverviewTextBox, _sessionOverviewTextBox })
        {
            textBox.BorderStyle = BorderStyle.None;
            textBox.ReadOnly = true;
            textBox.ScrollBars = RichTextBoxScrollBars.None;
            textBox.WordWrap = true;
            textBox.DetectUrls = false;
            textBox.ShortcutsEnabled = false;
            textBox.TabStop = false;
            textBox.Margin = new Padding(0);
            textBox.Dock = DockStyle.Top;
            textBox.ContentsResized += HandleOverviewTextContentsResized;
        }
    }

    private static void HandleOverviewTextContentsResized(object? sender, ContentsResizedEventArgs e)
    {
        if (sender is not RichTextBox textBox)
        {
            return;
        }

        textBox.Height = Math.Max(32, e.NewRectangle.Height + 6);
    }

    private static Panel CreatePageCanvas()
    {
        var page = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = SurfaceColor,
            Padding = new Padding(0, 2, 0, 0)
        };
        page.HorizontalScroll.Enabled = false;
        page.HorizontalScroll.Visible = false;
        return page;
    }

    private void ShowPage(MainPage page)
    {
        _currentPage = page;

        foreach (var entry in _pageViews)
        {
            var visible = entry.Key == page;
            entry.Value.Visible = visible;
            if (visible)
            {
                entry.Value.BringToFront();
            }
        }

        foreach (var entry in _pageButtons)
        {
            var isActive = entry.Key == page;
            entry.Value.BackColor = isActive ? AccentColor : PanelColor;
            entry.Value.ForeColor = isActive ? Color.White : HeadingColor;
            entry.Value.FlatAppearance.BorderColor = isActive ? AccentColor : BorderColor;
        }
    }

    private void StyleNavigationButtons()
    {
        foreach (var button in new[] { _catalogPageButton, _packagePageButton, _replacementPageButton, _publicationPageButton, _historyPageButton, _settingsPageButton })
        {
            button.AutoSize = false;
            button.Dock = DockStyle.Top;
            button.Width = 180;
            button.MinimumSize = new Size(180, 48);
            button.TextAlign = ContentAlignment.MiddleLeft;
            button.Padding = new Padding(18, 0, 18, 0);
            button.Margin = new Padding(0, 0, 0, 10);
            button.BackColor = PanelColor;
            button.ForeColor = HeadingColor;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = BorderColor;
            button.FlatAppearance.MouseOverBackColor = PanelAltColor;
            button.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
        }
    }

    private Control BuildCatalogArea()
    {
        var card = CreateCardPanel();
        card.Padding = new Padding(24);
        card.Margin = new Padding(0, 0, 0, 8);
        card.Paint += (sender, e) =>
        {
            var p = (Panel)sender!;
            using var pen = new Pen(BorderColor, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
        };

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = PanelColor };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(CreateSectionHeader("Tableau des paquets", "La liste ci-dessous reste separee du bloc d'inventaire pour lire plus facilement les details utiles de chaque paquet."), 0, 0);

        var filterRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 4,
            BackColor = PanelAltColor,
            Margin = new Padding(0, 0, 0, 16),
            Padding = new Padding(16, 12, 16, 12)
        };
        filterRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        filterRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        filterRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        filterRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        filterRow.Controls.Add(new Label { Text = "\uD83D\uDD0D", AutoSize = true, Padding = new Padding(0, 7, 8, 0), ForeColor = SubtleColor, Font = new Font("Segoe UI", 10F) }, 0, 0);
        _searchTextBox.Width = 300;
        _searchTextBox.Font = new Font("Segoe UI", 10F);
        filterRow.Controls.Add(_searchTextBox, 1, 0);
        filterRow.Controls.Add(new Label { Text = "Type", AutoSize = true, Padding = new Padding(18, 7, 8, 0), ForeColor = SubtleColor, Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold) }, 2, 0);
        _categoryFilterComboBox.Width = 140;
        _categoryFilterComboBox.Font = new Font("Segoe UI", 10F);
        filterRow.Controls.Add(_categoryFilterComboBox, 3, 0);
        layout.Controls.Add(filterRow, 0, 1);

        layout.Controls.Add(_catalogGrid, 0, 2);

        card.Controls.Add(layout);
        return card;
    }

    private Control BuildDetailsArea()
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = SurfaceColor, Margin = new Padding(0) };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 32));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 28));

        var summaryRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = SurfaceColor, Margin = new Padding(0, 0, 0, 8) };
        summaryRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        summaryRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        summaryRow.Controls.Add(BuildPackageSummaryCard(), 0, 0);
        summaryRow.Controls.Add(BuildAssistantCard(), 1, 0);

        layout.Controls.Add(summaryRow, 0, 0);
        layout.Controls.Add(BuildActionFamiliesCard(), 0, 1);
        layout.Controls.Add(BuildActivityArea(), 0, 2);
        return layout;
    }

    private void WireEvents()
    {
        _catalogPageButton.Click += (_, _) => ShowPage(MainPage.Catalogue);
        _packagePageButton.Click += (_, _) => ShowPage(MainPage.Package);
        _replacementPageButton.Click += (_, _) => ShowPage(MainPage.Replacement);
        _publicationPageButton.Click += (_, _) => ShowPage(MainPage.Publication);
        _historyPageButton.Click += (_, _) => ShowPage(MainPage.History);
        _settingsPageButton.Click += (_, _) => ShowPage(MainPage.Settings);
        _openPackagePageButton.Click += (_, _) => ShowPage(MainPage.Package);
        _browseCatalogButton.Click += BrowseCatalogRootFolder;
        _scanCatalogButton.Click += async (_, _) => await ScanCatalogAsync().ConfigureAwait(true);
        _settingsButton.Click += async (_, _) => await OpenSettingsAsync().ConfigureAwait(true);
        _searchTextBox.TextChanged += (_, _) => RefreshCatalogGrid();
        _categoryFilterComboBox.SelectedIndexChanged += (_, _) => RefreshCatalogGrid();
        _catalogGrid.SelectionChanged += async (_, _) => await HandleSelectedCatalogItemChangedAsync().ConfigureAwait(true);
        _catalogGrid.ColumnHeaderMouseClick += (_, eventArgs) => ApplyCatalogSort(_catalogGrid.Columns[eventArgs.ColumnIndex].DataPropertyName);
        _historyGrid.CellDoubleClick += async (_, _) => await ShowSelectedHistoryDetailsAsync().ConfigureAwait(true);
        _analyzeButton.Click += async (_, _) => await AnalyzeSelectedPackageAsync().ConfigureAwait(true);
        _replaceInstallerButton.Click += async (_, _) => await ReplaceInstallerAsync().ConfigureAwait(true);
        _validateButton.Click += async (_, _) => await ValidateSelectedPackageAsync(includeWaptValidation: true).ConfigureAwait(true);
        _buildButton.Click += async (_, _) => await ExecuteBuildAsync().ConfigureAwait(true);
        _signButton.Click += async (_, _) => await ExecuteSignAsync().ConfigureAwait(true);
        _uploadButton.Click += async (_, _) => await ExecutePreparePublicationAsync().ConfigureAwait(true);
        _buildAndUploadButton.Click += async (_, _) => await ExecuteBuildAndUploadAsync().ConfigureAwait(true);
        _restoreBackupButton.Click += async (_, _) => await RestoreLatestBackupAsync().ConfigureAwait(true);
        _openBackupFolderButton.Click += (_, _) => OpenFolder(AppPaths.ResolveBackupsDirectory(_settings));
        _updateCertificateSessionButton.Click += (_, _) => OpenCertificateSessionEditor();
        _updateServerSessionButton.Click += (_, _) => OpenServerSessionEditor();
        _clearSessionSecretsButton.Click += (_, _) => ClearSessionSecrets(interactive: true);
        _saveReportButton.Click += async (_, _) => await SaveReportAsync().ConfigureAwait(true);
        _showAdvancedDetailsButton.Click += (_, _) =>
        {
            ShowPage(MainPage.History);
            _activityTabControl.SelectedIndex = 2;
        };
        _uiPulseTimer.Tick += (_, _) => PulseSelectionAccent();
        _loadingAnimTimer.Tick += (_, _) =>
        {
            _loadingDotCount = (_loadingDotCount + 1) % 4;
            var dots = new string('.', _loadingDotCount);
            _scanCatalogButton.Text = $"Chargement{dots}";
            var alpha = 180 + (int)(75 * Math.Sin(_loadingDotCount * Math.PI / 2));
            _scanCatalogButton.BackColor = Color.FromArgb(alpha, AccentColor.R, AccentColor.G, AccentColor.B);
        };
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
        var resolvedRoot = ResolveCatalogRootFolder(_settings);
        _catalogRootFolderTextBox.Text = resolvedRoot;
        _settings.CatalogRootFolder = resolvedRoot;
        UpdatePageOverviewTexts(_selectedCatalogItem, _currentValidationResult);
    }

    private async Task ScanCatalogAsync()
    {
        try
        {
            var rootFolder = ResolveCatalogRootFolder(_settings, _catalogRootFolderTextBox.Text.Trim());
            if (string.IsNullOrWhiteSpace(rootFolder) || !Directory.Exists(rootFolder))
            {
                throw new DirectoryNotFoundException("Le dossier catalogue est introuvable. WaptStudio attend par defaut C:\\waptdev.");
            }

            _scanCatalogButton.Text = "Chargement...";
            _scanCatalogButton.Enabled = false;
            _loadingDotCount = 0;
            _loadingAnimTimer.Start();
            _catalogSummaryLabel.Text = "Scan en cours...";
            AppendLog($"Scan catalogue complet: {rootFolder}");
            _settings.CatalogRootFolder = rootFolder;
            _catalogRootFolderTextBox.Text = rootFolder;
            _settings.CatalogScanRecursively = true;
            await _runtime.SettingsService.SaveAsync(_settings).ConfigureAwait(true);

            _catalogItems = await _runtime.PackageCatalogService
                .ScanAsync(rootFolder, recursive: true, semiRecursiveDepth: _settings.CatalogSemiRecursiveDepth)
                .ConfigureAwait(true);

            ApplyCatalogSort(_currentSortColumn, preserveDirection: true);
            _catalogSummaryLabel.Text = $"{_catalogItems.Count} paquet(s) detecte(s)";
            SetActionResult("Inventaire charge.", InfoColor);
            AppendLog($"Catalogue charge: {_catalogItems.Count} paquet(s) detecte(s) dans {rootFolder}.");
            UpdatePageOverviewTexts(_selectedCatalogItem, _currentValidationResult);
        }
        catch (Exception ex)
        {
            await HandleUiOperationErrorAsync("Scan catalogue impossible.", ex).ConfigureAwait(true);
        }
        finally
        {
            _loadingAnimTimer.Stop();
            _scanCatalogButton.Text = "Charger les paquets";
            _scanCatalogButton.Enabled = true;
            _scanCatalogButton.BackColor = AccentColor;
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
            nameof(PackageCatalogItem.PackageFolder) => ascending ? source.OrderBy(item => item.PackageFolder, StringComparer.OrdinalIgnoreCase) : source.OrderByDescending(item => item.PackageFolder, StringComparer.OrdinalIgnoreCase),
            _ => ascending ? source.OrderBy(item => item.LastModifiedUtc) : source.OrderByDescending(item => item.LastModifiedUtc)
        };
    }

    private async Task HandleSelectedCatalogItemChangedAsync()
    {
        var previousPackageFolder = _selectedCatalogItem?.PackageFolder;
        if (_catalogGrid.CurrentRow?.DataBoundItem is not PackageCatalogItem item)
        {
            return;
        }

        _selectedCatalogItem = item;
        _lastKnownWaptFilePackageFolder = item.PackageFolder;
        _lastKnownWaptFilePath = ResolveExpectedWaptFilePath(item.PackageInfo.PackageFolder, allowExpectedPathWhenMissing: false);

        if (_currentValidationResult is null || !_currentValidationResult.Issues.Any() || !string.Equals(item.PackageFolder, previousPackageFolder, StringComparison.OrdinalIgnoreCase))
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
            _selectedPackageLabel.ForeColor = SubtleColor;
            _selectedPackageMetaLabel.Text = "Selectionnez un paquet dans le catalogue.";
            _selectedPackageMetaLabel.ForeColor = SubtleColor;
            _statusPackageValueLabel.Text = "Aucun paquet choisi";
            _navigationContextValueLabel.Text = "Aucun paquet choisi";
            _navigationContextMetaLabel.Text = "Selectionnez un paquet pour afficher son contexte.";
            _assistantVerdictBadgeLabel.Text = "SYNTHESE DU PAQUET";
            _assistantVerdictBadgeLabel.BackColor = AccentSoftColor;
            _assistantVerdictBadgeLabel.ForeColor = AccentColor;
            _assistantDecisionPanel.BackColor = PanelAltColor;
            _packageSummaryTextBox.Text = string.Empty;
            _assistantTextBox.Text = "Selectionnez un paquet dans le catalogue pour afficher une synthese technique factuelle et les actions disponibles.";
            _packageDetailsTextBox.Text = string.Empty;
            _readinessTextBox.Text = string.Empty;
            _nextStepTitleLabel.Text = "Choisir un paquet";
            _nextStepDescriptionLabel.Text = "Chargez le catalogue puis selectionnez un paquet pour commencer.";
            UpdatePageOverviewTexts(null, null);
            StyleActionButtons();
            return;
        }

        _selectedPackageLabel.Text = string.IsNullOrWhiteSpace(item.VisibleName) ? item.PackageId : item.VisibleName;
        _selectedPackageLabel.ForeColor = HeadingColor;
        _selectedPackageMetaLabel.Text = $"{item.PackageId}  \u00b7  Version {(string.IsNullOrWhiteSpace(item.Version) ? "non detectee" : item.Version)}  \u00b7  {item.Category.ToString().ToUpperInvariant()}  \u00b7  {item.Maturity}";
        _selectedPackageMetaLabel.ForeColor = InfoColor;
        _statusPackageValueLabel.Text = $"{item.PackageId} - {item.VisibleName}";
        _navigationContextValueLabel.Text = item.PackageId;
        _navigationContextMetaLabel.Text = string.IsNullOrWhiteSpace(item.VisibleName)
            ? $"Version {(string.IsNullOrWhiteSpace(item.Version) ? "non detectee" : item.Version)}  ·  {item.Maturity}"
            : $"{item.VisibleName}\r\nVersion {(string.IsNullOrWhiteSpace(item.Version) ? "non detectee" : item.Version)}  ·  {item.Maturity}";
        _assistantVerdictBadgeLabel.Text = "SYNTHESE DU PAQUET";
        _assistantVerdictBadgeLabel.BackColor = AccentSoftColor;
        _assistantVerdictBadgeLabel.ForeColor = AccentColor;
        _assistantDecisionPanel.BackColor = PanelAltColor;
        _packageSummaryTextBox.Text = BuildPackageSummaryText(item, validationResult);
        _assistantTextBox.Text = BuildAssistantText(item, validationResult);
        _packageDetailsTextBox.Text = BuildPackageDetailsText(item);
        _readinessTextBox.Text = BuildReadinessText(item, validationResult);

        var nextStep = BuildNextStepGuidance(item, validationResult);
        _nextStepTitleLabel.Text = nextStep.Title;
        _nextStepDescriptionLabel.Text = nextStep.Description;
        UpdatePageOverviewTexts(item, validationResult);
        StyleActionButtons(nextStep.RecommendedButton);
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
            var suggestedVersion = PackageVersioning.InferVersionFromInstallerFileName(dialog.FileName);
            using var versionForm = new PackageVersionStrategyForm(item.PackageInfo, Path.GetFileName(dialog.FileName), suggestedVersion);
            if (versionForm.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            var versionSelection = versionForm.Selection;
            var plan = await _runtime.PackageUpdateService.PreviewReplacementAsync(item.PackageInfo, dialog.FileName, versionSelection).ConfigureAwait(true);
            using (var previewForm = new PackageSynchronizationPreviewForm(plan))
            {
                if (previewForm.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }
            }

            var result = await _runtime.PackageUpdateService.ReplaceInstallerAsync(item.PackageInfo, dialog.FileName, versionSelection).ConfigureAwait(true);
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
            _lastKnownWaptFilePackageFolder = updatedItem.PackageFolder;
            _lastKnownWaptFilePath = ResolveExpectedWaptFilePath(updatedItem.PackageFolder, allowExpectedPathWhenMissing: false);

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

    private async Task<PackageCatalogItem> RefreshSelectedPackageStateAsync(bool includeValidation)
    {
        var item = RequireSelectedPackage();
        var refreshedPackage = await _runtime.PackageInspectorService.AnalyzePackageAsync(item.PackageFolder).ConfigureAwait(true);
        var validationResult = includeValidation || _currentValidationResult is null
            ? await _runtime.PackageValidationService.ValidateAsync(refreshedPackage.PackageFolder, refreshedPackage, includeWaptValidation: false).ConfigureAwait(true)
            : _currentValidationResult;

        var updatedItem = new PackageCatalogItem
        {
            PackageId = refreshedPackage.PackageName ?? item.PackageId,
            VisibleName = refreshedPackage.VisibleName ?? item.VisibleName,
            Version = refreshedPackage.Version ?? item.Version,
            Category = refreshedPackage.Category,
            Maturity = refreshedPackage.Maturity,
            ReadinessVerdict = validationResult.Verdict,
            ReadinessLabel = validationResult.VerdictLabel,
            LastModifiedUtc = refreshedPackage.LastModifiedUtc,
            PackageFolder = refreshedPackage.PackageFolder,
            PrimaryInstallerName = Path.GetFileName(refreshedPackage.InstallerPath ?? string.Empty),
            PackageInfo = refreshedPackage
        };

        ReplaceCatalogItem(item, validationResult, updatedItem);
        _selectedCatalogItem = updatedItem;
        _currentValidationResult = validationResult;
        RenderSelectedPackage(updatedItem, validationResult);
        return updatedItem;
    }

    private async Task<(PackageCatalogItem Item, ValidationResult Validation, PublicationPreparationResult Preparation)> EvaluatePublicationPreparationAsync(string? explicitWaptFilePath = null)
    {
        var item = await RefreshSelectedPackageStateAsync(includeValidation: true).ConfigureAwait(true);
        var validation = _currentValidationResult
            ?? await _runtime.PackageValidationService.ValidateAsync(item.PackageFolder, item.PackageInfo, includeWaptValidation: false).ConfigureAwait(true);
        var preparation = PublicationPreparation.Evaluate(item.PackageFolder, item.PackageInfo, validation, _settings, explicitWaptFilePath);
        return (item, validation, preparation);
    }

    private async Task<bool> ExecutePreparePublicationAsync(string? explicitWaptFilePath = null)
    {
        try
        {
            var context = await EvaluatePublicationPreparationAsync(explicitWaptFilePath).ConfigureAwait(true);
            var item = context.Item;
            var validation = context.Validation;
            var preparation = context.Preparation;

            if (preparation.RecommendedMode == PublicationMode.WaptConsole)
            {
                await RegisterHistoryAsync(
                    PublicationPreparation.GetRecommendationHistoryAction(preparation),
                    true,
                    item.PackageFolder,
                    item.PackageId,
                    preparation.RecommendationMessage,
                    null,
                    item.PackageInfo.Version,
                    item.PackageInfo.Version,
                    preparation.WaptFilePath,
                    validation.VerdictLabel).ConfigureAwait(true);
            }

            if (!preparation.CanPrepareForConsolePublish)
            {
                AppendLog(preparation.StatusMessage);
                SetActionResult(preparation.StatusMessage, preparation.PackageReady ? WarningColor : BlockedColor);
                await RegisterHistoryAsync(
                    PublicationPreparation.GetPreparationHistoryAction(preparation),
                    false,
                    item.PackageFolder,
                    item.PackageId,
                    preparation.StatusMessage,
                    null,
                    item.PackageInfo.Version,
                    item.PackageInfo.Version,
                    preparation.WaptFilePath,
                    validation.VerdictLabel).ConfigureAwait(true);

                using var blockedForm = new PublicationSummaryForm(preparation);
                blockedForm.ShowDialog(this);
                return false;
            }

            using var form = new PublicationSummaryForm(preparation);
            if (form.ShowDialog(this) != DialogResult.OK)
            {
                return false;
            }

            if (form.SelectedAction == PublicationSummaryAction.MarkForWaptConsole)
            {
                var message = $"Le paquet est pret a etre publie via WAPT Console. Package: {item.PackageId}, Version: {item.PackageInfo.Version}, .wapt: {preparation.WaptFilePath}";
                AppendLog(message);
                SetActionResult("Paquet pret pour WAPT Console.", ReadyColor);
                await RegisterHistoryAsync(
                    PublicationPreparation.GetPreparationHistoryAction(preparation),
                    true,
                    item.PackageFolder,
                    item.PackageId,
                    message,
                    null,
                    item.PackageInfo.Version,
                    item.PackageInfo.Version,
                    preparation.WaptFilePath,
                    validation.VerdictLabel).ConfigureAwait(true);
                return true;
            }

            if (form.SelectedAction == PublicationSummaryAction.DirectUpload)
            {
                return await ExecuteUploadAsync(preparation.WaptFilePath).ConfigureAwait(true);
            }

            return false;
        }
        catch (Exception ex)
        {
            await HandleUiOperationErrorAsync("Preparation de publication impossible.", ex).ConfigureAwait(true);
            return false;
        }
    }

    private async Task<bool> ExecuteBuildAsync(WaptExecutionContext? providedContext = null)
    {
        try
        {
            var item = await RefreshSelectedPackageStateAsync(includeValidation: false).ConfigureAwait(true);
            var ownsContext = providedContext is null;
            var executionContext = providedContext;

            try
            {
                if (!_settings.DryRunEnabled
                    && !EnsureExecutionContext(
                        ref executionContext,
                        "Build WAPT assiste",
                        "Saisissez le mot de passe du certificat pour tenter le build assiste. Si WAPT ne supporte pas l'automatisation non interactive, WaptStudio preparera un workflow manuel securise.",
                        requireCertificatePassword: true,
                        requireAdminCredentials: false))
                {
                    return false;
                }

                var result = await _runtime.WaptCommandService.BuildPackageAsync(item.PackageFolder, executionContext).ConfigureAwait(true);
                var outcome = await HandleActionResultAsync("Build", item, result, executionContext?.GetSensitiveValues(), null).ConfigureAwait(true);
                _lastKnownWaptFilePath = outcome.ArtifactPath
                    ?? ExtractWaptFilePathFromOutput(result.StandardOutput)
                    ?? ResolveExpectedWaptFilePath(item.PackageFolder, allowExpectedPathWhenMissing: result.IsDryRun);
                _lastKnownWaptFilePackageFolder = outcome.ArtifactPath is null && string.IsNullOrWhiteSpace(_lastKnownWaptFilePath)
                    ? _lastKnownWaptFilePackageFolder
                    : item.PackageFolder;
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
            var item = await RefreshSelectedPackageStateAsync(includeValidation: false).ConfigureAwait(true);
            var ownsContext = providedContext is null;
            var executionContext = providedContext;

            try
            {
                if (!_settings.DryRunEnabled
                    && !EnsureExecutionContext(
                        ref executionContext,
                        "Signature WAPT assistee",
                        "Saisissez le mot de passe du certificat pour tenter la signature assistee. Si WAPT refuse l'automatisation non interactive, WaptStudio basculera vers un workflow manuel.",
                        requireCertificatePassword: true,
                        requireAdminCredentials: false))
                {
                    return false;
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
            var item = await RefreshSelectedPackageStateAsync(includeValidation: false).ConfigureAwait(true);
            var selection = ResolveUploadWaptSelection(item.PackageFolder, explicitWaptFilePath, allowExpectedPathWhenMissing: _settings.DryRunEnabled);
            var waptFilePath = selection.ResolvedPath;

            if (string.IsNullOrWhiteSpace(waptFilePath))
            {
                var chooseManually = MessageBox.Show(this, "Aucun fichier .wapt coherent n'a ete trouve pour ce paquet. Voulez-vous en selectionner un manuellement ?", "Upload direct", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
                if (!chooseManually)
                {
                    await RegisterHistoryAsync("DirectUploadFailed", false, item.PackageFolder, item.PackageId, "Upload direct bloque: aucun .wapt coherent detecte.", null, item.PackageInfo.Version, item.PackageInfo.Version, null, _currentValidationResult?.VerdictLabel).ConfigureAwait(true);
                    return false;
                }

                using var dialog = new OpenFileDialog
                {
                    Filter = "Paquets WAPT (*.wapt)|*.wapt|Tous les fichiers (*.*)|*.*",
                    Title = "Selectionner le paquet .wapt a uploader",
                    FileName = selection.ExpectedName ?? string.Empty,
                    InitialDirectory = Directory.Exists(item.PackageFolder) ? item.PackageFolder : Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                };

                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return false;
                }

                if (!IsCoherentWaptForPackage(Path.GetFileName(dialog.FileName), item.PackageInfo.PackageName ?? item.PackageId, item.PackageInfo.Version ?? item.Version))
                {
                    MessageBox.Show(this, "Le fichier selectionne ne correspond pas au package/version courants.", "Upload direct", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    await RegisterHistoryAsync("DirectUploadFailed", false, item.PackageFolder, item.PackageId, "Upload direct bloque: .wapt manuel incoherent.", null, item.PackageInfo.Version, item.PackageInfo.Version, dialog.FileName, _currentValidationResult?.VerdictLabel).ConfigureAwait(true);
                    return false;
                }

                waptFilePath = dialog.FileName;
                selection = selection with { ResolvedPath = waptFilePath, CandidateName = Path.GetFileName(dialog.FileName), ExactMatch = string.Equals(Path.GetFileName(dialog.FileName), selection.ExpectedName, StringComparison.OrdinalIgnoreCase) };
            }

            var expectedWaptName = selection.ExpectedName;
            var candidateName = selection.CandidateName;

            if (!_settings.DryRunEnabled)
            {
                if (!File.Exists(waptFilePath))
                {
                    MessageBox.Show(this, $"Le fichier .wapt a uploader est introuvable: {waptFilePath}", "Upload direct", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    await RegisterHistoryAsync("DirectUploadFailed", false, item.PackageFolder, item.PackageId, $"Upload direct bloque: fichier .wapt introuvable ({waptFilePath})", null, item.PackageInfo.Version, item.PackageInfo.Version, null, _currentValidationResult?.VerdictLabel).ConfigureAwait(true);
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(expectedWaptName) && !string.Equals(Path.GetFileName(waptFilePath), expectedWaptName, StringComparison.OrdinalIgnoreCase))
                {
                    var mismatchMessage = $"Upload direct bloque: le .wapt detecte ({Path.GetFileName(waptFilePath)}) ne correspond pas au nom attendu ({expectedWaptName}).";

                    if (MessageBox.Show(this, mismatchMessage + "\r\nVoulez-vous selectionner manuellement le bon .wapt ?", "Upload direct", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    {
                        using var dialog = new OpenFileDialog
                        {
                            Filter = "Paquets WAPT (*.wapt)|*.wapt|Tous les fichiers (*.*)|*.*",
                            Title = "Selectionner le paquet .wapt a uploader",
                            FileName = expectedWaptName,
                            InitialDirectory = Directory.Exists(item.PackageFolder) ? item.PackageFolder : Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                        };

                        if (dialog.ShowDialog(this) != DialogResult.OK)
                        {
                            return false;
                        }

                        if (!string.Equals(Path.GetFileName(dialog.FileName), expectedWaptName, StringComparison.OrdinalIgnoreCase))
                        {
                            MessageBox.Show(this, "Le fichier selectionne ne correspond toujours pas au nom attendu.", "Upload direct", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            await RegisterHistoryAsync("DirectUploadFailed", false, item.PackageFolder, item.PackageId, mismatchMessage, null, item.PackageInfo.Version, item.PackageInfo.Version, dialog.FileName, _currentValidationResult?.VerdictLabel).ConfigureAwait(true);
                            return false;
                        }

                        waptFilePath = dialog.FileName;
                        candidateName = Path.GetFileName(dialog.FileName);
                    }
                    else
                    {
                        await RegisterHistoryAsync("DirectUploadFailed", false, item.PackageFolder, item.PackageId, mismatchMessage, null, item.PackageInfo.Version, item.PackageInfo.Version, waptFilePath, _currentValidationResult?.VerdictLabel).ConfigureAwait(true);
                        return false;
                    }
                }
            }

            var selectionText = BuildUploadSelectionText(expectedWaptName, candidateName, waptFilePath);
            AppendLog(selectionText);
            SetActionResult(selectionText.Replace("\r\n", " | "), InfoColor);

            var ownsContext = providedContext is null;
            var executionContext = providedContext;
            try
            {
                if (!_settings.DryRunEnabled
                    && !EnsureExecutionContext(
                        ref executionContext,
                        "Upload direct WAPT authentifie",
                        "Saisissez l'identifiant administrateur WAPT et le mot de passe associe pour tenter l'upload direct assiste. Si cela n'est pas fiable, utilisez plutot le mode WAPT Console.",
                        requireCertificatePassword: false,
                        requireAdminCredentials: true))
                {
                    return false;
                }

                if (executionContext is not null)
                {
                    executionContext.WaptFilePath = waptFilePath;
                }
                _lastKnownWaptFilePath = waptFilePath;
                _lastKnownWaptFilePackageFolder = item.PackageFolder;
                await RegisterHistoryAsync("DirectUploadPrepared", true, item.PackageFolder, item.PackageId, $"Upload direct prepare avec: {waptFilePath}", null, item.PackageInfo.Version, item.PackageInfo.Version, waptFilePath, _currentValidationResult?.VerdictLabel).ConfigureAwait(true);
                AppendLog($"Upload direct cible: {waptFilePath}");
                var result = await _runtime.WaptCommandService.UploadPackageAsync(item.PackageFolder, waptFilePath, executionContext).ConfigureAwait(true);
                var outcome = await HandleActionResultAsync("DirectUpload", item, result, executionContext?.GetSensitiveValues(), waptFilePath).ConfigureAwait(true);
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
            await HandleUiOperationErrorAsync("Upload direct impossible.", ex).ConfigureAwait(true);
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
                var item = await RefreshSelectedPackageStateAsync(includeValidation: false).ConfigureAwait(true);

                if (!_settings.DryRunEnabled
                    && !EnsureExecutionContext(
                        ref executionContext,
                        "Construire puis publier",
                        "Saisissez uniquement le mot de passe certificat pour construire le vrai .wapt. La publication finale pourra ensuite passer par WAPT Console ou, si votre environnement le permet, par upload direct.",
                        requireCertificatePassword: true,
                        requireAdminCredentials: false))
                {
                    return;
                }

                await RegisterHistoryAsync("BuildAndPublishPrepared", true, item.PackageFolder, item.PackageId, "Workflow construction puis publication demarre.", null, item.PackageInfo.Version, item.PackageInfo.Version, null, _currentValidationResult?.VerdictLabel).ConfigureAwait(true);

                var buildCompleted = await ExecuteBuildAsync(executionContext).ConfigureAwait(true);
                if (!buildCompleted)
                {
                    await RegisterHistoryAsync("BuildAndPublishFailed", false, item.PackageFolder, item.PackageId, "Construction puis publication interrompues: le build n'a pas abouti.", null, item.PackageInfo.Version, item.PackageInfo.Version, null, _currentValidationResult?.VerdictLabel).ConfigureAwait(true);
                    return;
                }

                await RegisterHistoryAsync("BuildAndPublishBuildSucceeded", true, item.PackageFolder, item.PackageId, $"Build reussi. Fichier .wapt: {_lastKnownWaptFilePath ?? "indetermine"}", null, item.PackageInfo.Version, item.PackageInfo.Version, _lastKnownWaptFilePath, _currentValidationResult?.VerdictLabel).ConfigureAwait(true);

                if (!_settings.DryRunEnabled && (string.IsNullOrWhiteSpace(_lastKnownWaptFilePath) || !File.Exists(_lastKnownWaptFilePath)))
                {
                    var noWaptMessage = "Construction puis publication: aucun fichier .wapt confirme apres le build. Verifiez que le build a produit un fichier .wapt.";
                    await RegisterHistoryAsync("PackageNotReadyForPublish", false, item.PackageFolder, item.PackageId, noWaptMessage, null, item.PackageInfo.Version, item.PackageInfo.Version, null, _currentValidationResult?.VerdictLabel).ConfigureAwait(true);
                    MessageBox.Show(this, noWaptMessage, "Construire puis publier", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                await ExecutePreparePublicationAsync(_lastKnownWaptFilePath).ConfigureAwait(true);
            }
            finally
            {
                executionContext?.Clear();
            }
        }
        catch (Exception ex)
        {
            await HandleUiOperationErrorAsync("Workflow construire puis publier impossible.", ex).ConfigureAwait(true);
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

        if (sanitizedResult.IsAuthenticationFailure)
        {
            var authMessage = string.IsNullOrWhiteSpace(sanitizedResult.StandardError)
                ? "Authentification echouee. Verifiez vos identifiants et reessayez."
                : sanitizedResult.StandardError;
            InvalidateSessionSecretsForAction(actionType);
            MessageBox.Show(this, authMessage, $"{actionType} - Erreur d'authentification", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            var authFailureAction = string.Equals(actionType, "DirectUpload", StringComparison.OrdinalIgnoreCase)
                ? PublicationPreparation.GetDirectUploadHistoryAction(success: false)
                : $"{actionType}AuthFailed";
            await RegisterHistoryAsync(authFailureAction, false, item.PackageFolder, item.PackageId, authMessage, sanitizedResult, item.PackageInfo.Version, item.PackageInfo.Version, artifactPath, _currentValidationResult?.VerdictLabel).ConfigureAwait(true);
            await RefreshWaptStatusAsync().ConfigureAwait(true);
            return new ActionHandlingOutcome(false, null);
        }

        var actionSucceeded = sanitizedResult.IsSuccess || sanitizedResult.IsDryRun;
        var historyActionType = string.Equals(actionType, "DirectUpload", StringComparison.OrdinalIgnoreCase)
            ? PublicationPreparation.GetDirectUploadHistoryAction(actionSucceeded)
            : BuildHistoryActionType(actionType, sanitizedResult);
        await RegisterHistoryAsync(historyActionType, actionSucceeded, item.PackageFolder, item.PackageId, actionMessage, sanitizedResult, item.PackageInfo.Version, item.PackageInfo.Version, artifactPath, _currentValidationResult?.VerdictLabel).ConfigureAwait(true);
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
        builder.AppendLine("SYNTHESE TECHNIQUE");
        builder.AppendLine(_readinessTextBox.Text);
        builder.AppendLine();
        builder.AppendLine("LOGS");
        builder.AppendLine(_logsTextBox.Text);
        builder.AppendLine();
        builder.AppendLine("HISTORIQUE RECENT");

        foreach (var entry in _historyEntries.Take(30))
        {
            builder.AppendLine($"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss}] {entry.ActionType} | OK={entry.Success} | .wapt={entry.WaptArtifactPath ?? "N/A"} | {entry.Message}");
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
            var environmentInfo = WaptEnvironmentDetector.Inspect(_settings);
            var result = await _runtime.WaptCommandService.CheckWaptAvailabilityAsync().ConfigureAwait(true);
            _waptStatusValueLabel.Text = BuildWaptStatusText(result, environmentInfo);
            _waptStatusValueLabel.ForeColor = ResolveResultColor(result);
            AppendEnvironmentStatusLog(environmentInfo, result);
            UpdatePageOverviewTexts(_selectedCatalogItem, _currentValidationResult);
        }
        catch
        {
            _waptStatusValueLabel.Text = "WAPT: indisponible";
            _waptStatusValueLabel.ForeColor = BlockedColor;
            UpdatePageOverviewTexts(_selectedCatalogItem, _currentValidationResult);
        }
    }

    private async Task ShowFirstRunEnvironmentDiagnosticsAsync()
    {
        if (_settings.HasCompletedFirstRunExperience)
        {
            return;
        }

        var environmentInfo = WaptEnvironmentDetector.Inspect(_settings);
        var diagnosticsText = WaptEnvironmentReportBuilder.Build(_settings, environmentInfo);
        using var form = new EnvironmentDiagnosticsForm(diagnosticsText);
        form.ShowDialog(this);

        _settings.HasCompletedFirstRunExperience = true;
        await _runtime.SettingsService.SaveAsync(_settings).ConfigureAwait(true);
    }

    private void AppendEnvironmentStatusLog(WaptEnvironmentInfo environmentInfo, CommandExecutionResult result)
    {
        var detectionLabel = environmentInfo.ExecutableDetectionSource switch
        {
            "configuration" => "configuration utilisateur",
            "path" => "PATH Windows",
            "common-location" => "emplacement WAPT standard",
            _ => "non detecte"
        };

        var message = environmentInfo.IsWaptExecutableAvailable
            ? $"WAPT detecte via {detectionLabel}: {environmentInfo.EffectiveExecutablePath}"
            : "WAPT non detecte automatiquement. Ouvrez Parametres pour renseigner le chemin WAPT si necessaire.";

        if (_logsTextBox.Text.Contains(message, StringComparison.Ordinal))
        {
            return;
        }

        AppendLog(message);

        if (!result.IsSuccess && !string.IsNullOrWhiteSpace(result.StandardError))
        {
            AppendLog(result.StandardError);
        }
    }

    private static string BuildWaptStatusText(CommandExecutionResult result, WaptEnvironmentInfo environmentInfo)
    {
        if (result.IsSuccess)
        {
            return result.IsDryRun
                ? "WAPT: dry-run"
                : environmentInfo.ExecutableDetectionSource == "configuration"
                    ? "WAPT: disponible"
                    : "WAPT: disponible (auto)";
        }

        if (environmentInfo.IsWaptExecutableAvailable)
        {
            return "WAPT: detecte mais indisponible";
        }

        return "WAPT: non detecte";
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
            ReadinessVerdict = null
        }).ConfigureAwait(true);

        await LoadHistoryAsync().ConfigureAwait(true);
    }

    private async Task LoadHistoryAsync()
    {
        _historyEntries = await _runtime.HistoryService.GetRecentEntriesAsync().ConfigureAwait(true);
        _historyBindingSource.DataSource = _historyEntries.ToList();
        _historyGrid.DataSource = _historyBindingSource;
        UpdatePageOverviewTexts(_selectedCatalogItem, _currentValidationResult);
    }

    private void ConfigureCatalogGrid()
    {
        _catalogGrid.DefaultCellStyle.Font = new Font("Segoe UI", 9.75F);
        _catalogGrid.DefaultCellStyle.Padding = new Padding(10, 7, 10, 7);
        _catalogGrid.DefaultCellStyle.BackColor = PanelColor;
        _catalogGrid.DefaultCellStyle.ForeColor = HeadingColor;
        _catalogGrid.DefaultCellStyle.SelectionBackColor = AccentSoftColor;
        _catalogGrid.DefaultCellStyle.SelectionForeColor = HeadingColor;
        _catalogGrid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 251, 254);
        _catalogGrid.AlternatingRowsDefaultCellStyle.ForeColor = HeadingColor;
        _catalogGrid.RowTemplate.Height = 38;
        _catalogGrid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        _catalogGrid.GridColor = Color.FromArgb(231, 237, 244);
        _catalogGrid.ColumnHeadersHeight = 42;
        _catalogGrid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        _catalogGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9.25F, FontStyle.Bold);
        _catalogGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(244, 247, 252);
        _catalogGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(81, 96, 122);
        _catalogGrid.ColumnHeadersDefaultCellStyle.Padding = new Padding(10, 8, 10, 8);
        _catalogGrid.ColumnHeadersDefaultCellStyle.SelectionBackColor = _catalogGrid.ColumnHeadersDefaultCellStyle.BackColor;
        _catalogGrid.ColumnHeadersDefaultCellStyle.SelectionForeColor = _catalogGrid.ColumnHeadersDefaultCellStyle.ForeColor;
        _catalogGrid.EnableHeadersVisualStyles = false;
        _catalogGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _catalogGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        _catalogGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _catalogGrid.MultiSelect = false;
        var packageIdColumn = new DataGridViewTextBoxColumn { DataPropertyName = nameof(PackageCatalogItem.PackageId), HeaderText = "Package ID", FillWeight = 19, DefaultCellStyle = new DataGridViewCellStyle { Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold), ForeColor = HeadingColor, SelectionBackColor = AccentSoftColor, SelectionForeColor = HeadingColor } };
        var visibleNameColumn = new DataGridViewTextBoxColumn { DataPropertyName = nameof(PackageCatalogItem.VisibleName), HeaderText = "Nom", FillWeight = 24, DefaultCellStyle = new DataGridViewCellStyle { ForeColor = Color.FromArgb(61, 82, 125), SelectionBackColor = AccentSoftColor, SelectionForeColor = HeadingColor } };
        var versionColumn = new DataGridViewTextBoxColumn { DataPropertyName = nameof(PackageCatalogItem.Version), HeaderText = "Version", FillWeight = 11, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, SelectionBackColor = AccentSoftColor, SelectionForeColor = HeadingColor } };
        var categoryColumn = new DataGridViewTextBoxColumn { DataPropertyName = nameof(PackageCatalogItem.Category), HeaderText = "Type", FillWeight = 8, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, SelectionBackColor = AccentSoftColor, SelectionForeColor = HeadingColor } };
        var maturityColumn = new DataGridViewTextBoxColumn { DataPropertyName = nameof(PackageCatalogItem.Maturity), HeaderText = "Maturite", FillWeight = 8, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, SelectionBackColor = AccentSoftColor, SelectionForeColor = HeadingColor } };
        var lastModifiedColumn = new DataGridViewTextBoxColumn { DataPropertyName = nameof(PackageCatalogItem.LastModifiedUtc), HeaderText = "Modifie le", FillWeight = 11, DefaultCellStyle = new DataGridViewCellStyle { Format = "g", Alignment = DataGridViewContentAlignment.MiddleCenter, SelectionBackColor = AccentSoftColor, SelectionForeColor = HeadingColor } };
        var packageFolderColumn = new DataGridViewTextBoxColumn { DataPropertyName = nameof(PackageCatalogItem.PackageFolder), HeaderText = "Dossier", FillWeight = 19, DefaultCellStyle = new DataGridViewCellStyle { ForeColor = InfoColor, SelectionBackColor = AccentSoftColor, SelectionForeColor = HeadingColor } };

        _catalogGrid.Columns.AddRange(packageIdColumn, visibleNameColumn, versionColumn, categoryColumn, maturityColumn, lastModifiedColumn, packageFolderColumn);

        packageIdColumn.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
        visibleNameColumn.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
        versionColumn.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
        categoryColumn.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
        maturityColumn.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
        lastModifiedColumn.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
        packageFolderColumn.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
        _catalogGrid.CellFormatting += (_, eventArgs) =>
        {
            var dataPropertyName = _catalogGrid.Columns[eventArgs.ColumnIndex].DataPropertyName;

            if (dataPropertyName == nameof(PackageCatalogItem.LastModifiedUtc) && eventArgs.Value is DateTime date)
            {
                eventArgs.Value = date.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
                eventArgs.FormattingApplied = true;
            }

            if (dataPropertyName == nameof(PackageCatalogItem.PackageFolder) && eventArgs.Value is string packageFolder)
            {
                eventArgs.Value = BuildCompactPackageFolder(packageFolder);
                eventArgs.FormattingApplied = true;
            }

            if (dataPropertyName == nameof(PackageCatalogItem.Category) && eventArgs.Value is PackageCategory category)
            {
                eventArgs.Value = category switch
                {
                    PackageCategory.Msi => "MSI",
                    PackageCategory.Exe => "EXE",
                    _ => "AUTRE"
                };
                eventArgs.FormattingApplied = true;
            }
        };
        _catalogGrid.CellToolTipTextNeeded += (_, eventArgs) =>
        {
            if (eventArgs.RowIndex < 0 || eventArgs.ColumnIndex < 0)
            {
                return;
            }

            if (_catalogGrid.Rows[eventArgs.RowIndex].DataBoundItem is not PackageCatalogItem item)
            {
                return;
            }

            var dataPropertyName = _catalogGrid.Columns[eventArgs.ColumnIndex].DataPropertyName;
            if (dataPropertyName == nameof(PackageCatalogItem.PackageFolder))
            {
                eventArgs.ToolTipText = item.PackageFolder;
            }
        };
    }

    private static string BuildCompactPackageFolder(string packageFolder)
    {
        if (string.IsNullOrWhiteSpace(packageFolder))
        {
            return string.Empty;
        }

        var normalized = packageFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var folderName = Path.GetFileName(normalized);
        var parentDirectory = Path.GetDirectoryName(normalized);

        if (string.IsNullOrWhiteSpace(folderName) || string.IsNullOrWhiteSpace(parentDirectory))
        {
            return packageFolder;
        }

        var parentName = Path.GetFileName(parentDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        return string.IsNullOrWhiteSpace(parentName)
            ? folderName
            : $"...\\{parentName}\\{folderName}";
    }

    private void ConfigureHistoryGrid()
    {
        _historyGrid.DefaultCellStyle.Font = new Font("Segoe UI", 9.25F);
        _historyGrid.DefaultCellStyle.Padding = new Padding(8, 5, 8, 5);
        _historyGrid.DefaultCellStyle.SelectionBackColor = AccentSoftColor;
        _historyGrid.DefaultCellStyle.SelectionForeColor = HeadingColor;
        _historyGrid.AlternatingRowsDefaultCellStyle.BackColor = PanelAltColor;
        _historyGrid.RowTemplate.Height = 36;
        _historyGrid.GridColor = Color.FromArgb(236, 240, 246);
        _historyGrid.ColumnHeadersHeight = 40;
        _historyGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
        _historyGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(244, 247, 251);
        _historyGrid.ColumnHeadersDefaultCellStyle.ForeColor = InfoColor;
        _historyGrid.EnableHeadersVisualStyles = false;
        _historyGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        _historyGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _historyGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(HistoryEntry.Timestamp), HeaderText = "Date", FillWeight = 14, DefaultCellStyle = new DataGridViewCellStyle { Format = "g" } });
        _historyGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(HistoryEntry.ActionType), HeaderText = "Etape", FillWeight = 10 });
        _historyGrid.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = nameof(HistoryEntry.Success), HeaderText = "OK", FillWeight = 5 });
        _historyGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(HistoryEntry.WaptArtifactPath), HeaderText = ".wapt", FillWeight = 18 });
        _historyGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(HistoryEntry.Message), HeaderText = "Resume", FillWeight = 53 });
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

    private static Color ResolveResultColor(CommandExecutionResult result)
    {
        if (result.IsDryRun)
        {
            return AccentColor;
        }

        if (result.IsAuthenticationFailure)
        {
            return BlockedColor;
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

        if (result.IsAuthenticationFailure)
        {
            return $"{actionType}: echec d'authentification. {result.StandardError}";
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

    private static string? ExtractWaptFilePathFromOutput(string? output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return null;
        }

        var match = System.Text.RegularExpressions.Regex.Match(output, @"(?<path>[A-Za-z]:\\[^\r\n""]*\.wapt)\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (match.Success)
        {
            var path = match.Groups["path"].Value.TrimEnd();
            if (File.Exists(path))
            {
                return path;
            }
        }

        return null;
    }

    private static bool SupportsManualWorkflow(string actionType)
        => string.Equals(actionType, "Build", StringComparison.OrdinalIgnoreCase)
            || string.Equals(actionType, "Sign", StringComparison.OrdinalIgnoreCase)
            || string.Equals(actionType, "Upload", StringComparison.OrdinalIgnoreCase)
            || string.Equals(actionType, "DirectUpload", StringComparison.OrdinalIgnoreCase);

    private static string GetManualWorkflowName(string actionType)
        => string.Equals(actionType, "Sign", StringComparison.OrdinalIgnoreCase)
            ? "signature"
            : string.Equals(actionType, "Upload", StringComparison.OrdinalIgnoreCase) || string.Equals(actionType, "DirectUpload", StringComparison.OrdinalIgnoreCase)
                ? "upload"
                : "build";

    private static string GetManualHistoryLabel(string actionType)
        => string.Equals(actionType, "Sign", StringComparison.OrdinalIgnoreCase)
            ? "Signature"
            : string.Equals(actionType, "Upload", StringComparison.OrdinalIgnoreCase) || string.Equals(actionType, "DirectUpload", StringComparison.OrdinalIgnoreCase)
                ? "Upload direct"
                : "Build";

    private static string GetManualInstructionText(string actionType)
        => string.Equals(actionType, "Upload", StringComparison.OrdinalIgnoreCase) || string.Equals(actionType, "DirectUpload", StringComparison.OrdinalIgnoreCase)
            ? "Cette action peut demander une authentification WAPT interactive. Copiez la commande, ouvrez un terminal dans le dossier du paquet, saisissez les secrets uniquement quand WAPT les demande, puis revenez rattacher le resultat manuel a l'historique."
            : "Cette action peut demander un secret interactif. Copiez la commande, ouvrez un terminal dans le dossier du paquet, saisissez les secrets uniquement quand WAPT les demande, puis revenez rattacher le resultat manuel a l'historique.";

    private static string GetManualArtifactLabel(string actionType)
        => string.Equals(actionType, "Upload", StringComparison.OrdinalIgnoreCase) || string.Equals(actionType, "DirectUpload", StringComparison.OrdinalIgnoreCase)
            ? "Chemin du .wapt uploade"
            : "Chemin du .wapt associe";

    private static string GetManualSelectArtifactButtonText(string actionType)
        => string.Equals(actionType, "Upload", StringComparison.OrdinalIgnoreCase) || string.Equals(actionType, "DirectUpload", StringComparison.OrdinalIgnoreCase)
            ? "Selectionner le .wapt uploade"
            : "Selectionner le .wapt associe";

    private sealed record CredentialPromptOutcome(WaptExecutionContext? ExecutionContext, bool RememberForSession);

    private bool EnsureExecutionContext(ref WaptExecutionContext? executionContext, string title, string description, bool requireCertificatePassword, bool requireAdminCredentials)
    {
        var needsCertificatePassword = requireCertificatePassword && !(executionContext?.HasCertificatePassword ?? false);
        var needsAdminCredentials = requireAdminCredentials && !(executionContext?.HasAdminCredentials ?? false);

        if (!needsCertificatePassword && !needsAdminCredentials)
        {
            return true;
        }

        var sessionContext = _runtime.WaptSessionService.CreateExecutionContext(needsCertificatePassword, needsAdminCredentials, executionContext?.WaptFilePath);
        if (sessionContext is not null)
        {
            executionContext = MergeExecutionContexts(executionContext, sessionContext);
            sessionContext.Clear();
            return true;
        }

        var promptOutcome = PromptForCredentials(title, description, needsCertificatePassword, needsAdminCredentials);
        if (promptOutcome.ExecutionContext is null)
        {
            return false;
        }

        if (promptOutcome.RememberForSession)
        {
            _runtime.WaptSessionService.StoreFromExecutionContext(promptOutcome.ExecutionContext, needsCertificatePassword, needsAdminCredentials);
        }

        executionContext = MergeExecutionContexts(executionContext, promptOutcome.ExecutionContext);
        UpdatePageOverviewTexts(_selectedCatalogItem, _currentValidationResult);
        return true;
    }

    private static WaptExecutionContext MergeExecutionContexts(WaptExecutionContext? target, WaptExecutionContext source)
    {
        var mergedContext = target ?? new WaptExecutionContext();

        if (!string.IsNullOrWhiteSpace(source.CertificatePassword))
        {
            mergedContext.CertificatePassword = source.CertificatePassword;
        }

        if (!string.IsNullOrWhiteSpace(source.AdminUser))
        {
            mergedContext.AdminUser = source.AdminUser;
        }

        if (!string.IsNullOrWhiteSpace(source.AdminPassword))
        {
            mergedContext.AdminPassword = source.AdminPassword;
        }

        if (!string.IsNullOrWhiteSpace(source.WaptFilePath))
        {
            mergedContext.WaptFilePath = source.WaptFilePath;
        }

        return mergedContext;
    }

    private CredentialPromptOutcome PromptForCredentials(
        string title,
        string description,
        bool requireCertificatePassword,
        bool requireAdminCredentials,
        string confirmButtonText = "Continuer",
        bool showRememberForSessionOption = true,
        string certificatePasswordLabelText = "Mot de passe certificat",
        string adminUserLabelText = "Identifiant administrateur WAPT",
        string adminPasswordLabelText = "Mot de passe administrateur WAPT")
    {
        using var form = new CredentialPromptForm(
            title,
            description,
            requireCertificatePassword,
            requireAdminCredentials,
            requireAdminCredentials,
            showRememberForSessionOption: showRememberForSessionOption,
            rememberForSessionByDefault: !_runtime.WaptSessionService.GetSnapshot().HasAnySecrets,
            confirmButtonText: confirmButtonText,
            certificatePasswordLabelText: certificatePasswordLabelText,
            adminUserLabelText: adminUserLabelText,
            adminPasswordLabelText: adminPasswordLabelText);

        return form.ShowDialog(this) == DialogResult.OK
            ? new CredentialPromptOutcome(form.ExecutionContext, form.RememberSecretsForSession)
            : new CredentialPromptOutcome(null, false);
    }

    private void OpenCertificateSessionEditor()
    {
        var promptOutcome = PromptForCredentials(
            "Session WAPT - Certificat",
            "Memorisez ou remplacez le mot de passe certificat pour les actions Build et Signature assistees. Cette valeur reste uniquement en memoire pendant la session en cours.",
            requireCertificatePassword: true,
            requireAdminCredentials: false,
            confirmButtonText: "Memoriser");

        if (promptOutcome.ExecutionContext?.HasCertificatePassword != true)
        {
            return;
        }

        _runtime.WaptSessionService.StoreFromExecutionContext(promptOutcome.ExecutionContext, includeCertificatePassword: true, includeAdminCredentials: false);
        UpdatePageOverviewTexts(_selectedCatalogItem, _currentValidationResult);
        SetActionResult("Mot de passe certificat memorise pour cette session.", ReadyColor);
        AppendLog("Session WAPT: mot de passe certificat memorise en memoire.");
    }

    private void OpenServerSessionEditor()
    {
        var promptOutcome = PromptForCredentials(
            "Session WAPT - Serveur",
            "Memorisez ou remplacez les identifiants administrateur WAPT pour l'upload direct assiste. Ils restent uniquement en memoire pendant la session en cours.",
            requireCertificatePassword: false,
            requireAdminCredentials: true,
            confirmButtonText: "Memoriser");

        if (promptOutcome.ExecutionContext?.HasAdminCredentials != true)
        {
            return;
        }

        _runtime.WaptSessionService.StoreFromExecutionContext(promptOutcome.ExecutionContext, includeCertificatePassword: false, includeAdminCredentials: true);
        UpdatePageOverviewTexts(_selectedCatalogItem, _currentValidationResult);
        SetActionResult("Identifiants serveur memorises pour cette session.", ReadyColor);
        AppendLog("Session WAPT: identifiants serveur memorises en memoire.");
    }

    private void ClearSessionSecrets(bool interactive)
    {
        var snapshot = _runtime.WaptSessionService.GetSnapshot();
        if (!snapshot.HasAnySecrets)
        {
            if (interactive)
            {
                MessageBox.Show(this, "Aucun secret de session n'est actuellement memorise.", "Session WAPT", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            return;
        }

        if (interactive && MessageBox.Show(this, "Effacer tous les secrets memorises pour cette session WaptStudio ?", "Session WAPT", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        _runtime.WaptSessionService.ClearAll();
        UpdatePageOverviewTexts(_selectedCatalogItem, _currentValidationResult);

        if (interactive)
        {
            SetActionResult("Secrets de session effaces.", InfoColor);
            AppendLog("Session WAPT: secrets de session effaces sur demande.");
        }
    }

    private void InvalidateSessionSecretsForAction(string actionType)
    {
        var snapshot = _runtime.WaptSessionService.GetSnapshot();
        var invalidated = false;

        if ((string.Equals(actionType, "Build", StringComparison.OrdinalIgnoreCase)
                || string.Equals(actionType, "Sign", StringComparison.OrdinalIgnoreCase))
            && snapshot.HasCertificatePassword)
        {
            _runtime.WaptSessionService.ClearCertificatePassword();
            AppendLog("Session WAPT: mot de passe certificat efface apres un echec d'authentification.");
            invalidated = true;
        }

        if ((string.Equals(actionType, "Upload", StringComparison.OrdinalIgnoreCase)
                || string.Equals(actionType, "DirectUpload", StringComparison.OrdinalIgnoreCase))
            && snapshot.HasServerCredentials)
        {
            _runtime.WaptSessionService.ClearServerCredentials();
            AppendLog("Session WAPT: identifiants serveur effaces apres un echec d'authentification.");
            invalidated = true;
        }

        if (invalidated)
        {
            UpdatePageOverviewTexts(_selectedCatalogItem, _currentValidationResult);
        }
    }

    private sealed record UploadSelection(string? ResolvedPath, string? ExpectedName, string? CandidateName, bool ExactMatch);

    private static bool IsCoherentWaptForPackage(string fileName, string? packageId, string? version)
    {
        if (string.IsNullOrWhiteSpace(packageId) || string.IsNullOrWhiteSpace(version))
        {
            return false;
        }

        return fileName.StartsWith(packageId, StringComparison.OrdinalIgnoreCase)
            && fileName.Contains(version, StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildUploadSelectionText(string? expectedName, string? candidateName, string resolvedPath)
    {
        var effectiveCandidate = string.IsNullOrWhiteSpace(candidateName) ? "non detecte" : candidateName;
        var effectiveExpected = string.IsNullOrWhiteSpace(expectedName) ? "non defini" : expectedName;
        return $".wapt attendu: {effectiveExpected}\r\nFichier trouve: {effectiveCandidate}\r\nFichier utilise: {Path.GetFileName(resolvedPath)}";
    }

    private UploadSelection ResolveUploadWaptSelection(string packageFolder, string? explicitWaptFilePath, bool allowExpectedPathWhenMissing)
    {
        var item = _selectedCatalogItem;
        var expectedName = item?.PackageInfo.ExpectedWaptFileName;
        var packageId = item?.PackageInfo.PackageName ?? item?.PackageId;
        var version = item?.PackageInfo.Version ?? item?.Version;

        if (!string.IsNullOrWhiteSpace(explicitWaptFilePath) && (File.Exists(explicitWaptFilePath) || allowExpectedPathWhenMissing))
        {
            var candidateNameFromExplicit = Path.GetFileName(explicitWaptFilePath);
            var exactFromExplicit = !string.IsNullOrWhiteSpace(expectedName) && string.Equals(candidateNameFromExplicit, expectedName, StringComparison.OrdinalIgnoreCase);
            return new UploadSelection(explicitWaptFilePath, expectedName, candidateNameFromExplicit, exactFromExplicit);
        }

        var parentDirectory = Path.GetDirectoryName(packageFolder);
        var searchDirectories = new[] { packageFolder, parentDirectory }
            .Where(d => !string.IsNullOrWhiteSpace(d) && Directory.Exists(d))
            .ToArray();

        var candidates = searchDirectories
            .SelectMany(d => Directory.EnumerateFiles(d!, "*.wapt", SearchOption.TopDirectoryOnly))
            .ToArray();

        var selectedPath = WaptNaming.SelectBestWaptCandidate(candidates, expectedName, packageId, version);
        var candidateName = string.IsNullOrWhiteSpace(selectedPath) ? null : Path.GetFileName(selectedPath);
        var exactMatch = !string.IsNullOrWhiteSpace(expectedName)
            && string.Equals(candidateName, expectedName, StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(selectedPath) && !string.IsNullOrWhiteSpace(expectedName) && allowExpectedPathWhenMissing)
        {
            selectedPath = Path.Combine(packageFolder, expectedName);
            candidateName = expectedName;
        }

        return new UploadSelection(selectedPath, expectedName, candidateName, exactMatch);
    }

    private string? ResolveExpectedWaptFilePath(string packageFolder, bool allowExpectedPathWhenMissing)
    {
        var item = _selectedCatalogItem;
        var expectedName = item?.PackageInfo.ExpectedWaptFileName;
        var packageId = item?.PackageInfo.PackageName ?? item?.PackageId;
        var version = item?.PackageInfo.Version;
        var parentDirectory = Path.GetDirectoryName(packageFolder);

        var searchDirectories = new[] { packageFolder, parentDirectory }
            .Where(d => !string.IsNullOrWhiteSpace(d) && Directory.Exists(d))
            .ToArray();

        var candidates = searchDirectories
            .SelectMany(d => Directory.EnumerateFiles(d!, "*.wapt", SearchOption.TopDirectoryOnly))
            .ToArray();

        var selected = WaptNaming.SelectBestWaptCandidate(candidates, expectedName, packageId, version);
        if (!string.IsNullOrWhiteSpace(selected))
        {
            return selected;
        }

        if (!string.IsNullOrWhiteSpace(expectedName) && allowExpectedPathWhenMissing)
        {
            return Path.Combine(packageFolder, expectedName);
        }

        return null;
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

    private string BuildReadinessText(PackageCatalogItem? item, ValidationResult? validationResult)
    {
        if (validationResult is null)
        {
            return "Aucune synthese technique n'est encore disponible pour le paquet selectionne.";
        }

        var actionLines = item is null
            ? Array.Empty<string>()
            : BuildAvailableActionLines(item, validationResult);

        var builder = new StringBuilder();
        builder.AppendLine("Synthese du paquet");
        builder.AppendLine(validationResult.Summary);
        builder.AppendLine();
        builder.AppendLine("Constats techniques:");
        foreach (var issue in validationResult.Issues)
        {
            builder.AppendLine($"- {FormatIssuePrefix(issue.Severity)} {issue.Message}");
        }

        if (actionLines.Length > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Actions disponibles:");
            foreach (var actionLine in actionLines)
            {
                builder.AppendLine($"- {actionLine}");
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

    private static Color GetSoftVerdictColor(ReadinessVerdict verdict)
        => verdict switch
        {
            ReadinessVerdict.ReadyForBuildUpload => RecommendedSoftColor,
            ReadinessVerdict.ReadyWithWarnings => WarningSoftColor,
            _ => DangerSoftColor
        };

    private void PulseSelectionAccent()
    {
        _pulseState = !_pulseState;
        _catalogGrid.GridColor = _pulseState ? Color.FromArgb(233, 237, 244) : Color.FromArgb(241, 243, 247);
    }

    private static Panel CreateCardPanel()
        => new()
        {
            Dock = DockStyle.Fill,
            BackColor = PanelColor,
            Margin = new Padding(0, 0, 0, 8),
            Padding = new Padding(1),
            BorderStyle = BorderStyle.None
        };

    private static Control CreateSectionCard(string title, string subtitle, Control content)
    {
        var card = CreateCardPanel();
        card.Padding = new Padding(22);
        card.Paint += (sender, e) =>
        {
            var panel = (Panel)sender!;
            using var shadowPen = new Pen(Color.FromArgb(236, 240, 246), 3);
            using var pen = new Pen(BorderColor, 1);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.DrawRectangle(shadowPen, 1, 2, panel.Width - 4, panel.Height - 5);
            e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
            using var accentBrush = new SolidBrush(Color.FromArgb(245, 247, 251));
            e.Graphics.FillRectangle(accentBrush, 1, 1, panel.Width - 2, 6);
        };

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = PanelColor };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(CreateSectionHeader(title, subtitle), 0, 0);
        layout.Controls.Add(content, 0, 1);
        card.Controls.Add(layout);
        return card;
    }

    private static Panel CreatePageSectionCard(string title, string subtitle, Control content)
    {
        var card = CreateCardPanel();
        card.Padding = new Padding(22);
        card.Dock = DockStyle.Top;
        card.AutoSize = true;
        card.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        card.Margin = new Padding(0, 0, 0, 12);
        card.Paint += (sender, e) =>
        {
            var panel = (Panel)sender!;
            using var shadowPen = new Pen(Color.FromArgb(236, 240, 246), 3);
            using var pen = new Pen(BorderColor, 1);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.DrawRectangle(shadowPen, 1, 2, panel.Width - 4, panel.Height - 5);
            e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
            using var accentBrush = new SolidBrush(Color.FromArgb(245, 247, 251));
            e.Graphics.FillRectangle(accentBrush, 1, 1, panel.Width - 2, 6);
        };

        var layout = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 1, RowCount = 2, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, BackColor = PanelColor };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.Dock = DockStyle.Top;
        layout.Controls.Add(CreateSectionHeader(title, subtitle), 0, 0);
        layout.Controls.Add(content, 0, 1);
        card.Controls.Add(layout);
        return card;
    }

    private static Panel CreateStackedSectionCard(string title, string subtitle, Control content)
    {
        var card = CreatePageSectionCard(title, subtitle, content);
        card.MinimumSize = new Size(0, 180);
        return card;
    }

    private static Control CreateSectionHeader(string title, string subtitle)
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 1, AutoSize = true, BackColor = PanelColor, Margin = new Padding(0, 0, 0, 14) };
        panel.Controls.Add(new Label { Text = title, AutoSize = true, Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold), ForeColor = HeadingColor, Margin = new Padding(0, 0, 0, 4) }, 0, 0);
        panel.Controls.Add(new Label { Text = subtitle, AutoSize = true, ForeColor = SubtleColor, Font = new Font("Segoe UI", 9.25F) }, 0, 1);
        return panel;
    }

    private void StyleActionButtons(Button? recommendedButton = null)
    {
        foreach (var button in new[] { _scanCatalogButton, _analyzeButton, _replaceInstallerButton, _validateButton, _buildButton, _signButton, _uploadButton, _buildAndUploadButton, _restoreBackupButton, _openBackupFolderButton, _saveReportButton, _settingsButton, _browseCatalogButton, _openPackagePageButton, _showAdvancedDetailsButton, _updateCertificateSessionButton, _updateServerSessionButton, _clearSessionSecretsButton })
        {
            button.FlatStyle = FlatStyle.Flat;
            button.Font = new Font("Segoe UI", 9.75F);
            button.AutoSize = true;
            button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            var isPrimary = button == _scanCatalogButton || button == _validateButton || button == _buildButton || button == _uploadButton || button == _buildAndUploadButton;
            var isRecommended = button == recommendedButton;
            var isHeroAction = button == _validateButton || button == _buildButton || button == _uploadButton || button == _buildAndUploadButton;
            if (isRecommended)
            {
                button.BackColor = RecommendedColor;
                button.ForeColor = Color.White;
                button.FlatAppearance.BorderSize = 0;
                button.FlatAppearance.BorderColor = RecommendedColor;
                button.FlatAppearance.MouseOverBackColor = Color.FromArgb(18, 108, 78);
                button.FlatAppearance.MouseDownBackColor = Color.FromArgb(14, 88, 64);
                button.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            }
            else if (isPrimary)
            {
                button.BackColor = AccentColor;
                button.ForeColor = Color.White;
                button.FlatAppearance.BorderSize = 0;
                button.FlatAppearance.BorderColor = AccentColor;
                button.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, 82, 158);
                button.FlatAppearance.MouseDownBackColor = Color.FromArgb(24, 66, 130);
                if (button == _buildAndUploadButton)
                {
                    button.BackColor = Color.FromArgb(19, 112, 159);
                    button.FlatAppearance.BorderColor = button.BackColor;
                    button.FlatAppearance.MouseOverBackColor = Color.FromArgb(17, 94, 134);
                    button.FlatAppearance.MouseDownBackColor = Color.FromArgb(14, 76, 110);
                }
            }
            else
            {
                button.BackColor = PanelColor;
                button.ForeColor = HeadingColor;
                button.FlatAppearance.BorderSize = 1;
                button.FlatAppearance.BorderColor = BorderColor;
                button.FlatAppearance.MouseOverBackColor = PanelAltColor;
                button.FlatAppearance.MouseDownBackColor = Color.FromArgb(225, 233, 246);
            }
            button.Padding = isHeroAction ? new Padding(18, 12, 18, 12) : new Padding(14, 9, 14, 9);
            button.Margin = new Padding(0, 0, 12, 12);
            button.MinimumSize = new Size(isHeroAction ? 220 : 170, isHeroAction ? 48 : 42);
            button.AutoEllipsis = false;
            button.TextAlign = ContentAlignment.MiddleCenter;
            button.Font = isHeroAction
                ? new Font("Segoe UI Semibold", 10F, button == recommendedButton ? FontStyle.Bold : FontStyle.Regular)
                : button.Font;
        }
    }

    private Control BuildPackageSummaryCard()
    {
        var content = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 1, RowCount = 2, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, BackColor = PanelColor };
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var identityPanel = new Panel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, BackColor = AccentSoftColor, Padding = new Padding(22, 22, 22, 18), Margin = new Padding(0, 0, 0, 14) };
        identityPanel.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(196, 214, 244), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, identityPanel.Width - 1, identityPanel.Height - 1);
        };
        var identityLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, AutoSize = true, BackColor = PanelAltColor };
        _selectedPackageLabel.Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold);
        _selectedPackageMetaLabel.Font = new Font("Segoe UI", 10F);
        _selectedPackageMetaLabel.ForeColor = InfoColor;
        identityLayout.BackColor = AccentSoftColor;
        identityLayout.Controls.Add(_selectedPackageLabel, 0, 0);
        identityLayout.Controls.Add(_selectedPackageMetaLabel, 0, 1);
        identityPanel.Controls.Add(identityLayout);

        var detailsPanel = new Panel { Dock = DockStyle.Top, Height = 260, MinimumSize = new Size(0, 260), BackColor = Color.FromArgb(250, 252, 255), Padding = new Padding(16) };
        detailsPanel.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(226, 232, 241), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, detailsPanel.Width - 1, detailsPanel.Height - 1);
        };
        _packageSummaryTextBox.Font = new Font("Segoe UI", 9.75F);
        _packageSummaryTextBox.BackColor = detailsPanel.BackColor;
        detailsPanel.Controls.Add(_packageSummaryTextBox);

        content.Controls.Add(identityPanel, 0, 0);
        content.Controls.Add(detailsPanel, 0, 1);
        return CreateStackedSectionCard("Paquet selectionne", "Nom, version, installateur, chemin source et lecture rapide du paquet courant.", content);
    }

    private Control BuildAssistantCard()
    {
        var content = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 1, RowCount = 2, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, BackColor = PanelColor };
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _assistantDecisionPanel.Controls.Clear();
        _assistantDecisionPanel.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(233, 214, 167), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, _assistantDecisionPanel.Width - 1, _assistantDecisionPanel.Height - 1);
        };

        var heroLayout = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 1, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, BackColor = _assistantDecisionPanel.BackColor, Margin = new Padding(0) };
        var topRow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = true, BackColor = _assistantDecisionPanel.BackColor, Margin = new Padding(0, 0, 0, 14) };
        topRow.Controls.Add(_assistantVerdictBadgeLabel);

        var nextStepPanel = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 1, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, BackColor = _assistantDecisionPanel.BackColor, Margin = new Padding(0) };
        _nextStepTitleLabel.Margin = new Padding(0, 0, 0, 6);
        _nextStepDescriptionLabel.Margin = new Padding(0, 0, 0, 0);
        _nextStepDescriptionLabel.MaximumSize = new Size(520, 0);
        nextStepPanel.Controls.Add(_nextStepTitleLabel, 0, 0);
        nextStepPanel.Controls.Add(_nextStepDescriptionLabel, 0, 1);
        heroLayout.Controls.Add(topRow, 0, 0);
        heroLayout.Controls.Add(nextStepPanel, 0, 1);
        _assistantDecisionPanel.Controls.Add(heroLayout);

        var narrativePanel = new Panel { Dock = DockStyle.Top, Height = 320, MinimumSize = new Size(0, 320), BackColor = Color.FromArgb(250, 252, 255), Padding = new Padding(16) };
        narrativePanel.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(226, 232, 241), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, narrativePanel.Width - 1, narrativePanel.Height - 1);
        };
        _assistantTextBox.Font = new Font("Segoe UI", 9.9F);
        _assistantTextBox.BackColor = narrativePanel.BackColor;
        narrativePanel.Controls.Add(_assistantTextBox);

        content.Controls.Add(_assistantDecisionPanel, 0, 0);
        content.Controls.Add(narrativePanel, 0, 1);
        return CreateStackedSectionCard("Synthese du paquet", "Constats techniques, points a surveiller et actions encore disponibles.", content);
    }

    private Control BuildActionFamiliesCard()
    {
        var actions = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 4, BackColor = PanelColor, Padding = new Padding(2, 2, 2, 0), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));

        actions.Controls.Add(CreateActionFamilyCard("Relire", "Relire le paquet et preparer le changement d'installeur.", _analyzeButton, _replaceInstallerButton), 0, 0);
        actions.Controls.Add(CreateActionFamilyCard("Construire", "Verifier le paquet puis generer le .wapt.", _validateButton, _buildButton), 1, 0);
        actions.Controls.Add(CreateActionFamilyCard("Signer / publier", "Signer puis publier via WAPT Console ou upload direct si utile.", _signButton, _uploadButton, _buildAndUploadButton), 2, 0);
        actions.Controls.Add(CreateActionFamilyCard("Sauvegardes", "Revenir a une sauvegarde ou ouvrir le dossier des sauvegardes.", _restoreBackupButton, _openBackupFolderButton, _saveReportButton), 3, 0);

        return CreateStackedSectionCard("Actions", "Seulement les actions utiles au cycle reel du paquet.", actions);
    }

    private static Control CreateActionFamilyCard(string title, string description, params Button[] buttons)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = title switch
            {
                "Signer / publier" => AccentSoftColor,
                "Construire" => WarningSoftColor,
                "Relire" => Color.FromArgb(242, 247, 255),
                _ => CardHoverColor
            },
            BorderStyle = BorderStyle.None,
            Margin = new Padding(0, 0, 12, 0),
            Padding = new Padding(16, 16, 16, 10),
            MinimumSize = new Size(0, 220)
        };

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = panel.BackColor, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        layout.Controls.Add(new Label { Text = title, AutoSize = true, Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold), ForeColor = HeadingColor, Margin = new Padding(0, 0, 0, 4) }, 0, 0);
        layout.Controls.Add(new Label { Text = description, AutoSize = true, MaximumSize = new Size(260, 0), ForeColor = InfoColor, Font = new Font("Segoe UI", 8.9F), Margin = new Padding(0, 0, 0, 10) }, 0, 1);

        var buttonsPanel = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, AutoScroll = false, WrapContents = true, BackColor = panel.BackColor, Padding = new Padding(0), MaximumSize = new Size(270, 0) };
        foreach (var button in buttons)
        {
            button.Margin = new Padding(0, 0, 8, 8);
        }
        buttonsPanel.Controls.AddRange(buttons);
        layout.Controls.Add(buttonsPanel, 0, 2);

        panel.Controls.Add(layout);
        return panel;
    }

    private Control BuildActivityArea()
    {
        _activityTabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
        _activityTabControl.ItemSize = new Size(150, 40);
        _activityTabControl.SizeMode = TabSizeMode.Fixed;
        _activityTabControl.DrawItem += (sender, e) =>
        {
            var tabControl = (TabControl)sender!;
            var isSelected = e.Index == tabControl.SelectedIndex;
            var tabPage = tabControl.TabPages[e.Index];
            using var bgBrush = new SolidBrush(isSelected ? PanelColor : Color.FromArgb(241, 245, 251));
            e.Graphics.FillRectangle(bgBrush, e.Bounds);
            using var textBrush = new SolidBrush(isSelected ? AccentColor : InfoColor);
            using var font = new Font("Segoe UI Semibold", 9.75F, isSelected ? FontStyle.Bold : FontStyle.Regular);
            var textSize = e.Graphics.MeasureString(tabPage.Text, font);
            var textX = e.Bounds.X + (e.Bounds.Width - (int)textSize.Width) / 2;
            var textY = e.Bounds.Y + (e.Bounds.Height - (int)textSize.Height) / 2;
            e.Graphics.DrawString(tabPage.Text, font, textBrush, textX, textY);
            if (isSelected)
            {
                using var accentPen = new Pen(AccentColor, 3);
                e.Graphics.DrawLine(accentPen, e.Bounds.Left + 10, e.Bounds.Bottom - 1, e.Bounds.Right - 10, e.Bounds.Bottom - 1);
            }
        };

        _activityTabControl.TabPages.Clear();
        _activityTabControl.TabPages.Add(new TabPage("Journal") { BackColor = PanelColor, Padding = new Padding(16) });
        _logsTextBox.Font = new Font("Consolas", 9.5F);
        _logsTextBox.BackColor = PanelAltColor;
        _activityTabControl.TabPages[0].Controls.Add(_logsTextBox);
        _activityTabControl.TabPages.Add(new TabPage("Historique") { BackColor = PanelColor, Padding = new Padding(16) });
        _activityTabControl.TabPages[1].Controls.Add(_historyGrid);
        _activityTabControl.TabPages.Add(new TabPage("Details paquet") { BackColor = PanelColor, Padding = new Padding(16) });
        _packageDetailsTextBox.Font = new Font("Segoe UI", 9.5F);
        _packageDetailsTextBox.BackColor = PanelAltColor;
        _activityTabControl.TabPages[2].Controls.Add(_packageDetailsTextBox);
        _activityTabControl.TabPages.Add(new TabPage("Synthese technique") { BackColor = PanelColor, Padding = new Padding(16) });
        _readinessTextBox.Font = new Font("Segoe UI", 9.5F);
        _readinessTextBox.BackColor = PanelAltColor;
        _activityTabControl.TabPages[3].Controls.Add(_readinessTextBox);

        return CreateSectionCard("Journal et details", "Journal, historique et details techniques avec une lecture plus confortable.", _activityTabControl);
    }

    private static string BuildPackageSummaryText(PackageCatalogItem item, ValidationResult? validationResult)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Nom du paquet          : {item.PackageId}");
        builder.AppendLine($"Version                : {(string.IsNullOrWhiteSpace(item.Version) ? "non detectee" : item.Version)}");
        builder.AppendLine($"Type                   : {item.Category.ToString().ToUpperInvariant()}");
        builder.AppendLine($"Maturite               : {item.Maturity}");
        builder.AppendLine($"Installeur detecte     : {(string.IsNullOrWhiteSpace(item.PrimaryInstallerName) ? "non detecte" : item.PrimaryInstallerName)}");
        builder.AppendLine($"Chemin source          : {item.PackageFolder}");
        builder.AppendLine($"Derniere modification  : {item.LastModifiedUtc.ToLocalTime():yyyy-MM-dd HH:mm}");

        if (validationResult is not null)
        {
            builder.AppendLine();
            builder.AppendLine("Synthese               :");
            builder.AppendLine(validationResult.Summary);
        }

        return builder.ToString();
    }

    private void UpdatePageOverviewTexts(PackageCatalogItem? item, ValidationResult? validationResult)
    {
        _packageOverviewTextBox.Text = BuildPackageOverviewText(item, validationResult);
        _replacementOverviewTextBox.Text = BuildReplacementOverviewText(item, validationResult);
        _publicationOverviewTextBox.Text = BuildPublicationOverviewText(item, validationResult);
        _settingsOverviewTextBox.Text = BuildSettingsOverviewText();
        _sessionOverviewTextBox.Text = BuildSessionOverviewText();
    }

    private string BuildPackageOverviewText(PackageCatalogItem? item, ValidationResult? validationResult)
    {
        if (item is null)
        {
            return "Aucun paquet n'est encore selectionne.\r\n\r\nOuvrez la page Catalogue, chargez les paquets detectes, puis choisissez une ligne pour afficher ici une fiche claire du paquet et ses actions utiles.";
        }

        var builder = new StringBuilder();
        builder.AppendLine($"Paquet actif      : {item.PackageId}");
        builder.AppendLine($"Nom visible       : {(string.IsNullOrWhiteSpace(item.VisibleName) ? item.PackageId : item.VisibleName)}");
        builder.AppendLine($"Version           : {(string.IsNullOrWhiteSpace(item.Version) ? "non detectee" : item.Version)}");
        builder.AppendLine($"Type              : {item.Category.ToString().ToUpperInvariant()}");
        builder.AppendLine($"Maturite          : {item.Maturity}");
        builder.AppendLine($"Installeur        : {(string.IsNullOrWhiteSpace(item.PrimaryInstallerName) ? "non detecte" : item.PrimaryInstallerName)}");
        builder.AppendLine($"Derniere modif.   : {item.LastModifiedUtc.ToLocalTime():yyyy-MM-dd HH:mm}");
        builder.AppendLine();
        builder.AppendLine("Synthese d'analyse:");
        builder.AppendLine(validationResult?.Summary ?? "Relisez le paquet pour afficher une synthese technique recente.");
        return builder.ToString();
    }

    private string BuildReplacementOverviewText(PackageCatalogItem? item, ValidationResult? validationResult)
    {
        if (item is null)
        {
            return "Le changement d'installeur se prepare paquet par paquet.\r\n\r\nSelectionnez d'abord un paquet, puis utilisez cette page pour lancer un apercu, appliquer le remplacement et revenir a la derniere sauvegarde si necessaire.";
        }

        var builder = new StringBuilder();
        builder.AppendLine($"Paquet cible           : {item.PackageId}");
        builder.AppendLine($"Installeur actuel      : {item.PrimaryInstallerName}");
        builder.AppendLine($"Sauvegardes            : {(_settings.CreateBackups ? "activees" : "desactivees")}");
        builder.AppendLine($"Dossier sauvegardes    : {AppPaths.ResolveBackupsDirectory(_settings)}");
        builder.AppendLine();
        builder.AppendLine(validationResult is null
            ? "Relisez d'abord le paquet pour repartir d'une analyse technique recente."
            : "Le remplacement suit toujours le meme flux: apercu, confirmation, puis relecture du paquet mis a jour.");
        return builder.ToString();
    }

    private string BuildPublicationOverviewText(PackageCatalogItem? item, ValidationResult? validationResult)
    {
        if (item is null)
        {
            return "La publication est separee du catalogue pour rester lisible.\r\n\r\nSelectionnez un paquet pour voir ici si le build est possible, si un .wapt est deja disponible et quelle action de publication est reellement utilisable.";
        }

        var publicationPreparation = PublicationPreparation.Evaluate(item.PackageFolder, item.PackageInfo, validationResult, _settings, _lastKnownWaptFilePackageFolder == item.PackageFolder ? _lastKnownWaptFilePath : null);
        var builder = new StringBuilder();
        builder.AppendLine($"Paquet cible            : {item.PackageId}");
        builder.AppendLine($"Build autorise          : {(validationResult?.BuildPossible == true ? "oui" : "non")}");
        builder.AppendLine($".wapt confirme          : {(publicationPreparation.HasRealWaptFile ? "oui" : "non")}");
        builder.AppendLine($"Publication recommandee : {(publicationPreparation.RecommendedMode == PublicationMode.WaptConsole ? "WAPT Console" : "Upload direct")}");
        builder.AppendLine($"Upload direct           : {(_settings.EnableUpload ? "configure" : "non configure")}");
        builder.AppendLine();
        builder.AppendLine(publicationPreparation.CanPrepareForConsolePublish
            ? "La synthese finale de publication est disponible."
            : "Commencez par construire le .wapt si aucun fichier reel n'est encore present.");
        return builder.ToString();
    }

    private string BuildSettingsOverviewText()
    {
        var sessionSnapshot = _runtime.WaptSessionService.GetSnapshot();
        var builder = new StringBuilder();
        builder.AppendLine($"Dossier catalogue       : {_catalogRootFolderTextBox.Text}");
        builder.AppendLine("Mode de scan principal  : complet avec sous-dossiers");
        builder.AppendLine($"WAPT                    : {_waptStatusValueLabel.Text}");
        builder.AppendLine($"Signature               : {(_settings.EnableSigning ? "activee" : "desactivee")}");
        builder.AppendLine($"Upload direct           : {(_settings.EnableUpload ? "active" : "desactive")}");
        builder.AppendLine($"Publication privilegiee : {(_settings.PreferWaptConsolePublish ? "WAPT Console" : "Upload direct")}");
        builder.AppendLine($"Dry-run                 : {(_settings.DryRunEnabled ? "active" : "desactive")}");
        builder.AppendLine($"Session WAPT            : {(sessionSnapshot.HasAnySecrets ? "secrets memorises en memoire" : "aucun secret memorise")}");
        builder.AppendLine($"Historique charge       : {_historyEntries.Count} entree(s)");
        return builder.ToString();
    }

    private string BuildSessionOverviewText()
    {
        var snapshot = _runtime.WaptSessionService.GetSnapshot();
        var builder = new StringBuilder();
        builder.AppendLine($"Certificat              : {(snapshot.HasCertificatePassword ? "mot de passe memorise en memoire" : "aucun mot de passe memorise")}");
        builder.AppendLine($"Serveur WAPT            : {(snapshot.HasServerCredentials ? "identifiants memorises en memoire" : "aucun identifiant memorise")}");
        builder.AppendLine();

        if (snapshot.HasAnySecrets)
        {
            builder.AppendLine("Ces secrets sont reutilises uniquement pour les actions assistees executees dans WaptStudio.");
            builder.AppendLine("Ils ne sont ni enregistres dans les parametres, ni ecrits dans les logs, ni conserves dans l'historique.");
        }
        else
        {
            builder.AppendLine("Aucun secret n'est memorise pour l'instant.");
            builder.AppendLine("Lors d'un prochain Build, Sign ou Upload direct assiste, vous pourrez choisir de le memoriser pour la session en cours.");
        }

        builder.AppendLine();
        builder.AppendLine("Les workflows manuels ou lances dans un terminal externe peuvent encore redemander les secrets hors de l'application.");
        return builder.ToString();
    }

    private string BuildAssistantText(PackageCatalogItem? item, ValidationResult? validationResult)
    {
        if (validationResult is null)
        {
            return "Aucune analyse recente n'est disponible. Lancez d'abord 'Relire le paquet' ou 'Verifier le paquet' pour afficher une synthese technique exploitable.";
        }

        var actionLines = item is null
            ? Array.Empty<string>()
            : BuildAvailableActionLines(item, validationResult);

        var builder = new StringBuilder();
        builder.AppendLine(validationResult.Summary);
        builder.AppendLine();
        builder.AppendLine("Constats techniques:");
        foreach (var issue in validationResult.Issues)
        {
            builder.AppendLine($"- {FormatIssuePrefix(issue.Severity)} {issue.Message}");
        }

        builder.AppendLine();
        builder.AppendLine("Actions disponibles:");
        foreach (var actionLine in actionLines)
        {
            builder.AppendLine($"- {actionLine}");
        }

        if (actionLines.Length > 0)
        {
            builder.AppendLine();
            builder.AppendLine("A retenir:");
            foreach (var actionLine in actionLines)
            {
                builder.AppendLine($"- {actionLine}");
            }
        }
        return builder.ToString();
    }

    private (string Title, string Description, Button? RecommendedButton) BuildNextStepGuidance(PackageCatalogItem? item, ValidationResult? validationResult)
    {
        if (item is null)
        {
            return ("Choisir un paquet", "Selectionnez un paquet dans le catalogue pour afficher un resume clair et les actions disponibles.", null);
        }

        if (validationResult is null)
        {
            return ("Relire le paquet", "Commencez par relire le paquet pour actualiser son installateur, ses metadonnees et son potentiel de build.", _analyzeButton);
        }

        if (validationResult.Verdict == ReadinessVerdict.Blocked)
        {
            return ("Corriger les blocages", "Consultez la synthese technique, corrigez le paquet si besoin, puis relancez 'Verifier le paquet'.", _validateButton);
        }

        var publicationPreparation = PublicationPreparation.Evaluate(item.PackageFolder, item.PackageInfo, validationResult, _settings, _lastKnownWaptFilePackageFolder == item.PackageFolder ? _lastKnownWaptFilePath : null);
        if (validationResult.BuildPossible && !publicationPreparation.HasRealWaptFile)
        {
            return ("Construire le .wapt", "Le paquet est suffisamment prepare pour une construction. Une fois le .wapt genere, vous pourrez preparer la publication via WAPT Console ou choisir l'upload direct si votre environnement le permet.", _buildButton);
        }

        if (publicationPreparation.CanPrepareForConsolePublish)
        {
            var recommendation = publicationPreparation.RecommendedMode == PublicationMode.WaptConsole
                ? "Le .wapt est pret. Ouvrez la synthese finale puis publiez via WAPT Console."
                : "Le .wapt est pret. Ouvrez la synthese finale pour choisir entre upload direct et WAPT Console.";
            return ("Preparer la publication", recommendation, _uploadButton);
        }

        if (validationResult.BuildPossible)
        {
            return ("Construire le .wapt", "Le build est autorise. Vous pourrez ensuite signer ou envoyer le paquet.", _buildButton);
        }

        return ("Relire le paquet", "Le paquet a ete analyse. Relancez une relecture apres correction ou changement d'installeur.", _analyzeButton);
    }

    private static string ResolveCatalogRootFolder(AppSettings settings, string? candidate = null)
    {
        var preferred = string.IsNullOrWhiteSpace(candidate)
            ? settings.CatalogRootFolder ?? settings.DefaultPackageFolder ?? DefaultCatalogRootFolder
            : candidate;

        return string.IsNullOrWhiteSpace(preferred) ? DefaultCatalogRootFolder : preferred.Trim();
    }

    private static string FormatIssuePrefix(string severity)
        => severity.ToUpperInvariant() switch
        {
            "ERROR" => "Blocage:",
            "WARNING" => "A surveiller:",
            "OK" => "OK:",
            _ => "Info:"
        };

    private string[] BuildAvailableActionLines(PackageCatalogItem item, ValidationResult validationResult)
    {
        var publicationPreparation = PublicationPreparation.Evaluate(
            item.PackageFolder,
            item.PackageInfo,
            validationResult,
            _settings,
            _lastKnownWaptFilePackageFolder == item.PackageFolder ? _lastKnownWaptFilePath : null);

        var lines = new List<string>
        {
            "Relire le paquet : disponible pour actualiser les metadonnees et l'installeur detecte.",
            "Changer l'installeur : disponible avec apercu avant application.",
            _settings.CreateBackups
                ? "Revenir a l'ancienne version : possible via la derniere sauvegarde." 
                : "Revenir a l'ancienne version : indisponible tant que les sauvegardes sont desactivees."
        };

        lines.Add(validationResult.BuildPossible
            ? "Construire le .wapt : disponible."
            : "Construire le .wapt : bloque tant que les erreurs techniques ne sont pas corrigees.");

        lines.Add(_settings.EnableSigning
            ? (validationResult.BuildPossible
                ? "Signer le .wapt : disponible si un paquet a deja ete construit."
                : "Signer le .wapt : sera disponible apres correction puis construction.")
            : "Signer le .wapt : desactive dans les parametres.");

        lines.Add(publicationPreparation.CanPrepareForConsolePublish
            ? "Publier via WAPT Console : disponible."
            : "Publier via WAPT Console : disponible apres generation d'un vrai fichier .wapt.");

        if (_settings.EnableUpload)
        {
            lines.Add(publicationPreparation.CanPrepareDirectUpload
                ? "Upload direct : configure et disponible."
                : "Upload direct : configure, mais utilisable seulement quand le .wapt reel est present.");
        }
        else
        {
            lines.Add("Upload direct : desactive dans les parametres.");
        }

        return lines.ToArray();
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