using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MultiDepartmentQueueing;

internal enum TicketKind
{
    Regular,
    Priority
}

internal sealed record DepartmentAccount(string Name, string Code, string Username, string Password, Color Accent);

internal sealed class AppSettings
{
    public LogoSettings Logos { get; set; } = new();
    public DisplayTextSettings DisplayText { get; set; } = new();
    public UiSettings UI { get; set; } = new();
    public AuthenticationSettings Authentication { get; set; } = new();
    public QueueStorageSettings QueueStorage { get; set; } = new();
    public PrintSettings Print { get; set; } = new();
    public SpeechSettings Speech { get; set; } = new();
    public LicenseSettings License { get; set; } = new();
    public List<DepartmentSettings> Departments { get; set; } = [];

    public static AppSettings Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(path))
        {
            path = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
        }

        if (!File.Exists(path))
        {
            return CreateFallback();
        }

        try
        {
            var rawJson = File.ReadAllText(path);
            var settings = JsonSerializer.Deserialize<AppSettings>(rawJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? CreateFallback();

            settings.Normalize();
            return settings;
        }
        catch
        {
            try
            {
                var repairedJson = RepairCommonWindowsPathJson(File.ReadAllText(path));
                var settings = JsonSerializer.Deserialize<AppSettings>(repairedJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? CreateFallback();

                settings.Normalize();
                return settings;
            }
            catch
            {
                return CreateFallback();
            }
        }
    }

    public string ResolvePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "";
        }

        var expanded = Environment.ExpandEnvironmentVariables(path).Replace('/', Path.DirectorySeparatorChar);
        return Path.IsPathRooted(expanded)
            ? expanded
            : Path.Combine(AppContext.BaseDirectory, expanded);
    }

    public ThemePalette Palette => string.Equals(UI.Theme, "Light", StringComparison.OrdinalIgnoreCase)
        ? UI.LightMode.ToPalette()
        : UI.DarkMode.ToPalette();

    public IReadOnlyList<DepartmentAccount> LoadUsers()
    {
        var usersPath = ResolvePath(Authentication.UsersFilePath);
        if (!File.Exists(usersPath))
        {
            return [];
        }

        var departments = Departments.ToDictionary(d => d.Code, StringComparer.OrdinalIgnoreCase);
        var users = new List<DepartmentAccount>();
        foreach (var rawLine in File.ReadLines(usersPath))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var parts = line.Split('|', StringSplitOptions.TrimEntries);
            if (parts.Length < 3 || !departments.TryGetValue(parts[2], out var department))
            {
                continue;
            }

            var displayName = parts.Length >= 4 && parts[3].Length > 0 ? parts[3] : department.Name;
            users.Add(new DepartmentAccount(displayName, department.Code, parts[0], parts[1], AppColors.FromHex(department.AccentColor, Color.FromArgb(29, 78, 216))));
        }

        return users;
    }

    public IReadOnlyList<DepartmentAccount> DepartmentAccounts()
    {
        return Departments
            .Select(department => new DepartmentAccount(
                department.Name,
                department.Code,
                department.Code.ToLowerInvariant(),
                "",
                AppColors.FromHex(department.AccentColor, Color.FromArgb(29, 78, 216))))
            .ToList();
    }

    public void Save()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    public void AddOrUpdateDepartment(DepartmentSettings department)
    {
        department.Code = department.Code.Trim().ToUpperInvariant();
        department.Name = department.Name.Trim();
        var existing = Departments.FirstOrDefault(d => string.Equals(d.Code, department.Code, StringComparison.OrdinalIgnoreCase));
        if (existing is null)
        {
            Departments.Add(department);
            return;
        }

        existing.Name = department.Name;
        existing.AccentColor = department.AccentColor;
    }

    public void AddOrUpdateUser(string username, string password, string departmentCode, string displayName)
    {
        var usersPath = ResolvePath(Authentication.UsersFilePath);
        Directory.CreateDirectory(Path.GetDirectoryName(usersPath) ?? AppContext.BaseDirectory);
        var lines = File.Exists(usersPath)
            ? File.ReadAllLines(usersPath).ToList()
            : ["# username|password|departmentCode|displayName"];

        var replacement = $"{username.Trim()}|{password}|{departmentCode.Trim().ToUpperInvariant()}|{displayName.Trim()}";
        var replaced = false;
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i].Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var parts = line.Split('|', StringSplitOptions.TrimEntries);
            if (parts.Length > 0 && string.Equals(parts[0], username.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                lines[i] = replacement;
                replaced = true;
                break;
            }
        }

        if (!replaced)
        {
            lines.Add(replacement);
        }

        File.WriteAllLines(usersPath, lines);
    }

    private void Normalize()
    {
        if (Departments.Count == 0)
        {
            Departments = CreateFallback().Departments;
        }

        foreach (var department in Departments)
        {
            if (string.IsNullOrWhiteSpace(department.Code))
            {
                department.Code = new string(department.Name.Where(char.IsLetterOrDigit).Take(3).ToArray()).ToUpperInvariant();
            }
        }
    }

    private static AppSettings CreateFallback()
    {
        return new AppSettings
        {
            Departments =
            [
                new() { Name = "Pharmacy", Code = "PHA", AccentColor = "#DB2777" },
                new() { Name = "Laboratory", Code = "LAB", AccentColor = "#CA8A04" },
                new() { Name = "Radiology", Code = "RAD", AccentColor = "#7C3AED" },
                new() { Name = "Yakap", Code = "YKP", AccentColor = "#0891B2" },
                new() { Name = "Cashier", Code = "PAY", AccentColor = "#059669" },
                new() { Name = "Out Patient Department", Code = "OPD", AccentColor = "#1D4ED8" }
            ]
        };
    }

    private static string RepairCommonWindowsPathJson(string json)
    {
        return Regex.Replace(json, @"(?<!\\)\\(?![""\\/bfnrtu])", @"\\");
    }
}

