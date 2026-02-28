using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Hostel.Core.Entities;
using Hostel.Core.Interfaces;

namespace Hostel.Core.Services;

// ═══════════════════════════════════════════════════════════════
//  #13 — APP CONFIGURATION (appsettings.json)
// ═══════════════════════════════════════════════════════════════
public class AppConfig
{
    public string DataDirectory { get; set; } = "hostel_data";
    public string ExportDirectory { get; set; } = "hostel_exports";
    public string BackupDirectory { get; set; } = "hostel_backups";
    public int SessionTimeoutMinutes { get; set; } = 15;
    public int MaxLoginAttempts { get; set; } = 3;
    public int LoginLockoutMinutes { get; set; } = 5;
    public bool EncryptJsonFiles { get; set; } = false;
    public string Theme { get; set; } = "Dark"; // Dark, Light, Blue
    public string Language { get; set; } = "en"; // en, ur
    public decimal LateFeePerDay { get; set; } = 50m;
    public int GracePeriodDays { get; set; } = 7;
    public int SchemaVersion { get; set; } = 2;
    public string HostelName { get; set; } = "Ultimate Hostel";
    public string Version { get; set; } = "2.0.0";

    private static readonly string _configPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

    public static AppConfig Load()
    {
        if (File.Exists(_configPath))
        {
            var json = File.ReadAllText(_configPath);
            return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
        }
        var config = new AppConfig();
        config.Save();
        return config;
    }

    public void Save()
    {
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_configPath, json);
    }
}

// ═══════════════════════════════════════════════════════════════
//  #12 — FILE-BASED LOGGER
// ═══════════════════════════════════════════════════════════════
public static class FileLogger
{
    private static readonly string _logDir = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "hostel_logs");
    private static readonly object _lock = new();

    public static void Log(string level, string module, string message)
    {
        try
        {
            Directory.CreateDirectory(_logDir);
            var file = Path.Combine(_logDir, $"hostel_{DateTime.Now:yyyy-MM-dd}.log");
            var line = $"[{DateTime.Now:HH:mm:ss}] [{level}] [{module}] {message}";
            lock (_lock) { File.AppendAllText(file, line + Environment.NewLine); }
        }
        catch { /* silent */ }
    }

    public static void Info(string module, string msg) => Log("INFO", module, msg);
    public static void Warn(string module, string msg) => Log("WARN", module, msg);
    public static void Error(string module, string msg) => Log("ERROR", module, msg);
    public static void Debug(string module, string msg) => Log("DEBUG", module, msg);
}

// ═══════════════════════════════════════════════════════════════
//  #5 — AES ENCRYPTION FOR JSON FILES
// ═══════════════════════════════════════════════════════════════
public static class DataEncryption
{
    private static readonly byte[] Key = SHA256.HashData(
        Encoding.UTF8.GetBytes("HostelManagementSystem2026SecureKey!"));
    private static readonly byte[] IV = new byte[16]; // fixed for simplicity

    public static string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = Key; aes.IV = IV;
        using var enc = aes.CreateEncryptor();
        var bytes = Encoding.UTF8.GetBytes(plainText);
        var result = enc.TransformFinalBlock(bytes, 0, bytes.Length);
        return Convert.ToBase64String(result);
    }

    public static string Decrypt(string cipherText)
    {
        try
        {
            using var aes = Aes.Create();
            aes.Key = Key; aes.IV = IV;
            using var dec = aes.CreateDecryptor();
            var bytes = Convert.FromBase64String(cipherText);
            var result = dec.TransformFinalBlock(bytes, 0, bytes.Length);
            return Encoding.UTF8.GetString(result);
        }
        catch { return cipherText; /* fallback: not encrypted */ }
    }
}

// ═══════════════════════════════════════════════════════════════
//  #6 — LOGIN LOCKOUT MANAGER
// ═══════════════════════════════════════════════════════════════
public class LoginLockoutManager
{
    private readonly Dictionary<string, (int attempts, DateTime? lockedUntil)> _tracker = new();
    private readonly int _maxAttempts;
    private readonly int _lockoutMinutes;

    public LoginLockoutManager(int maxAttempts = 3, int lockoutMinutes = 5)
    {
        _maxAttempts = maxAttempts;
        _lockoutMinutes = lockoutMinutes;
    }

