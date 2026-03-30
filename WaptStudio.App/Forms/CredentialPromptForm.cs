using System;
using System.Drawing;
using System.Windows.Forms;
using WaptStudio.Core.Models;

namespace WaptStudio.App.Forms;

public sealed class CredentialPromptForm : Form
{
    private static readonly Color SurfaceColor = Color.FromArgb(243, 245, 249);
    private static readonly Color PanelColor = Color.White;
    private static readonly Color BorderColor = Color.FromArgb(228, 233, 240);
    private static readonly Color AccentColor = Color.FromArgb(37, 99, 186);
    private static readonly Color InfoColor = Color.FromArgb(100, 116, 139);
    private static readonly Color HeadingColor = Color.FromArgb(15, 23, 42);

    private readonly bool _requireCertificatePassword;
    private readonly bool _requireAdminUser;
    private readonly bool _requireAdminPassword;
    private readonly TextBox _certificatePasswordTextBox = new() { Dock = DockStyle.Fill, UseSystemPasswordChar = true };
    private readonly TextBox _adminUserTextBox = new() { Dock = DockStyle.Fill };
    private readonly TextBox _adminPasswordTextBox = new() { Dock = DockStyle.Fill, UseSystemPasswordChar = true };

    public CredentialPromptForm(string title, string description, bool requireCertificatePassword, bool requireAdminUser, bool requireAdminPassword)
    {
        _requireCertificatePassword = requireCertificatePassword;
        _requireAdminUser = requireAdminUser;
        _requireAdminPassword = requireAdminPassword;

        Text = title;
        Width = 760;
        Height = 420;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        BackColor = SurfaceColor;
        Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(16),
            BackColor = SurfaceColor
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = PanelColor,
            BorderStyle = BorderStyle.None,
            Padding = new Padding(20),
            Margin = new Padding(0, 0, 0, 10)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            AutoSize = true,
            BackColor = PanelColor
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var row = 0;
        var titleLabel = new Label
        {
            AutoSize = true,
            Text = title,
            Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
            ForeColor = HeadingColor,
            Margin = new Padding(0, 0, 0, 6)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(titleLabel, 0, row);
        layout.SetColumnSpan(titleLabel, 2);
        row++;

        var descriptionLabel = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(660, 0),
            Text = description + Environment.NewLine + Environment.NewLine + "Les secrets restent uniquement en memoire pendant l'action en cours. Aucun mot de passe ni identifiant n'est enregistre dans les parametres, les logs ou l'historique.",
            ForeColor = InfoColor,
            Margin = new Padding(0, 0, 0, 12)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(descriptionLabel, 0, row);
        layout.SetColumnSpan(descriptionLabel, 2);
        row++;

        var reassuranceLabel = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(660, 0),
            Text = "Renseignez uniquement les champs demandes ci-dessous, puis continuez. WaptStudio purge ces donnees des que l'action est terminee.",
            ForeColor = InfoColor,
            Margin = new Padding(0, 0, 0, 12)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(reassuranceLabel, 0, row);
        layout.SetColumnSpan(reassuranceLabel, 2);
        row++;

        if (_requireCertificatePassword)
        {
            AddRow(layout, row++, "Mot de passe certificat", _certificatePasswordTextBox);
        }

        if (_requireAdminUser)
        {
            AddRow(layout, row++, "Identifiant administrateur WAPT", _adminUserTextBox);
        }

        if (_requireAdminPassword)
        {
            AddRow(layout, row++, "Mot de passe administrateur WAPT", _adminPasswordTextBox);
        }

        card.Controls.Add(layout);

        var buttonsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0),
            BackColor = SurfaceColor
        };

        var confirmButton = new Button { Text = "Continuer", AutoSize = true };
        confirmButton.Click += Confirm;
        var cancelButton = new Button { Text = "Annuler", AutoSize = true, DialogResult = DialogResult.Cancel };

        foreach (var button in new[] { confirmButton, cancelButton })
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = BorderColor;
            button.FlatAppearance.BorderSize = button == confirmButton ? 0 : 1;
            button.Padding = new Padding(14, 8, 14, 8);
            button.Margin = new Padding(8, 0, 0, 0);
            button.BackColor = button == confirmButton ? AccentColor : PanelColor;
            button.ForeColor = button == confirmButton ? Color.White : HeadingColor;
            button.Font = new Font("Segoe UI", 9.5F, button == confirmButton ? FontStyle.Bold : FontStyle.Regular);
        }

        buttonsPanel.Controls.Add(confirmButton);
        buttonsPanel.Controls.Add(cancelButton);

        root.Controls.Add(card, 0, 0);
        root.Controls.Add(buttonsPanel, 0, 1);
        Controls.Add(root);
        AcceptButton = confirmButton;
        CancelButton = cancelButton;
    }

    public WaptExecutionContext? ExecutionContext { get; private set; }

    private void Confirm(object? sender, EventArgs e)
    {
        if (_requireCertificatePassword && string.IsNullOrWhiteSpace(_certificatePasswordTextBox.Text))
        {
            ShowValidation("Le mot de passe du certificat est requis.");
            return;
        }

        if (_requireAdminUser && string.IsNullOrWhiteSpace(_adminUserTextBox.Text))
        {
            ShowValidation("Le login administrateur WAPT est requis.");
            return;
        }

        if (_requireAdminPassword && string.IsNullOrWhiteSpace(_adminPasswordTextBox.Text))
        {
            ShowValidation("Le mot de passe administrateur WAPT est requis.");
            return;
        }

        ExecutionContext = new WaptExecutionContext
        {
            CertificatePassword = EmptyToNull(_certificatePasswordTextBox.Text),
            AdminUser = EmptyToNull(_adminUserTextBox.Text),
            AdminPassword = EmptyToNull(_adminPasswordTextBox.Text)
        };

        DialogResult = DialogResult.OK;
        Close();
    }

    private void ShowValidation(string message)
        => MessageBox.Show(this, message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);

    private static string? EmptyToNull(string value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void AddRow(TableLayoutPanel layout, int rowIndex, string labelText, Control control)
    {
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        control.Margin = new Padding(0, 0, 0, 10);
        layout.Controls.Add(new Label { Text = labelText, AutoSize = true, Padding = new Padding(0, 6, 8, 0), Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold), ForeColor = HeadingColor }, 0, rowIndex);
        layout.Controls.Add(control, 1, rowIndex);
    }
}