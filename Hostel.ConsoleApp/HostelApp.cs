using Hostel.Core.Entities;
using Hostel.Core.Interfaces;

namespace Hostel.ConsoleApp;

/// <summary>
/// Main application — Login, Main Menu, Dashboard
/// </summary>
public partial class HostelApp
{
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
    private string _currentUser = "Admin";

    public HostelApp(
        IStudentService students, IRoomService rooms,
        IPaymentService payments, IFeeStructureService fees,
        IComplaintService complaints, IStaffService staff,
        IVisitorService visitors, IAttendanceService attendance,
        IMessMenuService mess, INoticeService notices,
        IAuditService audit, IAdminService admins)
    {
        _students = students; _rooms = rooms;
        _payments = payments; _fees = fees;
        _complaints = complaints; _staff = staff;
        _visitors = visitors; _attendance = attendance;
        _mess = mess; _notices = notices;
        _audit = audit; _admins = admins;
    }

    // ═══════════════════════════════════════════════════════════════
    //  RUN — Entry point
    // ═══════════════════════════════════════════════════════════════
    public async Task RunAsync()
    {
        // Ensure default admin exists
        if (!await _admins.AdminExistsAsync())
        {
            await _admins.CreateAdminAsync(new Admin
            {
                Username = "admin",
                FullName = "System Administrator",
                Role = "SuperAdmin"
            }, "admin123");
        }

        // Show banner
        ConsoleUI.ShowBanner();
        ConsoleUI.Pause();

        // Login
        if (!await LoginAsync()) return;

        // Main loop
        await MainMenuAsync();
    }