    public bool IsLockedOut(string username)
    {
        if (!_tracker.TryGetValue(username.ToLower(), out var info)) return false;
        if (info.lockedUntil.HasValue && DateTime.Now < info.lockedUntil.Value) return true;
        if (info.lockedUntil.HasValue && DateTime.Now >= info.lockedUntil.Value)
        {
            _tracker.Remove(username.ToLower());
            return false;
        }
        return false;
    }

    public TimeSpan GetRemainingLockout(string username)
    {
        if (_tracker.TryGetValue(username.ToLower(), out var info) && info.lockedUntil.HasValue)
            return info.lockedUntil.Value - DateTime.Now;
        return TimeSpan.Zero;
    }

    public void RecordFailedAttempt(string username)
    {
        var key = username.ToLower();
        if (!_tracker.ContainsKey(key))
            _tracker[key] = (1, null);
        else
        {
            var (attempts, _) = _tracker[key];
            attempts++;
            if (attempts >= _maxAttempts)
                _tracker[key] = (attempts, DateTime.Now.AddMinutes(_lockoutMinutes));
            else
                _tracker[key] = (attempts, null);
        }
    }

    public void ResetAttempts(string username) => _tracker.Remove(username.ToLower());
}

// ═══════════════════════════════════════════════════════════════
//  #4 — INPUT SANITIZER (CSV Injection Prevention)
// ═══════════════════════════════════════════════════════════════
public static class InputSanitizer
{
    private static readonly char[] DangerousChars = { '=', '+', '-', '@', '\t', '\r', '\n' };

    public static string SanitizeForCsv(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        input = input.Replace("\"", "\"\"");
        if (DangerousChars.Any(c => input.StartsWith(c)))
            input = "'" + input;
        return $"\"{input}\"";
    }

    public static string SanitizeInput(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return input.Replace("<", "&lt;").Replace(">", "&gt;")
                    .Replace("&", "&amp;").Replace("'", "&#39;").Trim();
    }
}

// ═══════════════════════════════════════════════════════════════
//  #18 — BACKUP & RESTORE SYSTEM
// ═══════════════════════════════════════════════════════════════
public class BackupService
{
    private readonly string _dataDir;
    private readonly string _backupDir;

    public BackupService(string dataDir, string backupDir)
    {
        _dataDir = dataDir;
        _backupDir = backupDir;
    }

    public string CreateBackup()
    {
        Directory.CreateDirectory(_backupDir);
        var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var backupPath = Path.Combine(_backupDir, $"backup_{stamp}");
        Directory.CreateDirectory(backupPath);

        foreach (var file in Directory.GetFiles(_dataDir, "*.json"))
        {
            var dest = Path.Combine(backupPath, Path.GetFileName(file));
            File.Copy(file, dest, true);
        }

        FileLogger.Info("Backup", $"Backup created at: {backupPath}");
        return backupPath;
    }

    public bool RestoreBackup(string backupPath)
    {
        if (!Directory.Exists(backupPath)) return false;
        foreach (var file in Directory.GetFiles(backupPath, "*.json"))
        {
            var dest = Path.Combine(_dataDir, Path.GetFileName(file));
            File.Copy(file, dest, true);
        }
        FileLogger.Info("Backup", $"Restored from: {backupPath}");
        return true;
    }

    public List<(string path, DateTime created, int fileCount)> ListBackups()
    {
        if (!Directory.Exists(_backupDir)) return new();
        return Directory.GetDirectories(_backupDir, "backup_*")
            .Select(d => (d, Directory.GetCreationTime(d), Directory.GetFiles(d, "*.json").Length))
            .OrderByDescending(x => x.Item2)
            .ToList();
    }
}

// ═══════════════════════════════════════════════════════════════
//  #21 — PAGINATION HELPER
// ═══════════════════════════════════════════════════════════════
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => PageNumber > 1;
    public bool HasNext => PageNumber < TotalPages;
}

public static class PaginationHelper
{
    public static PagedResult<T> Paginate<T>(IEnumerable<T> source, int page, int pageSize = 10)
    {
        var items = source.ToList();
        return new PagedResult<T>
        {
            Items = items.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            TotalCount = items.Count,
            PageNumber = page,
            PageSize = pageSize
        };
    }
}

