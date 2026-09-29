using System.Diagnostics;

namespace TubeVault;

public sealed class MainForm : Form
{
    private static readonly Color AccentColor = Color.FromArgb(0, 103, 192);
    private static readonly Color AccentHoverColor = Color.FromArgb(0, 90, 158);
    private static readonly HttpClient ThumbnailHttpClient = CreateThumbnailHttpClient();

    private readonly Dictionary<Control, string> localizedControls = [];
    private readonly Dictionary<Control, string> localizedControlsWithoutColon = [];
    private readonly ToolTip toolTip = new();

    private readonly HeaderActionButton settingsButton = new();

    private readonly VerticallyCenteredTextBox urlTextBox = new();
    private readonly RoundedButton analyzeButton = new();

    private readonly RoundedPanel resultHost = new();
    private readonly Panel emptyResultPanel = new();
    private readonly Label emptyStateTitleLabel = new();
    private readonly Label emptyStateDescriptionLabel = new();
    private readonly EmptyStateGraphic emptyStateGraphic = new();
    private readonly Panel successResultPanel = new();
    private readonly SuccessStateGraphic successStateGraphic = new();
    private readonly Label successTitleLabel = new();
    private readonly Label successDescriptionLabel = new();
    private readonly Panel successActionsHost = new();
    private readonly Panel messagePanel = new();
    private readonly Label messageLabel = new();
    private readonly TableLayoutPanel songPanel = new();
    private readonly TableLayoutPanel playlistPanel = new();

    private readonly Label songTitleValue = CreateValueLabel();
    private readonly Label songChannelValue = CreateValueLabel();
    private readonly Label songDurationValue = CreateValueLabel();
    private readonly Label songPublishedValue = CreateValueLabel();
    private readonly ArtworkBox songThumbnail = new();

    private readonly Label playlistNameValue = CreateValueLabel();
    private readonly Label playlistChannelValue = CreateValueLabel();
    private readonly Label playlistCountValue = CreateValueLabel();
    private readonly TableLayoutPanel playlistSummary = new();
    private readonly ArtworkBox playlistThumbnail = new();
    private readonly ListView playlistList = new();
    private readonly TableLayoutPanel playlistSelectionBar = new();
    private readonly RoundedButton selectAllButton = new();
    private readonly RoundedButton deselectAllButton = new();
    private readonly Label playlistSelectionCountLabel = new();
    private readonly VerticallyCenteredTextBox playlistFolderTextBox = new();

    private readonly VerticallyCenteredTextBox destinationTextBox = new();
    private readonly RoundedButton chooseFolderButton = new();
    private readonly TableLayoutPanel qualitySelector = new();
    private readonly RoundedButton highQualityButton = new();
    private readonly RoundedButton mediumQualityButton = new();
    private readonly RoundedButton lowQualityButton = new();
    private readonly RoundedButton downloadButton = new();
    private readonly RoundedButton cancelButton = new();
    private readonly TableLayoutPanel downloadActionsPanel = new();

    private readonly ProgressBar progressBar = new();
    private readonly Label progressPercentageLabel = new();
    private readonly Label statusLabel = new();

    private readonly TableLayoutPanel finalActionsPanel = new();
    private readonly Panel finalActionsHost = new();
    private readonly RoundedButton openFolderButton = new();
    private readonly RoundedButton newDownloadButton = new();

    private readonly LogService logService;
    private readonly SettingsService settingsService;
    private readonly TextService textService;
    private readonly YtDlpService ytDlpService;
    private readonly YtDlpUpdateService ytDlpUpdateService;
    private readonly DependencyService dependencyService;
    private readonly DependencyBootstrapService dependencyBootstrapService;
    private readonly DownloadService downloadService;

    private CancellationTokenSource? analysisCancellation;
    private CancellationTokenSource? downloadCancellation;
    private CancellationTokenSource? updateCancellation;
    private CancellationTokenSource? ffmpegUpdateCancellation;
    private CancellationTokenSource? thumbnailCancellation;
    private MediaInfo? currentMedia;
    private DownloadResult? lastDownloadResult;
    private YtDlpUpdateInfo? availableUpdate;
    private FfmpegUpdateInfo? availableFfmpegUpdate;
    private string currentUrl = string.Empty;
    private string currentMessageKey = string.Empty;
    private string lastDownloadFolder = string.Empty;
    private UiState currentState;
    private ResultKind currentResult;
    private int currentItemCount;
    private int selectedPlaylistCount;
    private string destinationPath = string.Empty;
    private bool resettingUrl;
    private bool loadingSettings;
    private bool changingPlaylistChecks;
    private bool showCompletionView;
    private AppTheme currentTheme;
    private AppLanguage currentLanguage;
    private AudioQuality selectedAudioQuality = AudioQuality.Medium;

    public MainForm()
    {
        logService = new LogService();
        settingsService = new SettingsService(logService);
        textService = new TextService();
        currentLanguage = settingsService.LoadLanguage();
        currentTheme = settingsService.LoadTheme();
        textService.SetLanguage(currentLanguage);
        ytDlpService = new YtDlpService(logService);
        ytDlpUpdateService = new YtDlpUpdateService(logService, ytDlpService);
        dependencyService = new DependencyService(logService);
        dependencyBootstrapService = new DependencyBootstrapService(
            ytDlpService,
            dependencyService,
            logService);
        var validationService = new MediaValidationService(logService, dependencyService);
        downloadService = new DownloadService(
            logService,
            ytDlpService,
            dependencyService,
            validationService);

        ConfigureWindow();
        BuildInterface();
        loadingSettings = true;
        ApplyLanguage();
        loadingSettings = false;
        LoadInitialSettings();
        ApplyTheme();
        SetState(UiState.Initial);

        Shown += MainForm_Shown;
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        analysisCancellation?.Cancel();
        analysisCancellation?.Dispose();
        downloadCancellation?.Cancel();
        downloadCancellation?.Dispose();
        updateCancellation?.Cancel();
        updateCancellation?.Dispose();
        ffmpegUpdateCancellation?.Cancel();
        ffmpegUpdateCancellation?.Dispose();
        thumbnailCancellation?.Cancel();
        thumbnailCancellation?.Dispose();
        ClearThumbnails();
        base.OnFormClosed(e);
    }

    private void ConfigureWindow()
    {
        Text = "TubeVault";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(820, 760);
        MinimumSize = new Size(720, 800);
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Segoe UI", 10F);
        DoubleBuffered = true;
    }

