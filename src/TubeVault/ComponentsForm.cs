using System.Globalization;
using System.Diagnostics;

namespace TubeVault;

// Mantiene las operaciones técnicas fuera de Ajustes sin duplicar su implementación.
internal sealed class ComponentsForm : Form
{
    private static readonly Color AccentColor = Color.FromArgb(0, 103, 192);
    private static readonly Color AccentHoverColor = Color.FromArgb(0, 90, 158);

    private readonly TextService text;
    private readonly AppTheme theme;
    private readonly YtDlpUpdateService updateService;
    private readonly TubeVaultUpdateService tubeVaultUpdateService;
    private readonly DependencyService dependencyService;
    private readonly DependencyBootstrapService bootstrapService;
    private readonly SettingsService settingsService;
    private readonly LogService log;
    private readonly CancellationTokenSource cancellation = new();
    private readonly ToolTip toolTip = new();
    private readonly RoundedButton checkYtDlpButton = new();
    private readonly RoundedButton checkTubeVaultButton = new();
    private readonly RoundedButton viewReleaseButton = new();
    private readonly Label tubeVaultVersionLabel = new();
    private readonly Label tubeVaultLastCheckLabel = new();
    private readonly Label tubeVaultStatusLabel = new();
    private readonly RoundedButton updateYtDlpButton = new();
    private readonly RoundedButton checkFfmpegButton = new();
    private readonly RoundedButton updateFfmpegButton = new();
    private readonly RoundedButton repairButton = new();
    private readonly RoundedButton closeButton = new();
    private readonly Label ytDlpVersionLabel = new();
    private readonly Label ytDlpLastCheckLabel = new();
    private readonly Label ytDlpStatusLabel = new();
    private readonly Label ffmpegVersionLabel = new();
    private readonly Label ffmpegLastCheckLabel = new();
    private readonly Label ffmpegStatusLabel = new();
    private readonly Label repairStatusLabel = new();
    private DateTimeOffset? lastYtDlpCheck;
    private DateTimeOffset? lastFfmpegCheck;
    private string installedYtDlpVersion;
    private string installedFfmpegVersion;
    private bool ytDlpAvailable = true;
    private bool ffmpegAvailable = true;
    private bool busy;
    private bool tubeVaultCheckFailed;

    public ComponentsForm(
        TextService text,
        AppTheme theme,
        YtDlpUpdateInfo? availableUpdate,
        FfmpegUpdateInfo? availableFfmpegUpdate,
        YtDlpUpdateService updateService,
        TubeVaultUpdateService tubeVaultUpdateService,
        DependencyService dependencyService,
        DependencyBootstrapService bootstrapService,
        SettingsService settingsService,
        LogService log)
    {
        this.text = text;
        this.theme = theme;
        AvailableUpdate = availableUpdate;
        AvailableFfmpegUpdate = availableFfmpegUpdate;
        installedYtDlpVersion = availableUpdate?.LocalVersion ?? "—";
        installedFfmpegVersion = availableFfmpegUpdate?.LocalVersion ?? "—";
        this.updateService = updateService;
        this.tubeVaultUpdateService = tubeVaultUpdateService;
        this.dependencyService = dependencyService;
        this.bootstrapService = bootstrapService;
        this.settingsService = settingsService;
        this.log = log;
        lastYtDlpCheck = settingsService.LoadLastYtDlpUpdateCheck();
        lastFfmpegCheck = settingsService.LoadLastFfmpegUpdateCheck();

        ConfigureWindow();
        BuildInterface();
        ConfigureToolTips();
        ApplyTheme();
        RefreshInformation();
        tubeVaultUpdateService.CheckStateChanged += TubeVaultUpdateStateChanged;
        Shown += async (_, _) =>
        {
            try
            {
                await LoadInstalledVersionsAsync();
            }
            catch (OperationCanceledException)
            {
                // Cerrar la ventana cancela únicamente la lectura informativa en curso.
            }
        };
    }

    public YtDlpUpdateInfo? AvailableUpdate { get; private set; }