// ═══════════════════════════════════════════════════════════════
//  #19 — FILE LOCKING FOR CONCURRENT ACCESS
// ═══════════════════════════════════════════════════════════════
public static class FileLock
{
    private static readonly Dictionary<string, SemaphoreSlim> _locks = new();

    public static SemaphoreSlim GetLock(string filePath)
    {
        lock (_locks)
        {
            if (!_locks.ContainsKey(filePath))
                _locks[filePath] = new SemaphoreSlim(1, 1);
            return _locks[filePath];
        }
    }
}

// ═══════════════════════════════════════════════════════════════
//  #25 — LATE FEE CALCULATOR
// ═══════════════════════════════════════════════════════════════
public static class LateFeeCalculator
{
    public static decimal Calculate(DateTime dueDate, decimal feePerDay, int gracePeriodDays = 7)
    {
        var overdueDays = (DateTime.Now - dueDate).Days - gracePeriodDays;
        if (overdueDays <= 0) return 0;
        return overdueDays * feePerDay;
    }

    public static string GetStatus(DateTime dueDate, int gracePeriodDays = 7)
    {
        var days = (DateTime.Now - dueDate).Days;
        if (days <= 0) return "On Time";
        if (days <= gracePeriodDays) return "Grace Period";
        if (days <= 30) return "Overdue";
        return "Severely Overdue";
    }
}

// ═══════════════════════════════════════════════════════════════
//  #26 — AUTO ROOM ALLOCATION ALGORITHM
// ═══════════════════════════════════════════════════════════════
public class RoomAllocationEngine
{
    private readonly IRoomService _rooms;

    public RoomAllocationEngine(IRoomService rooms) => _rooms = rooms;

    public async Task<Room?> FindBestRoomAsync(RoomType? preferred = null, bool preferAC = false)
    {
        var available = await _rooms.GetAvailableRoomsAsync();
        if (available.Count == 0) return null;

        var candidates = available.AsEnumerable();

        // Filter by preferred type if specified
        if (preferred.HasValue)
        {
            var typed = candidates.Where(r => r.RoomType == preferred.Value).ToList();
            if (typed.Count > 0) candidates = typed;
        }

        // Prefer AC rooms if requested
        if (preferAC)
        {
            var acRooms = candidates.Where(r => r.HasAC).ToList();
            if (acRooms.Count > 0) candidates = acRooms;
        }

        // Sort by: most occupied first (to fill rooms), then cheapest
        return candidates
            .OrderByDescending(r => r.CurrentOccupancy)
            .ThenBy(r => r.MonthlyRent)
            .FirstOrDefault();
    }

    public async Task<List<(Room room, int score)>> GetRoomScoresAsync()
    {
        var available = await _rooms.GetAvailableRoomsAsync();
        return available.Select(r => {
            int score = 100;
            score -= (r.Capacity - r.CurrentOccupancy) * 10; // prefer fuller
            if (r.HasAC) score += 15;
            if (r.HasAttachedBath) score += 10;
            score -= (int)(r.MonthlyRent / 1000); // cheaper = higher
            return (r, Math.Max(0, score));
        }).OrderByDescending(x => x.Item2).ToList();
    }
}

// ═══════════════════════════════════════════════════════════════
//  #27 — NOTIFICATION SERVICE
// ═══════════════════════════════════════════════════════════════
public class NotificationService
{
    private readonly IGenericRepository<Notification> _notifications;

    public NotificationService(IGenericRepository<Notification> notifications)
        => _notifications = notifications;

    public async Task SendAsync(string title, string message, NotificationType type, int? studentId = null)
    {
        var notif = new Notification
        {
            Title = title,
            Message = message,
            Type = type,
            TargetStudentId = studentId,
            CreatedAt = DateTime.Now,
            IsSent = true // In real app, would queue for SMS/Email
        };
        await _notifications.AddAsync(notif);
        await _notifications.SaveChangesAsync();
        FileLogger.Info("Notification", $"[{type}] {title} → Student #{studentId ?? 0}");
    }

    public async Task<IReadOnlyList<Notification>> GetUnreadAsync(int? studentId = null)
    {
        var all = await _notifications.GetAllAsync();
        var query = all.Where(n => !n.IsRead);
        if (studentId.HasValue) query = query.Where(n => n.TargetStudentId == studentId);
        return query.OrderByDescending(n => n.CreatedAt).ToList();
    }

