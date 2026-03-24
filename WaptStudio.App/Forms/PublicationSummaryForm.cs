using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using WaptStudio.Core.Models;

namespace WaptStudio.App.Forms;

public sealed class PublicationSummaryForm : Form
{
    private static readonly Color SurfaceColor = Color.FromArgb(243, 245, 249);
    private static readonly Color PanelColor = Color.White;
    private static readonly Color BorderColor = Color.FromArgb(228, 233, 240);
    private static readonly Color AccentColor = Color.FromArgb(37, 99, 186);
    private static readonly Color RecommendedColor = Color.FromArgb(22, 128, 92);
    private static readonly Color WarningColor = Color.FromArgb(234, 149, 17);
    private static readonly Color BlockedColor = Color.FromArgb(220, 53, 53);
    private static readonly Color InfoColor = Color.FromArgb(100, 116, 139);
    private static readonly Color HeadingColor = Color.FromArgb(15, 23, 42);

    private readonly PublicationPreparationResult _result;

    public PublicationSummaryForm(PublicationPreparationResult result)
    {
        _result = result;

        Text = "Publication finale";
        Width = 980;
        Height = 720;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = SurfaceColor;
        Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);

        InitializeComponent();
    }

    public PublicationSummaryAction SelectedAction { get; private set; }

    private void InitializeComponent()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(20),
            BackColor = SurfaceColor
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var header = CreateCard();
        var headerLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, BackColor = PanelColor };
        headerLayout.Controls.Add(new Label
        {
            Text = _result.PackageReady && _result.HasRealWaptFile ? "Paquet pret a publier" : "Publication a completer",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold),
            ForeColor = HeadingColor,
            Margin = new Padding(0, 0, 0, 4)
        }, 0, 0);
        headerLayout.Controls.Add(new Label
        {
            Text = _result.StatusMessage,
            AutoSize = true,
            ForeColor = _result.PackageReady && _result.HasRealWaptFile ? RecommendedColor : BlockedColor,
            Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 6)
        }, 0, 1);
        headerLayout.Controls.Add(new Label
        {
            Text = _result.RecommendationMessage,
            AutoSize = true,
            MaximumSize = new Size(860, 0),
            ForeColor = InfoColor
        }, 0, 2);
        header.Controls.Add(headerLayout);

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            BackColor = SurfaceColor
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));

        var detailsCard = CreateCard();
        var detailsLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 9, BackColor = PanelColor };
        detailsLayout.Controls.Add(CreateTitle("Synthese du paquet"), 0, 0);
        detailsLayout.Controls.Add(CreateField("Package ID", _result.PackageId ?? "Non detecte"), 0, 1);
        detailsLayout.Controls.Add(CreateField("Version", _result.Version ?? "Non detectee"), 0, 2);
        detailsLayout.Controls.Add(CreateField("Maturite", _result.Maturity ?? "Non detectee"), 0, 3);
        detailsLayout.Controls.Add(CreateField("Paquet pret", _result.PackageReady ? "Oui" : "Non"), 0, 4);
        detailsLayout.Controls.Add(CreateField("Vrai fichier .wapt", _result.HasRealWaptFile ? "Oui" : "Non"), 0, 5);
        detailsLayout.Controls.Add(CreateField("Action recommandee", _result.RecommendedMode == PublicationMode.WaptConsole ? "Publication via WAPT Console" : "Upload direct"), 0, 6);
        detailsLayout.Controls.Add(CreateMultiLineField("Chemin exact du .wapt", _result.WaptFilePath ?? "Aucun fichier .wapt reel detecte."), 0, 7);
        detailsLayout.Controls.Add(CreateMultiLineField("Dossier source du paquet", _result.PackageFolder), 0, 8);
        detailsCard.Controls.Add(detailsLayout);

        var actionsCard = CreateCard();
        var actionsLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6, BackColor = PanelColor };
        actionsLayout.Controls.Add(CreateTitle("Actions utiles"), 0, 0);
        actionsLayout.Controls.Add(new Label
        {
            Text = "Utilisez WaptStudio pour preparer et verifier le .wapt, puis publiez via WAPT Console si l'upload direct n'est pas maitrise dans votre environnement.",
            AutoSize = true,
            MaximumSize = new Size(360, 0),
            ForeColor = InfoColor,
            Margin = new Padding(0, 0, 0, 12)
        }, 0, 1);

        var buttonPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true, BackColor = PanelColor };
        var copyButton = CreateSecondaryButton("Copier le chemin du .wapt");
        copyButton.Enabled = _result.HasRealWaptFile;
        copyButton.Click += (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(_result.WaptFilePath))
            {
                Clipboard.SetText(_result.WaptFilePath);
            }
        };

        var openWaptFolderButton = CreateSecondaryButton("Ouvrir le dossier du .wapt");
        openWaptFolderButton.Enabled = _result.HasRealWaptFile;
        openWaptFolderButton.Click += (_, _) => OpenFolder(string.IsNullOrWhiteSpace(_result.WaptFilePath) ? null : Path.GetDirectoryName(_result.WaptFilePath));

        var openPackageFolderButton = CreateSecondaryButton("Ouvrir le dossier source du paquet");
        openPackageFolderButton.Click += (_, _) => OpenFolder(_result.PackageFolder);

        var markConsoleButton = CreatePrimaryButton("Marquer a publier dans WAPT Console", _result.RecommendedMode == PublicationMode.WaptConsole ? RecommendedColor : AccentColor);
        markConsoleButton.Enabled = _result.CanPrepareForConsolePublish;
        markConsoleButton.Click += (_, _) =>
        {
            SelectedAction = PublicationSummaryAction.MarkForWaptConsole;
            DialogResult = DialogResult.OK;
            Close();
        };

        var directUploadButton = CreatePrimaryButton("Upload direct", _result.RecommendedMode == PublicationMode.DirectUpload ? RecommendedColor : WarningColor);
        directUploadButton.Enabled = _result.CanPrepareForConsolePublish && _result.DirectUploadAvailable;
        directUploadButton.Click += (_, _) =>
        {
            SelectedAction = PublicationSummaryAction.DirectUpload;
            DialogResult = DialogResult.OK;
            Close();
        };

        buttonPanel.Controls.Add(copyButton);
        buttonPanel.Controls.Add(openWaptFolderButton);
        buttonPanel.Controls.Add(openPackageFolderButton);
        buttonPanel.Controls.Add(markConsoleButton);
        buttonPanel.Controls.Add(directUploadButton);
        actionsLayout.Controls.Add(buttonPanel, 0, 2);
        actionsLayout.Controls.Add(CreateMultiLineField("Mode recommande", _result.RecommendedMode == PublicationMode.WaptConsole
            ? "Le paquet est pret a etre publie via WAPT Console. Utilisez l'upload direct uniquement si votre poste dispose reellement d'un acces serveur operationnel."
            : "L'upload direct est configure comme mode recommande dans cet environnement. La publication via WAPT Console reste disponible si besoin."), 0, 3);
        actionsLayout.Controls.Add(CreateMultiLineField("Ce que WaptStudio garantit", "- le vrai .wapt a ete detecte\r\n- le package id, la version et la maturite sont affiches\r\n- aucun secret n'est stocke\r\n- aucun faux succes d'upload n'est affiche"), 0, 4);
        actionsCard.Controls.Add(actionsLayout);

        content.Controls.Add(detailsCard, 0, 0);
        content.Controls.Add(actionsCard, 1, 0);

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = SurfaceColor,
            Padding = new Padding(0)
        };
        var closeButton = CreateSecondaryButton("Fermer");
        closeButton.Click += (_, _) => Close();
        footer.Controls.Add(closeButton);

        root.Controls.Add(header, 0, 0);
        root.Controls.Add(content, 0, 1);
        root.Controls.Add(footer, 0, 2);
        Controls.Add(root);
    }

    private static Panel CreateCard()
        => new()
        {
            Dock = DockStyle.Fill,
            BackColor = PanelColor,
            BorderStyle = BorderStyle.None,
            Padding = new Padding(18),
            Margin = new Padding(0, 0, 10, 10)
        };

    private static Label CreateTitle(string text)
        => new()
        {
            Text = text,
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 11.5F, FontStyle.Bold),
            ForeColor = HeadingColor,
            Margin = new Padding(0, 0, 0, 8)
        };

    private static Control CreateField(string label, string value)
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 1, AutoSize = true, BackColor = PanelColor, Margin = new Padding(0, 0, 0, 8) };
        layout.Controls.Add(new Label { Text = label, AutoSize = true, ForeColor = InfoColor, Font = new Font("Segoe UI", 9F), Margin = new Padding(0, 0, 0, 2) }, 0, 0);
        layout.Controls.Add(new Label { Text = value, AutoSize = true, ForeColor = HeadingColor, Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold) }, 0, 1);
        return layout;
    }

    private static Control CreateMultiLineField(string label, string value)
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 1, AutoSize = true, BackColor = PanelColor, Margin = new Padding(0, 0, 0, 10) };
        layout.Controls.Add(new Label { Text = label, AutoSize = true, ForeColor = InfoColor, Font = new Font("Segoe UI", 9F), Margin = new Padding(0, 0, 0, 4) }, 0, 0);
        layout.Controls.Add(new TextBox { Multiline = true, ReadOnly = true, BorderStyle = BorderStyle.FixedSingle, Text = value, Height = 72, Dock = DockStyle.Top }, 0, 1);
        return layout;
    }

    private static Button CreatePrimaryButton(string text, Color backColor)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            FlatStyle = FlatStyle.Flat,
            BackColor = backColor,
            ForeColor = Color.White,
            Padding = new Padding(14, 8, 14, 8),
            Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
            Margin = new Padding(0, 0, 8, 8)
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
            FlatStyle = FlatStyle.Flat,
            BackColor = PanelColor,
            ForeColor = HeadingColor,
            Padding = new Padding(14, 8, 14, 8),
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
            Margin = new Padding(0, 0, 8, 8)
        };
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = BorderColor;
        return button;
    }

    private static void OpenFolder(string? folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
        {
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = folderPath,
            UseShellExecute = true
        });
    }
}

public enum PublicationSummaryAction
{
    None = 0,
    MarkForWaptConsole = 1,
    DirectUpload = 2
}