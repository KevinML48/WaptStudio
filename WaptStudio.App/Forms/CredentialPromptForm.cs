using System;
using System.Windows.Forms;
using WaptStudio.Core.Models;

namespace WaptStudio.App.Forms;

public sealed class CredentialPromptForm : Form
{
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

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            Padding = new Padding(12),
            AutoSize = true
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var row = 0;
        var descriptionLabel = new Label
        {
            AutoSize = true,
            MaximumSize = new System.Drawing.Size(700, 0),
            Text = description + Environment.NewLine + Environment.NewLine + "Les secrets restent uniquement en memoire pendant l'action en cours. Aucun mot de passe ni identifiant n'est enregistre dans les parametres, les logs ou l'historique."
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(descriptionLabel, 0, row);
        layout.SetColumnSpan(descriptionLabel, 2);
        row++;

        if (_requireCertificatePassword)
        {
            AddRow(layout, row++, "Mot de passe certificat", _certificatePasswordTextBox);
        }

        if (_requireAdminUser)
        {
            AddRow(layout, row++, "Admin User", _adminUserTextBox);
        }

        if (_requireAdminPassword)
        {
            AddRow(layout, row++, "Password", _adminPasswordTextBox);
        }

        var buttonsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(12)
        };

        var confirmButton = new Button { Text = "Continuer", AutoSize = true };
        confirmButton.Click += Confirm;
        var cancelButton = new Button { Text = "Annuler", AutoSize = true, DialogResult = DialogResult.Cancel };

        buttonsPanel.Controls.Add(confirmButton);
        buttonsPanel.Controls.Add(cancelButton);

        Controls.Add(layout);
        Controls.Add(buttonsPanel);
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
        layout.Controls.Add(new Label { Text = labelText, AutoSize = true, Padding = new Padding(0, 6, 8, 0) }, 0, rowIndex);
        layout.Controls.Add(control, 1, rowIndex);
    }
}