internal sealed class LogoSettings
{
    public string MainLogoPath { get; set; } = "";
    public string SecondaryLogoPath { get; set; } = "";
    public string MultiDepartmentQueueingIconPath { get; set; } = "";
    public string HospitalQueueCallerIconPath { get; set; } = "";
}

internal sealed class DisplayTextSettings
{
    public string WindowTitle { get; set; } = "Hospital Queueing Display";
    public string HeaderTitle { get; set; } = "HOSPITAL QUEUEING SYSTEM";
    public string NowServingTitle { get; set; } = "Now Serving";
}

internal sealed class UiSettings
{
    public string Theme { get; set; } = "Dark";
    public ThemeSettings DarkMode { get; set; } = ThemeSettings.Dark();
    public ThemeSettings LightMode { get; set; } = ThemeSettings.Light();
    public AccessibilitySettings Accessibility { get; set; } = new();
}

internal sealed class ThemeSettings
{
    public string BackgroundColor { get; set; } = "#111827";
    public string SurfaceColor { get; set; } = "#1F2937";
    public string CardColor { get; set; } = "#0F172A";
    public string PrimaryTextColor { get; set; } = "#F9FAFB";
    public string SecondaryTextColor { get; set; } = "#D1D5DB";
    public string CardTextColor { get; set; } = "#F8FAFC";
    public string CardSecondaryTextColor { get; set; } = "#CBD5E1";
    public string AccentColor { get; set; } = "#60A5FA";
    public string PriorityColor { get; set; } = "#F59E0B";
    public string DangerColor { get; set; } = "#DC2626";
    public string SuccessColor { get; set; } = "#059669";

    public ThemePalette ToPalette()
    {
        return new ThemePalette(
            AppColors.FromHex(BackgroundColor, Color.FromArgb(17, 24, 39)),
            AppColors.FromHex(SurfaceColor, Color.FromArgb(31, 41, 55)),
            AppColors.FromHex(CardColor, Color.FromArgb(15, 23, 42)),
            AppColors.FromHex(PrimaryTextColor, Color.FromArgb(249, 250, 251)),
            AppColors.FromHex(SecondaryTextColor, Color.FromArgb(209, 213, 219)),
            AppColors.FromHex(CardTextColor, Color.FromArgb(248, 250, 252)),
            AppColors.FromHex(CardSecondaryTextColor, Color.FromArgb(203, 213, 225)),
            AppColors.FromHex(AccentColor, Color.FromArgb(96, 165, 250)),
            AppColors.FromHex(PriorityColor, Color.FromArgb(245, 158, 11)),
            AppColors.FromHex(DangerColor, Color.FromArgb(220, 38, 38)),
            AppColors.FromHex(SuccessColor, Color.FromArgb(5, 150, 105)));
    }

    public static ThemeSettings Dark() => new();

    public static ThemeSettings Light() => new()
    {
        BackgroundColor = "#F1F5F9",
        SurfaceColor = "#FFFFFF",
        CardColor = "#FFFFFF",
        PrimaryTextColor = "#111827",
        SecondaryTextColor = "#475569",
        CardTextColor = "#111827",
        CardSecondaryTextColor = "#475569",
        AccentColor = "#1D4ED8",
        PriorityColor = "#B45309",
        DangerColor = "#B91C1C",
        SuccessColor = "#047857"
    };
}

internal sealed class AccessibilitySettings
{
    public string ContrastRatio { get; set; } = "WCAG-AA";
    public string FontWeight { get; set; } = "Medium";
    public bool ResponsiveText { get; set; } = true;
}

internal sealed class AuthenticationSettings
{
    public string UsersFilePath { get; set; } = "config/users.txt";
}