    public FfmpegUpdateInfo? AvailableFfmpegUpdate { get; private set; }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        tubeVaultUpdateService.CheckStateChanged -= TubeVaultUpdateStateChanged;
        cancellation.Cancel();
        cancellation.Dispose();
        toolTip.Dispose();
        base.OnFormClosed(e);
    }

    private void ConfigureWindow()
    {
        Text = text.Get("SettingsComponentsAndUpdates");
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(560, 820);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Segoe UI", 10F);
    }

    private void BuildInterface()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(26, 22, 26, 20),
            ColumnCount = 1,
            RowCount = 7
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        root.Controls.Add(new Label
        {
            AutoSize = true,
            Text = text.Get("SettingsComponentsAndUpdates"),
            Font = new Font("Segoe UI Semibold", 20F),
            Margin = new Padding(0, 0, 0, 16)
        }, 0, 0);
        root.Controls.Add(CreateTubeVaultCard(), 0, 1);
        root.Controls.Add(CreateYtDlpCard(), 0, 2);
        root.Controls.Add(CreateFfmpegCard(), 0, 3);
        root.Controls.Add(CreateRepairSection(), 0, 4);

        ConfigureSecondaryButton(closeButton, text.Get("Close"));
        closeButton.Width = 112;
        closeButton.DialogResult = DialogResult.OK;
        closeButton.Anchor = AnchorStyles.Right;
        closeButton.Margin = Padding.Empty;
        root.Controls.Add(closeButton, 0, 6);

        Controls.Add(root);
        AcceptButton = closeButton;
        CancelButton = closeButton;
    }

    private Control CreateYtDlpCard()
    {
        ConfigureStatusLabel(ytDlpVersionLabel);
        ConfigureStatusLabel(ytDlpLastCheckLabel);
        ConfigureResultLabel(ytDlpStatusLabel);
        ConfigureSecondaryButton(checkYtDlpButton, text.Get("SettingsCheckUpdates"));
        checkYtDlpButton.Click += async (_, _) => await CheckYtDlpAsync();
        ConfigurePrimaryButton(updateYtDlpButton, text.Get("UpdateButton"));
        updateYtDlpButton.Click += async (_, _) => await UpdateYtDlpAsync();

        return CreateComponentCard(
            "yt-dlp",
            ytDlpVersionLabel,
            ytDlpLastCheckLabel,
            ytDlpStatusLabel,
            checkYtDlpButton,
            updateYtDlpButton);
    }

    private Control CreateTubeVaultCard()
    {
        ConfigureStatusLabel(tubeVaultVersionLabel);
        ConfigureStatusLabel(tubeVaultLastCheckLabel);
        ConfigureResultLabel(tubeVaultStatusLabel);
        tubeVaultStatusLabel.MaximumSize = new Size(460, 0);
        ConfigureSecondaryButton(checkTubeVaultButton, text.Get("TubeVaultCheckUpdates"));
        checkTubeVaultButton.Click += async (_, _) => await CheckTubeVaultAsync();
        ConfigurePrimaryButton(viewReleaseButton, text.Get("TubeVaultViewRelease"));
        viewReleaseButton.Click += (_, _) => OpenTubeVaultRelease();

        return CreateComponentCard("TubeVault", tubeVaultVersionLabel, tubeVaultLastCheckLabel,
            tubeVaultStatusLabel, checkTubeVaultButton, viewReleaseButton);
    }

    private Control CreateFfmpegCard()
    {
        ConfigureStatusLabel(ffmpegVersionLabel);
        ConfigureStatusLabel(ffmpegLastCheckLabel);
        ConfigureResultLabel(ffmpegStatusLabel);
        ConfigureSecondaryButton(checkFfmpegButton, text.Get("SettingsCheckFfmpegUpdates"));
        checkFfmpegButton.Click += async (_, _) => await CheckFfmpegAsync();
        ConfigurePrimaryButton(updateFfmpegButton, text.Get("UpdateButton"));
        updateFfmpegButton.Click += async (_, _) => await UpdateFfmpegAsync();

        return CreateComponentCard(
            "FFmpeg",
            ffmpegVersionLabel,
            ffmpegLastCheckLabel,
            ffmpegStatusLabel,
            checkFfmpegButton,
            updateFfmpegButton);
    }

    private static Control CreateComponentCard(
        string title,
        Label versionLabel,
        Label lastCheckLabel,
        Label statusLabel,
        Button checkButton,
        Button updateButton)
    {
        var card = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            Tag = "surface",
            Padding = new Padding(18, 14, 18, 14),
            Margin = new Padding(0, 0, 0, 12)
        };
        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            Tag = "surface",
            ColumnCount = 1,
            RowCount = 5,
            Margin = Padding.Empty
        };
        content.Controls.Add(new Label
        {
            AutoSize = true,
            Text = title,
            Font = new Font("Segoe UI Semibold", 12F),
            Margin = new Padding(0, 0, 0, 7)
        }, 0, 0);
        content.Controls.Add(versionLabel, 0, 1);
        content.Controls.Add(lastCheckLabel, 0, 2);
        content.Controls.Add(statusLabel, 0, 3);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            Tag = "surface",
            WrapContents = false,
            Margin = new Padding(0, 9, 0, 0)
        };
        actions.Controls.Add(checkButton);
        actions.Controls.Add(updateButton);
        content.Controls.Add(actions, 0, 4);
        card.Controls.Add(content);
        return card;
    }

    private Control CreateRepairSection()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty
        };
        ConfigureSecondaryButton(repairButton, text.Get("SettingsRepairComponents"));
        repairButton.Dock = DockStyle.Fill;
        repairButton.TextAlign = ContentAlignment.MiddleLeft;
        repairButton.Click += async (_, _) => await RepairComponentsAsync();
        ConfigureStatusLabel(repairStatusLabel);
        repairStatusLabel.Margin = new Padding(0, 5, 0, 0);
        panel.Controls.Add(repairButton, 0, 0);
        panel.Controls.Add(repairStatusLabel, 0, 1);
        return panel;
    }

    private void ConfigureToolTips()
    {
        toolTip.SetToolTip(checkYtDlpButton, text.Get("TooltipCheckUpdates"));
        toolTip.SetToolTip(updateYtDlpButton, text.Get("TooltipUpdate"));
        toolTip.SetToolTip(checkFfmpegButton, text.Get("TooltipCheckFfmpegUpdates"));
        toolTip.SetToolTip(updateFfmpegButton, text.Get("TooltipUpdateFfmpeg"));
        toolTip.SetToolTip(repairButton, text.Get("TooltipRepairComponents"));
    }

    private static void ConfigureStatusLabel(Label label)
    {
        label.AutoSize = true;
        label.Tag = "secondary";
        label.Margin = new Padding(0, 2, 0, 2);
    }

    private static void ConfigureResultLabel(Label label)
    {
        label.AutoSize = true;
        label.Font = new Font("Segoe UI Semibold", 9.5F);
        label.Margin = new Padding(0, 6, 0, 2);
    }

    private static void ConfigureSecondaryButton(Button button, string label)
    {
        button.Text = label;
        button.AutoSize = true;
        button.Height = 36;
        button.Margin = new Padding(0, 0, 8, 0);
        button.Padding = new Padding(8, 0, 8, 0);
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.Font = new Font("Segoe UI", 9.5F);
        button.Cursor = Cursors.Hand;
        button.UseVisualStyleBackColor = false;
    }

    private static void ConfigurePrimaryButton(Button button, string label)
    {
        ConfigureSecondaryButton(button, label);
        button.FlatAppearance.BorderSize = 0;
        button.Font = new Font("Segoe UI Semibold", 9.5F);
    }

    private async Task LoadInstalledVersionsAsync()
    {
        await LoadYtDlpVersionAsync();
        await LoadFfmpegVersionAsync();
        RefreshInformation();
    }

    private async Task LoadYtDlpVersionAsync()
    {
        try
        {
            installedYtDlpVersion = await updateService.GetInstalledVersionAsync(cancellation.Token);
            ytDlpAvailable = installedYtDlpVersion != "—";
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            installedYtDlpVersion = "—";
            ytDlpAvailable = false;
            log.Error("Leer versión de yt-dlp", ("Detalle", exception.ToString()));
        }
    }

    private async Task LoadFfmpegVersionAsync()
    {
        try
        {
            installedFfmpegVersion = await dependencyService.GetFfmpegVersionAsync(cancellation.Token);
            ffmpegAvailable = installedFfmpegVersion != "—";
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            installedFfmpegVersion = "—";
            ffmpegAvailable = false;
            log.Error("Leer versión de FFmpeg", ("Detalle", exception.ToString()));
        }
    }

    private async Task CheckYtDlpAsync()
    {
        if (busy) return;
        SetBusy(true);
        ytDlpStatusLabel.Text = text.Get("SettingsCheckingUpdates");

        try
        {
            var result = await updateService.CheckAsync(cancellation.Token);
            installedYtDlpVersion = result.LocalVersion;
            ytDlpAvailable = true;
            AvailableUpdate = result.IsUpdateAvailable ? result : null;
            lastYtDlpCheck = DateTimeOffset.UtcNow;
            settingsService.SaveYtDlpUpdateCheck(lastYtDlpCheck.Value);
            RefreshInformation();
        }
        catch (OperationCanceledException) { }
        catch (Exception exception)
        {
            log.Error("Comprobar actualización yt-dlp", ("Detalle", exception.ToString()));
            ytDlpStatusLabel.Text = text.Get("SettingsUpdateCheckFailed");
            ytDlpStatusLabel.ForeColor = ThemeService.GetColors(theme).Error;
        }
        finally { if (!IsDisposed) SetBusy(false); }
    }

    private async Task CheckTubeVaultAsync()
    {
        if (busy) return;
        SetBusy(true);
        tubeVaultCheckFailed = false;
        tubeVaultStatusLabel.Text = text.Get("SettingsCheckingUpdates");

        try
        {
            var result = await tubeVaultUpdateService.CheckAsync(cancellation.Token);
            settingsService.SaveTubeVaultUpdateCheck(result.CheckedAt);
            if (!IsDisposed) RefreshTubeVaultInformation();
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        catch (Exception)
        {
            // Incluye timeout y errores de API; el servicio conserva el detalle técnico.
            if (!IsDisposed)
            {
                tubeVaultCheckFailed = true;
                RefreshTubeVaultInformation();
            }
        }
        finally { if (!IsDisposed) SetBusy(false); }
    }

    private void TubeVaultUpdateStateChanged(object? sender, EventArgs e)
    {
        if (IsDisposed) return;
        tubeVaultCheckFailed = false;
        RefreshTubeVaultInformation();
    }

    private void RefreshTubeVaultInformation()
    {
        var update = tubeVaultUpdateService.LatestCheck;
        tubeVaultVersionLabel.Text = text.Get("SettingsInstalledVersion", AppMetadata.Version);
        tubeVaultLastCheckLabel.Text = FormatLastCheck(
            update?.CheckedAt ?? settingsService.LoadLastTubeVaultUpdateCheck());
        tubeVaultStatusLabel.Text = tubeVaultCheckFailed
            ? text.Get("TubeVaultUpdateCheckFailed")
            : update is null
                ? text.Get("TubeVaultNotChecked")
                : update.IsUpdateAvailable
                    ? text.Get("TubeVaultUpdateAvailable", update.AvailableVersion)
                    : text.Get(update.IsLocalVersionNewer ? "TubeVaultLocalNewer" : "TubeVaultUpToDate");
        tubeVaultStatusLabel.ForeColor = tubeVaultCheckFailed
            ? ThemeService.GetColors(theme).Error
            : ThemeService.GetColors(theme).SecondaryText;
        viewReleaseButton.Visible = update?.IsUpdateAvailable == true && !tubeVaultCheckFailed;
        viewReleaseButton.Enabled = !busy;
    }

    private void OpenTubeVaultRelease()
    {
        var update = tubeVaultUpdateService.LatestCheck;
        if (busy || tubeVaultCheckFailed || update?.IsUpdateAvailable != true) return;

        try
        {
            Process.Start(new ProcessStartInfo(update.ReleaseUrl) { UseShellExecute = true });
        }
        catch (Exception exception)
        {
            log.Error("Abrir release de TubeVault", ("Detalle", exception.ToString()));
            tubeVaultStatusLabel.Text = text.Get("TubeVaultReleaseOpenFailed");
            tubeVaultStatusLabel.ForeColor = ThemeService.GetColors(theme).Error;
        }
    }

    private async Task CheckFfmpegAsync()
    {
        if (busy) return;
        SetBusy(true);
        ffmpegStatusLabel.Text = text.Get("SettingsCheckingUpdates");

        try
        {
            var result = await dependencyService.CheckForUpdateAsync(cancellation.Token);
            installedFfmpegVersion = result.LocalVersion;
            ffmpegAvailable = true;
            AvailableFfmpegUpdate = result.IsUpdateAvailable ? result : null;
            lastFfmpegCheck = DateTimeOffset.UtcNow;
            settingsService.SaveFfmpegUpdateCheck(lastFfmpegCheck.Value);
            RefreshInformation();
        }
        catch (OperationCanceledException) { }
        catch (Exception exception)
        {
            log.Error("Comprobar actualización FFmpeg", ("Detalle", exception.ToString()));
            ffmpegStatusLabel.Text = text.Get("SettingsUpdateCheckFailed");
            ffmpegStatusLabel.ForeColor = ThemeService.GetColors(theme).Error;
        }
        finally { if (!IsDisposed) SetBusy(false); }
    }

    private async Task UpdateYtDlpAsync()
    {
        if (busy || AvailableUpdate is null) return;
        SetBusy(true);
        ytDlpStatusLabel.Text = text.Get("UpdatingYtDlp");

        try
        {
            installedYtDlpVersion = await updateService.UpdateAsync(
                AvailableUpdate.AvailableVersion,
                cancellation.Token);
            ytDlpAvailable = true;
            AvailableUpdate = null;
            ytDlpVersionLabel.Text = text.Get("SettingsInstalledVersion", installedYtDlpVersion);
            ytDlpStatusLabel.Text = text.Get("UpdateSuccess");
            ytDlpStatusLabel.ForeColor = ThemeService.GetColors(theme).SecondaryText;
            updateYtDlpButton.Visible = false;
        }
        catch (OperationCanceledException) { }
        catch (Exception exception)
        {
            log.Error("Actualizar yt-dlp", ("Detalle", exception.ToString()));
            ytDlpStatusLabel.Text = text.Get("UpdateFailed");
            ytDlpStatusLabel.ForeColor = ThemeService.GetColors(theme).Error;
        }
        finally { if (!IsDisposed) SetBusy(false); }
    }

    private async Task UpdateFfmpegAsync()
    {
        if (busy || AvailableFfmpegUpdate is null) return;
        SetBusy(true);
        ffmpegStatusLabel.Text = text.Get("UpdatingFfmpeg");

        try
        {
            installedFfmpegVersion = await dependencyService.UpdateAsync(
                AvailableFfmpegUpdate.AvailableVersion,
                cancellation.Token);
            ffmpegAvailable = true;
            AvailableFfmpegUpdate = null;
            ffmpegVersionLabel.Text = text.Get("SettingsInstalledVersion", installedFfmpegVersion);
            ffmpegStatusLabel.Text = text.Get("FfmpegUpdateSuccess");
            ffmpegStatusLabel.ForeColor = ThemeService.GetColors(theme).SecondaryText;
            updateFfmpegButton.Visible = false;
        }
        catch (OperationCanceledException) { }
        catch (Exception exception)
        {
            log.Error("Actualizar FFmpeg", ("Detalle", exception.ToString()));
            ffmpegStatusLabel.Text = text.Get("FfmpegUpdateFailed");
            ffmpegStatusLabel.ForeColor = ThemeService.GetColors(theme).Error;
        }
        finally { if (!IsDisposed) SetBusy(false); }
    }

    private async Task RepairComponentsAsync()
    {
        if (busy) return;
        SetBusy(true);
        repairStatusLabel.Text = text.Get("SetupRepairing");

        try
        {
            var progress = new Progress<DependencyProgress>(item =>
                repairStatusLabel.Text = text.Get(item.TextKey));
            await bootstrapService.RepairDependenciesAsync(progress, cancellation.Token);
            await LoadInstalledVersionsAsync();
            ClearSatisfiedUpdates();
            RefreshInformation();
            repairStatusLabel.Text = text.Get("SetupCompleted");
        }
        catch (OperationCanceledException) { }
        catch (Exception exception)
        {
            log.Error("Reparar componentes", ("Detalle", exception.ToString()));
            repairStatusLabel.Text = text.Get("SetupFailed");
        }
        finally { if (!IsDisposed) SetBusy(false); }
    }

    private void ClearSatisfiedUpdates()
    {
        if (AvailableUpdate is not null
            && VersionsMatch(installedYtDlpVersion, AvailableUpdate.AvailableVersion))
        {
            AvailableUpdate = null;
        }

        if (AvailableFfmpegUpdate is not null
            && VersionsMatch(installedFfmpegVersion, AvailableFfmpegUpdate.AvailableVersion))
        {
            AvailableFfmpegUpdate = null;
        }
    }

    private static bool VersionsMatch(string installed, string available)
    {
        return string.Equals(
            installed.Trim().TrimStart('v'),
            available.Trim().TrimStart('v'),
            StringComparison.OrdinalIgnoreCase);
    }

    private void RefreshInformation()
    {
        RefreshTubeVaultInformation();
        ytDlpVersionLabel.Text = text.Get("SettingsInstalledVersion", installedYtDlpVersion);
        ytDlpLastCheckLabel.Text = FormatLastCheck(lastYtDlpCheck);
        ytDlpStatusLabel.Text = !ytDlpAvailable
            ? text.Get("ComponentUnavailable")
            : AvailableUpdate?.IsUpdateAvailable == true
                ? text.Get("UpdateAvailable")
                : lastYtDlpCheck is null
                    ? text.Get("SettingsNotChecked")
                    : text.Get("SettingsUpToDate");
        updateYtDlpButton.Visible = AvailableUpdate?.IsUpdateAvailable == true;

        ffmpegVersionLabel.Text = text.Get("SettingsInstalledVersion", installedFfmpegVersion);
        ffmpegLastCheckLabel.Text = FormatLastCheck(lastFfmpegCheck);
        ffmpegStatusLabel.Text = !ffmpegAvailable
            ? text.Get("ComponentUnavailable")
            : AvailableFfmpegUpdate?.IsUpdateAvailable == true
                ? text.Get("FfmpegUpdateAvailable")
                : lastFfmpegCheck is null
                    ? text.Get("SettingsNotChecked")
                    : text.Get("FfmpegUpToDate");
        updateFfmpegButton.Visible = AvailableFfmpegUpdate?.IsUpdateAvailable == true;
        StyleStatusLabels();
    }

    private string FormatLastCheck(DateTimeOffset? value)
    {
        if (value is null) return text.Get("SettingsLastCheckNever");
        var culture = CultureInfo.GetCultureInfo(
            text.Language == AppLanguage.English ? "en" : "es");
        return text.Get("SettingsLastCheck", value.Value.ToLocalTime().ToString("g", culture));
    }

    private void SetBusy(bool value)
    {
        busy = value;
        checkTubeVaultButton.Enabled = !value;
        viewReleaseButton.Enabled = !value && tubeVaultUpdateService.LatestCheck?.IsUpdateAvailable == true;
        checkYtDlpButton.Enabled = !value;
        checkFfmpegButton.Enabled = !value;
        updateYtDlpButton.Enabled = !value && AvailableUpdate is not null;
        updateFfmpegButton.Enabled = !value && AvailableFfmpegUpdate is not null;
        repairButton.Enabled = !value;
        closeButton.Enabled = !value;
        ControlBox = !value;
        Cursor = value ? Cursors.WaitCursor : Cursors.Default;
        StyleButtons();
    }

    private void ApplyTheme()
    {
        ThemeService.Apply(this, theme);
        StyleButtons();
        StyleStatusLabels();
    }

    private void StyleButtons()
    {
        var colors = ThemeService.GetColors(theme);
        foreach (var button in new Button[]
                 { checkTubeVaultButton, checkYtDlpButton, checkFfmpegButton, repairButton, closeButton })
        {
            button.BackColor = button.Enabled ? colors.SecondaryButton : colors.Disabled;
            button.ForeColor = button.Enabled ? colors.Text : colors.DisabledText;
            button.FlatAppearance.BorderColor = colors.Border;
            button.FlatAppearance.MouseOverBackColor = colors.SecondaryButtonHover;
        }

        foreach (var button in new Button[] { viewReleaseButton, updateYtDlpButton, updateFfmpegButton })
        {
            button.BackColor = button.Enabled ? AccentColor : colors.Disabled;
            button.ForeColor = button.Enabled ? Color.White : colors.DisabledText;
            button.FlatAppearance.MouseOverBackColor = AccentHoverColor;
        }
    }

    private void StyleStatusLabels()
    {
        var colors = ThemeService.GetColors(theme);
        ytDlpStatusLabel.ForeColor = !ytDlpAvailable
                                    || AvailableUpdate?.IsUpdateAvailable == true
            ? colors.Error
            : colors.SecondaryText;
        ffmpegStatusLabel.ForeColor = !ffmpegAvailable
                                     || AvailableFfmpegUpdate?.IsUpdateAvailable == true
            ? colors.Error
            : colors.SecondaryText;
    }
}