    public async Task<IReadOnlyList<Notification>> GetAllAsync()
    {
        var all = await _notifications.GetAllAsync();
        return all.OrderByDescending(n => n.CreatedAt).ToList();
    }

    public async Task MarkAsReadAsync(int notifId)
    {
        var n = await _notifications.GetByIdAsync(notifId);
        if (n != null) { n.IsRead = true; _notifications.Update(n); await _notifications.SaveChangesAsync(); }
    }
}

// ═══════════════════════════════════════════════════════════════
//  #28 — STUDENT CHECK-IN/CHECK-OUT SERVICE
// ═══════════════════════════════════════════════════════════════
public class StudentCheckInOutService
{
    private readonly IGenericRepository<StudentCheckInOut> _records;

    public StudentCheckInOutService(IGenericRepository<StudentCheckInOut> records)
        => _records = records;

    public async Task<StudentCheckInOut> RecordAsync(int studentId, string studentName, CheckInOutType type, string remarks = "")
    {
        var record = new StudentCheckInOut
        {
            StudentId = studentId, StudentName = studentName,
            Type = type, Timestamp = DateTime.Now, Remarks = remarks
        };
        await _records.AddAsync(record);
        await _records.SaveChangesAsync();
        return record;
    }

    public async Task<CheckInOutType?> GetLastStatusAsync(int studentId)
    {
        var all = await _records.GetAllAsync();
        return all.Where(r => r.StudentId == studentId)
                  .OrderByDescending(r => r.Timestamp)
                  .FirstOrDefault()?.Type;
    }

    public async Task<IReadOnlyList<StudentCheckInOut>> GetHistoryAsync(int studentId)
    {
        var all = await _records.GetAllAsync();
        return all.Where(r => r.StudentId == studentId)
                  .OrderByDescending(r => r.Timestamp).ToList();
    }

    public async Task<IReadOnlyList<StudentCheckInOut>> GetTodayRecordsAsync()
    {
        var all = await _records.GetAllAsync();
        return all.Where(r => r.Timestamp.Date == DateTime.Today)
                  .OrderByDescending(r => r.Timestamp).ToList();
    }
}

// ═══════════════════════════════════════════════════════════════
//  #29 — LEAVE MANAGEMENT SERVICE
// ═══════════════════════════════════════════════════════════════
public class LeaveService
{
    private readonly IGenericRepository<LeaveRequest> _leaves;

    public LeaveService(IGenericRepository<LeaveRequest> leaves)
        => _leaves = leaves;

    public async Task<LeaveRequest> RequestLeaveAsync(LeaveRequest request)
    {
        request.RequestedAt = DateTime.Now;
        request.Status = LeaveStatus.Pending;
        await _leaves.AddAsync(request);
        await _leaves.SaveChangesAsync();
        return request;
    }

    public async Task ApproveLeaveAsync(int leaveId, string approvedBy)
    {
        var leave = await _leaves.GetByIdAsync(leaveId)
            ?? throw new InvalidOperationException("Leave request not found");
        leave.Status = LeaveStatus.Approved;
        leave.ApprovedBy = approvedBy;
        _leaves.Update(leave);
        await _leaves.SaveChangesAsync();
    }

    public async Task RejectLeaveAsync(int leaveId, string rejectedBy)
    {
        var leave = await _leaves.GetByIdAsync(leaveId)
            ?? throw new InvalidOperationException("Leave request not found");
        leave.Status = LeaveStatus.Rejected;
        leave.ApprovedBy = rejectedBy;
        _leaves.Update(leave);
        await _leaves.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<LeaveRequest>> GetPendingAsync()
    {
        var all = await _leaves.GetAllAsync();
        return all.Where(l => l.Status == LeaveStatus.Pending)
                  .OrderByDescending(l => l.RequestedAt).ToList();
    }

    public async Task<IReadOnlyList<LeaveRequest>> GetByStudentAsync(int studentId)
    {
        var all = await _leaves.GetAllAsync();
        return all.Where(l => l.StudentId == studentId)
                  .OrderByDescending(l => l.RequestedAt).ToList();
    }

    public async Task<IReadOnlyList<LeaveRequest>> GetAllAsync()
    {
        var all = await _leaves.GetAllAsync();
        return all.OrderByDescending(l => l.RequestedAt).ToList();
    }
}

// ═══════════════════════════════════════════════════════════════
//  #30 — INVENTORY MANAGEMENT SERVICE
// ═══════════════════════════════════════════════════════════════
public class InventoryService
{
    private readonly IGenericRepository<InventoryItem> _items;

