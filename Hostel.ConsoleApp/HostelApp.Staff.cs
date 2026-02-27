using Hostel.Core.Entities;

namespace Hostel.ConsoleApp;

// Staff & Visitor Management Modules
public partial class HostelApp
{
    // ═══════════════════════════════════════════════════════════════
    //  STAFF MANAGEMENT
    // ═══════════════════════════════════════════════════════════════
    private async Task StaffMenuAsync()
    {
        while (true)
        {
            ConsoleUI.ShowMenu("👥 STAFF MANAGEMENT",
                ("1", "Add New Staff", "➕"),
                ("2", "List Active Staff", "📋"),
                ("3", "View Staff Details", "👁️"),
                ("4", "Edit Staff", "✏️"),
                ("5", "Filter by Role", "🔍"),
                ("6", "Deactivate Staff", "❌"),
                ("7", "Staff Summary", "📊"),
                ("0", "Back to Main Menu", "↩️"));

            var input = Console.ReadLine()?.Trim();
            switch (input)
            {
                case "1": await AddStaffAsync(); break;
                case "2": await ListStaffAsync(); break;
                case "3": await ViewStaffDetailsAsync(); break;
                case "4": await EditStaffAsync(); break;
                case "5": await FilterStaffByRoleAsync(); break;
                case "6": await DeactivateStaffAsync(); break;
                case "7": await StaffSummaryAsync(); break;
                case "0": return;
                default: ConsoleUI.ShowError("Invalid option!"); ConsoleUI.Pause(); break;
            }
        }
    }

    private async Task AddStaffAsync()
    {
        ConsoleUI.ShowHeader("➕ ADD NEW STAFF");
        var staff = new Staff
        {
            FullName = ConsoleUI.ReadInput("Full Name"),
            Phone = ConsoleUI.ReadInput("Phone"),
            Email = ConsoleUI.ReadInput("Email"),
            CNIC = ConsoleUI.ReadInput("CNIC"),
            Role = ConsoleUI.ReadEnum<StaffRole>("Role"),
            Salary = ConsoleUI.ReadDecimal("Monthly Salary (Rs.)"),
            Shift = ConsoleUI.ReadInput("Shift (Day/Night/Rotating)")
        };
        if (string.IsNullOrEmpty(staff.FullName)) { ConsoleUI.ShowError("Name is required!"); ConsoleUI.Pause(); return; }
        if (string.IsNullOrEmpty(staff.Shift)) staff.Shift = "Day";

        await _staff.AddStaffAsync(staff);
        await _audit.LogActionAsync("Staff", "Add", _currentUser, $"Added {staff.FullName} as {staff.Role}");
        ConsoleUI.ShowSuccess($"Staff '{staff.FullName}' added! (ID: {staff.Id})");
        ConsoleUI.Pause();
    }

    private async Task ListStaffAsync()
    {
        ConsoleUI.ShowHeader("📋 ACTIVE STAFF");
        var staff = await _staff.GetActiveStaffAsync();
        var rows = staff.Select(s => new[] {
            s.Id.ToString(), s.FullName, s.Role.ToString(), s.Phone, s.Shift, $"Rs. {s.Salary:N0}"
        }).ToList();
        ConsoleUI.ShowTable(new[] { "ID", "Name", "Role", "Phone", "Shift", "Salary" }, rows);
        ConsoleUI.Pause();
    }

