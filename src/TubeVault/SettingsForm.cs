namespace TubeVault;

internal sealed class SettingsForm : Form
{
    private static readonly Color AccentColor = Color.FromArgb(0, 103, 192);
    private static readonly Color AccentHoverColor = Color.FromArgb(0, 90, 158);

    private readonly TextService text;
    private readonly AppTheme displayTheme;
    private readonly YtDlpUpdateService updateService;
    private readonly TubeVaultUpdateService tubeVaultUpdateService;
    private readonly DependencyService dependencyService;
    private readonly DependencyBootstrapService bootstrapService;
    private readonly SettingsService settingsService;
    private readonly LogService log;
    private readonly CancellationTokenSource cancellation = new();
    private readonly ToolTip toolTip = new();
    private readonly RoundedButton lightButton = new();
    private readonly RoundedButton darkButton = new();
    private readonly ComboBox languageComboBox = new();
    private readonly CheckBox embedCoverArtworkCheckBox = new();
    private readonly HeaderActionButton componentsButton = new();
    private readonly HeaderActionButton aboutButton = new();
    private readonly RoundedButton acceptButton = new();
    private readonly RoundedButton cancelButton = new();
    private AppTheme pendingTheme;
    private AppLanguage pendingLanguage;
    private bool pendingEmbedCoverArtwork;

    public SettingsForm(
        TextService text,
        AppTheme theme,
        AppLanguage language,
        bool embedCoverArtwork,
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
        displayTheme = theme;
        pendingTheme = theme;
        pendingLanguage = language;
        pendingEmbedCoverArtwork = embedCoverArtwork;
        AvailableUpdate = availableUpdate;
        AvailableFfmpegUpdate = availableFfmpegUpdate;
        this.updateService = updateService;
        this.tubeVaultUpdateService = tubeVaultUpdateService;
        this.dependencyService = dependencyService;
        this.bootstrapService = bootstrapService;
        this.settingsService = settingsService;
        this.log = log;

        ConfigureWindow();
        BuildInterface();
        ConfigureToolTips();
        ApplyTheme();
        RefreshComponentsNotification();
        tubeVaultUpdateService.CheckStateChanged += TubeVaultUpdateStateChanged;
    }

    public AppTheme SelectedTheme => pendingTheme;

    public AppLanguage SelectedLanguage => pendingLanguage;

    public bool SelectedEmbedCoverArtwork => pendingEmbedCoverArtwork;

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
        Text = text.Get("SettingsTitle");
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(520, 660);
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
            Padding = new Padding(28, 22, 28, 22),
            ColumnCount = 1,
            RowCount = 12
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        root.Controls.Add(new Label
        {
            AutoSize = true,
            Text = text.Get("SettingsTitle"),
            Font = new Font("Segoe UI Semibold", 22F),
            Margin = new Padding(0, 0, 0, 18)
        }, 0, 0);
        root.Controls.Add(CreateAppearanceSection(), 0, 1);
        root.Controls.Add(CreateSeparator(), 0, 2);
        root.Controls.Add(CreateLanguageSection(), 0, 3);
        root.Controls.Add(CreateSeparator(), 0, 4);
        root.Controls.Add(CreateDownloadsSection(), 0, 5);
        root.Controls.Add(CreateSeparator(), 0, 6);
        root.Controls.Add(CreateNavigationButton(
            componentsButton,
            "SettingsComponentsAndUpdates",
            ComponentsButton_Click), 0, 7);
        root.Controls.Add(CreateSeparator(), 0, 8);
        root.Controls.Add(CreateNavigationButton(
            aboutButton,
            "SettingsAboutTubeVault",
            async (_, _) => await OpenAboutAsync()), 0, 9);
        root.Controls.Add(new Panel { Dock = DockStyle.Fill }, 0, 10);
        root.Controls.Add(CreateActions(), 0, 11);

        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 25F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 25F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 25F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Insert(5, new RowStyle(SizeType.AutoSize));
        root.RowStyles.Insert(6, new RowStyle(SizeType.Absolute, 25F));