    public InventoryService(IGenericRepository<InventoryItem> items) => _items = items;

    public async Task<InventoryItem> AddItemAsync(InventoryItem item)
    {
        item.IsActive = true;
        item.PurchaseDate = item.PurchaseDate == default ? DateTime.Now : item.PurchaseDate;
        await _items.AddAsync(item);
        await _items.SaveChangesAsync();
        return item;
    }

    public async Task UpdateItemAsync(InventoryItem item)
    {
        _items.Update(item);
        await _items.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<InventoryItem>> GetAllAsync()
    {
        var all = await _items.GetAllAsync();
        return all.Where(i => i.IsActive).ToList();
    }

    public async Task<IReadOnlyList<InventoryItem>> GetByRoomAsync(int roomId)
    {
        var all = await _items.GetAllAsync();
        return all.Where(i => i.RoomId == roomId && i.IsActive).ToList();
    }

    public async Task<IReadOnlyList<InventoryItem>> GetByCategoryAsync(InventoryCategory cat)
    {
        var all = await _items.GetAllAsync();
        return all.Where(i => i.Category == cat && i.IsActive).ToList();
    }

    public async Task<decimal> GetTotalValueAsync()
    {
        var all = await _items.GetAllAsync();
        return all.Where(i => i.IsActive).Sum(i => i.TotalValue);
    }

    public async Task DeactivateAsync(int id)
    {
        var item = await _items.GetByIdAsync(id);
        if (item != null) { item.IsActive = false; _items.Update(item); await _items.SaveChangesAsync(); }
    }
}

// ═══════════════════════════════════════════════════════════════
//  #39 — TREND ANALYSIS ENGINE
// ═══════════════════════════════════════════════════════════════
public class TrendAnalyzer
{
    private readonly IPaymentService _payments;
    private readonly IStudentService _students;

    public TrendAnalyzer(IPaymentService payments, IStudentService students)
    { _payments = payments; _students = students; }

    public async Task<Dictionary<string, decimal>> GetMonthlyRevenueTrendAsync(int months = 6)
    {
        var trend = new Dictionary<string, decimal>();
        var now = DateTime.Now;
        for (int i = months - 1; i >= 0; i--)
        {
            var date = now.AddMonths(-i);
            var rev = await _payments.GetRevenueByMonthAsync(date.Month, date.Year);
            trend[$"{date:MMM yyyy}"] = rev;
        }
        return trend;
    }

    public async Task<(decimal currentMonth, decimal lastMonth, double changePercent)> GetRevenueChangeAsync()
    {
        var now = DateTime.Now;
        var current = await _payments.GetRevenueByMonthAsync(now.Month, now.Year);
        var last = await _payments.GetRevenueByMonthAsync(now.AddMonths(-1).Month, now.AddMonths(-1).Year);
        var change = last > 0 ? (double)((current - last) / last * 100) : 0;
        return (current, last, Math.Round(change, 1));
    }
}

// ═══════════════════════════════════════════════════════════════
//  #41 — OCCUPANCY FORECASTING
// ═══════════════════════════════════════════════════════════════
public class OccupancyForecaster
{
    private readonly IRoomService _rooms;
    private readonly IStudentService _students;

    public OccupancyForecaster(IRoomService rooms, IStudentService students)
    { _rooms = rooms; _students = students; }

    public async Task<(int current, int capacity, double rate, string prediction)> ForecastAsync()
    {
        var allRooms = await _rooms.GetAllRoomsAsync();
        var cap = allRooms.Sum(r => r.Capacity);
        var occ = allRooms.Sum(r => r.CurrentOccupancy);
        var occRate = cap > 0 ? (double)occ / cap * 100 : 0;

        string pred;
        if (occRate >= 90) pred = "🔴 Critical — nearly full, need more rooms";
        else if (occRate >= 70) pred = "🟡 High demand — expect full within 2 months";
        else if (occRate >= 50) pred = "🟢 Healthy — balanced occupancy";
        else pred = "🔵 Low — marketing or outreach needed";

        return (occ, cap, Math.Round(occRate, 1), pred);
    }

