using Hostel.Core.Entities;
using Hostel.Core.Interfaces;
using Hostel.Core.Services;

namespace Hostel.ConsoleApp;

/// <summary>
/// Main application — Login, Main Menu, Dashboard, Admin Settings
/// </summary>
public partial class HostelApp
{
    // ── Original services ──
    private readonly IStudentService _students;
    private readonly IRoomService _rooms;
    private readonly IPaymentService _payments;
    private readonly IFeeStructureService _fees;
    private readonly IComplaintService _complaints;
    private readonly IStaffService _staff;
    private readonly IVisitorService _visitors;
    private readonly IAttendanceService _attendance;
    private readonly IMessMenuService _mess;
    private readonly INoticeService _notices;
    private readonly IAuditService _audit;
    private readonly IAdminService _admins;

    // ── New services (Phase 5) ──
    private readonly LeaveService _leaves;
    private readonly StudentCheckInOutService _checkInOut;
    private readonly InventoryService _inventory;
    private readonly NotificationService _notifService;
    private readonly TrendAnalyzer _trends;
    private readonly OccupancyForecaster _forecaster;
    private readonly RoomAllocationEngine _roomAllocator;
    private readonly BackupService _backup;
    private readonly FormattedReportExporter _reportExporter;
    private readonly LoginLockoutManager _lockout;
    private readonly AppConfig _config;

    private string _currentUser = "Admin";
    private UserRole _currentRole = UserRole.Admin;
    private DateTime _lastActivity = DateTime.Now;

    public HostelApp(
        IStudentService students, IRoomService rooms,
        IPaymentService payments, IFeeStructureService fees,
        IComplaintService complaints, IStaffService staff,
        IVisitorService visitors, IAttendanceService attendance,
        IMessMenuService mess, INoticeService notices,
        IAuditService audit, IAdminService admins,
        LeaveService leaves, StudentCheckInOutService checkInOut,
        InventoryService inventory, NotificationService notifService,
        TrendAnalyzer trends, OccupancyForecaster forecaster,
        RoomAllocationEngine roomAllocator, BackupService backup,
        FormattedReportExporter reportExporter, LoginLockoutManager lockout,
        AppConfig config)
    {
        _students = students; _rooms = rooms;
        _payments = payments; _fees = fees;
        _complaints = complaints; _staff = staff;
        _visitors = visitors; _attendance = attendance;
        _mess = mess; _notices = notices;
        _audit = audit; _admins = admins;
        _leaves = leaves; _checkInOut = checkInOut;
        _inventory = inventory; _notifService = notifService;
        _trends = trends; _forecaster = forecaster;
        _roomAllocator = roomAllocator; _backup = backup;
        _reportExporter = reportExporter; _lockout = lockout;
        _config = config;
    }

    // ═══════════════════════════════════════════════════════════════
    //  RUN — Entry point
    // ═══════════════════════════════════════════════════════════════
    public async Task RunAsync()
    {
        if (!await _admins.AdminExistsAsync())
        {
            await _admins.CreateAdminAsync(new Admin
            {
                Username = "admin",
                FullName = "System Administrator",
                Role = "SuperAdmin"
            }, "admin123");
        }

        ConsoleUI.ShowBanner();
        ConsoleUI.Pause();

        if (!await LoginAsync()) return;
        await MainMenuAsync();
        FileLogger.Info("App", "Application closed gracefully");
    }