    private async Task ViewStaffDetailsAsync()
    {
        ConsoleUI.ShowHeader("👁️ STAFF DETAILS");
        var id = ConsoleUI.ReadInt("Staff ID");
        var s = await _staff.GetStaffByIdAsync(id);
        if (s == null) { ConsoleUI.ShowError("Staff not found!"); ConsoleUI.Pause(); return; }
        ConsoleUI.ShowDetailRow("ID", s.Id.ToString());
        ConsoleUI.ShowDetailRow("Name", s.FullName);
        ConsoleUI.ShowDetailRow("Role", s.Role.ToString());
        ConsoleUI.ShowDetailRow("CNIC", s.CNIC);
        ConsoleUI.ShowDetailRow("Phone", s.Phone);
        ConsoleUI.ShowDetailRow("Email", s.Email);
        ConsoleUI.ShowDetailRow("Shift", s.Shift);
        ConsoleUI.ShowDetailRow("Salary", $"Rs. {s.Salary:N0}");
        ConsoleUI.ShowDetailRow("Join Date", s.JoinDate.ToString("dd-MMM-yyyy"));
        ConsoleUI.ShowDetailRow("Status", s.IsActive ? "✅ Active" : "❌ Inactive");
        ConsoleUI.Pause();
    }

    private async Task EditStaffAsync()
    {
        ConsoleUI.ShowHeader("✏️ EDIT STAFF");
        var id = ConsoleUI.ReadInt("Staff ID");
        var s = await _staff.GetStaffByIdAsync(id);
        if (s == null) { ConsoleUI.ShowError("Staff not found!"); ConsoleUI.Pause(); return; }

        ConsoleUI.ShowInfo($"Editing: {s.FullName} — press Enter to keep current value");
        var name = ConsoleUI.ReadInput($"Name [{s.FullName}]");
        if (!string.IsNullOrEmpty(name)) s.FullName = name;
        var ph = ConsoleUI.ReadInput($"Phone [{s.Phone}]");
        if (!string.IsNullOrEmpty(ph)) s.Phone = ph;
        var em = ConsoleUI.ReadInput($"Email [{s.Email}]");
        if (!string.IsNullOrEmpty(em)) s.Email = em;
        var salStr = ConsoleUI.ReadInput($"Salary [{s.Salary:N0}]");
        if (decimal.TryParse(salStr, out var sal)) s.Salary = sal;
        var shift = ConsoleUI.ReadInput($"Shift [{s.Shift}]");
        if (!string.IsNullOrEmpty(shift)) s.Shift = shift;

        await _staff.UpdateStaffAsync(s);
        await _audit.LogActionAsync("Staff", "Edit", _currentUser, $"Updated {s.FullName}");
        ConsoleUI.ShowSuccess("Staff updated!");
        ConsoleUI.Pause();
    }

    private async Task FilterStaffByRoleAsync()
    {
        ConsoleUI.ShowHeader("🔍 FILTER STAFF BY ROLE");
        var role = ConsoleUI.ReadEnum<StaffRole>("Select Role");
        var staff = await _staff.GetStaffByRoleAsync(role);
        var rows = staff.Select(s => new[] {
            s.Id.ToString(), s.FullName, s.Phone, s.Shift, $"Rs. {s.Salary:N0}"
        }).ToList();
        ConsoleUI.ShowTable(new[] { "ID", "Name", "Phone", "Shift", "Salary" }, rows);
        ConsoleUI.Pause();
    }

    private async Task DeactivateStaffAsync()
    {
        ConsoleUI.ShowHeader("❌ DEACTIVATE STAFF");
        var id = ConsoleUI.ReadInt("Staff ID");
        var s = await _staff.GetStaffByIdAsync(id);
        if (s == null) { ConsoleUI.ShowError("Staff not found!"); ConsoleUI.Pause(); return; }
        if (ConsoleUI.ReadConfirm($"Deactivate {s.FullName}?"))
        {
            await _staff.DeactivateStaffAsync(id);
            await _audit.LogActionAsync("Staff", "Deactivate", _currentUser, $"Deactivated {s.FullName}");
            ConsoleUI.ShowSuccess("Staff deactivated.");
        }
        ConsoleUI.Pause();
    }

