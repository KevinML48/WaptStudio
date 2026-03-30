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
    private static readonly Color WarningColor = Color.FromArgb(234, 149, 17);
    private static readonly Color WarningSoftColor = Color.FromArgb(255, 245, 226);
    private static readonly Color BlockedColor = Color.FromArgb(220, 53, 53);
    private static readonly Color DangerSoftColor = Color.FromArgb(255, 234, 236);
    private static readonly Color ReadyColor = Color.FromArgb(18, 122, 86);
    private static readonly Color ReadySoftColor = Color.FromArgb(227, 248, 240);
    private static readonly Color HeadingColor = Color.FromArgb(9, 18, 35);
    private static readonly Color InfoColor = Color.FromArgb(82, 96, 120);

    private readonly PublicationPreparationResult _result;

    public PublicationSummaryForm(PublicationPreparationResult result)
    {
        _result = result;

        Text = "Publication finale";
        Width = 1180;
        Height = 860;
        MinimumSize = new Size(1060, 780);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = SurfaceColor;
        Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);

        InitializeComponent();
    }

    public PublicationSummaryAction SelectedAction { get; private set; }

    internal bool IsDirectUploadActionAvailable() => _result.CanPrepareDirectUpload;

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

        root.Controls.Add(BuildHeaderCard(), 0, 0);
        root.Controls.Add(BuildContentArea(), 0, 1);
        root.Controls.Add(BuildFooter(), 0, 2);

        Controls.Add(root);
    }

    private Control BuildHeaderCard()
    {
        var isReady = _result.PackageReady && _result.HasRealWaptFile;
        var backColor = isReady ? ReadySoftColor : (_result.PackageReady ? WarningSoftColor : DangerSoftColor);
        var accentColor = isReady ? ReadyColor : (_result.PackageReady ? WarningColor : BlockedColor);

        var card = CreateCard();
        card.BackColor = backColor;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            BackColor = backColor
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        layout.Controls.Add(new Label
        {
            Text = isReady ? "Publication prete" : "Publication a completer",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold),
            ForeColor = HeadingColor,
            Margin = new Padding(0, 0, 0, 6)
        }, 0, 0);
        layout.Controls.Add(new Label
        {
            Text = _result.StatusMessage,
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
            ForeColor = accentColor,
            Margin = new Padding(0, 0, 0, 8)
        }, 0, 1);
        layout.Controls.Add(new Label
        {
            Text = _result.RecommendationMessage,
            AutoSize = true,
            MaximumSize = new Size(980, 0),
            ForeColor = InfoColor,
            Margin = new Padding(0)
        }, 0, 2);

        card.Controls.Add(layout);
        return card;
    }

    private Control BuildContentArea()
    {
        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            BackColor = SurfaceColor,
            Margin = new Padding(0, 12, 0, 0)
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));

        content.Controls.Add(BuildSummaryCard(), 0, 0);
        content.Controls.Add(BuildActionsCard(), 1, 0);
        return content;
    }

    private Control BuildSummaryCard()
    {
        var card = CreateCard();
        var layout = CreateScrollableLayout();

        layout.Controls.Add(CreateSectionTitle("Synthese du paquet"), 0, 0);
        layout.Controls.Add(CreateFactsPanel(), 0, 1);
        layout.Controls.Add(CreateMultiLineField("Chemin du .wapt", _result.WaptFilePath ?? "Aucun fichier .wapt reel detecte."), 0, 2);
        layout.Controls.Add(CreateMultiLineField("Dossier source", _result.PackageFolder), 0, 3);

        card.Controls.Add(layout);
        return card;
    }

    private Control BuildActionsCard()
    {
        var card = CreateCard();
        var layout = CreateScrollableLayout();

        layout.Controls.Add(CreateSectionTitle("Actions utiles"), 0, 0);
        layout.Controls.Add(new Label
        {
            Text = "Gardez seulement les actions utiles a la fin du cycle: publier, copier le chemin et ouvrir les dossiers utiles.",
            AutoSize = true,
            MaximumSize = new Size(440, 0),
            ForeColor = InfoColor,
            Margin = new Padding(0, 0, 0, 12)
        }, 0, 1);
        layout.Controls.Add(BuildActionButtonsPanel(), 0, 2);
        layout.Controls.Add(CreateMultiLineField("Mode conseille", _result.RecommendedMode == PublicationMode.WaptConsole
            ? "WAPT Console reste le chemin prioritaire pour la publication finale dans cet environnement."
            : "L'upload direct est configure et peut etre utilise si l'acces serveur est reellement operationnel."), 0, 3);

        card.Controls.Add(layout);
        return card;
    }

    private Control CreateFactsPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            BackColor = PanelColor,
            Margin = new Padding(0, 0, 0, 14)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        AddFactCard(panel, 0, 0, "Nom du paquet", _result.PackageId ?? "Non detecte");
        AddFactCard(panel, 1, 0, "Version", _result.Version ?? "Non detectee");
        AddFactCard(panel, 0, 1, "Maturite", _result.Maturity ?? "Non detectee");
        AddFactCard(panel, 1, 1, "Fichier .wapt", _result.HasRealWaptFile ? "Present" : "Absent");
        AddFactCard(panel, 0, 2, "Publication WAPT Console", _result.CanPrepareForConsolePublish ? "Disponible" : "A preparer");
        AddFactCard(panel, 1, 2, "Upload direct", _result.CanPrepareDirectUpload ? "Disponible" : (_result.DirectUploadAvailable ? "Configure mais indisponible" : "Non configure"));

        return panel;
    }

    private Control BuildActionButtonsPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            BackColor = PanelAltColor,
            Padding = new Padding(14, 14, 14, 4),
            Margin = new Padding(0, 0, 0, 12)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var publishConsoleButton = CreatePrimaryButton("Publier via WAPT Console", AccentColor);
        publishConsoleButton.Enabled = _result.CanPrepareForConsolePublish;
        publishConsoleButton.Click += (_, _) => CloseWithAction(PublicationSummaryAction.MarkForWaptConsole);
        AddButton(panel, publishConsoleButton, 0);

        var directUploadButton = CreatePrimaryButton("Upload direct", WarningColor);
        directUploadButton.Enabled = _result.CanPrepareDirectUpload;
        directUploadButton.Click += (_, _) => CloseWithAction(PublicationSummaryAction.DirectUpload);
        AddButton(panel, directUploadButton, 1);

        var copyButton = CreateSecondaryButton("Copier le chemin .wapt");
        copyButton.Enabled = _result.HasRealWaptFile;
        copyButton.Click += (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(_result.WaptFilePath))
            {
                Clipboard.SetText(_result.WaptFilePath);
            }
        };
        AddButton(panel, copyButton, 2);

        var openWaptFolderButton = CreateSecondaryButton("Ouvrir le dossier .wapt");
        openWaptFolderButton.Enabled = _result.HasRealWaptFile;
        openWaptFolderButton.Click += (_, _) => OpenFolder(string.IsNullOrWhiteSpace(_result.WaptFilePath) ? null : Path.GetDirectoryName(_result.WaptFilePath));
        AddButton(panel, openWaptFolderButton, 3);

        var openSourceFolderButton = CreateSecondaryButton("Ouvrir le dossier source");
        openSourceFolderButton.Click += (_, _) => OpenFolder(_result.PackageFolder);
        AddButton(panel, openSourceFolderButton, 4);

        return panel;
    }

    private Control BuildFooter()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = SurfaceColor,
            Padding = new Padding(0, 14, 0, 0)
        };

        var closeButton = CreateSecondaryButton("Fermer");
        closeButton.Click += (_, _) => Close();
        panel.Controls.Add(closeButton);
        return panel;
    }

    private void CloseWithAction(PublicationSummaryAction action)
    {
        SelectedAction = action;
        DialogResult = DialogResult.OK;
        Close();
    }

    private static TableLayoutPanel CreateScrollableLayout()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            ColumnCount = 1,
            BackColor = PanelColor
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        return layout;
    }

    private static Panel CreateCard()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = PanelColor,
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

    private static Label CreateSectionTitle(string text)
        => new()
        {
            Text = text,
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold),
            ForeColor = HeadingColor,
            Margin = new Padding(0, 0, 0, 12)
        };

    private static void AddFactCard(TableLayoutPanel panel, int column, int row, string label, string value)
    {
        while (panel.RowStyles.Count <= row)
        {
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        var card = new Panel
        {
            Dock = DockStyle.Top,
            BackColor = PanelAltColor,
            Padding = new Padding(14),
            Margin = new Padding(column == 0 ? 0 : 10, 0, 0, 10),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            BackColor = PanelAltColor
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.Controls.Add(new Label { Text = label, AutoSize = true, ForeColor = InfoColor, Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold), Margin = new Padding(0, 0, 0, 4) }, 0, 0);
        layout.Controls.Add(new Label { Text = value, AutoSize = true, ForeColor = HeadingColor, Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold) }, 0, 1);
        card.Controls.Add(layout);
        panel.Controls.Add(card, column, row);
    }

    private static Control CreateMultiLineField(string label, string value)
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            BackColor = PanelColor,
            Margin = new Padding(0, 0, 0, 12)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.Controls.Add(new Label { Text = label, AutoSize = true, ForeColor = InfoColor, Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold), Margin = new Padding(0, 0, 0, 5) }, 0, 0);
        layout.Controls.Add(new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            BorderStyle = BorderStyle.FixedSingle,
            Text = value,
            Height = 94,
            Dock = DockStyle.Top,
            BackColor = PanelAltColor,
            Font = new Font("Segoe UI", 9.25F),
            ScrollBars = ScrollBars.Vertical
        }, 0, 1);
        return layout;
    }

    private static Button CreatePrimaryButton(string text, Color backColor)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = false,
            FlatStyle = FlatStyle.Flat,
            BackColor = backColor,
            ForeColor = Color.White,
            Dock = DockStyle.Fill,
            Height = 48,
            MinimumSize = new Size(260, 48),
            Padding = new Padding(18, 11, 18, 11),
            Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 10),
            TextAlign = ContentAlignment.MiddleCenter,
            UseVisualStyleBackColor = false
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = ControlPaint.Dark(backColor);
        button.FlatAppearance.MouseDownBackColor = ControlPaint.DarkDark(backColor);
        return button;
    }

    private static Button CreateSecondaryButton(string text)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = false,
            FlatStyle = FlatStyle.Flat,
            BackColor = PanelColor,
            ForeColor = HeadingColor,
            Dock = DockStyle.Fill,
            Height = 44,
            MinimumSize = new Size(220, 44),
            Padding = new Padding(16, 10, 16, 10),
            Font = new Font("Segoe UI", 9.75F),
            Margin = new Padding(0, 0, 0, 10),
            TextAlign = ContentAlignment.MiddleCenter,
            UseVisualStyleBackColor = false
        };
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = BorderColor;
        button.FlatAppearance.MouseOverBackColor = AccentSoftColor;
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(220, 230, 250);
        return button;
    }

    private static void AddButton(TableLayoutPanel panel, Button button, int rowIndex)
    {
        while (panel.RowStyles.Count <= rowIndex)
        {
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        panel.Controls.Add(button, 0, rowIndex);
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
