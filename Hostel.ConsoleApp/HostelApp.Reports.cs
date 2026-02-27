using System.Text;
using Hostel.Core.Entities;

namespace Hostel.ConsoleApp;

// Reports, Data Export, and Audit Log Modules
public partial class HostelApp
{
    // ═══════════════════════════════════════════════════════════════
    //  REPORTS & EXPORT
    // ═══════════════════════════════════════════════════════════════
    private async Task ReportsMenuAsync()
    {
        while (true)
        {
            ConsoleUI.ShowMenu("📁 REPORTS & DATA EXPORT",
                ("1", "Export Student List to CSV", "👨‍🎓"),
                ("2", "Export Room Details to CSV", "🏠"),
                ("3", "Export Payment Records to CSV", "💰"),
                ("4", "Export Complaint Records to CSV", "📋"),
                ("5", "Export Staff List to CSV", "👥"),
                ("6", "Export Full Hostel Report (TXT)", "📊"),
                ("7", "Export Visitor Log to CSV", "🚪"),
                ("0", "Back to Main Menu", "↩️"));

            var input = Console.ReadLine()?.Trim();
            switch (input)
            {
                case "1": await ExportStudentsCsvAsync(); break;
                case "2": await ExportRoomsCsvAsync(); break;
                case "3": await ExportPaymentsCsvAsync(); break;
                case "4": await ExportComplaintsCsvAsync(); break;
                case "5": await ExportStaffCsvAsync(); break;
                case "6": await ExportFullReportAsync(); break;
                case "7": await ExportVisitorsCsvAsync(); break;
                case "0": return;
                default: ConsoleUI.ShowError("Invalid option!"); ConsoleUI.Pause(); break;
            }
        }
    }