internal sealed class QueueStorageSettings
{
    public string StateFilePath { get; set; } = "data/hospital_queue_state.json";
}

internal sealed class PrintSettings
{
    public string PrinterMode { get; set; } = "Default";
    public string PrinterName { get; set; } = "";
    public string TicketTempFolder { get; set; } = "%TEMP%/HospitalQueueTickets";
    public string PaperName { get; set; } = "Queue Ticket 58x90mm";
    public int PaperWidthHundredthsInch { get; set; } = 228;
    public int PaperHeightHundredthsInch { get; set; } = 354;
}

internal sealed class SpeechSettings
{
    public bool Enabled { get; set; } = true;
    public string VoiceName { get; set; } = "";
    public int Rate { get; set; }
    public int Volume { get; set; } = 100;
    public int RepeatCount { get; set; } = 1;
    public int RepeatDelayMilliseconds { get; set; } = 1200;
    public string AnnouncementFormat { get; set; } = "Now serving number {Number}. Please proceed to {Counter}, {Department}.";
}

internal sealed class LicenseSettings
{
    public string Token { get; set; } = "";
}

internal sealed record LicenseStatus(bool IsRegistered, bool IsExpired, DateTimeOffset? ExpiresAt, string Message);

internal static class AppLicense
{
    public static LicenseStatus CheckDisplayLicense(LicenseSettings license)
    {
        if (string.IsNullOrWhiteSpace(license.Token))
        {
            return new LicenseStatus(false, true, null, "No display license is registered.");
        }

        var expiresAt = TryReadExpiration(license.Token);
        if (expiresAt is null)
        {
            return new LicenseStatus(false, true, null, "The display license could not be read.");
        }

        if (DateTimeOffset.UtcNow >= expiresAt.Value)
        {
            return new LicenseStatus(true, true, expiresAt, "The display license has expired.");
        }

        return new LicenseStatus(true, false, expiresAt, "The display license is active.");
    }

    private static DateTimeOffset? TryReadExpiration(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length < 2)
            {
                return null;
            }

            var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
            using var payload = JsonDocument.Parse(payloadJson);
            if (!payload.RootElement.TryGetProperty("exp", out var expProperty) ||
                !expProperty.TryGetInt64(out var exp))
            {
                return null;
            }

            return DateTimeOffset.FromUnixTimeSeconds(exp);
        }
        catch
        {
            return null;
        }
    }

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + ((4 - padded.Length % 4) % 4), '=');
        return Convert.FromBase64String(padded);
    }
}

internal sealed class DepartmentSettings
{
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public string AccentColor { get; set; } = "#1D4ED8";
}

internal sealed record ThemePalette(
    Color Background,
    Color Surface,
    Color Card,
    Color PrimaryText,
    Color SecondaryText,
    Color CardText,
    Color CardSecondaryText,
    Color Accent,
    Color Priority,
    Color Danger,
    Color Success);

internal static class AppColors
{
    public static Color FromHex(string? hex, Color fallback)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            return fallback;
        }

        try
        {
            return ColorTranslator.FromHtml(hex);
        }
        catch
        {
            return fallback;
        }
    }
}

internal static class AppImages
{
    public static Image? LoadImage(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return null;
        }

        using var source = Image.FromFile(path);
        return new Bitmap(source);
    }

    public static Icon? LoadIconFromImage(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return null;
        }

        using var bitmap = new Bitmap(path);
        var handle = bitmap.GetHicon();
        try
        {
            using var icon = Icon.FromHandle(handle);
            return (Icon)icon.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);
}

internal sealed class SpeechVoiceInfo
{
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public override string ToString() => string.IsNullOrWhiteSpace(Description) ? Name : Description;
}

internal sealed class QueueAnnouncer
{
    private readonly SpeechSettings settings;
    private readonly string[] departmentCodes;
    private readonly object speechQueueLock = new();
    private readonly Queue<SpeechRequest> speechQueue = new();
    private bool isProcessingSpeechQueue;
    private dynamic? voice;

    public QueueAnnouncer(SpeechSettings settings, IEnumerable<DepartmentSettings>? departments = null)
    {
        this.settings = settings;
        departmentCodes = (departments ?? [])
            .Select(d => d.Code?.Trim().ToUpperInvariant())
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(code => code!.Length)
            .Cast<string>()
            .ToArray();
        voice = CreateVoice();
        ConfigureVoice(settings.VoiceName);
    }

    public static IReadOnlyList<SpeechVoiceInfo> GetInstalledVoices()
    {
        var voices = new List<SpeechVoiceInfo>();
        try
        {
            var speaker = CreateVoice();
            if (speaker is null)
            {
                return voices;
            }

            dynamic tokens = speaker.GetVoices();
            for (var i = 0; i < tokens.Count; i++)
            {
                dynamic token = tokens.Item(i);
                var description = Convert.ToString(token.GetDescription()) ?? "";
                var name = Convert.ToString(token.GetAttribute("Name")) ?? description;
                voices.Add(new SpeechVoiceInfo { Name = name, Description = description });
            }
        }
        catch
        {
            return voices;
        }

        return voices;
    }