    private async Task StaffSummaryAsync()
    {
        ConsoleUI.ShowHeader("📊 STAFF SUMMARY");
        var all = await _staff.GetAllStaffAsync();
        var active = all.Where(s => s.IsActive).ToList();

        ConsoleUI.ShowDetailRow("Total Staff", all.Count.ToString());
        ConsoleUI.ShowDetailRow("Active", active.Count.ToString());
        ConsoleUI.ShowDetailRow("Inactive", all.Count(s => !s.IsActive).ToString());
        ConsoleUI.ShowDetailRow("Total Salary Bill", $"Rs. {active.Sum(s => s.Salary):N0}/month");

        var roleData = new Dictionary<string, double>();
        foreach (StaffRole r in Enum.GetValues<StaffRole>())
        {
            var count = active.Count(s => s.Role == r);
            if (count > 0) roleData[r.ToString()] = count;
        }
        if (roleData.Count > 0)
            ConsoleUI.ShowBarChart("Staff by Role", roleData, ConsoleColor.Magenta);
        ConsoleUI.Pause();
    }

    // ═══════════════════════════════════════════════════════════════
    //  VISITOR LOG
    // ═══════════════════════════════════════════════════════════════
    private async Task VisitorMenuAsync()
    {
        while (true)
        {
            ConsoleUI.ShowMenu("🚪 VISITOR LOG",
                ("1", "Check In Visitor", "➕"),
                ("2", "Check Out Visitor", "🚶"),
                ("3", "Currently Checked-In", "📋"),
                ("4", "All Visitors (History)", "📑"),
                ("5", "Visitors by Date", "📅"),
                ("6", "Visitors by Student", "👤"),
                ("0", "Back to Main Menu", "↩️"));

            var input = Console.ReadLine()?.Trim();
            switch (input)
            {
                case "1": await CheckInVisitorAsync(); break;
                case "2": await CheckOutVisitorAsync(); break;
                case "3": await ActiveVisitorsAsync(); break;
                case "4": await AllVisitorsAsync(); break;
                case "5": await VisitorsByDateAsync(); break;
                case "6": await VisitorsByStudentAsync(); break;
                case "0": return;
                default: ConsoleUI.ShowError("Invalid option!"); ConsoleUI.Pause(); break;
            }
        }
    }

    private async Task CheckInVisitorAsync()
    {
        ConsoleUI.ShowHeader("➕ CHECK IN VISITOR");
        var studentId = ConsoleUI.ReadInt("Visiting Student ID");
        var student = await _students.GetStudentByIdAsync(studentId);
        if (student == null) { ConsoleUI.ShowError("Student not found!"); ConsoleUI.Pause(); return; }

        ConsoleUI.ShowInfo($"Visiting: {student.FullName} (Room: {student.RoomNumber ?? "N/A"})");

        var visitor = new Visitor
        {
            VisitorName = ConsoleUI.ReadInput("Visitor Name"),
            CNIC = ConsoleUI.ReadInput("Visitor CNIC"),
            Phone = ConsoleUI.ReadInput("Visitor Phone"),
            Relationship = ConsoleUI.ReadInput("Relationship (e.g. Father, Friend)"),
            StudentId = studentId,
            StudentName = student.FullName,
            Purpose = ConsoleUI.ReadInput("Purpose of Visit")
        };

        await _visitors.CheckInVisitorAsync(visitor);
        await _audit.LogActionAsync("Visitor", "Check In", _currentUser,
            $"{visitor.VisitorName} visiting {student.FullName} (Pass: {visitor.PassNumber})");

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n    ┌─────────────────────────────────┐");
        Console.WriteLine($"    │ 🎫 VISITOR PASS: {visitor.PassNumber,-15}│");
        Console.WriteLine($"    │ Visitor: {visitor.VisitorName,-22}│");
        Console.WriteLine($"    │ Student: {student.FullName,-22}│");
        Console.WriteLine($"    │ Time: {visitor.CheckInTime:HH:mm dd-MMM-yyyy,-19}│");
        Console.WriteLine("    └─────────────────────────────────┘");
        Console.ResetColor();
        ConsoleUI.Pause();
    }