    private void BuildInterface()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(30, 20, 30, 20),
            ColumnCount = 1,
            RowCount = 6
        };

        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 14F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 350F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 14F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        root.Controls.Add(CreateHeader(), 0, 0);
        root.Controls.Add(CreateUrlSection(), 0, 1);
        root.Controls.Add(CreateResultSection(), 0, 3);
        root.Controls.Add(CreateActionCard(), 0, 5);

        Controls.Add(root);
    }

    private Control CreateHeader()
    {
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 2,
            Margin = new Padding(0, 0, 0, 14)
        };

        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var title = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 24F),
            Text = "TubeVault",
            Margin = Padding.Empty
        };

        ConfigureHeaderButton(settingsButton, HeaderActionIcon.Settings, RoundedCorners.All);
        settingsButton.Anchor = AnchorStyles.Right;
        settingsButton.Click += SettingsButton_Click;

        header.Controls.Add(title, 0, 0);
        header.Controls.Add(settingsButton, 1, 0);
        return header;
    }

    private Control CreateUrlSection()
    {
        var section = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 3,
            Margin = Padding.Empty
        };

        section.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        section.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 118F));
        section.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        section.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var label = CreateSectionLabel("UrlLabel");
        section.SetColumnSpan(label, 2);

        urlTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        urlTextBox.Height = VerticallyCenteredTextBox.CompactHeight;
        urlTextBox.Font = new Font("Segoe UI", 11F);
        urlTextBox.SetPlaceholderText(textService.Get("UrlPlaceholder"));
        urlTextBox.Margin = new Padding(0, 7, 10, 0);
        urlTextBox.MinimumSize = new Size(0, 36);
        urlTextBox.AccessibleName = "Enlace de YouTube";
        urlTextBox.TextChanged += UrlTextBox_TextChanged;

        ConfigurePrimaryButton(analyzeButton, "Analyze");
        analyzeButton.Dock = DockStyle.Fill;
        analyzeButton.Margin = new Padding(0, 7, 0, 0);
        analyzeButton.Click += AnalyzeButton_Click;

        section.Controls.Add(label, 0, 0);
        section.Controls.Add(urlTextBox, 0, 1);
        section.Controls.Add(analyzeButton, 1, 1);
        return section;
    }

    private Control CreateResultSection()
    {
        resultHost.Dock = DockStyle.Fill;
        resultHost.Margin = Padding.Empty;
        resultHost.Padding = new Padding(1);
        resultHost.Tag = "surface";
        resultHost.CornerRadius = 12;

        BuildEmptyResultPanel();
        BuildSuccessResultPanel();

        BuildMessagePanel();
        BuildSongPanel();
        BuildPlaylistPanel();

        resultHost.Controls.Add(emptyResultPanel);
        resultHost.Controls.Add(successResultPanel);
        resultHost.Controls.Add(messagePanel);
        resultHost.Controls.Add(songPanel);
        resultHost.Controls.Add(playlistPanel);
        return resultHost;
    }

    private void BuildEmptyResultPanel()
    {
        emptyResultPanel.Dock = DockStyle.Fill;
        emptyResultPanel.Tag = "surface";
        emptyResultPanel.Padding = new Padding(28, 22, 28, 22);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Tag = "surface",
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230F));

        var textLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Tag = "surface",
            ColumnCount = 1,
            RowCount = 4,
            Margin = Padding.Empty
        };
        textLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        textLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        textLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        textLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

        emptyStateTitleLabel.AutoSize = true;
        emptyStateTitleLabel.Font = new Font("Segoe UI Semibold", 15F);
        emptyStateTitleLabel.MaximumSize = new Size(440, 0);
        emptyStateTitleLabel.Margin = new Padding(0, 0, 18, 10);
        localizedControls[emptyStateTitleLabel] = "EmptyStateTitle";

        emptyStateDescriptionLabel.AutoSize = true;
        emptyStateDescriptionLabel.Font = new Font("Segoe UI", 10F);
        emptyStateDescriptionLabel.MaximumSize = new Size(440, 0);
        emptyStateDescriptionLabel.Tag = "secondary";
        emptyStateDescriptionLabel.Margin = new Padding(0, 0, 18, 0);
        localizedControls[emptyStateDescriptionLabel] = "EmptyStateDescription";

        textLayout.Controls.Add(emptyStateTitleLabel, 0, 1);
        textLayout.Controls.Add(emptyStateDescriptionLabel, 0, 2);

        emptyStateGraphic.Anchor = AnchorStyles.None;
        emptyStateGraphic.Size = new Size(205, 220);
        emptyStateGraphic.Margin = Padding.Empty;

        layout.Controls.Add(textLayout, 0, 0);
        layout.Controls.Add(emptyStateGraphic, 1, 0);
        emptyResultPanel.Controls.Add(layout);
    }

    private void BuildSuccessResultPanel()
    {
        successResultPanel.Dock = DockStyle.Fill;
        successResultPanel.Tag = "surface";
        successResultPanel.Padding = new Padding(28, 18, 28, 18);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Tag = "surface",
            ColumnCount = 1,
            RowCount = 6,
            Margin = Padding.Empty
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120F));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

        successStateGraphic.Anchor = AnchorStyles.None;
        successStateGraphic.Size = new Size(128, 116);
        successStateGraphic.Margin = Padding.Empty;

        successTitleLabel.AutoSize = true;
        successTitleLabel.Anchor = AnchorStyles.None;
        successTitleLabel.Font = new Font("Segoe UI Semibold", 17F);
        successTitleLabel.Margin = new Padding(0, 4, 0, 5);
        localizedControls[successTitleLabel] = "SuccessViewTitle";

        successDescriptionLabel.AutoSize = true;
        successDescriptionLabel.Anchor = AnchorStyles.None;
        successDescriptionLabel.Font = new Font("Segoe UI", 10.5F);
        successDescriptionLabel.Tag = "secondary";
        successDescriptionLabel.Margin = new Padding(0, 0, 0, 14);

        successActionsHost.Dock = DockStyle.Fill;
        successActionsHost.Tag = "surface";
        successActionsHost.Margin = Padding.Empty;

        layout.Controls.Add(successStateGraphic, 0, 1);
        layout.Controls.Add(successTitleLabel, 0, 2);
        layout.Controls.Add(successDescriptionLabel, 0, 3);
        layout.Controls.Add(successActionsHost, 0, 4);
        successResultPanel.Controls.Add(layout);
    }

    private void BuildMessagePanel()
    {
        messagePanel.Dock = DockStyle.Fill;
        messagePanel.Tag = "surface";
        messagePanel.Padding = new Padding(24);

        messageLabel.Dock = DockStyle.Fill;
        messageLabel.AutoSize = false;
        messageLabel.Font = new Font("Segoe UI", 11F);
        messageLabel.TextAlign = ContentAlignment.MiddleCenter;

        messagePanel.Controls.Add(messageLabel);
    }

    private void BuildSongPanel()
    {
        songPanel.Dock = DockStyle.Fill;
        songPanel.Tag = "surface";
        songPanel.Padding = new Padding(22, 18, 22, 18);
        songPanel.ColumnCount = 3;
        songPanel.RowCount = 6;
        songPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));
        songPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        songPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 228F));
        songPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));

        for (var row = 1; row < 5; row++)
        {
            songPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        }
        songPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var heading = CreateResultHeading("SongHeading");
        heading.Margin = new Padding(0, 0, 0, 8);
        songPanel.SetColumnSpan(heading, 2);
        songPanel.Controls.Add(heading, 0, 0);
        AddInformationRow(songPanel, 1, "TitleLabel", songTitleValue);
        AddInformationRow(songPanel, 2, "ChannelLabel", songChannelValue);
        AddInformationRow(songPanel, 3, "DurationLabel", songDurationValue);
        AddInformationRow(songPanel, 4, "PublishedLabel", songPublishedValue);

        ConfigureThumbnailPreview(songThumbnail);
        songThumbnail.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        songThumbnail.Size = new Size(210, 210);
        songThumbnail.Margin = new Padding(18, 2, 0, 0);
        songPanel.Controls.Add(songThumbnail, 2, 0);
        songPanel.SetRowSpan(songThumbnail, 6);
    }

    private void BuildPlaylistPanel()
    {
        playlistPanel.Dock = DockStyle.Fill;
        playlistPanel.Tag = "surface";
        playlistPanel.Padding = new Padding(16, 10, 16, 10);
        playlistPanel.ColumnCount = 1;
        playlistPanel.RowCount = 4;
        playlistPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        playlistPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        playlistPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        playlistPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        playlistSummary.Dock = DockStyle.Fill;
        playlistSummary.Tag = "surface";
        playlistSummary.AutoSize = true;
        playlistSummary.ColumnCount = 3;
        playlistSummary.RowCount = 4;
        playlistSummary.Margin = new Padding(0, 0, 0, 4);
        playlistSummary.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82F));
        playlistSummary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        playlistSummary.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 154F));

        var heading = CreateResultHeading("PlaylistHeading");
        heading.Margin = new Padding(0, 0, 0, 4);
        playlistSummary.SetColumnSpan(heading, 2);
        playlistSummary.Controls.Add(heading, 0, 0);
        AddInformationRow(playlistSummary, 1, "PlaylistLabel", playlistNameValue, 2, omitTrailingColon: true);
        AddInformationRow(playlistSummary, 2, "ChannelLabel", playlistChannelValue, 2, omitTrailingColon: true);
        AddInformationRow(playlistSummary, 3, "ItemsLabel", playlistCountValue, 2, omitTrailingColon: true);

        ConfigureThumbnailPreview(playlistThumbnail);
        playlistThumbnail.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        playlistThumbnail.Size = new Size(138, 92);
        playlistThumbnail.Margin = new Padding(16, 2, 0, 0);
        playlistSummary.Controls.Add(playlistThumbnail, 2, 0);
        playlistSummary.SetRowSpan(playlistThumbnail, 4);

        ConfigurePlaylistList();
        ConfigurePlaylistSelectionBar();

        var folderSection = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Tag = "surface",
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 6, 0, 0)
        };

        folderSection.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        folderSection.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        var folderLabel = CreateSectionLabel("FolderNameLabel");
        folderLabel.Anchor = AnchorStyles.Left;
        folderLabel.Margin = new Padding(0, 0, 12, 0);
        folderSection.Controls.Add(folderLabel, 0, 0);

        playlistFolderTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        playlistFolderTextBox.Height = VerticallyCenteredTextBox.CompactHeight;
        playlistFolderTextBox.Margin = Padding.Empty;
        playlistFolderTextBox.AccessibleName = "Nombre de la carpeta para la playlist";
        folderSection.Controls.Add(playlistFolderTextBox, 1, 0);

        playlistPanel.Controls.Add(playlistSummary, 0, 0);
        playlistPanel.Controls.Add(playlistSelectionBar, 0, 1);
        playlistPanel.Controls.Add(playlistList, 0, 2);
        playlistPanel.Controls.Add(folderSection, 0, 3);
    }

    private void ConfigurePlaylistList()
    {
        playlistList.Dock = DockStyle.Fill;
        playlistList.View = View.Details;
        playlistList.HeaderStyle = ColumnHeaderStyle.None;
        playlistList.FullRowSelect = true;
        playlistList.HideSelection = true;
        playlistList.MultiSelect = false;
        playlistList.CheckBoxes = true;
        playlistList.BorderStyle = BorderStyle.FixedSingle;
        playlistList.Font = new Font("Segoe UI", 9.5F);
        playlistList.Margin = Padding.Empty;
        playlistList.AccessibleName = "Canciones de la playlist";

        playlistList.Columns.Add("Canción", 560);
        playlistList.Columns.Add("Duración", 85, HorizontalAlignment.Right);
        playlistList.ItemCheck += PlaylistList_ItemCheck;
        playlistList.Resize += (_, _) => ResizePlaylistColumns();
    }

    private static void ConfigureThumbnailPreview(ArtworkBox thumbnail)
    {
        thumbnail.Tag = "artwork";
        thumbnail.CornerRadius = 10;
        thumbnail.TabStop = false;
    }

    private void ConfigurePlaylistSelectionBar()
    {
        playlistSelectionBar.Dock = DockStyle.Fill;
        playlistSelectionBar.Tag = "surface";
        playlistSelectionBar.AutoSize = true;
        playlistSelectionBar.ColumnCount = 2;
        playlistSelectionBar.RowCount = 1;
        playlistSelectionBar.Margin = new Padding(0, 0, 0, 6);
        playlistSelectionBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        playlistSelectionBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        var actions = new FlowLayoutPanel
        {
            AutoSize = true,
            Tag = "surface",
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty
        };

        ConfigureSecondaryButton(selectAllButton, "SelectAll");
        selectAllButton.AutoSize = true;
        selectAllButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        selectAllButton.MinimumSize = new Size(0, 24);
        selectAllButton.Padding = new Padding(5, 0, 5, 0);
        selectAllButton.Margin = new Padding(0, 0, 5, 0);
        selectAllButton.Font = new Font("Segoe UI", 9F);
        selectAllButton.CornerRadius = 6;
        selectAllButton.Click += (_, _) => SetAllPlaylistItemsChecked(true);

        ConfigureSecondaryButton(deselectAllButton, "DeselectAll");
        deselectAllButton.AutoSize = true;
        deselectAllButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        deselectAllButton.MinimumSize = new Size(0, 24);
        deselectAllButton.Padding = new Padding(5, 0, 5, 0);
        deselectAllButton.Margin = Padding.Empty;
        deselectAllButton.Font = new Font("Segoe UI", 9F);
        deselectAllButton.CornerRadius = 6;
        deselectAllButton.Click += (_, _) => SetAllPlaylistItemsChecked(false);

        actions.Controls.Add(selectAllButton);
        actions.Controls.Add(deselectAllButton);

        playlistSelectionCountLabel.AutoSize = true;
        playlistSelectionCountLabel.Anchor = AnchorStyles.Right;
        playlistSelectionCountLabel.Tag = "secondary";
        playlistSelectionCountLabel.Margin = new Padding(12, 0, 0, 0);

        playlistSelectionBar.Controls.Add(actions, 0, 0);
        playlistSelectionBar.Controls.Add(playlistSelectionCountLabel, 1, 0);
    }

    private Control CreateActionCard()
    {
        var card = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            Tag = "surface",
            CornerRadius = 12,
            Padding = new Padding(16),
            Margin = Padding.Empty
        };

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Tag = "surface",
            ColumnCount = 1,
            RowCount = 4,
            Margin = Padding.Empty
        };

        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        content.Controls.Add(CreateDestinationRow(), 0, 0);
        content.Controls.Add(CreateQualityRow(), 0, 1);
        content.Controls.Add(CreateActionHost(), 0, 2);
        content.Controls.Add(CreateProgressSection(), 0, 3);
        card.Controls.Add(content);
        return card;
    }

    private Control CreateDestinationRow()
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Tag = "surface",
            AutoSize = true,
            ColumnCount = 3,
            RowCount = 1,
            Margin = Padding.Empty
        };

        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 152F));

        var label = CreateSectionLabel("SaveInLabel");
        label.Anchor = AnchorStyles.Left;

        destinationTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        destinationTextBox.Height = VerticallyCenteredTextBox.CompactHeight;
        destinationTextBox.SetReadOnly(true);
        destinationTextBox.BorderStyle = BorderStyle.FixedSingle;
        destinationTextBox.Margin = new Padding(0, 0, 10, 0);
        destinationTextBox.MinimumSize = new Size(0, 36);
        destinationTextBox.AccessibleName = "Carpeta de destino";

        ConfigureSecondaryButton(chooseFolderButton, "ChooseFolder");
        chooseFolderButton.FlatAppearance.BorderSize = 0;
        chooseFolderButton.DrawRoundedBorder = true;
        chooseFolderButton.Dock = DockStyle.Fill;
        chooseFolderButton.Margin = Padding.Empty;
        chooseFolderButton.Click += ChooseFolderButton_Click;

        row.Controls.Add(label, 0, 0);
        row.Controls.Add(destinationTextBox, 1, 0);
        row.Controls.Add(chooseFolderButton, 2, 0);
        return row;
    }

    private Control CreateQualityRow()
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Tag = "surface",
            AutoSize = true,
            ColumnCount = 3,
            RowCount = 1,
            Margin = new Padding(0, 10, 0, 0)
        };

        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 306F));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        var qualityLabel = CreateSectionLabel("QualityLabel");
        qualityLabel.Anchor = AnchorStyles.Left;
        qualityLabel.Margin = Padding.Empty;

        ConfigureQualitySelector();

        row.Controls.Add(qualityLabel, 0, 0);
        row.Controls.Add(qualitySelector, 1, 0);
        return row;
    }

    private void ConfigureQualitySelector()
    {
        qualitySelector.Dock = DockStyle.Fill;
        qualitySelector.Height = 34;
        qualitySelector.ColumnCount = 3;
        qualitySelector.RowCount = 1;
        qualitySelector.Margin = Padding.Empty;
        qualitySelector.AccessibleName = "Calidad MP3";

        for (var column = 0; column < 3; column++)
        {
            qualitySelector.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
        }

        ConfigureQualityButton(highQualityButton, "QualityHigh", AudioQuality.High);
        ConfigureQualityButton(mediumQualityButton, "QualityMedium", AudioQuality.Medium);
        ConfigureQualityButton(lowQualityButton, "QualityLow", AudioQuality.Low);

        highQualityButton.CornerRadius = 8;
        highQualityButton.RoundedCorners = RoundedCorners.TopLeft | RoundedCorners.BottomLeft;
        mediumQualityButton.CornerRadius = 0;
        lowQualityButton.CornerRadius = 8;
        lowQualityButton.RoundedCorners = RoundedCorners.TopRight | RoundedCorners.BottomRight;

        qualitySelector.Controls.Add(highQualityButton, 0, 0);
        qualitySelector.Controls.Add(mediumQualityButton, 1, 0);
        qualitySelector.Controls.Add(lowQualityButton, 2, 0);
    }

    private void ConfigureQualityButton(
        RoundedButton button,
        string textKey,
        AudioQuality quality)
    {
        localizedControls[button] = textKey;
        button.Dock = DockStyle.Fill;
        button.Margin = Padding.Empty;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.Font = new Font("Segoe UI Semibold", 9.5F);
        button.Cursor = Cursors.Hand;
        button.UseVisualStyleBackColor = false;
        button.Click += (_, _) => SelectAudioQuality(quality, save: true);
    }

    private Control CreateDownloadActions()
    {
        downloadActionsPanel.Dock = DockStyle.Fill;
        downloadActionsPanel.Tag = "surface";
        downloadActionsPanel.ColumnCount = 4;
        downloadActionsPanel.Margin = Padding.Empty;
        downloadActionsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        downloadActionsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 174F));
        downloadActionsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112F));
        downloadActionsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

        ConfigurePrimaryButton(downloadButton, "Download");
        downloadButton.Dock = DockStyle.Fill;
        downloadButton.Margin = new Padding(0, 0, 10, 0);
        downloadButton.Click += DownloadButton_Click;

        ConfigureSecondaryButton(cancelButton, "Cancel");
        cancelButton.Dock = DockStyle.Fill;
        cancelButton.Margin = Padding.Empty;
        cancelButton.Click += CancelButton_Click;

        downloadActionsPanel.Controls.Add(downloadButton, 1, 0);
        downloadActionsPanel.Controls.Add(cancelButton, 2, 0);
        return downloadActionsPanel;
    }

    private Control CreateActionHost()
    {
        finalActionsHost.Dock = DockStyle.Fill;
        finalActionsHost.Tag = "surface";
        finalActionsHost.Margin = new Padding(0, 12, 0, 0);

        var downloads = CreateDownloadActions();
        var finals = CreateFinalActions();
        finalActionsHost.Controls.Add(downloads);
        finalActionsHost.Controls.Add(finals);
        return finalActionsHost;
    }

    private Control CreateProgressSection()
    {
        var section = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Tag = "surface",
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 2,
            Margin = new Padding(0, 10, 0, 0)
        };

        section.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        section.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 54F));

        progressBar.Dock = DockStyle.Fill;
        progressBar.Height = 18;
        progressBar.Style = ProgressBarStyle.Continuous;
        progressBar.Margin = new Padding(0, 0, 10, 7);

        progressPercentageLabel.AutoSize = true;
        progressPercentageLabel.TextAlign = ContentAlignment.MiddleRight;
        progressPercentageLabel.Anchor = AnchorStyles.Right;
        progressPercentageLabel.Margin = new Padding(0, 0, 0, 7);

        statusLabel.AutoSize = true;
        statusLabel.Tag = "secondary";
        statusLabel.Margin = Padding.Empty;
        section.SetColumnSpan(statusLabel, 2);

        section.Controls.Add(progressBar, 0, 0);
        section.Controls.Add(progressPercentageLabel, 1, 0);
        section.Controls.Add(statusLabel, 0, 1);
        return section;
    }

    private Control CreateFinalActions()
    {
        finalActionsPanel.Dock = DockStyle.Fill;
        finalActionsPanel.Tag = "surface";
        finalActionsPanel.ColumnCount = 4;
        finalActionsPanel.Margin = Padding.Empty;
        finalActionsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        finalActionsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145F));
        finalActionsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 154F));
        finalActionsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

        ConfigureSecondaryButton(openFolderButton, "OpenFolder");
        openFolderButton.Dock = DockStyle.Fill;
        openFolderButton.Margin = new Padding(0, 0, 10, 0);
        openFolderButton.Click += OpenFolderButton_Click;

        ConfigureSecondaryButton(newDownloadButton, "NewDownload");
        newDownloadButton.Dock = DockStyle.Fill;
        newDownloadButton.Margin = Padding.Empty;
        newDownloadButton.Click += NewDownloadButton_Click;

        finalActionsPanel.Controls.Add(openFolderButton, 1, 0);
        finalActionsPanel.Controls.Add(newDownloadButton, 2, 0);
        return finalActionsPanel;
    }

    private static Panel CreateSeparator()
    {
        return new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 1,
            Tag = "separator",
            Margin = new Padding(0, 8, 0, 8)
        };
    }

    private Label CreateSectionLabel(string key)
    {
        var label = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 10F),
            Text = textService.Get(key),
            Margin = Padding.Empty
        };
        localizedControls[label] = key;
        return label;
    }

    private Label CreateResultHeading(string key)
    {
        var label = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 12F),
            Text = textService.Get(key),
            Margin = new Padding(0, 0, 0, 12)
        };
        localizedControls[label] = key;
        return label;
    }

    private static Label CreateValueLabel()
    {
        return new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 10F),
            MaximumSize = new Size(620, 0),
            Margin = new Padding(0, 4, 0, 4)
        };
    }

    private void AddInformationRow(
        TableLayoutPanel table,
        int row,
        string labelKey,
        Label valueLabel,
        int verticalMargin = 4,
        bool omitTrailingColon = false)
    {
        var labelText = textService.Get(labelKey);

        var label = new Label
        {
            AutoSize = true,
            Text = omitTrailingColon ? labelText.TrimEnd(':') : labelText,
            Tag = "secondary",
            Margin = new Padding(0, verticalMargin, 10, verticalMargin)
        };

        valueLabel.Margin = new Padding(0, verticalMargin, 0, verticalMargin);

        if (!omitTrailingColon)
        {
            localizedControls[label] = labelKey;
        }
        else
        {
            localizedControlsWithoutColon[label] = labelKey;
        }

        table.Controls.Add(label, 0, row);
        table.Controls.Add(valueLabel, 1, row);
    }

    private void ConfigurePrimaryButton(Button button, string textKey)
    {
        button.Text = textService.Get(textKey);
        localizedControls[button] = textKey;
        button.AutoSize = false;
        button.Height = 36;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = AccentHoverColor;
        button.BackColor = AccentColor;
        button.ForeColor = Color.White;
        button.Font = new Font("Segoe UI Semibold", 10F);
        button.Cursor = Cursors.Hand;
        button.UseVisualStyleBackColor = false;
    }

    private void ConfigureSecondaryButton(Button button, string textKey)
    {
        button.Text = textService.Get(textKey);
        localizedControls[button] = textKey;
        button.AutoSize = false;
        button.Height = 36;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.Cursor = Cursors.Hand;
        button.UseVisualStyleBackColor = false;
    }

    private static void ConfigureHeaderButton(
        HeaderActionButton button,
        HeaderActionIcon icon,
        RoundedCorners roundedCorners)
    {
        button.Icon = icon;
        button.RoundedCorners = roundedCorners;
        button.Size = new Size(42, 36);
        button.Dock = DockStyle.Fill;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.Font = new Font("Segoe UI Semibold", 9.5F);
        button.Margin = Padding.Empty;
        button.Padding = Padding.Empty;
        button.TextAlign = ContentAlignment.MiddleCenter;
        button.Cursor = Cursors.Hand;
        button.UseVisualStyleBackColor = false;
    }

    private void LoadInitialSettings()
    {
        loadingSettings = true;
        destinationPath = settingsService.LoadDestinationFolder();
        destinationTextBox.Text = destinationPath;
        SelectAudioQuality(settingsService.LoadAudioQuality(), save: false);
        loadingSettings = false;
    }

    private void ApplyLanguage()
    {
        foreach (var (control, key) in localizedControls)
        {
            control.Text = textService.Get(key);
        }

        foreach (var (control, key) in localizedControlsWithoutColon)
        {
            control.Text = textService.Get(key).TrimEnd(':');
        }

        urlTextBox.SetPlaceholderText(textService.Get("UrlPlaceholder"));
        toolTip.SetToolTip(analyzeButton, textService.Get("TooltipAnalyze"));
        toolTip.SetToolTip(downloadButton, textService.Get("TooltipDownload"));
        toolTip.SetToolTip(cancelButton, textService.Get("TooltipCancel"));
        toolTip.SetToolTip(chooseFolderButton, textService.Get("TooltipFolder"));
        toolTip.SetToolTip(qualitySelector, textService.Get("TooltipQuality"));
        toolTip.SetToolTip(highQualityButton, textService.Get("TooltipQuality"));
        toolTip.SetToolTip(mediumQualityButton, textService.Get("TooltipQuality"));
        toolTip.SetToolTip(lowQualityButton, textService.Get("TooltipQuality"));
        toolTip.SetToolTip(settingsButton, textService.Get("TooltipSettings"));
        toolTip.SetToolTip(selectAllButton, textService.Get("SelectAll"));
        toolTip.SetToolTip(deselectAllButton, textService.Get("DeselectAll"));
        toolTip.SetToolTip(openFolderButton, textService.Get("TooltipOpenFolder"));
        toolTip.SetToolTip(newDownloadButton, textService.Get("TooltipNewDownload"));

        RefreshUpdateNotification();
        RefreshDynamicText();
    }

    private void ApplyTheme()
    {
        ThemeService.Apply(this, currentTheme);

        var colors = ThemeService.GetColors(currentTheme);
        foreach (var button in new[]
                 {
                     chooseFolderButton,
                     cancelButton,
                     openFolderButton,
                     newDownloadButton,
                     selectAllButton,
                     deselectAllButton,
                     settingsButton
                 })
        {
            button.BackColor = colors.SecondaryButton;
            button.ForeColor = colors.Text;
            button.FlatAppearance.BorderColor = colors.Border;
            button.FlatAppearance.MouseOverBackColor = colors.SecondaryButtonHover;
        }

        StyleHeaderActions(colors);
        StyleQualitySelector();
        SetState(currentState);
    }

    private void RefreshDynamicText()
    {
        if (lastDownloadResult is not null
            && currentState is UiState.Success or UiState.Error)
        {
            ShowDownloadResult(lastDownloadResult);
            return;
        }

        if (currentMedia?.Type == MediaType.Video)
        {
            songDurationValue.Text = FormatDuration(
                currentMedia.DurationSeconds,
                textService.Get("NotAvailable"));
            songPublishedValue.Text = currentMedia.PublicationDate?.ToString("dd/MM/yyyy")
                                      ?? textService.Get("NotAvailable");
        }

        if (currentResult == ResultKind.Playlist)
        {
            playlistSelectionCountLabel.Text = textService.Get(
                "PlaylistSelectionCount",
                selectedPlaylistCount,
                playlistList.Items.Count);
        }

        UpdateProgressAndMessage(currentState);

        if (!string.IsNullOrEmpty(currentMessageKey) && messagePanel.Visible)
        {
            messageLabel.Text = GetDetailedMessage(currentMessageKey);
        }

    }

    private async Task CheckYtDlpUpdateIfDueAsync()
    {
        if (!settingsService.ShouldCheckYtDlpUpdate(DateTimeOffset.UtcNow))
        {
            return;
        }

        updateCancellation?.Cancel();
        updateCancellation?.Dispose();
        updateCancellation = new CancellationTokenSource();

        try
        {
            var update = await ytDlpUpdateService.CheckAsync(updateCancellation.Token);
            settingsService.SaveYtDlpUpdateCheck(DateTimeOffset.UtcNow);

            if (IsDisposed)
            {
                return;
            }

            availableUpdate = update.IsUpdateAvailable ? update : null;
            RefreshUpdateNotification();
        }
        catch (OperationCanceledException)
        {
            // Cerrar la aplicación cancela la comprobación silenciosamente.
        }
        catch (Exception exception)
        {
            logService.Error(
                "Comprobar actualización yt-dlp",
                ("Detalle", exception.ToString()));
        }
    }

    private async Task CheckFfmpegUpdateIfDueAsync()
    {
        if (!settingsService.ShouldCheckFfmpegUpdate(DateTimeOffset.UtcNow))
        {
            return;
        }

        ffmpegUpdateCancellation?.Cancel();
        ffmpegUpdateCancellation?.Dispose();
        ffmpegUpdateCancellation = new CancellationTokenSource();

        try
        {
            var update = await dependencyService.CheckForUpdateAsync(
                ffmpegUpdateCancellation.Token);
            settingsService.SaveFfmpegUpdateCheck(DateTimeOffset.UtcNow);

            if (IsDisposed)
            {
                return;
            }

            availableFfmpegUpdate = update.IsUpdateAvailable ? update : null;
            RefreshUpdateNotification();
        }
        catch (OperationCanceledException)
        {
            // Cerrar la aplicación cancela la comprobación silenciosamente.
        }
        catch (Exception exception)
        {
            logService.Error(
                "Comprobar actualización FFmpeg",
                ("Detalle", exception.ToString()));
        }
    }

    private void SettingsButton_Click(object? sender, EventArgs e)
    {
        using var settings = new SettingsForm(
            textService,
            currentTheme,
            currentLanguage,
            availableUpdate,
            availableFfmpegUpdate,
            ytDlpUpdateService,
            dependencyService,
            dependencyBootstrapService,
            settingsService,
            logService);

        var result = settings.ShowDialog(this);
        availableUpdate = settings.AvailableUpdate;
        availableFfmpegUpdate = settings.AvailableFfmpegUpdate;
        RefreshUpdateNotification();

        if (result != DialogResult.OK)
        {
            SetState(currentState);
            return;
        }

        var themeChanged = settings.SelectedTheme != currentTheme;
        var languageChanged = settings.SelectedLanguage != currentLanguage;

        if (themeChanged)
        {
            currentTheme = settings.SelectedTheme;
            settingsService.SaveTheme(currentTheme);
            logService.Info("Cambiar tema", ("Tema", currentTheme));
        }

        if (languageChanged)
        {
            currentLanguage = settings.SelectedLanguage;
            textService.SetLanguage(currentLanguage);
            settingsService.SaveLanguage(currentLanguage);
            logService.Info("Cambiar idioma", ("Idioma", currentLanguage));
            ApplyLanguage();
        }

        if (themeChanged)
        {
            ApplyTheme();
        }
        else
        {
            SetState(currentState);
        }
    }

    private void RefreshUpdateNotification()
    {
        var hasPendingUpdate = UpdateNotification.HasPendingUpdate(
            availableUpdate,
            availableFfmpegUpdate);
        settingsButton.HasNotification = hasPendingUpdate;
        settingsButton.AccessibleDescription = hasPendingUpdate
            ? textService.Get("UpdatesAvailable")
            : textService.Get("TooltipSettings");
        toolTip.SetToolTip(
            settingsButton,
            textService.Get(hasPendingUpdate ? "UpdatesAvailable" : "TooltipSettings"));
    }

    private async void MainForm_Shown(object? sender, EventArgs e)
    {
        analysisCancellation = new CancellationTokenSource();

        try
        {
            SetState(UiState.Preparing);
            var dependenciesValid = await dependencyBootstrapService.ValidateDependenciesAsync(
                analysisCancellation.Token);

            if (!dependenciesValid)
            {
                using var setup = new DependencySetupForm(
                    textService,
                    currentTheme,
                    dependencyBootstrapService);
                var setupResult = setup.ShowDialog(this);

                if (setupResult != DialogResult.OK)
                {
                    Close();
                    return;
                }
            }

            if (!IsDisposed)
            {
                SetState(UiState.Initial);
            }

            _ = CheckYtDlpUpdateIfDueAsync();
            _ = CheckFfmpegUpdateIfDueAsync();
        }
        catch (OperationCanceledException)
        {
            // Cerrar la ventana cancela silenciosamente la preparación.
        }
        catch (YtDlpException)
        {
            if (!IsDisposed)
            {
                ShowAnalysisMessage(
                    UiState.PreparationFailed,
                    "PreparationFailed",
                    "PreparationFailed");
            }
        }
    }

    private async void AnalyzeButton_Click(object? sender, EventArgs e)
    {
        var url = urlTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(url))
        {
            ShowAnalysisMessage(
                UiState.InvalidUrl,
                "EnterLink",
                "EnterLink");
            return;
        }

        analysisCancellation?.Cancel();
        analysisCancellation?.Dispose();
        analysisCancellation = new CancellationTokenSource();

        ClearResult();

        try
        {
            if (!ytDlpService.IsAvailable)
            {
                SetState(UiState.Preparing);
                await ytDlpService.EnsureAvailableAsync(analysisCancellation.Token);
            }

            SetState(UiState.Analyzing);
            var media = await ytDlpService.AnalyzeAsync(url, analysisCancellation.Token);

            if (!IsDisposed)
            {
                currentUrl = url;
                currentMedia = media;
                ShowMedia(media);
            }
        }
        catch (OperationCanceledException)
        {
            if (!IsDisposed)
            {
                SetState(UiState.Initial);
                statusLabel.Text = textService.Get("AnalyzingCancelled");
            }
        }
        catch (YtDlpException exception)
        {
            if (!IsDisposed)
            {
                ShowYtDlpError(exception);
            }
        }
        catch (Exception exception)
        {
            logService.Error(
                "Analizar URL",
                ("URL", url),
                ("Detalle", exception.ToString()));

            if (!IsDisposed)
            {
                ShowAnalysisMessage(
                    UiState.AnalysisError,
                    "AnalysisFailed",
                    "AnalysisFailed");
            }
        }
    }

    private void ShowMedia(MediaInfo media)
    {
        if (media.Type == MediaType.Video)
        {
            currentResult = ResultKind.Song;
            currentItemCount = 1;
            songTitleValue.Text = media.Title;
            songChannelValue.Text = media.Creator;
            songDurationValue.Text = FormatDuration(media.DurationSeconds, textService.Get("NotAvailable"));
            songPublishedValue.Text = media.PublicationDate?.ToString("dd/MM/yyyy")
                                      ?? textService.Get("NotAvailable");
            SetState(UiState.SongReady);
            _ = LoadThumbnailAsync(media, songThumbnail);
            return;
        }

        currentResult = ResultKind.Playlist;
        currentItemCount = media.Items.Count;
        playlistNameValue.Text = media.Title;
        playlistChannelValue.Text = media.Creator;
        playlistCountValue.Text = media.Items.Count.ToString();
        playlistFolderTextBox.Text = media.Title;

        changingPlaylistChecks = true;
        playlistList.BeginUpdate();
        playlistList.Items.Clear();

        foreach (var item in media.Items)
        {
            var row = new ListViewItem($"{item.Index:00}. {item.Title}")
            {
                Checked = true,
                Tag = item
            };
            row.SubItems.Add(FormatDuration(item.DurationSeconds, "--:--"));
            playlistList.Items.Add(row);
        }

        playlistList.EndUpdate();
        changingPlaylistChecks = false;
        UpdatePlaylistSelection(media.Items.Count);
        ResizePlaylistColumns();
        SetState(UiState.PlaylistReady);
        _ = LoadThumbnailAsync(media, playlistThumbnail);
    }

    private async Task LoadThumbnailAsync(
        MediaInfo media,
        ArtworkBox target)
    {
        thumbnailCancellation?.Cancel();
        thumbnailCancellation?.Dispose();
        thumbnailCancellation = null;
        ClearThumbnails();

        var urls = media.ThumbnailUrls.Count > 0
            ? media.ThumbnailUrls
            : string.IsNullOrWhiteSpace(media.ThumbnailUrl)
                ? []
                : [media.ThumbnailUrl];

        if (urls.Count == 0)
        {
            return;
        }

        thumbnailCancellation = new CancellationTokenSource();
        var cancellationToken = thumbnailCancellation.Token;

        try
        {
            foreach (var url in urls)
            {
                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
                    || uri.Scheme is not ("http" or "https"))
                {
                    continue;
                }

                Image? preview = null;

                try
                {
                    using var response = await ThumbnailHttpClient.GetAsync(
                        uri,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken);

                    if (!response.IsSuccessStatusCode)
                    {
                        continue;
                    }

                    await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                    using var downloadedImage = Image.FromStream(stream);
                    preview = new Bitmap(downloadedImage);
                    cancellationToken.ThrowIfCancellationRequested();

                    if (IsDisposed || !ReferenceEquals(currentMedia, media))
                    {
                        return;
                    }

                    target.Image = preview;
                    preview = null;
                    target.Invalidate();
                    return;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch
                {
                    // Una variante inválida no impide probar la siguiente del mismo análisis.
                }
                finally
                {
                    preview?.Dispose();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Cambiar de enlace o cerrar la ventana cancela la preview silenciosamente.
        }
    }

    private void ClearThumbnails()
    {
        ClearThumbnail(songThumbnail);
        ClearThumbnail(playlistThumbnail);
    }

    private static void ClearThumbnail(ArtworkBox thumbnail)
    {
        var previousImage = thumbnail.Image;
        thumbnail.Image = null;
        previousImage?.Dispose();
        thumbnail.Invalidate();
    }

    private void PlaylistList_ItemCheck(object? sender, ItemCheckEventArgs e)
    {
        if (changingPlaylistChecks)
        {
            return;
        }

        var selectedCount = playlistList.CheckedItems.Count;

        if (e.CurrentValue != CheckState.Checked && e.NewValue == CheckState.Checked)
        {
            selectedCount++;
        }
        else if (e.CurrentValue == CheckState.Checked && e.NewValue != CheckState.Checked)
        {
            selectedCount--;
        }

        UpdatePlaylistSelection(selectedCount);
    }

    private void SetAllPlaylistItemsChecked(bool isChecked)
    {
        changingPlaylistChecks = true;
        playlistList.BeginUpdate();

        foreach (ListViewItem item in playlistList.Items)
        {
            item.Checked = isChecked;
        }

        playlistList.EndUpdate();
        changingPlaylistChecks = false;
        UpdatePlaylistSelection(isChecked ? playlistList.Items.Count : 0);
    }

    private void UpdatePlaylistSelection(int selectedCount)
    {
        selectedPlaylistCount = Math.Clamp(selectedCount, 0, playlistList.Items.Count);
        currentItemCount = selectedPlaylistCount;
        playlistSelectionCountLabel.Text = textService.Get(
            "PlaylistSelectionCount",
            selectedPlaylistCount,
            playlistList.Items.Count);

        if (currentState is UiState.PlaylistReady or UiState.Cancelled)
        {
            SetPrimaryButtonEnabled(downloadButton, selectedPlaylistCount > 0);
        }
    }

    private IReadOnlyList<PlaylistItemInfo> GetSelectedPlaylistItems()
    {
        return playlistList.Items
            .Cast<ListViewItem>()
            .Where(item => item.Checked)
            .Select(item => item.Tag)
            .OfType<PlaylistItemInfo>()
            .ToList();
    }

    private void ShowYtDlpError(YtDlpException exception)
    {
        var (state, messageKey, statusKey) = exception.Kind switch
        {
            YtDlpErrorKind.Preparation => (
                UiState.PreparationFailed,
                "PreparationFailed",
                "PreparationFailed"),

            YtDlpErrorKind.Unavailable => (
                UiState.AnalysisError,
                "ContentUnavailable",
                "ContentUnavailable"),

            YtDlpErrorKind.Network => (
                UiState.AnalysisError,
                "NetworkFailed",
                "NetworkFailed"),

            _ => (
                UiState.AnalysisError,
                "AnalysisFailed",
                "AnalysisFailed")
        };

        ShowAnalysisMessage(state, messageKey, statusKey);
    }

    private void ShowAnalysisMessage(UiState state, string messageKey, string statusKey)
    {
        currentResult = ResultKind.None;
        currentMessageKey = messageKey;
        SetState(state);
        messageLabel.Text = GetDetailedMessage(messageKey);
        statusLabel.Text = textService.Get(statusKey);
    }

    private async void DownloadButton_Click(object? sender, EventArgs e)
    {
        if (currentResult == ResultKind.None)
        {
            return;
        }

        if (currentMedia is null)
        {
            return;
        }

        IReadOnlyList<PlaylistItemInfo>? selectedPlaylistItems = null;

        if (currentResult == ResultKind.Playlist)
        {
            selectedPlaylistItems = GetSelectedPlaylistItems();

            if (selectedPlaylistItems.Count == 0)
            {
                statusLabel.Text = textService.Get("SelectAtLeastOne");
                return;
            }

            currentItemCount = selectedPlaylistItems.Count;
        }

        downloadCancellation?.Cancel();
        downloadCancellation?.Dispose();
        downloadCancellation = new CancellationTokenSource();

        try
        {
            SetState(UiState.Downloading);
            var progress = new Progress<DownloadProgress>(UpdateRealDownloadProgress);
            var result = await downloadService.DownloadAsync(
                currentUrl,
                currentMedia,
                selectedPlaylistItems,
                destinationPath,
                playlistFolderTextBox.Text,
                GetSelectedAudioQuality(),
                progress,
                downloadCancellation.Token);

            if (!IsDisposed)
            {
                lastDownloadResult = result;
                ShowDownloadResult(result);
            }
        }
        catch (OperationCanceledException)
        {
            if (IsDisposed)
            {
                return;
            }

            progressBar.Value = 0;
            progressPercentageLabel.Text = "0 %";
            SetState(UiState.Cancelled);
        }
        catch (Exception exception)
        {
            logService.Error(
                "Descarga",
                ("URL", currentUrl),
                ("Destino", destinationPath),
                ("Detalle", exception.ToString()));

            if (!IsDisposed)
            {
                lastDownloadFolder = Directory.Exists(destinationPath) ? destinationPath : string.Empty;
                currentMessageKey = "DownloadFailed";
                SetState(UiState.Error);
                messageLabel.Text = GetDetailedMessage("DownloadFailed");
            }
        }
    }

    private void UpdateRealDownloadProgress(DownloadProgress progress)
    {
        var percentage = (int)Math.Round(progress.GlobalPercent);
        progressBar.Value = Math.Clamp(percentage, progressBar.Minimum, progressBar.Maximum);
        progressPercentageLabel.Text = $"{percentage} %";

        if (progress.IsValidating)
        {
            currentState = UiState.Validating;
            statusLabel.Text = currentResult == ResultKind.Playlist
                ? textService.Get("CheckingPlaylist", progress.ItemIndex, progress.TotalItems)
                : textService.Get("CheckingSong");
            return;
        }

        currentState = UiState.Downloading;
        statusLabel.Text = currentResult == ResultKind.Playlist
            ? textService.Get("DownloadingPlaylist", progress.ItemIndex, progress.TotalItems)
            : textService.Get("DownloadingSong");
    }

    private void ShowDownloadResult(DownloadResult result)
    {
        currentMessageKey = string.Empty;
        showCompletionView = false;
        lastDownloadFolder = result.DestinationFolder;
        progressBar.Value = 100;
        progressPercentageLabel.Text = "100 %";

        if (currentResult == ResultKind.Song)
        {
            var item = result.Items.SingleOrDefault();

            if (item?.Status == DownloadItemStatus.Downloaded)
            {
                showCompletionView = true;
                SetState(UiState.Success);
                messageLabel.Text = textService.Get("DownloadSuccess");
                statusLabel.Text = textService.Get("DownloadSuccess");
            }
            else if (item?.Status == DownloadItemStatus.AlreadyExists)
            {
                SetState(UiState.Success);
                messageLabel.Text = textService.Get("ExistingSong");
                statusLabel.Text = textService.Get("ExistingSong");
            }
            else
            {
                SetState(UiState.Error);
                progressBar.Value = 100;
                progressPercentageLabel.Text = "100 %";
                messageLabel.Text = textService.Get("SongFailedAfterAttempts");
                statusLabel.Text = textService.Get("DownloadFailed");
            }

            return;
        }

        var total = result.Items.Count;

        if (result.FailedCount == 0 && result.ExistingCount == 0)
        {
            showCompletionView = true;
            SetState(UiState.Success);
            messageLabel.Text = textService.Get("PlaylistAllSuccess", result.DownloadedCount, total);
            statusLabel.Text = textService.Get("DownloadSuccess");
            return;
        }

        var summary = new List<string>
        {
            result.FailedCount == 0
                ? textService.Get("PlaylistFinished")
                : textService.Get("PlaylistWithIssues"),
            string.Empty,
            FormatCount(result.DownloadedCount, "DownloadedOne", "DownloadedMany"),
            FormatCount(result.ExistingCount, "ExistingOne", "ExistingMany"),
            FormatCount(result.FailedCount, "FailedOne", "FailedMany")
        };

        if (result.FailedCount > 0)
        {
            summary.Add(string.Empty);
            summary.Add(textService.Get("FailedTitles"));
            summary.AddRange(result.FailedTitles.Take(5).Select(title => $"• {title}"));

            if (result.FailedCount > 5)
            {
                summary.Add(textService.Get("AndMore", result.FailedCount - 5));
            }
        }

        SetState(result.FailedCount == 0 ? UiState.Success : UiState.Error);
        progressBar.Value = 100;
        progressPercentageLabel.Text = "100 %";
        messageLabel.Text = string.Join(Environment.NewLine, summary);
        statusLabel.Text = result.FailedCount == 0
            ? textService.Get("PlaylistFinished")
            : textService.Get("PlaylistWithIssues");
    }

    private string FormatCount(int count, string singularKey, string pluralKey)
    {
        return textService.Get(count == 1 ? singularKey : pluralKey, count);
    }

    private void CancelButton_Click(object? sender, EventArgs e)
    {
        cancelButton.Enabled = false;

        if (currentState == UiState.Analyzing)
        {
            statusLabel.Text = textService.Get("CancellingAnalysis");
            analysisCancellation?.Cancel();
        }
        else
        {
            statusLabel.Text = textService.Get("Cancelling");
            downloadCancellation?.Cancel();
        }
    }

    private void UrlTextBox_TextChanged(object? sender, EventArgs e)
    {
        if (resettingUrl || currentState is UiState.Initial or UiState.Analyzing)
        {
            return;
        }

        ClearResult();
        SetState(UiState.Initial);
    }

    private void ChooseFolderButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = textService.Get("FolderDialog"),
            UseDescriptionForTitle = true,
            ShowNewFolderButton = true,
            SelectedPath = Directory.Exists(destinationPath) ? destinationPath : string.Empty
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        destinationPath = Path.GetFullPath(dialog.SelectedPath);
        destinationTextBox.Text = destinationPath;
        settingsService.SaveDestinationFolder(destinationPath);
    }

    private void SelectAudioQuality(AudioQuality quality, bool save)
    {
        selectedAudioQuality = quality;
        StyleQualitySelector();

        if (save && !loadingSettings)
        {
            settingsService.SaveAudioQuality(selectedAudioQuality);
        }
    }

    private void StyleQualitySelector()
    {
        var colors = ThemeService.GetColors(currentTheme);
        var enabled = qualitySelector.Enabled;

        foreach (var (button, quality) in new[]
                 {
                     (highQualityButton, AudioQuality.High),
                     (mediumQualityButton, AudioQuality.Medium),
                     (lowQualityButton, AudioQuality.Low)
                 })
        {
            var selected = quality == selectedAudioQuality;
            button.BackColor = !enabled
                ? colors.Disabled
                : selected ? AccentColor : colors.SecondaryButton;
            button.ForeColor = !enabled
                ? colors.DisabledText
                : selected ? Color.White : colors.Text;
            button.FlatAppearance.BorderColor = selected && enabled
                ? AccentColor
                : colors.Border;
            button.FlatAppearance.MouseOverBackColor = selected
                ? AccentHoverColor
                : colors.SecondaryButtonHover;
            button.Cursor = enabled ? Cursors.Hand : Cursors.Default;
        }
    }

    private AudioQuality GetSelectedAudioQuality()
    {
        return selectedAudioQuality;
    }

    private void OpenFolderButton_Click(object? sender, EventArgs e)
    {
        var folderToOpen = string.IsNullOrWhiteSpace(lastDownloadFolder)
            ? destinationPath
            : lastDownloadFolder;

        if (!Directory.Exists(folderToOpen))
        {
            MessageBox.Show(
                this,
                textService.Get("FolderMissing"),
                "TubeVault",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "explorer.exe",
            UseShellExecute = true
        };

        startInfo.ArgumentList.Add(folderToOpen);
        Process.Start(startInfo);
    }

    private void NewDownloadButton_Click(object? sender, EventArgs e)
    {
        analysisCancellation?.Cancel();
        downloadCancellation?.Cancel();
        ClearResult();

        resettingUrl = true;
        urlTextBox.Clear();
        resettingUrl = false;

        SetState(UiState.Initial);
        urlTextBox.Focus();
    }

    private void ClearResult()
    {
        thumbnailCancellation?.Cancel();
        thumbnailCancellation?.Dispose();
        thumbnailCancellation = null;
        ClearThumbnails();

        currentResult = ResultKind.None;
        currentItemCount = 0;
        selectedPlaylistCount = 0;
        currentMedia = null;
        lastDownloadResult = null;
        currentUrl = string.Empty;
        currentMessageKey = string.Empty;
        lastDownloadFolder = string.Empty;
        showCompletionView = false;
        songTitleValue.Text = string.Empty;
        songChannelValue.Text = string.Empty;
        songDurationValue.Text = string.Empty;
        songPublishedValue.Text = string.Empty;
        playlistNameValue.Text = string.Empty;
        playlistChannelValue.Text = string.Empty;
        playlistCountValue.Text = string.Empty;
        playlistFolderTextBox.Clear();
        playlistList.Items.Clear();
        playlistSelectionCountLabel.Text = string.Empty;
    }

    private void SetState(UiState state)
    {
        currentState = state;

        var resultReady = state is UiState.SongReady or UiState.PlaylistReady or UiState.Cancelled;
        var isBusy = state is UiState.Preparing or UiState.Analyzing or UiState.Downloading or UiState.Validating;
        var isFinished = state is UiState.Success or UiState.Error;

        urlTextBox.Enabled = !isBusy && !isFinished;
        SetPrimaryButtonEnabled(analyzeButton, !isBusy && !isFinished);
        var canDownload = resultReady
                          && (currentResult != ResultKind.Playlist || selectedPlaylistCount > 0);
        SetPrimaryButtonEnabled(downloadButton, canDownload);
        cancelButton.Enabled = state is UiState.Analyzing or UiState.Downloading or UiState.Validating;
        chooseFolderButton.Enabled = !isBusy;
        qualitySelector.Enabled = !isBusy;
        playlistFolderTextBox.Enabled = !isBusy;
        playlistList.Enabled = !isBusy && !isFinished;
        selectAllButton.Enabled = !isBusy && !isFinished;
        deselectAllButton.Enabled = !isBusy && !isFinished;
        settingsButton.Enabled = !isBusy;

        downloadActionsPanel.Visible = !isFinished;
        finalActionsPanel.Visible = isFinished;

        var finalActionsParent = showCompletionView && state == UiState.Success
            ? successActionsHost
            : finalActionsHost;
        if (finalActionsPanel.Parent != finalActionsParent)
        {
            finalActionsParent.Controls.Add(finalActionsPanel);
        }

        if (isFinished)
        {
            finalActionsPanel.BringToFront();
        }
        else
        {
            downloadActionsPanel.BringToFront();
        }
        var canOpenFolder = isFinished
                            && !string.IsNullOrWhiteSpace(lastDownloadFolder)
                            && Directory.Exists(lastDownloadFolder);
        openFolderButton.Visible = canOpenFolder;
        openFolderButton.Enabled = canOpenFolder;
        newDownloadButton.Visible = isFinished;
        newDownloadButton.Enabled = isFinished;

        successDescriptionLabel.Text = currentResult == ResultKind.Playlist
            ? textService.Get("SuccessPlaylistReady")
            : textService.Get("SuccessFileReady");

        UpdateResultVisibility(state);
        UpdateProgressAndMessage(state);
        StyleButtons();
    }

    private void SetPrimaryButtonEnabled(Button button, bool enabled)
    {
        var colors = ThemeService.GetColors(currentTheme);
        button.Enabled = enabled;
        button.BackColor = enabled ? AccentColor : colors.Disabled;
        button.ForeColor = enabled ? Color.White : colors.DisabledText;
        button.Cursor = enabled ? Cursors.Hand : Cursors.Default;
    }

    private void StyleButtons()
    {
        var colors = ThemeService.GetColors(currentTheme);

        foreach (var button in new[]
                 {
                     chooseFolderButton,
                     cancelButton,
                     openFolderButton,
                     newDownloadButton,
                     selectAllButton,
                     deselectAllButton,
                     settingsButton
                 })
        {
            button.BackColor = button.Enabled ? colors.SecondaryButton : colors.Disabled;
            button.ForeColor = button.Enabled ? colors.Text : colors.DisabledText;
            button.FlatAppearance.BorderColor = colors.Border;
        }

        if (showCompletionView && currentState == UiState.Success && openFolderButton.Enabled)
        {
            openFolderButton.BackColor = AccentColor;
            openFolderButton.ForeColor = Color.White;
            openFolderButton.FlatAppearance.BorderColor = AccentColor;
            openFolderButton.FlatAppearance.MouseOverBackColor = AccentHoverColor;
        }

        StyleHeaderActions(colors);
        StyleQualitySelector();
    }

    private void StyleHeaderActions(ThemeColors colors)
    {
        var pressed = ControlPaint.Dark(colors.SecondaryButtonHover, 0.06F);

        foreach (var button in new[] { settingsButton })
        {
            button.BackColor = button.Enabled ? colors.SecondaryButton : colors.Disabled;
            button.ForeColor = button.Enabled ? colors.Text : colors.DisabledText;
            button.FlatAppearance.BorderColor = colors.Border;
            button.FlatAppearance.MouseOverBackColor = colors.SecondaryButtonHover;
            button.FlatAppearance.MouseDownBackColor = pressed;
        }
    }

    private void UpdateResultVisibility(UiState state)
    {
        emptyResultPanel.Visible = state is UiState.Initial or UiState.Preparing or UiState.Analyzing;
        successResultPanel.Visible = state == UiState.Success && showCompletionView;
        messagePanel.Visible = state is UiState.InvalidUrl
            or UiState.AnalysisError
            or UiState.PreparationFailed
            or UiState.Error
            || state == UiState.Success && !showCompletionView;
        songPanel.Visible = currentResult == ResultKind.Song
                            && !messagePanel.Visible
                            && !successResultPanel.Visible;
        playlistPanel.Visible = currentResult == ResultKind.Playlist
                                && !messagePanel.Visible
                                && !successResultPanel.Visible;

        if (successResultPanel.Visible)
        {
            successResultPanel.BringToFront();
        }
        else if (messagePanel.Visible)
        {
            messagePanel.BringToFront();
        }
        else if (songPanel.Visible)
        {
            songPanel.BringToFront();
        }
        else if (playlistPanel.Visible)
        {
            playlistPanel.BringToFront();
        }
        else
        {
            emptyResultPanel.BringToFront();
        }
    }

    private void UpdateProgressAndMessage(UiState state)
    {
        switch (state)
        {
            case UiState.Initial:
                ResetProgress(textService.Get("Waiting"));
                break;

            case UiState.Preparing:
                ResetProgress(textService.Get("Preparing"));
                break;

            case UiState.Analyzing:
                ResetProgress(textService.Get("Analyzing"));
                break;

            case UiState.InvalidUrl:
                ResetProgress(textService.Get("AnalysisFailed"));
                messageLabel.Text = GetDetailedMessage("AnalysisFailed");
                break;

            case UiState.AnalysisError:
                ResetProgress(textService.Get("AnalysisFailed"));
                break;

            case UiState.PreparationFailed:
                ResetProgress(textService.Get("PreparationFailed"));
                break;

            case UiState.SongReady:
            case UiState.PlaylistReady:
                ResetProgress(textService.Get("AnalysisComplete"));
                break;

            case UiState.Downloading:
                ResetProgress(currentResult == ResultKind.Playlist
                    ? textService.Get("DownloadingPlaylist", 1, currentItemCount)
                    : textService.Get("DownloadingSong"));
                break;

            case UiState.Validating:
                progressBar.Value = 100;
                progressPercentageLabel.Text = "100 %";
                statusLabel.Text = textService.Get("CheckingFiles");
                break;

            case UiState.Success:
                progressBar.Value = 100;
                progressPercentageLabel.Text = "100 %";
                statusLabel.Text = currentResult == ResultKind.Playlist
                    ? textService.Get("PlaylistAllSuccess", currentItemCount, currentItemCount)
                    : textService.Get("DownloadSuccess");
                break;

            case UiState.Error:
                ResetProgress(textService.Get("DownloadFailed"));
                messageLabel.Text = GetDetailedMessage("DownloadFailed");
                break;

            case UiState.Cancelled:
                ResetProgress(textService.Get("DownloadCancelled"));
                break;
        }
    }

    private string GetDetailedMessage(string key)
    {
        var hintKey = key switch
        {
            "AnalysisFailed" => "AnalysisHint",
            "ContentUnavailable" => "ContentUnavailableHint",
            "NetworkFailed" => "NetworkHint",
            "PreparationFailed" => "TryAgain",
            "DownloadFailed" => "TryNewDownload",
            _ => string.Empty
        };

        return string.IsNullOrEmpty(hintKey)
            ? textService.Get(key)
            : textService.Get(key) + Environment.NewLine + Environment.NewLine + textService.Get(hintKey);
    }

    private void ResetProgress(string status)
    {
        progressBar.Value = 0;
        progressPercentageLabel.Text = "0 %";
        statusLabel.Text = status;
    }

    private static string FormatDuration(double? durationSeconds, string missingValue)
    {
        if (durationSeconds is null || durationSeconds < 0)
        {
            return missingValue;
        }

        var duration = TimeSpan.FromSeconds(Math.Round(durationSeconds.Value));

        return duration.TotalHours >= 1
            ? $"{(int)duration.TotalHours:00}:{duration.Minutes:00}:{duration.Seconds:00}"
            : $"{duration.Minutes:00}:{duration.Seconds:00}";
    }

    private static HttpClient CreateThumbnailHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(12)
        };

        client.DefaultRequestHeaders.UserAgent.ParseAdd(AppMetadata.UserAgent);
        return client;
    }

    private void ResizePlaylistColumns()
    {
        if (playlistList.Columns.Count < 2 || playlistList.ClientSize.Width <= 0)
        {
            return;
        }

        const int durationWidth = 82;
        var availableWidth = playlistList.ClientSize.Width
                             - durationWidth
                             - SystemInformation.VerticalScrollBarWidth
                             - 6;

        playlistList.Columns[0].Width = Math.Max(180, availableWidth);
        playlistList.Columns[1].Width = durationWidth;
    }

    private enum UiState
    {
        Initial,
        Preparing,
        Analyzing,
        InvalidUrl,
        AnalysisError,
        PreparationFailed,
        SongReady,
        PlaylistReady,
        Downloading,
        Validating,
        Success,
        Error,
        Cancelled
    }

    private enum ResultKind
    {
        None,
        Song,
        Playlist
    }
}
