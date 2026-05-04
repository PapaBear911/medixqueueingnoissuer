using MultiDepartmentQueueing;

namespace HospitalQueueCaller;

internal sealed class CallerForm : Form
{
    private AppSettings settings = null!;
    private ThemePalette theme = null!;
    private IReadOnlyList<DepartmentAccount> users = [];
    private bool useLightMode;
    private Panel loginPanel = null!;
    private Panel workPanel = null!;
    private TextBox usernameBox = null!;
    private TextBox passwordBox = null!;
    private TextBox counterBox = null!;
    private NumericUpDown regularNextBox = null!;
    private NumericUpDown priorityNextBox = null!;
    private Label errorLabel = null!;
    private Label currentNumberLabel = null!;
    private Label currentMetaLabel = null!;
    private Label accountLabel = null!;
    private Label nextLabel = null!;
    private HiddenScrollFlowPanel departmentPreviewList = null!;
    private Button repeatButton = null!;
    private System.Windows.Forms.Timer repeatCooldownTimer = null!;
    private DateTime repeatAvailableAt = DateTime.MinValue;
    private DepartmentAccount? activeAccount;
    private CalledTicket? currentTicket;

    public CallerForm()
    {
        Text = "Hospital Queue Caller";
        MinimumSize = new Size(940, 650);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10F);
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        LoadSettings();
        BuildUi();
        ShowLogin();
    }

    private void LoadSettings()
    {
        settings = AppSettings.Load();
        users = settings.LoadUsers();
        useLightMode = string.Equals(settings.UI.Theme, "Light", StringComparison.OrdinalIgnoreCase);
        theme = settings.Palette;
        Icon = AppImages.LoadIconFromImage(settings.ResolvePath(settings.Logos.HospitalQueueCallerIconPath)) ?? Icon;
    }

    private void ApplyThemeChoice()
    {
        theme = useLightMode ? settings.UI.LightMode.ToPalette() : settings.UI.DarkMode.ToPalette();
    }

    private void BuildUi()
    {
        SuspendLayout();
        Controls.Clear();
        BackColor = theme.Background;
        loginPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(36), BackColor = theme.Background };
        workPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(28), BackColor = theme.Background };
        Controls.Add(workPanel);
        Controls.Add(loginPanel);
        BuildLoginPanel();
        BuildWorkPanel();
        ResumeLayout();
    }

    private void BuildLoginPanel()
    {
        var shell = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = theme.Background };
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));

        var intro = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(0, 24, 30, 0), RowCount = 5, BackColor = theme.Background };
        intro.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
        intro.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        intro.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
        intro.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
        intro.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        intro.Controls.Add(MakeLogoBox(settings.ResolvePath(settings.Logos.MainLogoPath)), 0, 0);
        intro.Controls.Add(MakeLabel("HOSPITAL COUNTER APP", 12, FontStyle.Bold, theme.Accent, theme.Background, DockStyle.Fill), 0, 1);
        intro.Controls.Add(MakeLabel("Call patients with automatic numbering", 27, FontStyle.Bold, theme.PrimaryText, theme.Background, DockStyle.Fill), 0, 2);
        intro.Controls.Add(MakeLabel("Staff call the next Regular or Priority number directly. Use Save Next to jump ahead when numbers need to be skipped.", 13, FontStyle.Regular, theme.SecondaryText, theme.Background, DockStyle.Fill), 0, 3);
        intro.Controls.Add(BuildDepartmentPreview(), 0, 4);

        var card = new RoundedPanel { Dock = DockStyle.Fill, Radius = 18, BackColor = theme.Card, Padding = new Padding(30) };
        var form = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 10, BackColor = theme.Card };
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        form.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        form.Controls.Add(MakeLabel("Department Login", 24, FontStyle.Bold, theme.CardText, theme.Card, DockStyle.Fill), 0, 0);
        form.Controls.Add(MakeLabel("Username", 10, FontStyle.Bold, theme.CardSecondaryText, theme.Card, DockStyle.Fill), 0, 1);
        usernameBox = MakeTextBox("", false);
        form.Controls.Add(usernameBox, 0, 2);
        form.Controls.Add(MakeLabel("Password", 10, FontStyle.Bold, theme.CardSecondaryText, theme.Card, DockStyle.Fill), 0, 3);
        passwordBox = MakeTextBox("", true);
        form.Controls.Add(passwordBox, 0, 4);
        form.Controls.Add(MakeLabel("Counter / Window Name", 10, FontStyle.Bold, theme.CardSecondaryText, theme.Card, DockStyle.Fill), 0, 5);
        counterBox = MakeTextBox("Window 1", false);
        form.Controls.Add(counterBox, 0, 6);

        var loginButton = new ModernButton("Sign In", theme.Accent) { Dock = DockStyle.Fill, Margin = new Padding(0, 12, 0, 8) };
        loginButton.Click += (_, _) => AttemptLogin();
        form.Controls.Add(loginButton, 0, 7);

        var themeButton = new ModernButton(useLightMode ? "Switch to Dark Mode" : "Switch to Light Mode", theme.Success)
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 8),
            Font = new Font("Segoe UI", 10F, FontStyle.Bold)
        };
        themeButton.Click += (_, _) => ToggleTheme();
        form.Controls.Add(themeButton, 0, 8);

        errorLabel = MakeLabel("", 10, FontStyle.Bold, theme.Danger, theme.Card, DockStyle.Top, 36);
        form.Controls.Add(errorLabel, 0, 9);
        card.Controls.Add(form);

        shell.Controls.Add(intro, 0, 0);
        shell.Controls.Add(card, 1, 0);
        loginPanel.Controls.Add(shell);
    }

    private Control BuildDepartmentPreview()
    {
        var card = new RoundedPanel { Dock = DockStyle.Fill, Radius = 14, BackColor = theme.Surface, Padding = new Padding(14), Margin = new Padding(0, 12, 0, 0) };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, BackColor = theme.Surface };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(MakeLabel("Available Departments", 12, FontStyle.Bold, theme.PrimaryText, theme.Surface, DockStyle.Fill), 0, 0);
        departmentPreviewList = new HiddenScrollFlowPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            BackColor = theme.Surface
        };
        foreach (var department in settings.Departments)
        {
            departmentPreviewList.Controls.Add(MakeDepartmentPill(department));
        }

        layout.Controls.Add(departmentPreviewList, 0, 1);
        card.Controls.Add(layout);
        return card;
    }

    private Control MakeDepartmentPill(DepartmentSettings department)
    {
        var label = MakeLabel($"{department.Code} - {department.Name}", 9, FontStyle.Bold, theme.CardText, theme.Card, DockStyle.Fill);
        label.TextAlign = ContentAlignment.MiddleCenter;
        var panel = new RoundedPanel
        {
            Width = 150,
            Height = 32,
            Radius = 10,
            BackColor = theme.Card,
            Margin = new Padding(0, 0, 8, 8),
            Padding = new Padding(8, 0, 8, 0)
        };
        panel.Controls.Add(label);
        return panel;
    }

    private void BuildWorkPanel()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, BackColor = theme.Background };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 56));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 44));
        workPanel.Controls.Add(root);

        var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, BackColor = theme.Background };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 19));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 19));
        header.Controls.Add(MakeLogoBox(settings.ResolvePath(settings.Logos.MainLogoPath)), 0, 0);
        accountLabel = MakeLabel("Not signed in", 19, FontStyle.Bold, theme.PrimaryText, theme.Background, DockStyle.Fill);
        var logout = new ModernButton("Logout", theme.CardSecondaryText) { Dock = DockStyle.Fill };
        logout.Click += (_, _) => ShowLogin();
        var close = new ModernButton("Close", theme.Danger) { Dock = DockStyle.Fill };
        close.Click += (_, _) => Close();
        header.Controls.Add(accountLabel, 1, 0);
        header.Controls.Add(logout, 2, 0);
        header.Controls.Add(close, 3, 0);
        root.Controls.Add(header, 0, 0);

        var nowCard = new RoundedPanel { Dock = DockStyle.Fill, Radius = 22, BackColor = theme.Card, Padding = new Padding(34), Margin = new Padding(0, 8, 0, 22) };
        currentNumberLabel = MakeLabel("--", 76, FontStyle.Bold, theme.Accent, theme.Card, DockStyle.Fill);
        currentNumberLabel.TextAlign = ContentAlignment.MiddleCenter;
        currentMetaLabel = MakeLabel("No current number", 20, FontStyle.Bold, theme.CardSecondaryText, theme.Card, DockStyle.Bottom, 58);
        currentMetaLabel.TextAlign = ContentAlignment.MiddleCenter;
        nowCard.Controls.Add(currentNumberLabel);
        nowCard.Controls.Add(currentMetaLabel);
        root.Controls.Add(nowCard, 0, 1);

        var controls = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3, BackColor = theme.Background };
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        controls.RowStyles.Add(new RowStyle(SizeType.Absolute, 86));
        controls.RowStyles.Add(new RowStyle(SizeType.Absolute, 86));
        controls.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var callRegular = new ModernButton("Call Regular Number", theme.Accent) { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 10, 14) };
        callRegular.Click += (_, _) => Call(TicketKind.Regular);
        var callPriority = new ModernButton("Call Priority Number", theme.Priority) { Dock = DockStyle.Fill, Margin = new Padding(10, 0, 0, 14) };
        callPriority.Click += (_, _) => Call(TicketKind.Priority);
        var skip = new ModernButton("Skip Current", theme.Danger) { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 10, 14) };
        skip.Click += (_, _) => Skip();
        repeatButton = new ModernButton("Repeat Call", theme.Success) { Dock = DockStyle.Fill, Margin = new Padding(10, 0, 0, 14) };
        repeatButton.Click += (_, _) => RepeatCall();

        var config = BuildNumberConfig();
        controls.Controls.Add(callRegular, 0, 0);
        controls.Controls.Add(callPriority, 1, 0);
        controls.Controls.Add(skip, 0, 1);
        controls.Controls.Add(repeatButton, 1, 1);
        controls.SetColumnSpan(config, 2);
        controls.Controls.Add(config, 0, 2);
        root.Controls.Add(controls, 0, 2);
    }

    private Control BuildNumberConfig()
    {
        var card = new RoundedPanel { Dock = DockStyle.Fill, Radius = 14, BackColor = theme.Card, Padding = new Padding(18) };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, BackColor = theme.Card };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16));
        nextLabel = MakeLabel("Next: Regular / Priority", 13, FontStyle.Bold, theme.CardText, theme.Card, DockStyle.Fill);
        regularNextBox = MakeNumberBox();
        priorityNextBox = MakeNumberBox();
        var save = new ModernButton("Save Next", theme.Success) { Dock = DockStyle.Fill, Margin = new Padding(8, 0, 0, 0) };
        save.Click += (_, _) => SaveNextNumbers();
        var themeButton = new ModernButton(useLightMode ? "Dark" : "Light", theme.Accent) { Dock = DockStyle.Fill, Margin = new Padding(8, 0, 0, 0) };
        themeButton.Click += (_, _) => ToggleTheme();
        layout.Controls.Add(nextLabel, 0, 0);
        layout.Controls.Add(regularNextBox, 1, 0);
        layout.Controls.Add(priorityNextBox, 2, 0);
        layout.Controls.Add(save, 3, 0);
        layout.Controls.Add(themeButton, 4, 0);
        card.Controls.Add(layout);
        return card;
    }

    private NumericUpDown MakeNumberBox()
    {
        return new NumericUpDown
        {
            Minimum = 1,
            Maximum = 9999,
            Value = 1,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 18F, FontStyle.Bold),
            Margin = new Padding(8, 6, 8, 6),
            BackColor = theme.Surface,
            ForeColor = theme.PrimaryText
        };
    }

    private void AttemptLogin()
    {
        users = settings.LoadUsers();
        var account = users.FirstOrDefault(a =>
            string.Equals(a.Username, usernameBox.Text.Trim(), StringComparison.OrdinalIgnoreCase) &&
            a.Password == passwordBox.Text);

        if (account is null)
        {
            errorLabel.Text = users.Count == 0
                ? "No users configured. Check config/users.txt."
                : "Invalid username or password.";
            return;
        }

        activeAccount = account;
        accountLabel.Text = $"{account.Name} - {counterBox.Text.Trim()}";
        currentTicket = HospitalQueueData.Load().Departments.FirstOrDefault(d => d.Code == account.Code)?.Current;
        ShowWork();
        RefreshCurrent();
    }

    private void Call(TicketKind kind)
    {
        if (activeAccount is null)
        {
            return;
        }

        currentTicket = HospitalQueueData.CallNext(activeAccount, kind, counterBox.Text);
        if (currentTicket is null)
        {
            currentNumberLabel.Text = "--";
            currentNumberLabel.ForeColor = theme.Danger;
            currentMetaLabel.Text = $"No waiting {kind} numbers for {activeAccount.Name}";
            return;
        }

        RefreshCurrent();
        StartRepeatCooldown();
    }

    private void Skip()
    {
        if (activeAccount is null)
        {
            return;
        }

        HospitalQueueData.SkipCurrent(activeAccount);
        currentTicket = null;
        RefreshCurrent();
    }

    private void RepeatCall()
    {
        if (activeAccount is null)
        {
            return;
        }

        currentTicket = HospitalQueueData.RepeatCurrent(activeAccount);
        if (currentTicket is null)
        {
            currentNumberLabel.Text = "--";
            currentNumberLabel.ForeColor = theme.Danger;
            currentMetaLabel.Text = "No current number to repeat";
            return;
        }

        currentNumberLabel.Text = currentTicket.DisplayNumber;
        currentNumberLabel.ForeColor = currentTicket.Kind == TicketKind.Priority ? theme.Priority : theme.Accent;
        currentMetaLabel.Text = $"Repeated {currentTicket.Kind} call at {currentTicket.CalledAt:hh:mm tt}";
        StartRepeatCooldown();
    }

    private void SaveNextNumbers()
    {
        if (activeAccount is null)
        {
            return;
        }

        HospitalQueueData.SetNextNumbers(activeAccount, (int)regularNextBox.Value, (int)priorityNextBox.Value);
        RefreshCurrent();
    }

    private void RefreshCurrent()
    {
        if (activeAccount is null)
        {
            return;
        }

        var department = HospitalQueueData.Load().Departments.First(d => d.Code == activeAccount.Code);
        currentTicket = department.Current?.Status == "Called" ? department.Current : null;
        regularNextBox.Value = Math.Clamp(department.NextRegular, 1, 9999);
        priorityNextBox.Value = Math.Clamp(department.NextPriority, 1, 9999);
        nextLabel.Text = "Next: Regular / Priority";

        if (currentTicket is null)
        {
            currentNumberLabel.Text = "--";
            currentNumberLabel.ForeColor = theme.Accent;
            currentMetaLabel.Text = "No current number";
            return;
        }

        currentNumberLabel.Text = currentTicket.DisplayNumber;
        currentNumberLabel.ForeColor = currentTicket.Kind == TicketKind.Priority ? theme.Priority : theme.Accent;
        currentMetaLabel.Text = $"{currentTicket.Kind} lane - called at {currentTicket.CalledAt:hh:mm tt}";
    }

    private void StartRepeatCooldown()
    {
        repeatAvailableAt = DateTime.Now.AddSeconds(5);
        repeatCooldownTimer?.Stop();
        repeatCooldownTimer?.Dispose();
        repeatCooldownTimer = new System.Windows.Forms.Timer { Interval = 250 };
        repeatCooldownTimer.Tick += (_, _) => UpdateRepeatCooldown();
        repeatCooldownTimer.Start();
        UpdateRepeatCooldown();
    }

    private void UpdateRepeatCooldown()
    {
        if (repeatButton is null)
        {
            return;
        }

        var remaining = repeatAvailableAt - DateTime.Now;
        if (remaining <= TimeSpan.Zero)
        {
            repeatCooldownTimer?.Stop();
            repeatButton.Enabled = true;
            repeatButton.Text = "Repeat Call";
            return;
        }

        repeatButton.Enabled = false;
        repeatButton.Text = $"Repeat Call ({Math.Ceiling(remaining.TotalSeconds):0}s)";
    }

    private void ShowLogin()
    {
        activeAccount = null;
        loginPanel.Visible = true;
        workPanel.Visible = false;
    }

    private void ShowWork()
    {
        errorLabel.Text = "";
        loginPanel.Visible = false;
        workPanel.Visible = true;
    }

    private TextBox MakeTextBox(string text, bool password)
    {
        var box = new TextBox
        {
            Text = text,
            UseSystemPasswordChar = password,
            Font = new Font("Segoe UI", 15F),
            BorderStyle = BorderStyle.FixedSingle,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 8),
            BackColor = theme.Surface,
            ForeColor = theme.PrimaryText
        };
        box.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                AttemptLogin();
                e.SuppressKeyPress = true;
            }
        };
        return box;
    }

    private PictureBox MakeLogoBox(string path)
    {
        return new PictureBox
        {
            Image = AppImages.LoadImage(path),
            SizeMode = PictureBoxSizeMode.Zoom,
            Dock = DockStyle.Fill,
            BackColor = theme.Background,
            Margin = new Padding(0, 4, 14, 4)
        };
    }

    private Label MakeLabel(string text, float size, FontStyle style, Color color, Color backColor, DockStyle dock, int height = 0)
    {
        return new Label
        {
            Text = text,
            Font = new Font("Segoe UI", size, style),
            ForeColor = color,
            BackColor = backColor,
            Dock = dock,
            Height = height,
            AutoSize = false,
            AutoEllipsis = true,
            TextAlign = ContentAlignment.MiddleLeft
        };
    }

    private void ToggleTheme()
    {
        useLightMode = !useLightMode;
        ApplyThemeChoice();
        var wasWorking = workPanel.Visible;
        var user = activeAccount;
        BuildUi();
        activeAccount = user;
        if (wasWorking)
        {
            ShowWork();
            if (activeAccount is not null)
            {
                accountLabel.Text = $"{activeAccount.Name} - {counterBox.Text.Trim()}";
                RefreshCurrent();
            }
        }
        else
        {
            ShowLogin();
        }
    }
}