        Controls.Add(root);
        AcceptButton = acceptButton;
        CancelButton = cancelButton;
    }

    private void ConfigureToolTips()
    {
        toolTip.SetToolTip(lightButton, text.Get("TooltipTheme"));
        toolTip.SetToolTip(darkButton, text.Get("TooltipTheme"));
        toolTip.SetToolTip(languageComboBox, text.Get("TooltipLanguage"));
        toolTip.SetToolTip(componentsButton, text.Get("TooltipComponentsAndUpdates"));
        toolTip.SetToolTip(aboutButton, text.Get("TooltipAbout"));
    }

    private Control CreateAppearanceSection()
    {
        var section = CreateSection("SettingsAppearance", 2);
        var selector = new TableLayoutPanel
        {
            Size = new Size(250, 38),
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 7, 0, 0)
        };
        selector.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        selector.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

        ConfigureSegment(lightButton, text.Get("SettingsLight"),
            RoundedCorners.TopLeft | RoundedCorners.BottomLeft);
        ConfigureSegment(darkButton, text.Get("SettingsDark"),
            RoundedCorners.TopRight | RoundedCorners.BottomRight);
        lightButton.Click += (_, _) => SelectTheme(AppTheme.Light);
        darkButton.Click += (_, _) => SelectTheme(AppTheme.Dark);

        selector.Controls.Add(lightButton, 0, 0);
        selector.Controls.Add(darkButton, 1, 0);
        section.Controls.Add(CreateFieldLabel("SettingsTheme"), 0, 1);
        section.Controls.Add(selector, 0, 2);
        return section;
    }

    private Control CreateLanguageSection()
    {
        var section = CreateSection("SettingsLanguage", 2);
        section.Controls.Add(CreateFieldLabel("SettingsApplicationLanguage"), 0, 1);

        languageComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        languageComboBox.DrawMode = DrawMode.OwnerDrawFixed;
        languageComboBox.FlatStyle = FlatStyle.Flat;
        languageComboBox.ItemHeight = 24;
        languageComboBox.Width = 250;
        languageComboBox.Font = new Font("Segoe UI", 10F);
        languageComboBox.Margin = new Padding(0, 7, 0, 0);
        languageComboBox.Items.AddRange([text.Get("LanguageSpanish"), text.Get("LanguageEnglish")]);
        languageComboBox.SelectedIndex = pendingLanguage == AppLanguage.Spanish ? 0 : 1;
        languageComboBox.SelectedIndexChanged += (_, _) =>
            pendingLanguage = languageComboBox.SelectedIndex == 1
                ? AppLanguage.English
                : AppLanguage.Spanish;
        languageComboBox.DrawItem += LanguageComboBox_DrawItem;
        section.Controls.Add(languageComboBox, 0, 2);
        return section;
    }

    private Control CreateNavigationButton(
        HeaderActionButton button,
        string textKey,
        EventHandler click)
    {
        button.Text = text.Get(textKey);
        button.ShowChevron = true;
        button.Dock = DockStyle.Fill;
        button.Height = 44;
        button.Margin = Padding.Empty;
        button.Padding = new Padding(12, 0, 38, 0);
        button.TextAlign = ContentAlignment.MiddleLeft;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.DrawRoundedBorder = true;
        button.Font = new Font("Segoe UI Semibold", 9.5F);
        button.Cursor = Cursors.Hand;
        button.UseVisualStyleBackColor = false;
        button.Click += click;
        return button;
    }

    private Control CreateDownloadsSection()
    {
        var section = CreateSection("SettingsDownloads", 2);
        embedCoverArtworkCheckBox.AutoSize = true;
        embedCoverArtworkCheckBox.Text = text.Get("SettingsEmbedCoverArtwork");
        embedCoverArtworkCheckBox.Checked = pendingEmbedCoverArtwork;
        embedCoverArtworkCheckBox.Margin = new Padding(0, 3, 0, 5);
        embedCoverArtworkCheckBox.CheckedChanged += (_, _) =>
            pendingEmbedCoverArtwork = embedCoverArtworkCheckBox.Checked;
        section.Controls.Add(embedCoverArtworkCheckBox, 0, 1);
        var description = CreateFieldLabel("SettingsCoverArtworkDescription");
        description.MaximumSize = new Size(440, 0);
        section.Controls.Add(description, 0, 2);
        return section;
    }

    private Control CreateActions()
    {
        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Margin = Padding.Empty
        };

        ConfigurePrimaryButton(acceptButton, text.Get("SettingsAccept"));
        acceptButton.Width = 112;
        acceptButton.DialogResult = DialogResult.OK;
        acceptButton.Margin = Padding.Empty;

        ConfigureSecondaryButton(cancelButton, text.Get("Cancel"));
        cancelButton.Width = 112;
        cancelButton.DialogResult = DialogResult.Cancel;
        cancelButton.Margin = new Padding(0, 0, 10, 0);

        actions.Controls.Add(acceptButton);
        actions.Controls.Add(cancelButton);
        return actions;
    }

    private TableLayoutPanel CreateSection(string titleKey, int contentRows)
    {
        var section = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = contentRows + 1,
            Margin = Padding.Empty
        };
        section.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        section.Controls.Add(new Label
        {
            AutoSize = true,
            Text = text.Get(titleKey),
            Font = new Font("Segoe UI Semibold", 11F),
            Margin = new Padding(0, 0, 0, 7)
        }, 0, 0);
        return section;
    }

    private Label CreateFieldLabel(string key)
    {
        return new Label
        {
            AutoSize = true,
            Text = text.Get(key),
            Tag = "secondary",
            Margin = Padding.Empty
        };
    }

    private static Control CreateSeparator()
    {
        return new Panel
        {
            Dock = DockStyle.Fill,
            Height = 1,
            Tag = "separator",
            Margin = new Padding(0, 12, 0, 12)
        };
    }

    private static void ConfigureSegment(
        RoundedButton button,
        string label,
        RoundedCorners roundedCorners)
    {
        button.Text = label;
        button.RoundedCorners = roundedCorners;
        button.Dock = DockStyle.Fill;
        button.Margin = Padding.Empty;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.Font = new Font("Segoe UI Semibold", 9.5F);
        button.Cursor = Cursors.Hand;
        button.UseVisualStyleBackColor = false;
    }

    private static void ConfigureSecondaryButton(Button button, string label)
    {
        button.Text = label;
        button.Height = 36;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.Font = new Font("Segoe UI", 9.5F);
        button.Cursor = Cursors.Hand;
        button.UseVisualStyleBackColor = false;
    }

    private static void ConfigurePrimaryButton(Button button, string label)
    {
        button.Text = label;
        button.Height = 36;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.Font = new Font("Segoe UI Semibold", 9.5F);
        button.Cursor = Cursors.Hand;
        button.UseVisualStyleBackColor = false;
    }

    private void SelectTheme(AppTheme theme)
    {
        pendingTheme = theme;
        StyleThemeSelector();
    }

    private void LanguageComboBox_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0)
        {
            return;
        }

        var colors = ThemeService.GetColors(displayTheme);
        var selected = e.State.HasFlag(DrawItemState.Selected)
                       && !e.State.HasFlag(DrawItemState.ComboBoxEdit);
        using var background = new SolidBrush(selected ? AccentColor : colors.Input);
        using var foreground = new SolidBrush(selected ? Color.White : colors.Text);
        using var format = new StringFormat { LineAlignment = StringAlignment.Center };
        e.Graphics.FillRectangle(background, e.Bounds);
        e.Graphics.DrawString(
            languageComboBox.Items[e.Index]?.ToString() ?? string.Empty,
            languageComboBox.Font,
            foreground,
            new RectangleF(e.Bounds.X + 4, e.Bounds.Y, e.Bounds.Width - 8, e.Bounds.Height),
            format);
    }

    private void ComponentsButton_Click(object? sender, EventArgs e)
    {
        using var components = new ComponentsForm(
            text,
            displayTheme,
            AvailableUpdate,
            AvailableFfmpegUpdate,
            updateService,
            tubeVaultUpdateService,
            dependencyService,
            bootstrapService,
            settingsService,
            log);
        components.ShowDialog(this);
        AvailableUpdate = components.AvailableUpdate;
        AvailableFfmpegUpdate = components.AvailableFfmpegUpdate;
        RefreshComponentsNotification();
    }

    private async Task OpenAboutAsync()
    {
        aboutButton.Enabled = false;
        string ytDlpVersion;
        string ffmpegVersion;

        try
        {
            var ytDlpTask = updateService.GetInstalledVersionAsync(cancellation.Token);
            var ffmpegTask = dependencyService.GetFfmpegVersionAsync(cancellation.Token);
            await Task.WhenAll(ytDlpTask, ffmpegTask);
            ytDlpVersion = await ytDlpTask;
            ffmpegVersion = await ffmpegTask;
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception exception)
        {
            log.Error("Leer versiones para About", ("Detalle", exception.ToString()));
            ytDlpVersion = "—";
            ffmpegVersion = "—";
        }
        finally
        {
            if (!IsDisposed)
            {
                aboutButton.Enabled = true;
            }
        }

        if (!IsDisposed)
        {
            using var about = new AboutForm(text, displayTheme, ytDlpVersion, ffmpegVersion);
            about.ShowDialog(this);
        }
    }

    private void RefreshComponentsNotification()
    {
        var hasPendingUpdate = UpdateNotification.HasPendingUpdate(
            AvailableUpdate,
            AvailableFfmpegUpdate,
            tubeVaultUpdateService.LatestCheck);
        componentsButton.HasNotification = hasPendingUpdate;
        componentsButton.AccessibleDescription = hasPendingUpdate
            ? text.Get("UpdatesAvailable")
            : text.Get("TooltipComponentsAndUpdates");
        toolTip.SetToolTip(
            componentsButton,
            text.Get(hasPendingUpdate
                ? "UpdatesAvailable"
                : "TooltipComponentsAndUpdates"));
    }

    private void TubeVaultUpdateStateChanged(object? sender, EventArgs e)
    {
        if (!IsDisposed) RefreshComponentsNotification();
    }

    private void ApplyTheme()
    {
        ThemeService.Apply(this, displayTheme);
        var colors = ThemeService.GetColors(displayTheme);

        foreach (var button in new Button[] { componentsButton, aboutButton, cancelButton })
        {
            button.BackColor = colors.SecondaryButton;
            button.ForeColor = colors.Text;
            button.FlatAppearance.BorderColor = colors.Border;
            button.FlatAppearance.MouseOverBackColor = colors.SecondaryButtonHover;
        }

        acceptButton.BackColor = AccentColor;
        acceptButton.ForeColor = Color.White;
        acceptButton.FlatAppearance.MouseOverBackColor = AccentHoverColor;
        StyleThemeSelector();
    }

    private void StyleThemeSelector()
    {
        var colors = ThemeService.GetColors(displayTheme);
        StyleThemeButton(lightButton, pendingTheme == AppTheme.Light, colors);
        StyleThemeButton(darkButton, pendingTheme == AppTheme.Dark, colors);
    }

    private static void StyleThemeButton(
        RoundedButton button,
        bool selected,
        ThemeColors colors)
    {
        button.BackColor = selected ? AccentColor : colors.SecondaryButton;
        button.ForeColor = selected ? Color.White : colors.Text;
        button.FlatAppearance.BorderColor = selected ? AccentColor : colors.Border;
        button.FlatAppearance.MouseOverBackColor = selected
            ? AccentHoverColor
            : colors.SecondaryButtonHover;
    }
}