    public void SetVoice(string voiceName)
    {
        settings.VoiceName = voiceName;
        ConfigureVoice(voiceName);
    }

    public void Announce(CalledTicket call)
    {
        if (!settings.Enabled || voice is null)
        {
            return;
        }

        var numberForSpeech = SpellDepartmentCodes(call.DisplayNumber);
        var departmentForSpeech = SpellDepartmentCodes(call.DepartmentName);
        var counterForSpeech = SpellDepartmentCodes(call.CounterName);
        var laneForSpeech = SpellDepartmentCodes(call.Kind == TicketKind.Priority ? "Priority Lane" : "Regular Lane");

        var text = settings.AnnouncementFormat
            .Replace("{Number}", numberForSpeech, StringComparison.OrdinalIgnoreCase)
            .Replace("{Counter}", counterForSpeech, StringComparison.OrdinalIgnoreCase)
            .Replace("{Department}", departmentForSpeech, StringComparison.OrdinalIgnoreCase)
            .Replace("{Lane}", laneForSpeech, StringComparison.OrdinalIgnoreCase);

        var repeatCount = Math.Clamp(settings.RepeatCount, 1, 3);
        EnqueueSpeech(text, repeatCount);
    }

    private void EnqueueSpeech(string text, int repeatCount)
    {
        lock (speechQueueLock)
        {
            speechQueue.Enqueue(new SpeechRequest(text, repeatCount));
            if (isProcessingSpeechQueue)
            {
                return;
            }

            isProcessingSpeechQueue = true;
        }

        _ = Task.Run(ProcessSpeechQueue);
    }

    private async Task ProcessSpeechQueue()
    {
        while (true)
        {
            SpeechRequest request;
            lock (speechQueueLock)
            {
                if (speechQueue.Count == 0)
                {
                    isProcessingSpeechQueue = false;
                    return;
                }

                request = speechQueue.Dequeue();
            }

            for (var i = 0; i < request.RepeatCount; i++)
            {
                Speak(request.Text);
                if (i < request.RepeatCount - 1)
                {
                    await Task.Delay(Math.Clamp(settings.RepeatDelayMilliseconds, 500, 4000));
                }
            }
        }
    }

    private string SpellDepartmentCodes(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || departmentCodes.Length == 0)
        {
            return text;
        }

        var spoken = text;
        foreach (var code in departmentCodes)
        {
            var spelled = string.Join(" ", code.ToCharArray());
            spoken = Regex.Replace(
                spoken,
                $@"\b{Regex.Escape(code)}\b",
                spelled,
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        return spoken;
    }

    private void ConfigureVoice(string voiceName)
    {
        if (voice is null)
        {
            return;
        }

        try
        {
            voice.Rate = Math.Clamp(settings.Rate, -5, 3);
            voice.Volume = Math.Clamp(settings.Volume, 0, 100);
            if (!string.IsNullOrWhiteSpace(voiceName))
            {
                dynamic tokens = voice.GetVoices();
                for (var i = 0; i < tokens.Count; i++)
                {
                    dynamic token = tokens.Item(i);
                    var description = Convert.ToString(token.GetDescription()) ?? "";
                    var name = Convert.ToString(token.GetAttribute("Name")) ?? "";
                    if (description.Contains(voiceName, StringComparison.OrdinalIgnoreCase) ||
                        name.Contains(voiceName, StringComparison.OrdinalIgnoreCase))
                    {
                        voice.Voice = token;
                        break;
                    }
                }
            }
        }
        catch
        {
            voice = null;
        }
    }

    private void Speak(string text)
    {
        try
        {
            const int speakSynchronously = 0;
            voice?.Speak(text, speakSynchronously);
        }
        catch
        {
            voice = null;
        }
    }

    private sealed record SpeechRequest(string Text, int RepeatCount);

    private static dynamic? CreateVoice()
    {
        try
        {
            var type = Type.GetTypeFromProgID("SAPI.SpVoice");
            return type is null ? null : Activator.CreateInstance(type);
        }
        catch
        {
            return null;
        }
    }
}

internal sealed class DepartmentQueue
{
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public string AccentColor { get; set; } = "#1D4ED8";
    public int NextRegular { get; set; } = 1;
    public int NextPriority { get; set; } = 1;
    public CalledTicket? Current { get; set; }
    public int ServedToday { get; set; }
    public int SkippedToday { get; set; }
}

internal sealed class IssuedTicket
{
    public string DepartmentCode { get; set; } = "";
    public string DepartmentName { get; set; } = "";
    public TicketKind Kind { get; set; }
    public int Number { get; set; }
    public DateTime IssuedAt { get; set; }
    public string Status { get; set; } = "Waiting";
    public string DisplayNumber => $"{DepartmentCode}-{(Kind == TicketKind.Priority ? "P" : "R")}{Number:000}";
}

internal sealed class CalledTicket
{
    public string DepartmentCode { get; set; } = "";
    public string DepartmentName { get; set; } = "";
    public TicketKind Kind { get; set; }
    public int Number { get; set; }
    public string CounterName { get; set; } = "";
    public DateTime CalledAt { get; set; }
    public string Status { get; set; } = "Called";
    public string DisplayNumber => $"{DepartmentCode}-{(Kind == TicketKind.Priority ? "P" : "R")}{Number:000}";
}

internal sealed class QueueState
{
    public string BusinessDate { get; set; } = DateTime.Today.ToString("yyyy-MM-dd");
    public List<DepartmentQueue> Departments { get; set; } = [];
    public List<IssuedTicket> IssuedTickets { get; set; } = [];
    public List<CalledTicket> RecentCalls { get; set; } = [];
}

internal static class HospitalQueueData
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static IReadOnlyList<DepartmentAccount> DepartmentAccounts => AppSettings.Load().DepartmentAccounts();

