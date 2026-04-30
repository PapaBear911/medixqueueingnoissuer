namespace MultiDepartmentQueueing;

public partial class Form1 : Form
{
    private AppSettings settings = null!;
    private ThemePalette theme = null!;
    private HiddenScrollFlowPanel departmentList = null!;
    private HiddenScrollFlowPanel recentList = null!;
    private Label headerTitleLabel = null!;
    private Label heroTimeLabel = null!;
    private Label headlineNumber = null!;
    private Label headlineMeta = null!;
    private PictureBox heroLeftLogo = null!;
    private PictureBox heroRightLogo = null!;
    private ComboBox voiceCombo = null!;
    private ToggleSwitch modeToggle = null!;
    private QueueAnnouncer announcer = null!;
    private string lastAnnouncedCallKey = "";
    private System.Windows.Forms.Timer refreshTimer = null!;
    private bool borderlessFullScreen = true;
    private bool useLightMode;
    private string lastClockText = "";
    private string lastQueueSignature = "";

    public Form1()
    {
        InitializeComponent();
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        LoadSettings();
        BuildUi();
        RefreshBoard(force: true);
    }

    private void LoadSettings()
    {
        settings = AppSettings.Load();
        useLightMode = string.Equals(settings.UI.Theme, "Light", StringComparison.OrdinalIgnoreCase);
        theme = settings.Palette;
        announcer = new QueueAnnouncer(settings.Speech, settings.Departments);
    }

    private void ApplyThemeChoice()
    {
        theme = useLightMode ? settings.UI.LightMode.ToPalette() : settings.UI.DarkMode.ToPalette();
    }