    // ═══════════════════════════════════════════════════════════════
    //  LOGIN (#6 — with lockout)
    // ═══════════════════════════════════════════════════════════════
    private async Task<bool> LoginAsync()
    {
        for (int attempt = 1; attempt <= _config.MaxLoginAttempts; attempt++)
        {
            ConsoleUI.ShowLoginScreen();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"    Attempt {attempt}/{_config.MaxLoginAttempts}");
            Console.ResetColor();
            Console.WriteLine();

            var username = ConsoleUI.ReadInput("Username");

            // #6 Check lockout
            if (_lockout.IsLockedOut(username))
            {
                var remaining = _lockout.GetRemainingLockout(username);
                ConsoleUI.ShowError($"Account locked! Try again in {remaining.Minutes}m {remaining.Seconds}s");
                ConsoleUI.Pause();
                continue;
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("    ▸ Password: ");
            Console.ResetColor();
            var password = ReadPassword();

            ConsoleUI.ShowLoading("Authenticating");

            var admin = await _admins.AuthenticateAsync(username, password);
            if (admin != null)
            {
                _currentUser = admin.FullName;
                _lastActivity = DateTime.Now;
                _lockout.ResetAttempts(username);
                await _audit.LogActionAsync("Auth", "Login", _currentUser, "Successful login");
                FileLogger.Info("Auth", $"User '{_currentUser}' logged in");
                ConsoleUI.ShowSuccess($"Welcome back, {admin.FullName}!");
                ConsoleUI.Pause();
                return true;
            }

            _lockout.RecordFailedAttempt(username);
            await _audit.LogActionAsync("Auth", "Failed Login", username, $"Failed attempt {attempt}/{_config.MaxLoginAttempts}");
            FileLogger.Warn("Auth", $"Failed login for '{username}' (attempt {attempt})");
            ConsoleUI.ShowError("Invalid username or password!");
            if (attempt < _config.MaxLoginAttempts) ConsoleUI.Pause();
        }

        ConsoleUI.ShowError("Too many failed attempts. Exiting...");
        ConsoleUI.Pause();
        return false;
    }

