namespace TubeVault;

internal sealed class AboutForm : Form
{
    public AboutForm(
        TextService text,
        AppTheme theme,
        string ytDlpVersion,
        string ffmpegVersion)
    {
        Text = text.Get("AboutTitle");
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(520, 475);
        MinimumSize = new Size(480, 430);
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Segoe UI", 10F);

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(28, 24, 28, 22),
            ColumnCount = 2,
            RowCount = 11
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        var title = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 22F),
            Text = "TubeVault",
            Margin = new Padding(0, 0, 0, 18)
        };
        content.SetColumnSpan(title, 2);
        content.Controls.Add(title, 0, 0);

        AddRow(content, 1, text.Get("AboutVersion"), AppMetadata.Version);
        AddRow(content, 2, "yt-dlp", ytDlpVersion);
        AddRow(content, 3, "FFmpeg", ffmpegVersion);
        AddRow(content, 4, text.Get("AboutArchitecture"), "x64");
        AddRow(content, 5, text.Get("AboutRuntime"), ".NET 10");
        AddRow(content, 6, text.Get("AboutAuthor"), "joseamc91");
        AddRow(content, 7, text.Get("AboutPlatform"), "Windows");
        AddRow(content, 8, text.Get("AboutCredits"), "yt-dlp · FFmpeg");

        var licenseHeading = CreateLabel(text.Get("AboutLicenses"), true);
        licenseHeading.Margin = new Padding(0, 16, 12, 4);
        content.Controls.Add(licenseHeading, 0, 9);

        var license = CreateLabel(text.Get("AboutLicenseText"), false);
        license.MaximumSize = new Size(320, 0);
        license.Margin = new Padding(0, 16, 0, 4);
        content.Controls.Add(license, 1, 9);

        var closeButton = new RoundedButton
        {
            Text = text.Get("Close"),
            AutoSize = false,
            Width = 110,
            Height = 36,
            Anchor = AnchorStyles.Right,
            DialogResult = DialogResult.OK,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(0, 18, 0, 0)
        };
        content.Controls.Add(closeButton, 1, 10);

        Controls.Add(content);
        AcceptButton = closeButton;
        CancelButton = closeButton;

        ThemeService.Apply(this, theme);
        var colors = ThemeService.GetColors(theme);
        closeButton.BackColor = colors.SecondaryButton;
        closeButton.ForeColor = colors.Text;
        closeButton.FlatAppearance.BorderColor = colors.Border;
        closeButton.FlatAppearance.MouseOverBackColor = colors.SecondaryButtonHover;
    }

    private static void AddRow(TableLayoutPanel content, int row, string name, string value)
    {
        var nameLabel = CreateLabel(name + ":", true);
        nameLabel.Tag = "secondary";
        content.Controls.Add(nameLabel, 0, row);
        content.Controls.Add(CreateLabel(value, false), 1, row);
    }

    private static Label CreateLabel(string value, bool semibold)
    {
        return new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI" + (semibold ? " Semibold" : string.Empty), 10F),
            Text = value,
            Margin = new Padding(0, 5, 12, 5)
        };
    }
}