    public async Task<List<(string roomNumber, int vacant, decimal rent)>> GetVacancyListAsync()
    {
        var rooms = await _rooms.GetAvailableRoomsAsync();
        return rooms.Select(r => (r.RoomNumber, r.Capacity - r.CurrentOccupancy, r.MonthlyRent)).ToList();
    }
}

// ═══════════════════════════════════════════════════════════════
//  #34 — THEME MANAGER
// ═══════════════════════════════════════════════════════════════
public static class ThemeManager
{
    public static ConsoleColor PrimaryColor { get; private set; } = ConsoleColor.Cyan;
    public static ConsoleColor SecondaryColor { get; private set; } = ConsoleColor.DarkCyan;
    public static ConsoleColor AccentColor { get; private set; } = ConsoleColor.Yellow;
    public static ConsoleColor SuccessColor { get; private set; } = ConsoleColor.Green;
    public static ConsoleColor ErrorColor { get; private set; } = ConsoleColor.Red;
    public static ConsoleColor WarningColor { get; private set; } = ConsoleColor.DarkYellow;
    public static ConsoleColor BackgroundColor { get; private set; } = ConsoleColor.Black;

    public static void ApplyTheme(string theme)
    {
        switch (theme.ToLower())
        {
            case "light":
                BackgroundColor = ConsoleColor.White;
                PrimaryColor = ConsoleColor.DarkBlue;
                SecondaryColor = ConsoleColor.DarkCyan;
                AccentColor = ConsoleColor.DarkMagenta;
                break;
            case "blue":
                BackgroundColor = ConsoleColor.DarkBlue;
                PrimaryColor = ConsoleColor.White;
                SecondaryColor = ConsoleColor.Cyan;
                AccentColor = ConsoleColor.Yellow;
                break;
            default: // dark (default)
                BackgroundColor = ConsoleColor.Black;
                PrimaryColor = ConsoleColor.Cyan;
                SecondaryColor = ConsoleColor.DarkCyan;
                AccentColor = ConsoleColor.Yellow;
                break;
        }
        Console.BackgroundColor = BackgroundColor;
    }
}

// ═══════════════════════════════════════════════════════════════
//  #36 — HELP SYSTEM
// ═══════════════════════════════════════════════════════════════
public static class HelpSystem
{
    private static readonly Dictionary<string, string[]> _help = new()
    {
        ["main"] = new[] {
            "📖 MAIN MENU HELP",
            "──────────────────",
            "1. Dashboard  — View stats, charts, and occupancy overview",
            "2. Students   — Register, edit, assign rooms, search students",
            "3. Rooms      — Create, edit, delete rooms, view occupancy",
            "4. Payments   — Record fees, generate receipts, view defaulters",
            "5. Complaints — File, track, assign, and resolve complaints",
            "6. Staff      — Add, edit, and manage hostel staff",
            "7. Visitors   — Check-in/out visitors, view history",
            "8. Attendance — Mark daily attendance, view stats",
            "9. Mess Menu  — Manage daily/weekly meal schedules",
            "10. Notices   — Post and view announcements",
            "11. Reports   — Export data, view analytics",
            "12. Audit Log — View system activity history",
            "13. Settings  — Change password, system info, backups",
            "",
            "💡 TIP: Type '?' at any menu to see help",
            "💡 TIP: Session auto-logs out after 15 minutes",
            "💡 TIP: Press Ctrl+C to exit at any time"
        },
        ["student"] = new[] {
            "📖 STUDENT MANAGEMENT HELP",
            "───────────────────────────",
            "• Register    — Add new student with full details",
            "• Assign Room — Auto-suggest or manual room assignment",
            "• Swap Rooms  — Exchange rooms between two students",
            "• Deactivate  — Mark a student as graduated/left",
            "• Search      — Find by name, reg#, phone, department",
        },
        ["payment"] = new[] {
            "📖 PAYMENT MANAGEMENT HELP",
            "───────────────────────────",
            "• Record Payment — Enter amount, method, period",
            "• Generate Receipt — Create printable receipt",
            "• Defaulter List  — Students with overdue payments",
            "• Fee Structure   — Set room-type-based fee plans",
            "• Late fees are auto-calculated after grace period",
        },
        ["keyboard"] = new[] {
            "⌨️ KEYBOARD SHORTCUTS",
            "──────────────────────",
            "• ?     — Show help for current menu",
            "• 0     — Go back to previous menu",
            "• Ctrl+C — Exit application",
        }
    };