    private static string ReadPassword()
    {
        var password = "";
        while (true)
        {
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Enter) { Console.WriteLine(); break; }
            if (key.Key == ConsoleKey.Backspace && password.Length > 0)
            { password = password[..^1]; Console.Write("\b \b"); }
            else if (!char.IsControl(key.KeyChar))
            { password += key.KeyChar; Console.Write("*"); }
        }
        return password;
    }

    // ═══════════════════════════════════════════════════════════════
    //  MAIN MENU (expanded with new features)
    // ═══════════════════════════════════════════════════════════════
    private async Task MainMenuAsync()
    {
        while (true)
        {
            if ((DateTime.Now - _lastActivity).TotalMinutes > _config.SessionTimeoutMinutes)
            {
                ConsoleUI.ShowWarning($"Session expired after {_config.SessionTimeoutMinutes} min.");
                await _audit.LogActionAsync("Auth", "Session Timeout", _currentUser, "Auto-logout");
                FileLogger.Info("Auth", $"Session timeout for '{_currentUser}'");
                ConsoleUI.Pause();
                return;
            }
            _lastActivity = DateTime.Now;

            ConsoleUI.ShowMenu("MAIN MENU",
                ("1", "Dashboard & Analytics", "📊"),
                ("2", "Student Management", "👨‍🎓"),
                ("3", "Room Management", "🏠"),
                ("4", "Fee & Payment Management", "💰"),
                ("5", "Complaint Management", "📋"),
                ("6", "Staff Management", "👥"),
                ("7", "Visitor Log", "🚪"),
                ("8", "Attendance Tracking", "📅"),
                ("9", "Mess Menu Management", "🍽️"),
                ("10", "Notice Board", "📢"),
                ("11", "Reports & Export", "📁"),
                ("12", "Audit Log", "📝"),
                ("13", "Leave Management", "🏖️"),
                ("14", "Inventory Management", "📦"),
                ("15", "Student Check-In/Out", "🔄"),
                ("16", "Admin Settings", "⚙️"),
                ("?", "Help", "❓"),
                ("0", "Exit System", "🚪"));

            var input = Console.ReadLine()?.Trim();
            switch (input)
            {
                case "1": await DashboardAsync(); break;
                case "2": await StudentMenuAsync(); break;
                case "3": await RoomMenuAsync(); break;
                case "4": await PaymentMenuAsync(); break;
                case "5": await ComplaintMenuAsync(); break;
                case "6": await StaffMenuAsync(); break;
                case "7": await VisitorMenuAsync(); break;
                case "8": await AttendanceMenuAsync(); break;
                case "9": await MessMenuAsync(); break;
                case "10": await NoticeMenuAsync(); break;
                case "11": await ReportsMenuAsync(); break;
                case "12": await AuditLogMenuAsync(); break;
                case "13": await LeaveMenuAsync(); break;
                case "14": await InventoryMenuAsync(); break;
                case "15": await CheckInOutMenuAsync(); break;
                case "16": await AdminSettingsAsync(); break;
                case "?":
                    foreach (var line in HelpSystem.GetHelp("main"))
                    { Console.ForegroundColor = ConsoleColor.Cyan; Console.WriteLine($"    {line}"); }
                    Console.ResetColor(); ConsoleUI.Pause(); break;
                case "0":
                    if (ConsoleUI.ReadConfirm("Are you sure you want to exit?"))
                    {
                        await _audit.LogActionAsync("Auth", "Logout", _currentUser, "User logged out");
                        FileLogger.Info("Auth", $"User '{_currentUser}' logged out");
                        ConsoleUI.ShowHeader("GOODBYE!", ConsoleColor.Green);
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine("    Thank you for using Hostel Management System!");
                        Console.WriteLine("    All data has been saved automatically. 💾");
                        Console.ResetColor(); ConsoleUI.Pause(); return;
                    }
                    break;
                default: ConsoleUI.ShowError("Invalid option!"); ConsoleUI.Pause(); break;
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  DASHBOARD (with #39 trend + #41 forecast)
    // ═══════════════════════════════════════════════════════════════
    private async Task DashboardAsync()
    {
        ConsoleUI.ShowHeader("📊 DASHBOARD & ANALYTICS");
        var activeStudents = await _students.GetActiveCountAsync();
        var allRooms = await _rooms.GetAllRoomsAsync();
        var totalCap = allRooms.Sum(r => r.Capacity);
        var totalOcc = allRooms.Sum(r => r.CurrentOccupancy);
        var availRooms = allRooms.Count(r => !r.IsFull && r.IsActive);
        var occRate = totalCap > 0 ? (double)totalOcc / totalCap * 100 : 0;
        var openComplaints = await _complaints.GetOpenCountAsync();
        var activeStaff = await _staff.GetActiveCountAsync();
        var totalRevenue = await _payments.GetTotalRevenueAsync();
        var monthRevenue = await _payments.GetRevenueByMonthAsync(DateTime.Now.Month, DateTime.Now.Year);
        var pendingPayments = (await _payments.GetPendingPaymentsAsync()).Count;

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("    ┌─────────────────────────────────────────────────────────────┐");
        Console.WriteLine("    │               HOSTEL STATISTICS OVERVIEW                    │");
        Console.WriteLine("    └─────────────────────────────────────────────────────────────┘");
        Console.ResetColor(); Console.WriteLine();

        ConsoleUI.ShowDetailRow("🎓 Active Students", activeStudents.ToString());
        ConsoleUI.ShowDetailRow("🏠 Rooms", $"{allRooms.Count} (Available: {availRooms})");
        ConsoleUI.ShowDetailRow("📊 Occupancy Rate", $"{occRate:F1}%");
        ConsoleUI.ShowDetailRow("👥 Active Staff", activeStaff.ToString());
        ConsoleUI.ShowDetailRow("📋 Open Complaints", openComplaints.ToString());
        ConsoleUI.ShowDetailRow("⏳ Pending Payments", pendingPayments.ToString());
        ConsoleUI.ShowSeparator();
        ConsoleUI.ShowDetailRow("💰 Total Revenue", $"Rs. {totalRevenue:N0}");
        ConsoleUI.ShowDetailRow("📅 This Month", $"Rs. {monthRevenue:N0}");

        // #39 Trend
        var (_, _, change) = await _trends.GetRevenueChangeAsync();
        var arrow = change >= 0 ? "▲" : "▼";
        Console.ForegroundColor = change >= 0 ? ConsoleColor.Green : ConsoleColor.Red;
        Console.WriteLine($"    {arrow} Revenue Change: {change}% vs last month");
        Console.ResetColor();

        Console.WriteLine();
        ConsoleUI.ShowProgressBar("Occupancy", occRate, occRate > 80 ? ConsoleColor.Red : ConsoleColor.Green);

        // #41 Forecast
        var (_, _, _, prediction) = await _forecaster.ForecastAsync();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"\n    📈 Forecast: {prediction}");
        Console.ResetColor();

        var trendData = await _trends.GetMonthlyRevenueTrendAsync(6);
        if (trendData.Values.Any(v => v > 0))
        {
            var chartData = trendData.ToDictionary(k => k.Key, v => (double)v.Value);
            ConsoleUI.ShowBarChart("Revenue Trend (6 months)", chartData, ConsoleColor.Green);
        }
        ConsoleUI.Pause();
    }

    // ═══════════════════════════════════════════════════════════════
    //  #29 — LEAVE MANAGEMENT
    // ═══════════════════════════════════════════════════════════════
    private async Task LeaveMenuAsync()
    {
        while (true)
        {
            ConsoleUI.ShowMenu("🏖️ LEAVE MANAGEMENT",
                ("1", "New Leave Request", "➕"),
                ("2", "Pending Requests", "⏳"),
                ("3", "Approve/Reject", "✅"),
                ("4", "Leave by Student", "👤"),
                ("5", "All Requests", "📋"),
                ("0", "Back", "↩️"));
            var input = Console.ReadLine()?.Trim();
            switch (input)
            {
                case "1":
                    ConsoleUI.ShowHeader("➕ NEW LEAVE REQUEST");
                    var sid = ConsoleUI.ReadInt("Student ID");
                    var student = await _students.GetStudentByIdAsync(sid);
                    if (student == null) { ConsoleUI.ShowError("Student not found!"); ConsoleUI.Pause(); break; }
                    var lr = new LeaveRequest
                    {
                        StudentId = sid, StudentName = student.FullName,
                        StartDate = ConsoleUI.ReadDate("Start Date"),
                        EndDate = ConsoleUI.ReadDate("End Date"),
                        Reason = ConsoleUI.ReadInput("Reason")
                    };
                    await _leaves.RequestLeaveAsync(lr);
                    await _audit.LogActionAsync("Leave", "Request", _currentUser, $"{student.FullName}: {lr.TotalDays} days");
                    ConsoleUI.ShowSuccess($"Leave requested! ({lr.TotalDays} days)");
                    ConsoleUI.Pause(); break;
                case "2":
                    ConsoleUI.ShowHeader("⏳ PENDING LEAVE REQUESTS");
                    var pending = await _leaves.GetPendingAsync();
                    var pRows = pending.Select(l => new[] {
                        l.Id.ToString(), l.StudentName, l.StartDate.ToString("dd-MMM"), l.EndDate.ToString("dd-MMM"),
                        l.TotalDays.ToString(), l.Reason, l.Status.ToString()
                    }).ToList();
                    ConsoleUI.ShowTable(new[] { "ID", "Student", "From", "To", "Days", "Reason", "Status" }, pRows);
                    ConsoleUI.Pause(); break;
                case "3":
                    ConsoleUI.ShowHeader("✅ APPROVE / REJECT LEAVE");
                    var leaveId = ConsoleUI.ReadInt("Leave ID");
                    var action = ConsoleUI.ReadInput("Action (A=Approve / R=Reject)").ToUpper();
                    try
                    {
                        if (action == "A") await _leaves.ApproveLeaveAsync(leaveId, _currentUser);
                        else await _leaves.RejectLeaveAsync(leaveId, _currentUser);
                        ConsoleUI.ShowSuccess($"Leave {(action == "A" ? "approved" : "rejected")}!");
                    }
                    catch (Exception ex) { ConsoleUI.ShowError(ex.Message); }
                    ConsoleUI.Pause(); break;
                case "4":
                    ConsoleUI.ShowHeader("👤 LEAVE BY STUDENT");
                    var sId = ConsoleUI.ReadInt("Student ID");
                    var leaves = await _leaves.GetByStudentAsync(sId);
                    var lRows = leaves.Select(l => new[] {
                        l.Id.ToString(), l.StartDate.ToString("dd-MMM"), l.EndDate.ToString("dd-MMM"),
                        l.TotalDays.ToString(), l.Reason, l.Status.ToString()
                    }).ToList();
                    ConsoleUI.ShowTable(new[] { "ID", "From", "To", "Days", "Reason", "Status" }, lRows);
                    ConsoleUI.Pause(); break;
                case "5":
                    ConsoleUI.ShowHeader("📋 ALL LEAVE REQUESTS");
                    var all = await _leaves.GetAllAsync();
                    var aRows = all.Select(l => new[] {
                        l.Id.ToString(), l.StudentName, l.StartDate.ToString("dd-MMM"), l.EndDate.ToString("dd-MMM"),
                        l.TotalDays.ToString(), l.Status.ToString()
                    }).ToList();
                    ConsoleUI.ShowTable(new[] { "ID", "Student", "From", "To", "Days", "Status" }, aRows);
                    ConsoleUI.Pause(); break;
                case "0": return;
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  #30 — INVENTORY MANAGEMENT
    // ═══════════════════════════════════════════════════════════════
    private async Task InventoryMenuAsync()
    {
        while (true)
        {
            ConsoleUI.ShowMenu("📦 INVENTORY MANAGEMENT",
                ("1", "Add Item", "➕"),
                ("2", "View All Items", "📋"),
                ("3", "Items by Room", "🏠"),
                ("4", "Items by Category", "📂"),
                ("5", "Total Value", "💰"),
                ("6", "Deactivate Item", "🗑️"),
                ("0", "Back", "↩️"));
            var input = Console.ReadLine()?.Trim();
            switch (input)
            {
                case "1":
                    ConsoleUI.ShowHeader("➕ ADD INVENTORY ITEM");
                    var item = new InventoryItem
                    {
                        Name = ConsoleUI.ReadInput("Item Name"),
                        Category = ConsoleUI.ReadEnum<InventoryCategory>("Category"),
                        Quantity = ConsoleUI.ReadInt("Quantity", 1),
                        UnitCost = ConsoleUI.ReadDecimal("Unit Cost (Rs.)"),
                        Condition = ConsoleUI.ReadInput("Condition (Good/Fair/Poor)")
                    };
                    var roomQ = ConsoleUI.ReadInput("Room Number (or Enter for general)");
                    if (!string.IsNullOrEmpty(roomQ)) item.RoomNumber = roomQ;
                    await _inventory.AddItemAsync(item);
                    await _audit.LogActionAsync("Inventory", "Add", _currentUser, $"{item.Name} x{item.Quantity}");
                    ConsoleUI.ShowSuccess($"Added! (Value: Rs. {item.TotalValue:N0})");
                    ConsoleUI.Pause(); break;
                case "2":
                    ConsoleUI.ShowHeader("📋 ALL INVENTORY");
                    var items = await _inventory.GetAllAsync();
                    var rows = items.Select(i => new[] {
                        i.Id.ToString(), i.Name, i.Category.ToString(), i.Quantity.ToString(),
                        $"Rs. {i.UnitCost:N0}", $"Rs. {i.TotalValue:N0}", i.Condition, i.RoomNumber ?? "General"
                    }).ToList();
                    ConsoleUI.ShowTable(new[] { "ID", "Name", "Category", "Qty", "Unit", "Total", "Condition", "Room" }, rows);
                    ConsoleUI.ShowDetailRow("Total Asset Value", $"Rs. {await _inventory.GetTotalValueAsync():N0}");
                    ConsoleUI.Pause(); break;
                case "3":
                    var rId = ConsoleUI.ReadInt("Room ID");
                    var rItems = await _inventory.GetByRoomAsync(rId);
                    var rRows = rItems.Select(i => new[] { i.Id.ToString(), i.Name, i.Quantity.ToString(), i.Condition }).ToList();
                    ConsoleUI.ShowTable(new[] { "ID", "Name", "Qty", "Condition" }, rRows);
                    ConsoleUI.Pause(); break;
                case "4":
                    var cat = ConsoleUI.ReadEnum<InventoryCategory>("Category");
                    var cItems = await _inventory.GetByCategoryAsync(cat);
                    var cRows = cItems.Select(i => new[] { i.Id.ToString(), i.Name, i.Quantity.ToString(), $"Rs. {i.TotalValue:N0}" }).ToList();
                    ConsoleUI.ShowTable(new[] { "ID", "Name", "Qty", "Value" }, cRows);
                    ConsoleUI.Pause(); break;
                case "5":
                    ConsoleUI.ShowDetailRow("Total Asset Value", $"Rs. {await _inventory.GetTotalValueAsync():N0}");
                    ConsoleUI.Pause(); break;
                case "6":
                    await _inventory.DeactivateAsync(ConsoleUI.ReadInt("Item ID"));
                    ConsoleUI.ShowSuccess("Deactivated!"); ConsoleUI.Pause(); break;
                case "0": return;
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  #28 — CHECK-IN / CHECK-OUT
    // ═══════════════════════════════════════════════════════════════
    private async Task CheckInOutMenuAsync()
    {
        while (true)
        {
            ConsoleUI.ShowMenu("🔄 STUDENT CHECK-IN/OUT",
                ("1", "Check In", "✅"), ("2", "Check Out", "🚪"),
                ("3", "Today's Records", "📋"), ("4", "Student History", "📜"),
                ("0", "Back", "↩️"));
            var input = Console.ReadLine()?.Trim();
            switch (input)
            {
                case "1": case "2":
                    var type = input == "1" ? CheckInOutType.CheckIn : CheckInOutType.CheckOut;
                    ConsoleUI.ShowHeader(type == CheckInOutType.CheckIn ? "✅ CHECK IN" : "🚪 CHECK OUT");
                    var sid = ConsoleUI.ReadInt("Student ID");
                    var student = await _students.GetStudentByIdAsync(sid);
                    if (student == null) { ConsoleUI.ShowError("Not found!"); ConsoleUI.Pause(); break; }
                    var remarks = ConsoleUI.ReadInput("Remarks (optional)");
                    await _checkInOut.RecordAsync(sid, student.FullName, type, remarks);
                    ConsoleUI.ShowSuccess($"{student.FullName} {type} at {DateTime.Now:HH:mm}");
                    ConsoleUI.Pause(); break;
                case "3":
                    ConsoleUI.ShowHeader("📋 TODAY'S RECORDS");
                    var today = await _checkInOut.GetTodayRecordsAsync();
                    var tRows = today.Select(r => new[] {
                        r.StudentName, r.Type.ToString(), r.Timestamp.ToString("HH:mm"), r.Remarks
                    }).ToList();
                    ConsoleUI.ShowTable(new[] { "Student", "Type", "Time", "Remarks" }, tRows);
                    ConsoleUI.Pause(); break;
                case "4":
                    var histId = ConsoleUI.ReadInt("Student ID");
                    var history = await _checkInOut.GetHistoryAsync(histId);
                    var hRows = history.Select(r => new[] {
                        r.Type.ToString(), r.Timestamp.ToString("dd-MMM HH:mm"), r.Remarks
                    }).ToList();
                    ConsoleUI.ShowTable(new[] { "Type", "Date/Time", "Remarks" }, hRows);
                    ConsoleUI.Pause(); break;
                case "0": return;
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  ADMIN SETTINGS (expanded: #18 backup, #34 theme, #47 about, #48 update)
    // ═══════════════════════════════════════════════════════════════
    private async Task AdminSettingsAsync()
    {
        while (true)
        {
            ConsoleUI.ShowMenu("⚙️ ADMIN SETTINGS",
                ("1", "Change Password", "🔑"), ("2", "System Info", "ℹ️"),
                ("3", "Backup Data", "💾"), ("4", "Restore Backup", "📂"),
                ("5", "List Backups", "📋"), ("6", "Change Theme", "🎨"),
                ("7", "About / License", "📄"), ("8", "Check Updates", "🔄"),
                ("0", "Back", "↩️"));
            var input = Console.ReadLine()?.Trim();
            switch (input)
            {
                case "1":
                    ConsoleUI.ShowHeader("🔑 CHANGE PASSWORD");
                    Console.Write("    ▸ Current Password: "); var oldPwd = ReadPassword();
                    Console.Write("    ▸ New Password: "); var newPwd = ReadPassword();
                    if (!await _admins.ValidatePasswordStrengthAsync(newPwd))
                    { ConsoleUI.ShowError("Too weak! Need 6+ chars, 1 upper, 1 digit."); ConsoleUI.Pause(); break; }
                    Console.Write("    ▸ Confirm: "); var confirmPwd = ReadPassword();
                    if (newPwd != confirmPwd) { ConsoleUI.ShowError("Mismatch!"); }
                    else
                    {
                        try { await _admins.ChangePasswordAsync(1, oldPwd, newPwd); ConsoleUI.ShowSuccess("Password changed!"); }
                        catch (Exception ex) { ConsoleUI.ShowError(ex.Message); }
                    }
                    ConsoleUI.Pause(); break;
                case "2":
                    ConsoleUI.ShowHeader("ℹ️ SYSTEM INFO");
                    ConsoleUI.ShowDetailRow("App", $"{_config.HostelName} v{_config.Version}");
                    ConsoleUI.ShowDetailRow("Framework", $".NET {Environment.Version}");
                    ConsoleUI.ShowDetailRow("Storage", "JSON File-Based");
                    ConsoleUI.ShowDetailRow("Encryption", _config.EncryptJsonFiles ? "AES" : "Off");
                    ConsoleUI.ShowDetailRow("Theme", _config.Theme);
                    ConsoleUI.ShowDetailRow("Timeout", $"{_config.SessionTimeoutMinutes} min");
                    ConsoleUI.ShowDetailRow("Late Fee", $"Rs. {_config.LateFeePerDay}/day");
                    ConsoleUI.ShowDetailRow("User", _currentUser);
                    ConsoleUI.ShowDetailRow("Screen", $"{ScreenHelper.GetWidth()}x{ScreenHelper.GetHeight()}");
                    ConsoleUI.Pause(); break;
                case "3":
                    var path = _backup.CreateBackup();
                    await _audit.LogActionAsync("Admin", "Backup", _currentUser, path);
                    ConsoleUI.ShowSuccess($"Backup: {path}"); ConsoleUI.Pause(); break;
                case "4":
                    var bkps = _backup.ListBackups();
                    if (bkps.Count == 0) { ConsoleUI.ShowInfo("No backups."); ConsoleUI.Pause(); break; }
                    for (int i = 0; i < bkps.Count; i++)
                        Console.WriteLine($"    [{i+1}] {Path.GetFileName(bkps[i].path)} ({bkps[i].created:dd-MMM HH:mm})");
                    var idx = ConsoleUI.ReadInt("Select #", 1, bkps.Count) - 1;
                    if (ConsoleUI.ReadConfirm("Overwrite current data?"))
                    { _backup.RestoreBackup(bkps[idx].path); ConsoleUI.ShowSuccess("Restored! Restart app."); }
                    ConsoleUI.Pause(); break;
                case "5":
                    var list = _backup.ListBackups();
                    var bRows = list.Select(b => new[] { Path.GetFileName(b.path), b.created.ToString("dd-MMM HH:mm"), b.fileCount.ToString() }).ToList();
                    ConsoleUI.ShowTable(new[] { "Backup", "Created", "Files" }, bRows);
                    ConsoleUI.Pause(); break;
                case "6":
                    Console.WriteLine("    [1] Dark  [2] Light  [3] Blue");
                    var t = ConsoleUI.ReadInt("Choose", 1, 3);
                    _config.Theme = new[] { "Dark", "Light", "Blue" }[t-1];
                    ThemeManager.ApplyTheme(_config.Theme); _config.Save();
                    ConsoleUI.ShowSuccess($"Theme: {_config.Theme}"); ConsoleUI.Pause(); break;
                case "7":
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("    ╔═══════════════════════════════════════════╗");
                    Console.WriteLine("    ║  HOSTEL MANAGEMENT SYSTEM v2.0           ║");
                    Console.WriteLine("    ║  © 2026 Muhammad Hanzala                 ║");
                    Console.WriteLine("    ║  MIT License — Open Source               ║");
                    Console.WriteLine("    ║  .NET 8 | 50 Features | Console Edition  ║");
                    Console.WriteLine("    ╚═══════════════════════════════════════════╝");
                    Console.ResetColor(); ConsoleUI.Pause(); break;
                case "8":
                    ConsoleUI.ShowLoading("Checking");
                    var (avail, cur, latest) = UpdateChecker.CheckForUpdates();
                    ConsoleUI.ShowDetailRow("Current", cur);
                    ConsoleUI.ShowDetailRow("Latest", latest);
                    Console.WriteLine(avail ? "    ⚠️ Update available!" : "    ✅ Up to date!");
                    ConsoleUI.Pause(); break;
                case "0": return;
            }
        }
    }
}