    public static QueueState Load()
    {
        EnsureStore();

        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                using var stream = new FileStream(GetStatePath(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var state = JsonSerializer.Deserialize<QueueState>(stream) ?? CreateDefaultState();
                EnsureConfiguredDepartments(state);
                EnsureDailyReset(state);
                return state;
            }
            catch (IOException)
            {
                Thread.Sleep(50);
            }
        }

        var fallback = CreateDefaultState();
        EnsureConfiguredDepartments(fallback);
        EnsureDailyReset(fallback);
        return fallback;
    }

    public static CalledTicket? CallNext(DepartmentAccount account, TicketKind kind, string counterName)
    {
        return Update(state =>
        {
            var department = FindDepartment(state, account);
            if (department.Current is { Status: "Called" })
            {
                department.ServedToday++;
            }

            var number = kind == TicketKind.Priority ? department.NextPriority++ : department.NextRegular++;
            var ticket = new CalledTicket
            {
                DepartmentCode = account.Code,
                DepartmentName = account.Name,
                Kind = kind,
                Number = number,
                CounterName = string.IsNullOrWhiteSpace(counterName) ? account.Name : counterName.Trim(),
                CalledAt = DateTime.Now,
                Status = "Called"
            };

            department.Current = ticket;
            state.RecentCalls.Insert(0, ticket);
            state.RecentCalls = state.RecentCalls.Take(16).ToList();
            return ticket;
        });
    }

    public static CalledTicket? RepeatCurrent(DepartmentAccount account)
    {
        return Update(state =>
        {
            var department = FindDepartment(state, account);
            if (department.Current is null)
            {
                return null;
            }

            department.Current.CalledAt = DateTime.Now;
            department.Current.Status = "Called";
            state.RecentCalls.Insert(0, department.Current);
            state.RecentCalls = state.RecentCalls.Take(16).ToList();
            return department.Current;
        });
    }

    public static void SkipCurrent(DepartmentAccount account)
    {
        Update(state =>
        {
            var department = FindDepartment(state, account);
            if (department.Current is not null)
            {
                var skipped = department.Current;
                department.Current.Status = "Skipped";
                var issued = state.IssuedTickets.FirstOrDefault(t =>
                    t.DepartmentCode == department.Current.DepartmentCode &&
                    t.Kind == department.Current.Kind &&
                    t.Number == department.Current.Number);
                if (issued is not null)
                {
                    issued.Status = "Skipped";
                }

                var recent = state.RecentCalls.FirstOrDefault(t =>
                    t.DepartmentCode == skipped.DepartmentCode &&
                    t.Kind == skipped.Kind &&
                    t.Number == skipped.Number &&
                    t.CalledAt == skipped.CalledAt);
                if (recent is not null)
                {
                    recent.Status = "Skipped";
                }

                department.SkippedToday++;
                department.Current = null;
            }

            return true;
        });
    }

    public static void SetNextNumbers(DepartmentAccount account, int regular, int priority)
    {
        Update(state =>
        {
            var department = FindDepartment(state, account);
            department.NextRegular = Math.Max(1, regular);
            department.NextPriority = Math.Max(1, priority);
            return true;
        });
    }

    public static IssuedTicket IssueNext(DepartmentAccount account, TicketKind kind)
    {
        return Update(state =>
        {
            var department = FindDepartment(state, account);
            var number = kind == TicketKind.Priority ? department.NextPriority++ : department.NextRegular++;
            var ticket = new IssuedTicket
            {
                DepartmentCode = department.Code,
                DepartmentName = department.Name,
                Kind = kind,
                Number = number,
                IssuedAt = DateTime.Now,
                Status = "Waiting"
            };
            state.IssuedTickets.Add(ticket);
            return ticket;
        });
    }