    // ═══════════════════════════════════════════════════════════════
    //  LOGIN
    // ═══════════════════════════════════════════════════════════════
    private async Task<bool> LoginAsync()
    {
        for (int attempt = 1; attempt <= 3; attempt++)
        {
            ConsoleUI.ShowLoginScreen();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"    Attempt {attempt}/3");
            Console.ResetColor();
            Console.WriteLine();

            var username = ConsoleUI.ReadInput("Username");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("    ▸ Password: ");
            Console.ResetColor();
            var password = ReadPassword();

            ConsoleUI.ShowLoading("Authenticating");

            var admin = await _admins.AuthenticateAsync(username, password);
            if (admin != null)
            {
                _currentUser = admin.FullName;
                await _audit.LogActionAsync("Auth", "Login", _currentUser, "Successful login");
                ConsoleUI.ShowSuccess($"Welcome back, {admin.FullName}!");
                ConsoleUI.Pause();
                return true;
            }

            ConsoleUI.ShowError("Invalid username or password!");
            if (attempt < 3) ConsoleUI.Pause();
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
            {
                password = password[..^1];
                Console.Write("\b \b");
            }
            else if (!char.IsControl(key.KeyChar))
            {
                password += key.KeyChar;
                Console.Write("*");
            }
        }
        return password;
    }

    // ═══════════════════════════════════════════════════════════════
    //  MAIN MENU
    // ═══════════════════════════════════════════════════════════════
    private async Task MainMenuAsync()
    {
        while (true)
        {
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
                ("13", "Admin Settings", "⚙️"),
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
                case "13": await AdminSettingsAsync(); break;
                case "0":
                    if (ConsoleUI.ReadConfirm("Are you sure you want to exit?"))
                    {
                        await _audit.LogActionAsync("Auth", "Logout", _currentUser, "User logged out");
                        ConsoleUI.ShowHeader("GOODBYE!", ConsoleColor.Green);
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine("    Thank you for using Hostel Management System!");
                        Console.WriteLine("    All data has been saved automatically. 💾");
                        Console.ResetColor();
                        ConsoleUI.Pause();
                        return;
                    }
                    break;
                default:
                    ConsoleUI.ShowError("Invalid option!");
                    ConsoleUI.Pause();
                    break;
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  DASHBOARD
    // ═══════════════════════════════════════════════════════════════
    private async Task DashboardAsync()
    {
        ConsoleUI.ShowHeader("📊 DASHBOARD & ANALYTICS");

        var activeStudents = await _students.GetActiveCountAsync();
        var allRooms = await _rooms.GetAllRoomsAsync();
        var totalRooms = allRooms.Count;
        var totalCap = allRooms.Sum(r => r.Capacity);
        var totalOcc = allRooms.Sum(r => r.CurrentOccupancy);
        var occupiedRooms = allRooms.Count(r => r.CurrentOccupancy > 0);
        var availRooms = allRooms.Count(r => !r.IsFull && r.IsActive);
        var occRate = totalCap > 0 ? (double)totalOcc / totalCap * 100 : 0;
        var openComplaints = await _complaints.GetOpenCountAsync();
        var activeStaff = await _staff.GetActiveCountAsync();
        var totalRevenue = await _payments.GetTotalRevenueAsync();
        var monthRevenue = await _payments.GetRevenueByMonthAsync(DateTime.Now.Month, DateTime.Now.Year);
        var activeVisitors = (await _visitors.GetActiveVisitorsAsync()).Count;
        var pendingPayments = (await _payments.GetPendingPaymentsAsync()).Count;

        // Stats section
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("    ┌─────────────────────────────────────────────────────────────┐");
        Console.WriteLine("    │               HOSTEL STATISTICS OVERVIEW                    │");
        Console.WriteLine("    └─────────────────────────────────────────────────────────────┘");
        Console.ResetColor();
        Console.WriteLine();

        ConsoleUI.ShowDetailRow("🎓 Active Students", activeStudents.ToString());
        ConsoleUI.ShowDetailRow("🏠 Total Rooms", $"{totalRooms} (Occupied: {occupiedRooms}, Available: {availRooms})");
        ConsoleUI.ShowDetailRow("📊 Occupancy Rate", $"{occRate:F1}%");
        ConsoleUI.ShowDetailRow("👥 Active Staff", activeStaff.ToString());
        ConsoleUI.ShowDetailRow("📋 Open Complaints", openComplaints.ToString());
        ConsoleUI.ShowDetailRow("🚪 Active Visitors", activeVisitors.ToString());
        ConsoleUI.ShowDetailRow("⏳ Pending Payments", pendingPayments.ToString());

        ConsoleUI.ShowSeparator();
        ConsoleUI.ShowDetailRow("💰 Total Revenue", $"Rs. {totalRevenue:N0}");
        ConsoleUI.ShowDetailRow("📅 This Month Revenue", $"Rs. {monthRevenue:N0}");

        // Occupancy bar
        Console.WriteLine();
        ConsoleUI.ShowProgressBar("Occupancy", occRate, occRate > 80 ? ConsoleColor.Red : ConsoleColor.Green);

        // Room type chart
        var roomTypeData = new Dictionary<string, double>();
        foreach (RoomType rt in Enum.GetValues<RoomType>())
        {
            var count = allRooms.Count(r => r.RoomType == rt);
            if (count > 0) roomTypeData[rt.ToString()] = count;
        }
        if (roomTypeData.Count > 0)
            ConsoleUI.ShowBarChart("Rooms by Type", roomTypeData, ConsoleColor.Cyan);

        ConsoleUI.Pause();
    }

    // ═══════════════════════════════════════════════════════════════
    //  ADMIN SETTINGS
    // ═══════════════════════════════════════════════════════════════
    private async Task AdminSettingsAsync()
    {
        while (true)
        {
            ConsoleUI.ShowMenu("⚙️ ADMIN SETTINGS",
                ("1", "Change Password", "🔑"),
                ("2", "View System Info", "ℹ️"),
                ("0", "Back to Main Menu", "↩️"));

            var input = Console.ReadLine()?.Trim();
            switch (input)
            {
                case "1":
                    ConsoleUI.ShowHeader("🔑 CHANGE PASSWORD");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("    ▸ Current Password: ");
                    Console.ResetColor();
                    var oldPwd = ReadPassword();
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("    ▸ New Password: ");
                    Console.ResetColor();
                    var newPwd = ReadPassword();
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("    ▸ Confirm Password: ");
                    Console.ResetColor();
                    var confirmPwd = ReadPassword();

                    if (newPwd != confirmPwd)
                    {
                        ConsoleUI.ShowError("Passwords do not match!");
                    }
                    else
                    {
                        try
                        {
                            await _admins.ChangePasswordAsync(1, oldPwd, newPwd);
                            ConsoleUI.ShowSuccess("Password changed successfully!");
                            await _audit.LogActionAsync("Admin", "Change Password", _currentUser, "Password updated");
                        }
                        catch (Exception ex) { ConsoleUI.ShowError(ex.Message); }
                    }
                    ConsoleUI.Pause();
                    break;
                case "2":
                    ConsoleUI.ShowHeader("ℹ️ SYSTEM INFORMATION");
                    ConsoleUI.ShowDetailRow("Application", "Hostel Management System v2.0");
                    ConsoleUI.ShowDetailRow("Framework", ".NET 8.0");
                    ConsoleUI.ShowDetailRow("Storage", "JSON File-Based Persistence");
                    ConsoleUI.ShowDetailRow("Current User", _currentUser);
                    ConsoleUI.ShowDetailRow("Date/Time", DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss"));
                    ConsoleUI.ShowDetailRow("Data Directory", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "hostel_data"));
                    ConsoleUI.Pause();
                    break;
                case "0": return;
            }
        }
    }
}
