using System.Globalization;

namespace TubeVault;

// Ventana de primer arranque. Solo presenta estados comprensibles;
// los detalles técnicos permanecen en el log.
internal sealed class DependencySetupForm : Form
{
    private static readonly Color AccentColor = Color.FromArgb(0, 103, 192);

    private readonly TextService text;
    private readonly AppTheme theme;
    private readonly DependencyBootstrapService bootstrap;
    private readonly bool isInitialPreparation;
    private readonly CancellationTokenSource cancellation = new();
    private readonly Label firstPreparationLabel = new();
    private readonly Label statusLabel = new();
    private readonly Label progressDetailsLabel = new();
    private readonly ProgressBar progressBar = new();
    private readonly RoundedButton retryButton = new();
    private readonly RoundedButton exitButton = new();
    private readonly FlowLayoutPanel actionsPanel = new();
    private bool running;

    public DependencySetupForm(
        TextService text,
        AppTheme theme,
        DependencyBootstrapService bootstrap,
        bool isInitialPreparation)
    {
        this.text = text;
        this.theme = theme;
        this.bootstrap = bootstrap;
        this.isInitialPreparation = isInitialPreparation;

        ConfigureWindow();
        BuildInterface(isInitialPreparation);
        ApplyTheme();
        Shown += async (_, _) => await PrepareAsync();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (running && e.CloseReason == CloseReason.UserClosing)
        {
            cancellation.Cancel();
        }

        base.OnFormClosing(e);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        cancellation.Cancel();
        cancellation.Dispose();
        base.OnFormClosed(e);
    }

    private void ConfigureWindow()
    {
        Text = text.Get("SetupTitle");
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(460, 265);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Segoe UI", 10F);
    }

    private void BuildInterface(bool isInitialPreparation)
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(28, 24, 28, 22),
            ColumnCount = 1,
            RowCount = 7
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var title = new Label
        {
            AutoSize = true,
            Text = text.Get("SetupTitle"),
            Font = new Font("Segoe UI Semibold", 18F),
            Margin = new Padding(0, 0, 0, 16)
        };

        firstPreparationLabel.AutoSize = true;
        firstPreparationLabel.Text = text.Get("SetupFirstPreparationHint");
        firstPreparationLabel.Tag = "secondary";
        firstPreparationLabel.Margin = new Padding(0, 0, 0, 12);
        firstPreparationLabel.Visible = isInitialPreparation;

        statusLabel.Dock = DockStyle.Fill;
        statusLabel.Text = text.Get("SetupChecking");
        statusLabel.Tag = "secondary";
        statusLabel.TextAlign = ContentAlignment.MiddleLeft;

        progressDetailsLabel.AutoSize = true;
        progressDetailsLabel.Tag = "secondary";
        progressDetailsLabel.Margin = new Padding(0, 5, 0, 10);
        progressDetailsLabel.Visible = false;

        progressBar.Dock = DockStyle.Fill;
        progressBar.Height = 8;
        progressBar.Style = ProgressBarStyle.Marquee;
        progressBar.MarqueeAnimationSpeed = 25;

        actionsPanel.Dock = DockStyle.Fill;
        actionsPanel.AutoSize = true;
        actionsPanel.FlowDirection = FlowDirection.RightToLeft;
        actionsPanel.WrapContents = false;
        actionsPanel.Margin = Padding.Empty;
        actionsPanel.Visible = false;

        ConfigureButton(retryButton, text.Get("SetupRetry"), primary: true);
        retryButton.Click += async (_, _) =>
        {
            firstPreparationLabel.Visible = false;
            await PrepareAsync();
        };
        ConfigureButton(exitButton, text.Get("SetupExit"), primary: false);
        exitButton.Click += (_, _) =>
        {
            DialogResult = DialogResult.Abort;
            Close();
        };
        actionsPanel.Controls.Add(retryButton);
        actionsPanel.Controls.Add(exitButton);