    public static IReadOnlyList<IssuedTicket> WaitingTickets(string departmentCode)
    {
        return Load().IssuedTickets
            .Where(t => t.DepartmentCode == departmentCode && t.Status == "Waiting")
            .OrderByDescending(t => t.Kind == TicketKind.Priority)
            .ThenBy(t => t.IssuedAt)
            .ToList();
    }

    private static T Update<T>(Func<QueueState, T> action)
    {
        EnsureStore();

        for (var attempt = 0; attempt < 10; attempt++)
        {
            try
            {
                using var stream = new FileStream(GetStatePath(), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                var state = JsonSerializer.Deserialize<QueueState>(stream) ?? CreateDefaultState();
                EnsureConfiguredDepartments(state);
                EnsureDailyReset(state);
                var result = action(state);
                stream.SetLength(0);
                JsonSerializer.Serialize(stream, state, JsonOptions);
                return result;
            }
            catch (IOException)
            {
                Thread.Sleep(70);
            }
        }

        throw new IOException("Queue data is busy. Please try again.");
    }

    private static DepartmentQueue FindDepartment(QueueState state, DepartmentAccount account)
    {
        var department = state.Departments.FirstOrDefault(d => d.Code == account.Code);
        if (department is not null)
        {
            return department;
        }

        department = new DepartmentQueue
        {
            Name = account.Name,
            Code = account.Code,
            AccentColor = ColorTranslator.ToHtml(account.Accent)
        };
        state.Departments.Add(department);
        return department;
    }

    private static void EnsureStore()
    {
        var statePath = GetStatePath();
        Directory.CreateDirectory(Path.GetDirectoryName(statePath) ?? AppContext.BaseDirectory);
        if (File.Exists(statePath))
        {
            return;
        }

        using var stream = new FileStream(statePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        JsonSerializer.Serialize(stream, CreateDefaultState(), JsonOptions);
    }

    public static void AddOrUpdateDepartment(DepartmentSettings department)
    {
        Update(state =>
        {
            var existing = state.Departments.FirstOrDefault(d => string.Equals(d.Code, department.Code, StringComparison.OrdinalIgnoreCase));
            if (existing is null)
            {
                state.Departments.Add(new DepartmentQueue
                {
                    Name = department.Name,
                    Code = department.Code,
                    AccentColor = department.AccentColor,
                    NextRegular = 1,
                    NextPriority = 1
                });
            }
            else
            {
                existing.Name = department.Name;
                existing.AccentColor = department.AccentColor;
            }

            return true;
        });
    }

    private static string GetStatePath()
    {
        var settings = AppSettings.Load();
        if (!string.IsNullOrWhiteSpace(settings.QueueStorage.StateFilePath))
        {
            return settings.ResolvePath(settings.QueueStorage.StateFilePath);
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HospitalQueueingSystem",
            "hospital_queue_state.json");
    }

    private static QueueState CreateDefaultState()
    {
        return new QueueState
        {
            BusinessDate = DateTime.Today.ToString("yyyy-MM-dd"),
            Departments = DepartmentAccounts.Select(account => new DepartmentQueue
            {
                Name = account.Name,
                Code = account.Code,
                AccentColor = ColorTranslator.ToHtml(account.Accent),
                NextRegular = 1,
                NextPriority = 1
            }).ToList()
        };
    }

    private static void EnsureConfiguredDepartments(QueueState state)
    {
        var configured = DepartmentAccounts;
        var configuredDepartments = new List<DepartmentQueue>();
        foreach (var account in configured)
        {
            var existing = state.Departments.FirstOrDefault(d => d.Code == account.Code);
            if (existing is null)
            {
                configuredDepartments.Add(new DepartmentQueue
                {
                    Name = account.Name,
                    Code = account.Code,
                    AccentColor = ColorTranslator.ToHtml(account.Accent),
                    NextRegular = 1,
                    NextPriority = 1
                });
            }
            else
            {
                var configuredAccent = ColorTranslator.ToHtml(account.Accent);
                if (string.IsNullOrWhiteSpace(existing.Name) ||
                    !string.Equals(existing.Name, account.Name, StringComparison.Ordinal))
                {
                    existing.Name = account.Name;
                }

                if (string.IsNullOrWhiteSpace(existing.AccentColor) ||
                    string.Equals(existing.AccentColor, "#1D4ED8", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(configuredAccent, "#1D4ED8", StringComparison.OrdinalIgnoreCase))
                {
                    existing.AccentColor = configuredAccent;
                }

                configuredDepartments.Add(existing);
            }
        }

        foreach (var existing in state.Departments)
        {
            if (configuredDepartments.All(d => !string.Equals(d.Code, existing.Code, StringComparison.OrdinalIgnoreCase)))
            {
                configuredDepartments.Add(existing);
            }
        }

        state.Departments = configuredDepartments;
    }

    private static void EnsureDailyReset(QueueState state)
    {
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        if (state.BusinessDate == today)
        {
            return;
        }

        state.BusinessDate = today;
        state.IssuedTickets.Clear();
        state.RecentCalls.Clear();
        foreach (var department in state.Departments)
        {
            department.NextRegular = 1;
            department.NextPriority = 1;
            department.Current = null;
            department.ServedToday = 0;
            department.SkippedToday = 0;
        }
    }
}

internal sealed class HiddenScrollFlowPanel : FlowLayoutPanel
{
    private const int SbBoth = 3;

    public HiddenScrollFlowPanel()
    {
        AutoScroll = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        HideScrollBars();
    }

    protected override void OnScroll(ScrollEventArgs se)
    {
        base.OnScroll(se);
        HideScrollBars();
    }

    protected override void OnResize(EventArgs eventargs)
    {
        base.OnResize(eventargs);
        HideScrollBars();
    }

    protected override void OnControlAdded(ControlEventArgs e)
    {
        base.OnControlAdded(e);
        HideScrollBars();
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        if (m.Msg is 0x000F or 0x0115 or 0x020A)
        {
            HideScrollBars();
        }
    }

    private void HideScrollBars()
    {
        if (IsHandleCreated)
        {
            ShowScrollBar(Handle, SbBoth, false);
        }
    }

    [DllImport("user32.dll")]
    private static extern bool ShowScrollBar(IntPtr hWnd, int wBar, bool bShow);
}

internal sealed class RoundedPanel : Panel
{
    public int Radius { get; set; } = 12;
    public Color BorderColor { get; set; } = Color.FromArgb(203, 213, 225);
    public int BorderSize { get; set; } = 1;

    public RoundedPanel()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        DoubleBuffered = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = RoundedRect(ClientRectangle, Radius);
        using var brush = new SolidBrush(BackColor);
        e.Graphics.FillPath(brush, path);
        if (BorderSize > 0)
        {
            var borderBounds = Rectangle.Inflate(ClientRectangle, -1, -1);
            using var borderPath = RoundedRect(borderBounds, Math.Max(1, Radius - 1));
            using var pen = new Pen(BorderColor, BorderSize);
            e.Graphics.DrawPath(pen, borderPath);
        }
    }

    protected override void OnResize(EventArgs eventargs)
    {
        base.OnResize(eventargs);
        Region?.Dispose();
        using var path = RoundedRect(ClientRectangle, Radius);
        Region = new Region(path);
    }

    private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return new GraphicsPath();
        }

        var diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
        var path = new GraphicsPath();
        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}

internal sealed class LicenseOverlayPanel : Control
{
    private readonly ThemePalette theme;
    private Image? blurredSnapshot;
    private LicenseStatus status = new(false, true, null, "The display license has expired.");

