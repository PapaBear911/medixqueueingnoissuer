using System.ComponentModel;
using System.Text.Json;
using System.Text.RegularExpressions;
using MultiDepartmentQueueing;

namespace HospitalQueueAdminSetup;

internal sealed class AdminSetupForm : Form
{
    private const string SamePcStatePath = "%LOCALAPPDATA%/HospitalQueueingSystem/hospital_queue_state.json";

    private readonly TextBox displayFolderBox = new();
    private readonly TextBox callerFolderBox = new();
    private readonly RadioButton samePcMode = new();
    private readonly RadioButton networkMode = new();
    private readonly TextBox statePathBox = new();
    private readonly DataGridView departmentsGrid = new();
    private readonly DataGridView usersGrid = new();
    private readonly Label statusLabel = new();
    private readonly BindingList<DepartmentRow> departments = [];
    private readonly BindingList<UserRow> users = [];
    private readonly JsonSerializerOptions jsonOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    public AdminSetupForm()
    {
        Text = "Hospital Queue Admin Setup";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1180, 760);
        Size = new Size(1240, 820);
        Font = new Font("Segoe UI", 10F);
        BackColor = Color.FromArgb(241, 245, 249);

        BuildUi();
        ApplyDefaults();
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(18),
            BackColor = BackColor
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 110));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 54));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 46));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));

        root.Controls.Add(BuildFolderPanel(), 0, 0);
        root.Controls.Add(BuildStatePathPanel(), 0, 1);
        root.Controls.Add(BuildDepartmentsPanel(), 0, 2);
        root.Controls.Add(BuildUsersPanel(), 0, 3);
        root.Controls.Add(BuildActionPanel(), 0, 4);
        Controls.Add(root);
    }

    private Control BuildFolderPanel()
    {
        var panel = MakePanel(2);
        panel.Controls.Add(MakeLabel("Display App Folder"), 0, 0);
        panel.Controls.Add(BuildPathPicker(displayFolderBox, BrowseDisplayFolder), 1, 0);
        panel.Controls.Add(MakeLabel("Caller App Folder"), 0, 1);
        panel.Controls.Add(BuildPathPicker(callerFolderBox, BrowseCallerFolder), 1, 1);
        return panel;
    }

    private Control BuildStatePathPanel()
    {
        var panel = MakePanel(2);
        var modePanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, BackColor = Color.White };
        samePcMode.Text = "Display and Caller on same PC";
        samePcMode.Checked = true;
        samePcMode.AutoSize = true;
        samePcMode.CheckedChanged += (_, _) =>
        {
            if (samePcMode.Checked)
            {
                statePathBox.Text = SamePcStatePath;
            }
        };
        networkMode.Text = "Display on another PC / shared network path";
        networkMode.AutoSize = true;
        networkMode.CheckedChanged += (_, _) =>
        {
            if (networkMode.Checked && string.Equals(statePathBox.Text, SamePcStatePath, StringComparison.OrdinalIgnoreCase))
            {
                statePathBox.Text = "//SERVER/HospitalQueueData/hospital_queue_state.json";
            }
        };
        modePanel.Controls.Add(samePcMode);
        modePanel.Controls.Add(networkMode);

        panel.Controls.Add(MakeLabel("Deployment Mode"), 0, 0);
        panel.Controls.Add(modePanel, 1, 0);
        panel.Controls.Add(MakeLabel("Queue State Path"), 0, 1);
        panel.Controls.Add(statePathBox, 1, 1);
        statePathBox.Dock = DockStyle.Fill;
        statePathBox.Font = new Font("Consolas", 10F);
        return panel;
    }

    private Control BuildDepartmentsPanel()
    {
        var group = MakeGroup("Departments");
        var layout = MakeGridWithActionsLayout();
        ConfigureGrid(departmentsGrid, departments);
        departmentsGrid.Columns.Add(MakeTextColumn(nameof(DepartmentRow.Name), "Name", 280));
        departmentsGrid.Columns.Add(MakeTextColumn(nameof(DepartmentRow.Code), "Code", 90));
        departmentsGrid.Columns.Add(MakeTextColumn(nameof(DepartmentRow.AccentColor), "Accent Color", 130));
        var remove = MakeButton("Remove Selected Department", Color.FromArgb(185, 28, 28));
        remove.Click += (_, _) => RemoveSelectedDepartment();
        layout.Controls.Add(departmentsGrid, 0, 0);
        layout.Controls.Add(remove, 0, 1);
        group.Controls.Add(layout);
        return group;
    }

    private Control BuildUsersPanel()
    {
        var group = MakeGroup("Caller Users");
        var layout = MakeGridWithActionsLayout();
        ConfigureGrid(usersGrid, users);
        usersGrid.Columns.Add(MakeTextColumn(nameof(UserRow.Username), "Username", 180));
        usersGrid.Columns.Add(MakeTextColumn(nameof(UserRow.Password), "Password", 160));
        usersGrid.Columns.Add(MakeTextColumn(nameof(UserRow.DepartmentCode), "Department Code", 130));
        usersGrid.Columns.Add(MakeTextColumn(nameof(UserRow.DisplayName), "Display Name", 260));
        var remove = MakeButton("Remove Selected User", Color.FromArgb(185, 28, 28));
        remove.Click += (_, _) => RemoveSelectedUser();
        layout.Controls.Add(usersGrid, 0, 0);
        layout.Controls.Add(remove, 0, 1);
        group.Controls.Add(layout);
        return group;
    }

    private Control BuildActionPanel()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, BackColor = BackColor };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));

        statusLabel.Dock = DockStyle.Fill;
        statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        statusLabel.ForeColor = Color.FromArgb(71, 85, 105);
        statusLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);

        var reload = MakeButton("Reload", Color.FromArgb(71, 85, 105));
        reload.Click += (_, _) => LoadConfiguration();
        var validate = MakeButton("Validate", Color.FromArgb(37, 99, 235));
        validate.Click += (_, _) => ValidateConfiguration(showSuccess: true);
        var save = MakeButton("Save and Sync", Color.FromArgb(5, 150, 105));
        save.Click += (_, _) => SaveConfiguration();

        panel.Controls.Add(statusLabel, 0, 0);
        panel.Controls.Add(reload, 1, 0);
        panel.Controls.Add(validate, 2, 0);
        panel.Controls.Add(save, 3, 0);
        return panel;
    }

    private static TableLayoutPanel MakePanel(int rows)
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = rows,
            BackColor = Color.White,
            Padding = new Padding(12),
            Margin = new Padding(0, 0, 0, 10)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < rows; i++)
        {
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / rows));
        }

        return panel;
    }

    private static GroupBox MakeGroup(string title)
    {
        return new GroupBox
        {
            Text = title,
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(12),
            Margin = new Padding(0, 0, 0, 10),
            Font = new Font("Segoe UI", 10F, FontStyle.Bold)
        };
    }

    private static TableLayoutPanel MakeGridWithActionsLayout()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            BackColor = Color.White
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        return layout;
    }

    private static Label MakeLabel(string text)
    {
        return new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(15, 23, 42),
            Font = new Font("Segoe UI", 10F, FontStyle.Bold)
        };
    }

    private static Control BuildPathPicker(TextBox textBox, EventHandler browseHandler)
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = Color.White };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
        textBox.Dock = DockStyle.Fill;
        textBox.Font = new Font("Consolas", 10F);
        var browse = MakeButton("Browse", Color.FromArgb(37, 99, 235));
        browse.Click += browseHandler;
        panel.Controls.Add(textBox, 0, 0);
        panel.Controls.Add(browse, 1, 0);
        return panel;
    }

    private static Button MakeButton(string text, Color color)
    {
        return new Button
        {
            Text = text,
            Dock = DockStyle.Fill,
            Margin = new Padding(6),
            BackColor = color,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            UseVisualStyleBackColor = false
        };
    }

    private static DataGridViewTextBoxColumn MakeTextColumn(string propertyName, string headerText, int width)
    {
        return new DataGridViewTextBoxColumn
        {
            DataPropertyName = propertyName,
            HeaderText = headerText,
            Width = width,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        };
    }

    private static void ConfigureGrid<T>(DataGridView grid, BindingList<T> source)
    {
        grid.Dock = DockStyle.Fill;
        grid.AutoGenerateColumns = false;
        grid.AllowUserToAddRows = true;
        grid.AllowUserToDeleteRows = true;
        grid.MultiSelect = false;
        grid.BackgroundColor = Color.White;
        grid.BorderStyle = BorderStyle.None;
        grid.RowHeadersWidth = 34;
        grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grid.ColumnHeadersHeight = 34;
        grid.DataSource = source;
    }

    private void ApplyDefaults()
    {
        var repoRoot = FindRepositoryRoot();
        if (!string.IsNullOrWhiteSpace(repoRoot))
        {
            displayFolderBox.Text = Path.Combine(repoRoot, "MultiDepartmentQueueing");
            callerFolderBox.Text = Path.Combine(repoRoot, "HospitalQueueCaller");
        }

        statePathBox.Text = SamePcStatePath;
        LoadConfiguration();
    }

    private string FindRepositoryRoot()
    {
        foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            var current = new DirectoryInfo(start);
            for (var i = 0; current is not null && i < 8; i++, current = current.Parent)
            {
                if (File.Exists(Path.Combine(current.FullName, "QueueuingNoIssuer.sln")) ||
                    Directory.Exists(Path.Combine(current.FullName, "MultiDepartmentQueueing")) &&
                    Directory.Exists(Path.Combine(current.FullName, "HospitalQueueCaller")))
                {
                    return current.FullName;
                }
            }
        }

        return "";
    }

    private void BrowseDisplayFolder(object? sender, EventArgs e) => BrowseFolder(displayFolderBox);

    private void BrowseCallerFolder(object? sender, EventArgs e) => BrowseFolder(callerFolderBox);

    private void BrowseFolder(TextBox target)
    {
        using var dialog = new FolderBrowserDialog { SelectedPath = Directory.Exists(target.Text) ? target.Text : Directory.GetCurrentDirectory() };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            target.Text = dialog.SelectedPath;
        }
    }

    private void LoadConfiguration()
    {
        try
        {
            var display = LoadSettings(displayFolderBox.Text);
            var caller = LoadSettings(callerFolderBox.Text);
            var sourceDepartments = display.Departments.Count > 0 ? display.Departments : caller.Departments;
            departments.Clear();
            foreach (var department in sourceDepartments.OrderBy(d => d.Name))
            {
                departments.Add(new DepartmentRow
                {
                    Name = department.Name,
                    Code = department.Code,
                    AccentColor = department.AccentColor
                });
            }

            users.Clear();
            foreach (var user in LoadUsers(callerFolderBox.Text, caller.Authentication.UsersFilePath))
            {
                users.Add(user);
            }

            var queuePath = !string.IsNullOrWhiteSpace(display.QueueStorage.StateFilePath)
                ? display.QueueStorage.StateFilePath
                : caller.QueueStorage.StateFilePath;
            statePathBox.Text = string.IsNullOrWhiteSpace(queuePath) ? SamePcStatePath : queuePath;
            samePcMode.Checked = string.Equals(statePathBox.Text, SamePcStatePath, StringComparison.OrdinalIgnoreCase);
            networkMode.Checked = !samePcMode.Checked;
            SetStatus("Configuration loaded.", isError: false);
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message, isError: true);
        }
    }

    private void SaveConfiguration()
    {
        try
        {
            var validation = ValidateConfiguration(showSuccess: false);
            if (!validation.IsValid)
            {
                SetStatus(validation.Message, isError: true);
                return;
            }

            var display = LoadSettings(displayFolderBox.Text);
            var caller = LoadSettings(callerFolderBox.Text);
            var syncedDepartments = BuildDepartments();
            var queuePath = samePcMode.Checked ? SamePcStatePath : statePathBox.Text.Trim();

            display.Departments = syncedDepartments.Select(CloneDepartment).ToList();
            caller.Departments = syncedDepartments.Select(CloneDepartment).ToList();
            display.QueueStorage.StateFilePath = queuePath;
            caller.QueueStorage.StateFilePath = queuePath;
            caller.Authentication.UsersFilePath = string.IsNullOrWhiteSpace(caller.Authentication.UsersFilePath)
                ? "config/users.txt"
                : caller.Authentication.UsersFilePath;

            SaveSettings(displayFolderBox.Text, display);
            SaveSettings(callerFolderBox.Text, caller);
            SaveUsers(callerFolderBox.Text, caller.Authentication.UsersFilePath, BuildUsers());
            SetStatus("Saved and synchronized display app, caller app, and caller users.", isError: false);
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message, isError: true);
        }
    }

    private void RemoveSelectedDepartment()
    {
        var index = SelectedBoundRowIndex(departmentsGrid);
        if (index < 0 || index >= departments.Count)
        {
            SetStatus("Select a department row to remove.", isError: true);
            return;
        }

        var department = departments[index];
        var code = department.Code.Trim();
        var matchingUsers = users.Where(u => string.Equals(u.DepartmentCode.Trim(), code, StringComparison.OrdinalIgnoreCase)).ToList();
        var message = matchingUsers.Count == 0
            ? $"Remove department '{department.Name}'?"
            : $"Remove department '{department.Name}' and {matchingUsers.Count} matching user(s)?";
        if (MessageBox.Show(this, message, "Remove Department", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
        {
            return;
        }

        departments.RemoveAt(index);
        foreach (var user in matchingUsers)
        {
            users.Remove(user);
        }

        SetStatus($"Removed department '{department.Name}'. Save and Sync to apply.", isError: false);
    }

    private void RemoveSelectedUser()
    {
        var index = SelectedBoundRowIndex(usersGrid);
        if (index < 0 || index >= users.Count)
        {
            SetStatus("Select a user row to remove.", isError: true);
            return;
        }

        var user = users[index];
        if (MessageBox.Show(this, $"Remove user '{user.Username}'?", "Remove User", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
        {
            return;
        }

        users.RemoveAt(index);
        SetStatus($"Removed user '{user.Username}'. Save and Sync to apply.", isError: false);
    }

    private static int SelectedBoundRowIndex(DataGridView grid)
    {
        if (grid.CurrentRow is null || grid.CurrentRow.IsNewRow)
        {
            return -1;
        }

        return grid.CurrentRow.DataBoundItem is null ? -1 : grid.CurrentRow.Index;
    }

    private ValidationResult ValidateConfiguration(bool showSuccess)
    {
        try
        {
            _ = LoadSettings(displayFolderBox.Text);
            var caller = LoadSettings(callerFolderBox.Text);
            var syncedDepartments = BuildDepartments();
            var departmentCodes = syncedDepartments.Select(d => d.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var userRows = BuildUsers();
            foreach (var user in userRows)
            {
                if (!departmentCodes.Contains(user.DepartmentCode))
                {
                    return new ValidationResult(false, $"User '{user.Username}' uses missing department code '{user.DepartmentCode}'.");
                }
            }

            var queuePath = samePcMode.Checked ? SamePcStatePath : statePathBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(queuePath))
            {
                return new ValidationResult(false, "Queue state path is required.");
            }

            if (string.IsNullOrWhiteSpace(caller.Authentication.UsersFilePath))
            {
                caller.Authentication.UsersFilePath = "config/users.txt";
            }

            var message = $"Valid: {syncedDepartments.Count} departments, {userRows.Count} users, queue path '{queuePath}'.";
            if (showSuccess)
            {
                SetStatus(message, isError: false);
            }

            return new ValidationResult(true, message);
        }
        catch (Exception ex)
        {
            if (showSuccess)
            {
                SetStatus(ex.Message, isError: true);
            }

            return new ValidationResult(false, ex.Message);
        }
    }

    private AppSettings LoadSettings(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            throw new InvalidOperationException("Select valid Display and Caller app folders.");
        }

        var path = Path.Combine(folder, "appsettings.json");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Missing appsettings.json in {folder}");
        }

        return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path), jsonOptions)
            ?? throw new InvalidOperationException($"Could not read {path}");
    }

    private void SaveSettings(string folder, AppSettings settings)
    {
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "appsettings.json"), JsonSerializer.Serialize(settings, jsonOptions));
    }

    private List<UserRow> LoadUsers(string callerFolder, string usersPathSetting)
    {
        var usersPath = ResolvePath(callerFolder, string.IsNullOrWhiteSpace(usersPathSetting) ? "config/users.txt" : usersPathSetting);
        if (!File.Exists(usersPath))
        {
            return [];
        }

        var rows = new List<UserRow>();
        foreach (var rawLine in File.ReadLines(usersPath))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var parts = line.Split('|', StringSplitOptions.TrimEntries);
            if (parts.Length < 3)
            {
                continue;
            }

            rows.Add(new UserRow
            {
                Username = parts[0],
                Password = parts.Length > 1 ? parts[1] : "",
                DepartmentCode = parts[2].ToUpperInvariant(),
                DisplayName = parts.Length > 3 ? parts[3] : ""
            });
        }

        return rows.OrderBy(u => u.DepartmentCode).ThenBy(u => u.Username).ToList();
    }

    private void SaveUsers(string callerFolder, string usersPathSetting, IReadOnlyList<UserRow> rows)
    {
        var usersPath = ResolvePath(callerFolder, string.IsNullOrWhiteSpace(usersPathSetting) ? "config/users.txt" : usersPathSetting);
        Directory.CreateDirectory(Path.GetDirectoryName(usersPath) ?? callerFolder);
        var lines = new List<string> { "# username|password|departmentCode|displayName" };
        lines.AddRange(rows.Select(user => $"{user.Username}|{user.Password}|{user.DepartmentCode}|{user.DisplayName}"));
        File.WriteAllLines(usersPath, lines);
    }

    private static string ResolvePath(string baseFolder, string path)
    {
        var expanded = Environment.ExpandEnvironmentVariables(path).Replace('/', Path.DirectorySeparatorChar);
        return Path.IsPathRooted(expanded) ? expanded : Path.Combine(baseFolder, expanded);
    }

    private List<DepartmentSettings> BuildDepartments()
    {
        var rows = departments
            .Where(d => !string.IsNullOrWhiteSpace(d.Name) || !string.IsNullOrWhiteSpace(d.Code))
            .Select(d => new DepartmentSettings
            {
                Name = d.Name.Trim(),
                Code = d.Code.Trim().ToUpperInvariant(),
                AccentColor = string.IsNullOrWhiteSpace(d.AccentColor) ? "#1D4ED8" : d.AccentColor.Trim()
            })
            .ToList();

        if (rows.Count == 0)
        {
            throw new InvalidOperationException("At least one department is required.");
        }

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.Name) || string.IsNullOrWhiteSpace(row.Code))
            {
                throw new InvalidOperationException("Every department needs a name and code.");
            }

            if (!Regex.IsMatch(row.Code, "^[A-Z0-9]{2,8}$"))
            {
                throw new InvalidOperationException($"Department code '{row.Code}' must use 2-8 letters or numbers.");
            }

            if (!Regex.IsMatch(row.AccentColor, "^#[0-9A-Fa-f]{6}$"))
            {
                throw new InvalidOperationException($"Department '{row.Name}' has invalid accent color '{row.AccentColor}'.");
            }
        }

        var duplicate = rows.GroupBy(d => d.Code, StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidOperationException($"Duplicate department code '{duplicate.Key}'.");
        }

        return rows.OrderBy(d => d.Name).ToList();
    }

    private List<UserRow> BuildUsers()
    {
        var departmentByCode = BuildDepartments().ToDictionary(d => d.Code, StringComparer.OrdinalIgnoreCase);
        var rows = users
            .Where(u => !string.IsNullOrWhiteSpace(u.Username) || !string.IsNullOrWhiteSpace(u.DepartmentCode))
            .Select(u =>
            {
                var code = u.DepartmentCode.Trim().ToUpperInvariant();
                var displayName = u.DisplayName.Trim();
                if (displayName.Length == 0 && departmentByCode.TryGetValue(code, out var department))
                {
                    displayName = department.Name;
                }

                return new UserRow
                {
                    Username = u.Username.Trim(),
                    Password = u.Password,
                    DepartmentCode = code,
                    DisplayName = displayName
                };
            })
            .ToList();

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.Username) ||
                string.IsNullOrWhiteSpace(row.Password) ||
                string.IsNullOrWhiteSpace(row.DepartmentCode))
            {
                throw new InvalidOperationException("Every user needs a username, password, and department code.");
            }
        }

        var duplicate = rows.GroupBy(u => u.Username, StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidOperationException($"Duplicate username '{duplicate.Key}'.");
        }

        return rows.OrderBy(u => u.DepartmentCode).ThenBy(u => u.Username).ToList();
    }

    private static DepartmentSettings CloneDepartment(DepartmentSettings department)
    {
        return new DepartmentSettings
        {
            Name = department.Name,
            Code = department.Code,
            AccentColor = department.AccentColor
        };
    }

    private void SetStatus(string message, bool isError)
    {
        statusLabel.Text = message;
        statusLabel.ForeColor = isError ? Color.FromArgb(185, 28, 28) : Color.FromArgb(22, 101, 52);
    }

    private sealed record ValidationResult(bool IsValid, string Message);

    private sealed class DepartmentRow
    {
        public string Name { get; set; } = "";
        public string Code { get; set; } = "";
        public string AccentColor { get; set; } = "#1D4ED8";
    }

    private sealed class UserRow
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string DepartmentCode { get; set; } = "";
        public string DisplayName { get; set; } = "";
    }
}
