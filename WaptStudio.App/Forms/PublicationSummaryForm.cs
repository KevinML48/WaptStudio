using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using WaptStudio.Core.Models;

namespace WaptStudio.App.Forms;

public sealed class PublicationSummaryForm : Form
{
    private static readonly Color SurfaceColor = Color.FromArgb(237, 241, 247);
    private static readonly Color PanelColor = Color.White;
    private static readonly Color PanelAltColor = Color.FromArgb(248, 250, 253);
    private static readonly Color BorderColor = Color.FromArgb(214, 223, 235);
    private static readonly Color AccentColor = Color.FromArgb(32, 76, 178);
    private static readonly Color AccentSoftColor = Color.FromArgb(232, 240, 255);
    private static readonly Color RecommendedColor = Color.FromArgb(18, 122, 86);
    private static readonly Color RecommendedSoftColor = Color.FromArgb(227, 248, 240);
    private static readonly Color WarningSoftColor = Color.FromArgb(255, 245, 226);
    private static readonly Color DangerSoftColor = Color.FromArgb(255, 234, 236);
    private static readonly Color WarningColor = Color.FromArgb(234, 149, 17);
    private static readonly Color BlockedColor = Color.FromArgb(220, 53, 53);
    private static readonly Color InfoColor = Color.FromArgb(82, 96, 120);
    private static readonly Color HeadingColor = Color.FromArgb(9, 18, 35);

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
            Padding = new Padding(24),
            BackColor = SurfaceColor
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var header = CreateCard();
        var headerTone = _result.PackageReady && _result.HasRealWaptFile ? RecommendedSoftColor : DangerSoftColor;
        header.BackColor = headerTone;
        var headerLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, BackColor = headerTone };
        headerLayout.Controls.Add(new Label
        {
            Text = _result.RecommendedMode == PublicationMode.WaptConsole ? "Mode recommande: WAPT Console" : "Mode recommande: Upload direct",
            AutoSize = true,
            ForeColor = _result.RecommendedMode == PublicationMode.WaptConsole ? RecommendedColor : WarningColor,
            BackColor = _result.RecommendedMode == PublicationMode.WaptConsole ? PanelColor : WarningSoftColor,
            Padding = new Padding(14, 8, 14, 8),
            Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 12)
        }, 0, 0);
        headerLayout.Controls.Add(new Label
        {
            Text = _result.PackageReady && _result.HasRealWaptFile ? "Paquet pret a publier" : "Publication a completer",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold),
            ForeColor = HeadingColor,
            Margin = new Padding(0, 0, 0, 4)
        }, 0, 1);
        headerLayout.Controls.Add(new Label
        {
            Text = _result.StatusMessage,
            AutoSize = true,
            ForeColor = _result.PackageReady && _result.HasRealWaptFile ? RecommendedColor : BlockedColor,
            Font = new Font("Segoe UI Semibold", 11.5F, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 8)
        }, 0, 2);
        headerLayout.Controls.Add(new Label
        {
            Text = _result.RecommendationMessage,
            AutoSize = true,
            MaximumSize = new Size(860, 0),
            ForeColor = InfoColor,
            Font = new Font("Segoe UI", 10F)
        }, 0, 3);
        header.Controls.Add(headerLayout);

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            BackColor = SurfaceColor,
            Margin = new Padding(0, 4, 0, 0)
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
            Font = new Font("Segoe UI", 9.75F),
            Margin = new Padding(0, 0, 0, 14)
        }, 0, 1);

        var buttonPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true, BackColor = PanelAltColor, Padding = new Padding(14, 14, 14, 6), Margin = new Padding(0, 0, 0, 12) };
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
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = PanelColor,
            BorderStyle = BorderStyle.None,
            Padding = new Padding(22),
            Margin = new Padding(0, 0, 12, 12)
        };

        panel.Paint += (_, e) =>
        {
            using var shadowPen = new Pen(Color.FromArgb(236, 240, 246), 3);
            using var borderPen = new Pen(BorderColor, 1);
            e.Graphics.DrawRectangle(shadowPen, 1, 2, panel.Width - 4, panel.Height - 5);
            e.Graphics.DrawRectangle(borderPen, 0, 0, panel.Width - 1, panel.Height - 1);
        };

        return panel;
    }

    private static Label CreateTitle(string text)
        => new()
        {
            Text = text,
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold),
            ForeColor = HeadingColor,
            Margin = new Padding(0, 0, 0, 10)
        };

    private static Control CreateField(string label, string value)
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 1, AutoSize = true, BackColor = PanelColor, Margin = new Padding(0, 0, 0, 10) };
        layout.Controls.Add(new Label { Text = label, AutoSize = true, ForeColor = InfoColor, Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold), Margin = new Padding(0, 0, 0, 4) }, 0, 0);
        layout.Controls.Add(new Label { Text = value, AutoSize = true, ForeColor = HeadingColor, Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold) }, 0, 1);
        return layout;
    }

    private static Control CreateMultiLineField(string label, string value)
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 1, AutoSize = true, BackColor = PanelColor, Margin = new Padding(0, 0, 0, 12) };
        layout.Controls.Add(new Label { Text = label, AutoSize = true, ForeColor = InfoColor, Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold), Margin = new Padding(0, 0, 0, 5) }, 0, 0);
        layout.Controls.Add(new TextBox { Multiline = true, ReadOnly = true, BorderStyle = BorderStyle.FixedSingle, Text = value, Height = 80, Dock = DockStyle.Top, BackColor = PanelAltColor, Font = new Font("Segoe UI", 9.25F) }, 0, 1);
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
            Padding = new Padding(18, 11, 18, 11),
            Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
            Margin = new Padding(0, 0, 10, 10)
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
            Padding = new Padding(16, 10, 16, 10),
            Font = new Font("Segoe UI", 9.75F, FontStyle.Regular),
            Margin = new Padding(0, 0, 10, 10)
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