    public LicenseOverlayPanel(ThemePalette theme)
    {
        this.theme = theme;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        Font = new Font("Segoe UI", 16F, FontStyle.Bold);
    }

    public void SetStatus(LicenseStatus status)
    {
        this.status = status;
        Invalidate();
    }

    public void CaptureBoard(Control source)
    {
        if (source.Width <= 0 || source.Height <= 0)
        {
            return;
        }

        using var snapshot = new Bitmap(source.Width, source.Height);
        source.DrawToBitmap(snapshot, new Rectangle(Point.Empty, source.Size));
        blurredSnapshot?.Dispose();
        blurredSnapshot = CreateSoftBlur(snapshot);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        if (blurredSnapshot is not null)
        {
            e.Graphics.DrawImage(blurredSnapshot, ClientRectangle);
        }
        else
        {
            using var backgroundBrush = new SolidBrush(theme.Background);
            e.Graphics.FillRectangle(backgroundBrush, ClientRectangle);
        }

        using var veil = new SolidBrush(Color.FromArgb(188, theme.Background));
        e.Graphics.FillRectangle(veil, ClientRectangle);

        var boxWidth = Math.Min(760, Math.Max(360, ClientSize.Width - 80));
        var boxHeight = 260;
        var box = new Rectangle(
            (ClientSize.Width - boxWidth) / 2,
            (ClientSize.Height - boxHeight) / 2,
            boxWidth,
            boxHeight);

        using var boxPath = RoundedRect(box, 24);
        using var boxBrush = new SolidBrush(Color.FromArgb(236, theme.Card));
        using var borderPen = new Pen(theme.Danger, 2);
        e.Graphics.FillPath(boxBrush, boxPath);
        e.Graphics.DrawPath(borderPen, boxPath);

        var title = "DISPLAY LICENSE EXPIRED";
        var expiryText = status.ExpiresAt is null
            ? status.Message
            : $"Expired on {status.ExpiresAt.Value.LocalDateTime:MMMM dd, yyyy hh:mm tt}.";
        var detail = "Please register a valid display license to continue showing the queue board.";

        DrawCenteredText(e.Graphics, title, new Font("Segoe UI", 28F, FontStyle.Bold), theme.Danger, box.X + 28, box.Y + 34, box.Width - 56, 58);
        DrawCenteredText(e.Graphics, expiryText, new Font("Segoe UI", 16F, FontStyle.Bold), theme.CardText, box.X + 28, box.Y + 104, box.Width - 56, 40);
        DrawCenteredText(e.Graphics, detail, new Font("Segoe UI", 13F, FontStyle.Regular), theme.CardSecondaryText, box.X + 40, box.Y + 158, box.Width - 80, 62);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            blurredSnapshot?.Dispose();
        }