    private async Task CheckOutVisitorAsync()
    {
        ConsoleUI.ShowHeader("🚶 CHECK OUT VISITOR");
        var active = await _visitors.GetActiveVisitorsAsync();
        if (active.Count == 0) { ConsoleUI.ShowWarning("No visitors currently checked in."); ConsoleUI.Pause(); return; }
        var rows = active.Select(v => new[] {
            v.Id.ToString(), v.VisitorName, v.StudentName, v.CheckInTime.ToString("HH:mm"), v.PassNumber
        }).ToList();
        ConsoleUI.ShowTable(new[] { "ID", "Visitor", "Visiting", "Check In", "Pass #" }, rows);

        var id = ConsoleUI.ReadInt("Visitor ID to check out");
        try
        {
            await _visitors.CheckOutVisitorAsync(id);
            await _audit.LogActionAsync("Visitor", "Check Out", _currentUser, $"Visitor #{id} checked out");
            ConsoleUI.ShowSuccess("Visitor checked out!");
        }
        catch (Exception ex) { ConsoleUI.ShowError(ex.Message); }
        ConsoleUI.Pause();
    }

    private async Task ActiveVisitorsAsync()
    {
        ConsoleUI.ShowHeader("📋 CURRENTLY CHECKED-IN VISITORS");
        var visitors = await _visitors.GetActiveVisitorsAsync();
        var rows = visitors.Select(v => new[] {
            v.Id.ToString(), v.VisitorName, v.Phone, v.StudentName, v.Purpose,
            v.CheckInTime.ToString("HH:mm"), v.PassNumber
        }).ToList();
        ConsoleUI.ShowTable(new[] { "ID", "Visitor", "Phone", "Visiting", "Purpose", "Since", "Pass" }, rows);
        ConsoleUI.Pause();
    }

    private async Task AllVisitorsAsync()
    {
        ConsoleUI.ShowHeader("📑 ALL VISITOR HISTORY");
        var visitors = await _visitors.GetAllVisitorsAsync();
        var rows = visitors.Select(v => new[] {
            v.Id.ToString(), v.VisitorName, v.StudentName, v.Purpose,
            v.CheckInTime.ToString("dd/MM HH:mm"),
            v.CheckOutTime?.ToString("HH:mm") ?? "—",
            v.Status.ToString()
        }).ToList();
        ConsoleUI.ShowTable(new[] { "ID", "Visitor", "Visiting", "Purpose", "Check In", "Check Out", "Status" }, rows);
        ConsoleUI.Pause();
    }

    private async Task VisitorsByDateAsync()
    {
        ConsoleUI.ShowHeader("📅 VISITORS BY DATE");
        var dateStr = ConsoleUI.ReadInput("Date (dd/MM/yyyy) or press Enter for today");
        var date = string.IsNullOrEmpty(dateStr) ? DateTime.Today : DateTime.ParseExact(dateStr, "dd/MM/yyyy", null);
        var visitors = await _visitors.GetVisitorsByDateAsync(date);
        var rows = visitors.Select(v => new[] {
            v.Id.ToString(), v.VisitorName, v.StudentName, v.CheckInTime.ToString("HH:mm"),
            v.CheckOutTime?.ToString("HH:mm") ?? "—", v.Status.ToString()
        }).ToList();
        ConsoleUI.ShowTable(new[] { "ID", "Visitor", "Visiting", "In", "Out", "Status" }, rows);
        ConsoleUI.Pause();
    }

    private async Task VisitorsByStudentAsync()
    {
        ConsoleUI.ShowHeader("👤 VISITORS BY STUDENT");
        var studentId = ConsoleUI.ReadInt("Student ID");
        var visitors = await _visitors.GetVisitorsByStudentAsync(studentId);
        var rows = visitors.Select(v => new[] {
            v.Id.ToString(), v.VisitorName, v.Relationship, v.Purpose,
            v.CheckInTime.ToString("dd/MM HH:mm"), v.Status.ToString()
        }).ToList();
        ConsoleUI.ShowTable(new[] { "ID", "Visitor", "Relation", "Purpose", "Date/Time", "Status" }, rows);
        ConsoleUI.Pause();
    }
}