    public static string[] GetHelp(string topic)
    {
        return _help.TryGetValue(topic.ToLower(), out var help) ? help
            : new[] { $"No help available for '{topic}'. Try: main, student, payment, keyboard" };
    }
}

// ═══════════════════════════════════════════════════════════════
//  #33 — SCREEN SIZE DETECTION
// ═══════════════════════════════════════════════════════════════
public static class ScreenHelper
{
    public static int GetWidth()
    {
        try { return Console.WindowWidth; } catch { return 80; }
    }

    public static int GetHeight()
    {
        try { return Console.WindowHeight; } catch { return 25; }
    }

    public static bool IsNarrow() => GetWidth() < 100;

    public static string FitText(string text, int maxWidth = 0)
    {
        if (maxWidth == 0) maxWidth = GetWidth() - 10;
        return text.Length > maxWidth ? text[..(maxWidth - 3)] + "..." : text;
    }
}

// ═══════════════════════════════════════════════════════════════
//  #37 — PDF-STYLE FORMATTED REPORT EXPORT
// ═══════════════════════════════════════════════════════════════
public class FormattedReportExporter
{
    private readonly string _exportDir;

    public FormattedReportExporter(string exportDir) => _exportDir = exportDir;

    public string ExportFormattedReport(string title, string[] headers, List<string[]> rows)
    {
        Directory.CreateDirectory(_exportDir);
        var file = Path.Combine(_exportDir, $"{title.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
        var sb = new StringBuilder();

        // Header box
        var lineWidth = Math.Max(60, title.Length + 10);
        sb.AppendLine(new string('═', lineWidth));
        sb.AppendLine($"  {title.ToUpper()}");
        sb.AppendLine($"  Generated: {DateTime.Now:dd-MMM-yyyy HH:mm:ss}");
        sb.AppendLine($"  Hostel Management System v2.0");
        sb.AppendLine(new string('═', lineWidth));
        sb.AppendLine();

        // Table
        var widths = new int[headers.Length];
        for (int i = 0; i < headers.Length; i++)
        {
            widths[i] = headers[i].Length;
            foreach (var row in rows)
                if (i < row.Length && row[i].Length > widths[i])
                    widths[i] = row[i].Length;
            widths[i] += 2;
        }

        var sep = "+" + string.Join("+", widths.Select(w => new string('-', w))) + "+";
        sb.AppendLine(sep);
        sb.AppendLine("|" + string.Join("|", headers.Select((h, i) => h.PadRight(widths[i]))) + "|");
        sb.AppendLine(sep);
        foreach (var row in rows)
            sb.AppendLine("|" + string.Join("|", row.Select((c, i) => (i < widths.Length ? c.PadRight(widths[i]) : c))) + "|");
        sb.AppendLine(sep);
        sb.AppendLine();
        sb.AppendLine($"  Total Records: {rows.Count}");
        sb.AppendLine(new string('═', lineWidth));

        File.WriteAllText(file, sb.ToString());
        return file;
    }
}

// ═══════════════════════════════════════════════════════════════
//  #49 — PLUGIN SYSTEM INTERFACE
// ═══════════════════════════════════════════════════════════════
public interface IHostelPlugin
{
    string Name { get; }
    string Version { get; }
    string Description { get; }
    Task InitializeAsync();
    Task ExecuteAsync();
}

// ═══════════════════════════════════════════════════════════════
//  #48 — AUTO-UPDATE CHECKER (Stub)
// ═══════════════════════════════════════════════════════════════
public static class UpdateChecker
{
    public static (bool available, string currentVersion, string latestVersion) CheckForUpdates()
    {
        // In production, this would call an API endpoint
        var current = "2.0.0";
        var latest = "2.0.0"; // Simulated
        return (current != latest, current, latest);
    }
}