        base.Dispose(disposing);
    }

    private static void DrawCenteredText(Graphics graphics, string text, Font font, Color color, int x, int y, int width, int height)
    {
        using (font)
        using (var brush = new SolidBrush(color))
        using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
        {
            graphics.DrawString(text, font, brush, new RectangleF(x, y, width, height), format);
        }
    }

    private static Bitmap CreateSoftBlur(Image source)
    {
        var smallWidth = Math.Max(1, source.Width / 14);
        var smallHeight = Math.Max(1, source.Height / 14);
        using var small = new Bitmap(smallWidth, smallHeight);
        using (var graphics = Graphics.FromImage(small))
        {
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.DrawImage(source, new Rectangle(0, 0, smallWidth, smallHeight));
        }

        var blurred = new Bitmap(source.Width, source.Height);
        using (var graphics = Graphics.FromImage(blurred))
        {
            graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
            graphics.DrawImage(small, new Rectangle(0, 0, blurred.Width, blurred.Height));
        }

        return blurred;
    }

    private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        var diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
        var path = new GraphicsPath();
        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}

internal sealed class ModernButton : Button
{
    private readonly Color fillColor;
    private readonly Color hoverColor;

    public ModernButton(string text, Color fillColor)
    {
        this.fillColor = fillColor;
        hoverColor = ControlPaint.Light(fillColor, 0.18F);
        Text = text;
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        FlatAppearance.BorderColor = ControlPaint.Light(fillColor, 0.45F);
        FlatAppearance.MouseOverBackColor = hoverColor;
        FlatAppearance.MouseDownBackColor = ControlPaint.Dark(fillColor, 0.12F);
        BackColor = fillColor;
        ForeColor = Color.White;
        Cursor = Cursors.Hand;
        Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        Margin = new Padding(8);
        Height = 56;
        AutoEllipsis = true;
        TextAlign = ContentAlignment.MiddleCenter;
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        BackColor = hoverColor;
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        BackColor = fillColor;
    }
}

internal sealed class ToggleSwitch : CheckBox
{
    public Color ParentBackColor { get; set; } = Color.Transparent;
    public Color TrackOnColor { get; set; } = Color.FromArgb(34, 197, 94);
    public Color TrackOffColor { get; set; } = Color.FromArgb(148, 163, 184);
    public Color ThumbColor { get; set; } = Color.White;

    public ToggleSwitch()
    {
        MinimumSize = new Size(52, 28);
        AutoSize = false;
        Cursor = Cursors.Hand;
        Text = string.Empty;
        Appearance = Appearance.Normal;
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        BackColor = Color.Transparent;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var bounds = ClientRectangle;
        e.Graphics.Clear(ParentBackColor == Color.Transparent ? (Parent?.BackColor ?? BackColor) : ParentBackColor);
        var trackColor = Checked ? TrackOnColor : TrackOffColor;

        using var trackPath = RoundedRect(new Rectangle(1, 1, bounds.Width - 2, bounds.Height - 2), (bounds.Height - 2) / 2);
        using var trackBrush = new SolidBrush(trackColor);
        e.Graphics.FillPath(trackBrush, trackPath);
        using (var borderPen = new Pen(Color.FromArgb(60, Color.Black), 1))
        {
            e.Graphics.DrawPath(borderPen, trackPath);
        }

        var thumbSize = bounds.Height - 8;
        var thumbX = Checked ? bounds.Width - thumbSize - 5 : 5;
        var thumbRect = new Rectangle(thumbX, 4, thumbSize, thumbSize);
        using var thumbBrush = new SolidBrush(ThumbColor);
        e.Graphics.FillEllipse(thumbBrush, thumbRect);
        using (var thumbPen = new Pen(Color.FromArgb(55, Color.Black), 1))
        {
            e.Graphics.DrawEllipse(thumbPen, thumbRect);
        }
    }

    private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        var diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
        var path = new GraphicsPath();
        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