    private string GetExportDir()
    {
        var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "hostel_exports");
        Directory.CreateDirectory(dir);
        return dir;
    }

    private async Task ExportStudentsCsvAsync()
    {
        ConsoleUI.ShowHeader("📁 EXPORT STUDENTS");
        var students = await _students.GetAllStudentsAsync();
        var sb = new StringBuilder();
        sb.AppendLine("ID,FirstName,LastName,RegNumber,CNIC,Phone,Email,Department,Room,Status,JoinDate");
        foreach (var s in students)
            sb.AppendLine($"{s.Id},{s.FirstName},{s.LastName},{s.RegistrationNumber},{s.CNIC},{s.Phone},{s.Email},{s.Department},{s.RoomNumber ?? "N/A"},{(s.IsActive ? "Active" : "Inactive")},{s.JoinDate:dd-MMM-yyyy}");

        var file = Path.Combine(GetExportDir(), $"students_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        await File.WriteAllTextAsync(file, sb.ToString());
        await _audit.LogActionAsync("Export", "Students CSV", _currentUser, file);
        ConsoleUI.ShowSuccess($"Exported {students.Count} students to:\n    {file}");
        ConsoleUI.Pause();
    }

    private async Task ExportRoomsCsvAsync()
    {
        ConsoleUI.ShowHeader("📁 EXPORT ROOMS");
        var rooms = await _rooms.GetAllRoomsAsync();
        var sb = new StringBuilder();
        sb.AppendLine("ID,RoomNumber,Floor,Type,Capacity,Occupancy,Rent,AC,AttBath,Status");
        foreach (var r in rooms)
            sb.AppendLine($"{r.Id},{r.RoomNumber},{r.Floor},{r.RoomType},{r.Capacity},{r.CurrentOccupancy},{r.MonthlyRent},{(r.HasAC ? "Yes" : "No")},{(r.HasAttachedBath ? "Yes" : "No")},{(r.IsFull ? "Full" : "Available")}");

        var file = Path.Combine(GetExportDir(), $"rooms_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        await File.WriteAllTextAsync(file, sb.ToString());
        await _audit.LogActionAsync("Export", "Rooms CSV", _currentUser, file);
        ConsoleUI.ShowSuccess($"Exported {rooms.Count} rooms to:\n    {file}");
        ConsoleUI.Pause();
    }

    private async Task ExportPaymentsCsvAsync()
    {
        ConsoleUI.ShowHeader("📁 EXPORT PAYMENTS");
        var payments = await _payments.GetAllPaymentsAsync();
        var sb = new StringBuilder();
        sb.AppendLine("ID,Receipt,StudentID,StudentName,Amount,Month,Year,Method,Status,Date,Remarks");
        foreach (var p in payments)
            sb.AppendLine($"{p.Id},{p.ReceiptNumber},{p.StudentId},{p.StudentName},{p.Amount},{p.Month},{p.Year},{p.Method},{p.Status},{p.PaymentDate:dd-MMM-yyyy},{p.Remarks}");

        var file = Path.Combine(GetExportDir(), $"payments_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        await File.WriteAllTextAsync(file, sb.ToString());
        await _audit.LogActionAsync("Export", "Payments CSV", _currentUser, file);
        ConsoleUI.ShowSuccess($"Exported {payments.Count} payments to:\n    {file}");
        ConsoleUI.Pause();
    }

    private async Task ExportComplaintsCsvAsync()
    {
        ConsoleUI.ShowHeader("📁 EXPORT COMPLAINTS");
        var complaints = await _complaints.GetAllComplaintsAsync();
        var sb = new StringBuilder();
        sb.AppendLine("ID,StudentName,Title,Category,Priority,Status,CreatedAt,AssignedTo,ResolvedAt");
        foreach (var c in complaints)
            sb.AppendLine($"{c.Id},{c.StudentName},{c.Title},{c.Category},{c.Priority},{c.Status},{c.CreatedAt:dd-MMM-yyyy},{c.AssignedStaffName ?? "N/A"},{c.ResolvedAt?.ToString("dd-MMM-yyyy") ?? "N/A"}");

        var file = Path.Combine(GetExportDir(), $"complaints_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        await File.WriteAllTextAsync(file, sb.ToString());
        await _audit.LogActionAsync("Export", "Complaints CSV", _currentUser, file);
        ConsoleUI.ShowSuccess($"Exported {complaints.Count} complaints to:\n    {file}");
        ConsoleUI.Pause();
    }

    private async Task ExportStaffCsvAsync()
    {
        ConsoleUI.ShowHeader("📁 EXPORT STAFF");
        var staff = await _staff.GetAllStaffAsync();
        var sb = new StringBuilder();
        sb.AppendLine("ID,Name,CNIC,Phone,Email,Role,Salary,Shift,JoinDate,Status");
        foreach (var s in staff)
            sb.AppendLine($"{s.Id},{s.FullName},{s.CNIC},{s.Phone},{s.Email},{s.Role},{s.Salary},{s.Shift},{s.JoinDate:dd-MMM-yyyy},{(s.IsActive ? "Active" : "Inactive")}");

        var file = Path.Combine(GetExportDir(), $"staff_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        await File.WriteAllTextAsync(file, sb.ToString());
        await _audit.LogActionAsync("Export", "Staff CSV", _currentUser, file);
        ConsoleUI.ShowSuccess($"Exported {staff.Count} staff to:\n    {file}");
        ConsoleUI.Pause();
    }

    private async Task ExportVisitorsCsvAsync()
    {
        ConsoleUI.ShowHeader("📁 EXPORT VISITORS");
        var visitors = await _visitors.GetAllVisitorsAsync();
        var sb = new StringBuilder();
        sb.AppendLine("ID,VisitorName,CNIC,Phone,Relationship,StudentName,Purpose,CheckIn,CheckOut,Status,Pass");
        foreach (var v in visitors)
            sb.AppendLine($"{v.Id},{v.VisitorName},{v.CNIC},{v.Phone},{v.Relationship},{v.StudentName},{v.Purpose},{v.CheckInTime:dd-MMM-yyyy HH:mm},{v.CheckOutTime?.ToString("dd-MMM-yyyy HH:mm") ?? "N/A"},{v.Status},{v.PassNumber}");

        var file = Path.Combine(GetExportDir(), $"visitors_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        await File.WriteAllTextAsync(file, sb.ToString());
        await _audit.LogActionAsync("Export", "Visitors CSV", _currentUser, file);
        ConsoleUI.ShowSuccess($"Exported {visitors.Count} visitors to:\n    {file}");
        ConsoleUI.Pause();
    }

    private async Task ExportFullReportAsync()
    {
        ConsoleUI.ShowHeader("📊 GENERATING FULL HOSTEL REPORT");
        ConsoleUI.ShowLoading("Compiling data");

        var sb = new StringBuilder();
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine("          HOSTEL MANAGEMENT SYSTEM — COMPLETE REPORT");
        sb.AppendLine($"          Generated: {DateTime.Now:dd-MMM-yyyy HH:mm:ss}");
        sb.AppendLine($"          Generated By: {_currentUser}");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine();

        // Students
        var students = await _students.GetAllStudentsAsync();
        var activeStudents = students.Count(s => s.IsActive);
        sb.AppendLine("── STUDENTS ──────────────────────────────────────────────────");
        sb.AppendLine($"  Total Students    : {students.Count}");
        sb.AppendLine($"  Active Students   : {activeStudents}");
        sb.AppendLine($"  Inactive Students : {students.Count - activeStudents}");
        sb.AppendLine($"  Without Room      : {students.Count(s => s.IsActive && !s.RoomId.HasValue)}");
        sb.AppendLine();

        // Rooms
        var rooms = await _rooms.GetAllRoomsAsync();
        var totalCap = rooms.Sum(r => r.Capacity);
        var totalOcc = rooms.Sum(r => r.CurrentOccupancy);
        sb.AppendLine("── ROOMS ─────────────────────────────────────────────────────");
        sb.AppendLine($"  Total Rooms       : {rooms.Count}");
        sb.AppendLine($"  Total Capacity    : {totalCap} beds");
        sb.AppendLine($"  Current Occupancy : {totalOcc} beds");
        sb.AppendLine($"  Available Beds    : {totalCap - totalOcc}");
        sb.AppendLine($"  Occupancy Rate    : {(totalCap > 0 ? (double)totalOcc / totalCap * 100 : 0):F1}%");
        sb.AppendLine();

        // Payments
        var totalRevenue = await _payments.GetTotalRevenueAsync();
        var allPayments = await _payments.GetAllPaymentsAsync();
        var monthRev = await _payments.GetRevenueByMonthAsync(DateTime.Now.Month, DateTime.Now.Year);
        sb.AppendLine("── FINANCE ───────────────────────────────────────────────────");
        sb.AppendLine($"  Total Revenue     : Rs. {totalRevenue:N0}");
        sb.AppendLine($"  This Month        : Rs. {monthRev:N0}");
        sb.AppendLine($"  Total Payments    : {allPayments.Count}");
        sb.AppendLine($"  Pending Payments  : {allPayments.Count(p => p.Status == PaymentStatus.Pending)}");
        sb.AppendLine();

        // Complaints
        var complaints = await _complaints.GetAllComplaintsAsync();
        sb.AppendLine("── COMPLAINTS ────────────────────────────────────────────────");
        sb.AppendLine($"  Total Complaints  : {complaints.Count}");
        sb.AppendLine($"  Open              : {complaints.Count(c => c.Status == ComplaintStatus.Open)}");
        sb.AppendLine($"  In Progress       : {complaints.Count(c => c.Status == ComplaintStatus.InProgress)}");
        sb.AppendLine($"  Resolved          : {complaints.Count(c => c.Status == ComplaintStatus.Resolved)}");
        sb.AppendLine();

        // Staff
        var allStaff = await _staff.GetAllStaffAsync();
        var activeStaff = allStaff.Where(s => s.IsActive).ToList();
        sb.AppendLine("── STAFF ─────────────────────────────────────────────────────");
        sb.AppendLine($"  Total Staff       : {allStaff.Count}");
        sb.AppendLine($"  Active Staff      : {activeStaff.Count}");
        sb.AppendLine($"  Monthly Salary    : Rs. {activeStaff.Sum(s => s.Salary):N0}");
        sb.AppendLine();

        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine("                        END OF REPORT");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");

        var file = Path.Combine(GetExportDir(), $"full_report_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
        await File.WriteAllTextAsync(file, sb.ToString());
        await _audit.LogActionAsync("Export", "Full Report", _currentUser, file);

        // Also display on console
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(sb.ToString());
        Console.ResetColor();
        ConsoleUI.ShowSuccess($"Report saved to:\n    {file}");
        ConsoleUI.Pause();
    }

    // ═══════════════════════════════════════════════════════════════
    //  AUDIT LOG
    // ═══════════════════════════════════════════════════════════════
    private async Task AuditLogMenuAsync()
    {
        while (true)
        {
            ConsoleUI.ShowMenu("📝 AUDIT LOG",
                ("1", "View Recent Activity (Last 20)", "🕐"),
                ("2", "View All Activity", "📋"),
                ("3", "Filter by Module", "🔍"),
                ("0", "Back to Main Menu", "↩️"));

            var input = Console.ReadLine()?.Trim();
            switch (input)
            {
                case "1": await ViewRecentAuditAsync(); break;
                case "2": await ViewAllAuditAsync(); break;
                case "3": await FilterAuditByModuleAsync(); break;
                case "0": return;
                default: ConsoleUI.ShowError("Invalid option!"); ConsoleUI.Pause(); break;
            }
        }
    }

    private async Task ViewRecentAuditAsync()
    {
        ConsoleUI.ShowHeader("🕐 RECENT ACTIVITY");
        var logs = await _audit.GetRecentLogsAsync(20);
        ShowAuditTable(logs);
        ConsoleUI.Pause();
    }

    private async Task ViewAllAuditAsync()
    {
        ConsoleUI.ShowHeader("📋 ALL AUDIT LOGS");
        var logs = await _audit.GetAllLogsAsync();
        ShowAuditTable(logs);
        ConsoleUI.Pause();
    }

    private async Task FilterAuditByModuleAsync()
    {
        ConsoleUI.ShowHeader("🔍 AUDIT BY MODULE");
        ConsoleUI.ShowInfo("Available modules: Auth, Student, Room, Payment, Fee, Complaint, Staff, Visitor, Attendance, Mess, Notice, Export, Admin");
        var module = ConsoleUI.ReadInput("Module name");
        var logs = await _audit.GetLogsByModuleAsync(module);
        ShowAuditTable(logs);
        ConsoleUI.Pause();
    }

    private void ShowAuditTable(IReadOnlyList<AuditLog> logs)
    {
        var rows = logs.Select(l => new[] {
            l.Id.ToString(), l.Timestamp.ToString("dd/MM HH:mm"), l.Module, l.Action, l.PerformedBy, l.Details
        }).ToList();
        ConsoleUI.ShowTable(new[] { "ID", "Time", "Module", "Action", "User", "Details" }, rows);
    }
}