    private void BuildUi()
    {
        SuspendLayout();
        Controls.Clear();
        Text = TextOrDefault(settings.DisplayText.WindowTitle, "Hospital Queueing Display");
        BackColor = theme.Background;
        Font = new Font("Segoe UI", 10F);
        FormBorderStyle = FormBorderStyle.None;
        WindowState = FormWindowState.Maximized;
        KeyPreview = true;
        KeyDown -= HandleHotKeys;
        KeyDown += HandleHotKeys;
        Icon = AppImages.LoadIconFromImage(settings.ResolvePath(settings.Logos.MultiDepartmentQueueingIconPath)) ?? Icon;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = theme.Background,
            Padding = new Padding(30),
            ColumnCount = 1,
            RowCount = 3
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 130));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 48));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 52));
        Controls.Add(root);

        root.Controls.Add(BuildHeader(), 0, 0);
        root.Controls.Add(BuildHero(), 0, 1);
        root.Controls.Add(BuildLowerBoard(), 0, 2);

        refreshTimer?.Stop();
        refreshTimer?.Dispose();
        refreshTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        refreshTimer.Tick += (_, _) => RefreshBoard(force: false);
        refreshTimer.Start();

        ResumeLayout();
        RequestHeaderTitleRefit();
    }

    private Control BuildHeader()
    {
        var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 1, BackColor = theme.Background };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        headerTitleLabel = new Label
        {
            Text = TextOrDefault(settings.DisplayText.HeaderTitle, "HOSPITAL QUEUEING SYSTEM").ToUpperInvariant(),
            ForeColor = theme.PrimaryText,
            BackColor = theme.Background,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            AutoSize = false,
            AutoEllipsis = false,
            UseCompatibleTextRendering = true,
            Padding = new Padding(16, 4, 16, 4),
            Font = new Font("Segoe UI", 40F, FontStyle.Bold)
        };
        headerTitleLabel.SizeChanged += (_, _) => RequestHeaderTitleRefit();
        headerTitleLabel.TextChanged += (_, _) => RequestHeaderTitleRefit();
        header.Controls.Add(headerTitleLabel, 0, 0);
        return header;
    }

    private Control BuildHero()
    {
        var hero = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            Radius = 24,
            BackColor = theme.Surface,
            Padding = new Padding(28),
            Margin = new Padding(0, 4, 0, 22)
        };

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, BackColor = theme.Surface, Margin = new Padding(0), Padding = new Padding(0) };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 16));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 84));
        heroTimeLabel = MakeLabel("", 30, FontStyle.Bold, theme.SecondaryText, theme.Surface);
        heroTimeLabel.TextAlign = ContentAlignment.MiddleCenter;
        layout.Controls.Add(heroTimeLabel, 0, 0);

        var main = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, BackColor = theme.Surface, Margin = new Padding(0), Padding = new Padding(0) };
        main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
        main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
        main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));

        heroLeftLogo = new PictureBox
        {
            Image = AppImages.LoadImage(settings.ResolvePath(settings.Logos.MainLogoPath)),
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = theme.Surface,
            Margin = new Padding(0, 0, 10, 0)
        };
        heroRightLogo = new PictureBox
        {
            Image = AppImages.LoadImage(settings.ResolvePath(settings.Logos.SecondaryLogoPath)),
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = theme.Surface,
            Margin = new Padding(10, 0, 0, 0)
        };

        var center = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, BackColor = theme.Surface, Margin = new Padding(0), Padding = new Padding(0) };
        center.RowStyles.Add(new RowStyle(SizeType.Percent, 64));
        center.RowStyles.Add(new RowStyle(SizeType.Percent, 36));
        headlineNumber = MakeLabel("--", 116, FontStyle.Bold, theme.PrimaryText, theme.Surface);
        headlineNumber.TextAlign = ContentAlignment.MiddleCenter;
        headlineMeta = MakeLabel("Please wait for your number to be called", 43, FontStyle.Bold, theme.CardText, theme.Surface);
        headlineMeta.TextAlign = ContentAlignment.MiddleCenter;
        center.Controls.Add(headlineNumber, 0, 0);
        center.Controls.Add(headlineMeta, 0, 1);

        main.Controls.Add(heroLeftLogo, 0, 0);
        main.Controls.Add(center, 1, 0);
        main.Controls.Add(heroRightLogo, 2, 0);
        layout.Controls.Add(main, 0, 1);
        hero.Controls.Add(layout);
        return hero;
    }

    private Control BuildLowerBoard()
    {
        var lower = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = theme.Background };
        lower.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
        lower.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));

        departmentList = new HiddenScrollFlowPanel
        {
            Dock = DockStyle.Fill,
            BackColor = theme.Background,
            Padding = new Padding(0, 0, 18, 0),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true
        };
        departmentList.Resize += (_, _) => ResizeDepartmentCards();

        lower.Controls.Add(departmentList, 0, 0);
        lower.Controls.Add(BuildRightRail(), 1, 0);
        return lower;
    }

    private Control BuildRightRail()
    {
        var rail = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, BackColor = theme.Background };
        rail.RowStyles.Add(new RowStyle(SizeType.Percent, 80));
        rail.RowStyles.Add(new RowStyle(SizeType.Percent, 20));
        rail.Controls.Add(BuildRecentPanel(), 0, 0);
        rail.Controls.Add(BuildDisplayControlsPanel(), 0, 1);
        return rail;
    }

    private Control BuildDisplayControlsPanel()
    {
        var card = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            Radius = 12,
            BackColor = theme.Card,
            Padding = new Padding(12),
            Margin = new Padding(0, 10, 0, 0)
        };

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, BackColor = theme.Card };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 72));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var voiceLabel = MakeLabel("Voice", 10, FontStyle.Bold, theme.CardSecondaryText, theme.Card);
        var modeLabel = MakeLabel("Dark/Light", 10, FontStyle.Bold, theme.CardSecondaryText, theme.Card);
        voiceCombo = MakeVoiceCombo();
        voiceCombo.Margin = new Padding(0, 2, 8, 0);
        modeToggle = new ToggleSwitch
        {
            Dock = DockStyle.Left,
            Width = 58,
            Height = 28,
            Margin = new Padding(0, 2, 0, 0),
            Checked = !useLightMode,
            ParentBackColor = theme.Card,
            TrackOnColor = useLightMode ? Color.FromArgb(37, 99, 235) : Color.FromArgb(34, 197, 94),
            TrackOffColor = useLightMode ? Color.FromArgb(148, 163, 184) : Color.FromArgb(100, 116, 139),
            ThumbColor = Color.White
        };
        modeToggle.CheckedChanged += (_, _) =>
        {
            var shouldBeLight = !modeToggle.Checked;
            if (shouldBeLight != useLightMode)
            {
                ToggleTheme();
            }
        };

        layout.Controls.Add(voiceLabel, 0, 0);
        layout.Controls.Add(modeLabel, 1, 0);
        layout.Controls.Add(voiceCombo, 0, 1);
        layout.Controls.Add(modeToggle, 1, 1);
        card.Controls.Add(layout);
        return card;
    }

    private Control BuildRecentPanel()
    {
        var card = new RoundedPanel { Dock = DockStyle.Fill, Radius = 18, BackColor = theme.Card, Padding = new Padding(18) };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, BackColor = theme.Card };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(MakeLabel("Recent Calls", 17, FontStyle.Bold, theme.CardText, theme.Card), 0, 0);

        recentList = new HiddenScrollFlowPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = theme.Card,
            Padding = new Padding(0)
        };
        recentList.Resize += (_, _) => ResizeRecentRows();

        layout.Controls.Add(recentList, 0, 1);
        card.Controls.Add(layout);
        return card;
    }

    private void RefreshBoard(bool force)
    {
        var now = DateTime.Now;
        var clockText = now.ToString("dddd, MMM dd yyyy  hh:mm:ss tt");
        if (clockText != lastClockText)
        {
            heroTimeLabel.Text = clockText;
            lastClockText = clockText;
        }

        var state = HospitalQueueData.Load();
        var signature = BuildSignature(state);
        if (!force && signature == lastQueueSignature)
        {
            return;
        }

        lastQueueSignature = signature;
        var latest = state.RecentCalls.FirstOrDefault(c => c.Status == "Called");
        headlineNumber.Text = latest?.DisplayNumber ?? "--";
        headlineMeta.Text = latest is null
            ? "Please wait for your number to be called"
            : $"{latest.DepartmentName}  |  {latest.CounterName}  |  {(latest.Kind == TicketKind.Priority ? "Priority Lane" : "Regular Lane")}";

        if (latest is not null)
        {
            var callKey = $"{latest.DisplayNumber}:{latest.CounterName}:{latest.CalledAt:O}";
            if (callKey != lastAnnouncedCallKey)
            {
                lastAnnouncedCallKey = callKey;
                announcer.Announce(latest);
            }
        }

        RebuildDepartmentGrid(state.Departments);
        RebuildRecentGrid(state.RecentCalls);
    }

    private static string BuildSignature(QueueState state)
    {
        var departmentPart = string.Join(";", state.Departments.Select(d =>
            $"{d.Code}:{d.Current?.DisplayNumber}:{d.Current?.CounterName}:{d.Current?.Status}:{d.NextRegular}:{d.NextPriority}"));
        var recentPart = string.Join(";", state.RecentCalls.Take(8).Select(c =>
            $"{c.DisplayNumber}:{c.CounterName}:{c.Status}:{c.CalledAt:O}"));
        return $"{departmentPart}|{recentPart}";
    }

    private void RebuildDepartmentGrid(IReadOnlyList<DepartmentQueue> departments)
    {
        departmentList.SuspendLayout();
        departmentList.Controls.Clear();

        foreach (var department in SortDepartments(departments))
        {
            departmentList.Controls.Add(MakeDepartmentCard(department));
        }

        ResizeDepartmentCards();
        departmentList.ResumeLayout();
    }

    private static IEnumerable<DepartmentQueue> SortDepartments(IEnumerable<DepartmentQueue> departments)
    {
        return departments
            .OrderByDescending(d => d.Current?.Status == "Called")
            .ThenByDescending(d => d.Current?.CalledAt ?? DateTime.MinValue)
            .ThenBy(d => d.Name);
    }

    private void ResizeDepartmentCards()
    {
        var availableWidth = Math.Max(320, departmentList.ClientSize.Width - 28);
        var availableHeight = Math.Max(300, departmentList.ClientSize.Height - 18);
        var columns = availableWidth >= 980 ? 3 : availableWidth >= 640 ? 2 : 1;
        var cardWidth = Math.Max(300, (availableWidth / columns) - 18);
        var cardHeight = Math.Max(136, (availableHeight / 2) - 18);
        foreach (Control control in departmentList.Controls)
        {
            control.Width = cardWidth;
            control.Height = cardHeight;
        }
    }

    private void RebuildRecentGrid(IReadOnlyList<CalledTicket> recentCalls)
    {
        recentList.SuspendLayout();
        recentList.Controls.Clear();

        foreach (var call in recentCalls.Take(6))
        {
            recentList.Controls.Add(MakeRecentRow(call));
        }

        ResizeRecentRows();
        recentList.ResumeLayout();
    }

    private void ResizeRecentRows()
    {
        var width = Math.Max(260, recentList.ClientSize.Width - 24);
        foreach (Control control in recentList.Controls)
        {
            control.Width = width;
        }
    }

    private Control MakeDepartmentCard(DepartmentQueue department)
    {
        var configuredDepartment = HospitalQueueData.DepartmentAccounts.FirstOrDefault(d => d.Code == department.Code);
        var accent = department.Current?.Kind == TicketKind.Priority ? theme.Priority : configuredDepartment?.Accent ?? theme.Accent;
        var card = new RoundedPanel
        {
            Width = 340,
            Height = 150,
            Radius = 18,
            BackColor = theme.Card,
            Margin = new Padding(0, 0, 18, 18),
            Padding = new Padding(18)
        };

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, BackColor = theme.Card };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 26));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 26));

        var departmentLabel = MakeLabel(department.Name, 16, FontStyle.Bold, theme.CardText, theme.Card);
        var numberLabel = MakeLabel(department.Current?.DisplayNumber ?? "--", 30, FontStyle.Bold, accent, theme.Card);
        var counterLabel = MakeLabel(department.Current is null ? "Awaiting next call" : department.Current.CounterName, 13, FontStyle.Bold, theme.CardSecondaryText, theme.Card);
        numberLabel.TextAlign = ContentAlignment.MiddleCenter;
        counterLabel.TextAlign = ContentAlignment.MiddleCenter;

        layout.Controls.Add(departmentLabel, 0, 0);
        layout.Controls.Add(numberLabel, 0, 1);
        layout.Controls.Add(counterLabel, 0, 2);
        card.Controls.Add(layout);
        return card;
    }

    private Control MakeRecentRow(CalledTicket call)
    {
        var backColor = call.Kind == TicketKind.Priority
            ? Blend(theme.Priority, theme.Card, 0.12F)
            : Blend(theme.Accent, theme.Card, 0.12F);
        var row = new RoundedPanel
        {
            Width = Math.Max(260, recentList.ClientSize.Width - 24),
            Height = 76,
            Radius = 12,
            BackColor = backColor,
            Margin = new Padding(0, 0, 0, 12),
            Padding = new Padding(12, 7, 12, 7)
        };

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, BackColor = backColor };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var number = MakeLabel(call.DisplayNumber, 16, FontStyle.Bold, call.Kind == TicketKind.Priority ? theme.Priority : theme.Accent, backColor);
        var time = MakeLabel(call.CalledAt.ToString("hh:mm tt"), 9, FontStyle.Bold, theme.CardSecondaryText, backColor);
        var meta = MakeLabel($"{call.DepartmentName} - {call.CounterName}", 9, FontStyle.Regular, theme.CardSecondaryText, backColor);
        time.TextAlign = ContentAlignment.MiddleRight;
        layout.Controls.Add(number, 0, 0);
        layout.Controls.Add(time, 1, 0);
        layout.Controls.Add(meta, 0, 1);
        layout.SetColumnSpan(meta, 2);
        row.Controls.Add(layout);
        return row;
    }

    private PictureBox MakeLogoBox(string path, PictureBoxSizeMode sizeMode, ContentAlignment alignment)
    {
        return new PictureBox
        {
            Image = AppImages.LoadImage(path),
            SizeMode = sizeMode,
            Dock = DockStyle.Fill,
            BackColor = theme.Background,
            Margin = alignment == ContentAlignment.MiddleRight ? new Padding(8, 6, 0, 6) : new Padding(0, 6, 8, 6)
        };
    }

    private ComboBox MakeVoiceCombo()
    {
        var combo = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = theme.Surface,
            ForeColor = theme.PrimaryText,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Margin = new Padding(0, 2, 0, 0)
        };

        combo.Items.Add("Default Windows Voice");
        foreach (var voice in QueueAnnouncer.GetInstalledVoices())
        {
            combo.Items.Add(voice);
        }

        var selectedIndex = 0;
        if (!string.IsNullOrWhiteSpace(settings.Speech.VoiceName))
        {
            for (var i = 1; i < combo.Items.Count; i++)
            {
                if (combo.Items[i]?.ToString()?.Contains(settings.Speech.VoiceName, StringComparison.OrdinalIgnoreCase) == true)
                {
                    selectedIndex = i;
                    break;
                }
            }
        }

        combo.SelectedIndex = selectedIndex;
        combo.SelectedIndexChanged += (_, _) =>
        {
            if (combo.SelectedIndex <= 0)
            {
                announcer.SetVoice("");
                return;
            }

            var selected = combo.SelectedItem as SpeechVoiceInfo;
            announcer.SetVoice(selected?.Name ?? combo.SelectedItem?.ToString() ?? "");
        };
        return combo;
    }

    private static Label MakeLabel(string text, float size, FontStyle style, Color color, Color backColor)
    {
        return new Label
        {
            Text = text,
            Font = new Font("Segoe UI", size, style),
            ForeColor = color,
            BackColor = backColor,
            Dock = DockStyle.Fill,
            AutoSize = false,
            AutoEllipsis = true,
            TextAlign = ContentAlignment.MiddleLeft,
            UseCompatibleTextRendering = false
        };
    }

    private static string TextOrDefault(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private void RefitHeaderTitle()
    {
        if (headerTitleLabel is null || headerTitleLabel.Width <= 0 || string.IsNullOrWhiteSpace(headerTitleLabel.Text))
        {
            return;
        }

        using var graphics = headerTitleLabel.CreateGraphics();
        var availableWidth = Math.Max(1, headerTitleLabel.ClientSize.Width - headerTitleLabel.Padding.Horizontal - 6);
        var availableHeight = Math.Max(1, headerTitleLabel.ClientSize.Height - headerTitleLabel.Padding.Vertical - 2);
        for (var size = 40F; size >= 14F; size -= 0.5F)
        {
            using var font = new Font("Segoe UI", size, FontStyle.Bold);
            var measured = graphics.MeasureString(headerTitleLabel.Text, font, new SizeF(availableWidth, availableHeight));
            if (measured.Width <= availableWidth && measured.Height <= availableHeight)
            {
                if (Math.Abs(headerTitleLabel.Font.Size - size) > 0.1F)
                {
                    headerTitleLabel.Font = new Font("Segoe UI", size, FontStyle.Bold);
                }

                return;
            }
        }

        headerTitleLabel.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
    }

    private void RequestHeaderTitleRefit()
    {
        if (IsDisposed)
        {
            return;
        }

        if (!IsHandleCreated)
        {
            HandleCreated -= RefitHeaderTitleAfterHandleCreated;
            HandleCreated += RefitHeaderTitleAfterHandleCreated;
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(new Action(RefitHeaderTitle));
            return;
        }

        RefitHeaderTitle();
    }

    private void RefitHeaderTitleAfterHandleCreated(object? sender, EventArgs e)
    {
        HandleCreated -= RefitHeaderTitleAfterHandleCreated;
        RequestHeaderTitleRefit();
    }

    private static Color Blend(Color foreground, Color background, float amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        return Color.FromArgb(
            (int)(background.R + (foreground.R - background.R) * amount),
            (int)(background.G + (foreground.G - background.G) * amount),
            (int)(background.B + (foreground.B - background.B) * amount));
    }

    private void ToggleTheme()
    {
        useLightMode = !useLightMode;
        ApplyThemeChoice();
        lastQueueSignature = "";
        BuildUi();
        RefreshBoard(force: true);
    }

    private void HandleHotKeys(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F11)
        {
            ToggleFullscreen();
        }
        else if (e.KeyCode == Keys.Escape && borderlessFullScreen)
        {
            ToggleFullscreen();
        }
    }

    private void ToggleFullscreen()
    {
        borderlessFullScreen = !borderlessFullScreen;
        FormBorderStyle = borderlessFullScreen ? FormBorderStyle.None : FormBorderStyle.Sizable;
        WindowState = FormWindowState.Maximized;
    }
}