        root.Controls.Add(title, 0, 0);
        root.Controls.Add(firstPreparationLabel, 0, 1);
        root.Controls.Add(statusLabel, 0, 2);
        root.Controls.Add(progressDetailsLabel, 0, 3);
        root.Controls.Add(progressBar, 0, 4);
        root.Controls.Add(actionsPanel, 0, 6);
        Controls.Add(root);
    }

    private async Task PrepareAsync()
    {
        if (running)
        {
            return;
        }

        running = true;
        actionsPanel.Visible = false;
        progressBar.Visible = true;
        progressBar.Style = ProgressBarStyle.Marquee;
        progressBar.MarqueeAnimationSpeed = 25;
        progressBar.Value = 0;
        progressDetailsLabel.Visible = false;
        statusLabel.Text = text.Get("SetupChecking");
        ControlBox = false;

        var progress = new Progress<DependencyProgress>(UpdateProgress);

        try
        {
            await bootstrap.EnsureDependenciesAsync(progress, cancellation.Token);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (OperationCanceledException)
        {
            DialogResult = DialogResult.Abort;
            Close();
        }
        catch
        {
            statusLabel.Text = text.Get(
                isInitialPreparation ? "SetupInitialFailed" : "SetupFailed");
            progressDetailsLabel.Visible = false;
            progressBar.Visible = false;
            actionsPanel.Visible = true;
            ControlBox = true;
        }
        finally
        {
            running = false;
        }
    }

    private void UpdateProgress(DependencyProgress progress)
    {
        if (IsDisposed)
        {
            return;
        }

        statusLabel.Text = text.Get(progress.TextKey);

        if (!progress.IsDownload)
        {
            progressDetailsLabel.Visible = false;
            progressBar.Style = ProgressBarStyle.Marquee;
            progressBar.MarqueeAnimationSpeed = 25;
            progressBar.Value = 0;
            return;
        }

        progressDetailsLabel.Visible = true;
        var downloaded = FormatMegabytes(progress.BytesDownloaded);

        if (progress.TotalBytes is > 0)
        {
            var percentage = (int)Math.Clamp(
                progress.BytesDownloaded * 100L / progress.TotalBytes.Value,
                0,
                100);
            progressBar.Style = ProgressBarStyle.Continuous;
            progressBar.MarqueeAnimationSpeed = 0;
            progressBar.Value = percentage;
            progressDetailsLabel.Text = text.Get(
                "SetupDownloadedOf",
                percentage,
                downloaded,
                FormatMegabytes(progress.TotalBytes.Value));
            return;
        }

        progressBar.Style = ProgressBarStyle.Marquee;
        progressBar.MarqueeAnimationSpeed = 25;
        progressBar.Value = 0;
        progressDetailsLabel.Text = text.Get("SetupDownloaded", downloaded);
    }

    private string FormatMegabytes(long bytes)
    {
        var culture = CultureInfo.GetCultureInfo(
            text.Language == AppLanguage.English ? "en" : "es");
        return (bytes / 1024D / 1024D).ToString("N1", culture);
    }

    private static void ConfigureButton(RoundedButton button, string label, bool primary)
    {
        button.Text = label;
        button.Size = new Size(112, 36);
        button.Margin = primary ? Padding.Empty : new Padding(0, 0, 10, 0);
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = primary ? 0 : 1;
        button.Font = new Font("Segoe UI Semibold", 9.5F);
        button.Cursor = Cursors.Hand;
        button.UseVisualStyleBackColor = false;
    }

    private void ApplyTheme()
    {
        ThemeService.Apply(this, theme);
        var colors = ThemeService.GetColors(theme);

        retryButton.BackColor = AccentColor;
        retryButton.ForeColor = Color.White;
        exitButton.BackColor = colors.SecondaryButton;
        exitButton.ForeColor = colors.Text;
        exitButton.FlatAppearance.BorderColor = colors.Border;
    }
}
