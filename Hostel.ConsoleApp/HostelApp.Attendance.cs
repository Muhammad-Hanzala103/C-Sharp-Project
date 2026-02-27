using Hostel.Core.Entities;

namespace Hostel.ConsoleApp;

// Attendance, Mess Menu, and Notice Board Modules
public partial class HostelApp
{
    // ═══════════════════════════════════════════════════════════════
    //  ATTENDANCE TRACKING
    // ═══════════════════════════════════════════════════════════════
    private async Task AttendanceMenuAsync()
    {
        while (true)
        {
            ConsoleUI.ShowMenu("📅 ATTENDANCE TRACKING",
                ("1", "Mark Attendance", "✏️"),
                ("2", "View Today's Attendance", "📋"),
                ("3", "View by Date", "📅"),
                ("4", "Student Attendance Report", "👤"),
                ("5", "Daily Stats", "📊"),
                ("0", "Back to Main Menu", "↩️"));

            var input = Console.ReadLine()?.Trim();
            switch (input)
            {
                case "1": await MarkAttendanceAsync(); break;
                case "2": await ViewTodayAttendanceAsync(); break;
                case "3": await ViewAttendanceByDateAsync(); break;
                case "4": await StudentAttendanceReportAsync(); break;
                case "5": await DailyAttendanceStatsAsync(); break;
                case "0": return;
                default: ConsoleUI.ShowError("Invalid option!"); ConsoleUI.Pause(); break;
            }
        }
    }

    private async Task MarkAttendanceAsync()
    {
        ConsoleUI.ShowHeader("✏️ MARK ATTENDANCE");
        var students = await _students.GetActiveStudentsAsync();
        if (students.Count == 0) { ConsoleUI.ShowWarning("No active students!"); ConsoleUI.Pause(); return; }

        ConsoleUI.ShowInfo($"Marking attendance for {DateTime.Today:dd-MMM-yyyy}");
        ConsoleUI.ShowInfo("For each student, enter: 1=Present, 2=Absent, 3=Leave, 4=Late");
        Console.WriteLine();

        int marked = 0;
        foreach (var s in students)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"    {s.Id,-5} {s.FullName,-25} [{s.RoomNumber ?? "N/A",-8}] → ");
            Console.ResetColor();
            var statusStr = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(statusStr)) continue;

            if (int.TryParse(statusStr, out var val) && Enum.IsDefined(typeof(AttendanceStatus), val))
            {
                await _attendance.MarkAttendanceAsync(new Attendance
                {
                    StudentId = s.Id,
                    StudentName = s.FullName,
                    Date = DateTime.Today,
                    Status = (AttendanceStatus)val,
                    Remarks = ""
                });
                marked++;
            }
        }

        await _audit.LogActionAsync("Attendance", "Mark", _currentUser, $"Marked {marked} students for {DateTime.Today:dd-MMM-yyyy}");
        ConsoleUI.ShowSuccess($"Attendance marked for {marked} students!");
        ConsoleUI.Pause();
    }

    private async Task ViewTodayAttendanceAsync()
    {
        ConsoleUI.ShowHeader($"📋 TODAY'S ATTENDANCE ({DateTime.Today:dd-MMM-yyyy})");
        var records = await _attendance.GetAttendanceByDateAsync(DateTime.Today);
        ShowAttendanceTable(records);
        ConsoleUI.Pause();
    }

    private async Task ViewAttendanceByDateAsync()
    {
        ConsoleUI.ShowHeader("📅 ATTENDANCE BY DATE");
        var dateStr = ConsoleUI.ReadInput("Date (dd/MM/yyyy)");
        if (!DateTime.TryParseExact(dateStr, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var date))
        {
            ConsoleUI.ShowError("Invalid date format!"); ConsoleUI.Pause(); return;
        }
        var records = await _attendance.GetAttendanceByDateAsync(date);
        ShowAttendanceTable(records);
        ConsoleUI.Pause();
    }

    private async Task StudentAttendanceReportAsync()
    {
        ConsoleUI.ShowHeader("👤 STUDENT ATTENDANCE REPORT");
        var studentId = ConsoleUI.ReadInt("Student ID");
        var student = await _students.GetStudentByIdAsync(studentId);
        if (student == null) { ConsoleUI.ShowError("Student not found!"); ConsoleUI.Pause(); return; }

        ConsoleUI.ShowInfo($"Attendance report for: {student.FullName}");
        var records = await _attendance.GetAttendanceByStudentAsync(studentId);
        ShowAttendanceTable(records);

        if (records.Count > 0)
        {
            var pct = await _attendance.GetStudentAttendancePercentageAsync(studentId);
            ConsoleUI.ShowSeparator();
            ConsoleUI.ShowDetailRow("Total Days", records.Count.ToString());
            ConsoleUI.ShowDetailRow("Present", records.Count(r => r.Status == AttendanceStatus.Present).ToString());
            ConsoleUI.ShowDetailRow("Absent", records.Count(r => r.Status == AttendanceStatus.Absent).ToString());
            ConsoleUI.ShowDetailRow("Leave", records.Count(r => r.Status == AttendanceStatus.Leave).ToString());
            ConsoleUI.ShowProgressBar("Attendance Rate", pct, pct >= 75 ? ConsoleColor.Green : ConsoleColor.Red);
        }
        ConsoleUI.Pause();
    }

    private async Task DailyAttendanceStatsAsync()
    {
        ConsoleUI.ShowHeader("📊 DAILY ATTENDANCE STATS");
        var dateStr = ConsoleUI.ReadInput("Date (dd/MM/yyyy) or Enter for today");
        var date = string.IsNullOrEmpty(dateStr) ? DateTime.Today :
            DateTime.TryParseExact(dateStr, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var d) ? d : DateTime.Today;

        var (present, absent, leave) = await _attendance.GetAttendanceStatsAsync(date);
        var total = present + absent + leave;

        ConsoleUI.ShowDetailRow("Date", date.ToString("dd-MMM-yyyy"));
        ConsoleUI.ShowDetailRow("Total Recorded", total.ToString());
        ConsoleUI.ShowSeparator();

        var data = new Dictionary<string, double>();
        if (present > 0) data["Present"] = present;
        if (absent > 0) data["Absent"] = absent;
        if (leave > 0) data["Leave"] = leave;

        if (data.Count > 0) ConsoleUI.ShowBarChart("Attendance Breakdown", data, ConsoleColor.Green);
        if (total > 0)
            ConsoleUI.ShowProgressBar("Attendance Rate", (double)present / total * 100);
        ConsoleUI.Pause();
    }

    private void ShowAttendanceTable(IReadOnlyList<Attendance> records)
    {
        var rows = records.Select(a => new[] {
            a.Id.ToString(), a.StudentName, a.Date.ToString("dd/MM/yyyy"),
            a.Status.ToString(), a.Remarks
        }).ToList();
        ConsoleUI.ShowTable(new[] { "ID", "Student", "Date", "Status", "Remarks" }, rows);
    }

    // ═══════════════════════════════════════════════════════════════
    //  MESS MENU MANAGEMENT
    // ═══════════════════════════════════════════════════════════════
    private async Task MessMenuAsync()
    {
        while (true)
        {
            ConsoleUI.ShowMenu("🍽️ MESS MENU MANAGEMENT",
                ("1", "Add Menu Item", "➕"),
                ("2", "View Today's Menu", "📋"),
                ("3", "View Full Week Menu", "📅"),
                ("4", "View Menu by Day", "🔍"),
                ("5", "Edit Menu Item", "✏️"),
                ("6", "Delete Menu Item", "🗑️"),
                ("0", "Back to Main Menu", "↩️"));

            var input = Console.ReadLine()?.Trim();
            switch (input)
            {
                case "1": await AddMenuItemAsync(); break;
                case "2": await ViewTodayMenuAsync(); break;
                case "3": await ViewWeekMenuAsync(); break;
                case "4": await ViewMenuByDayAsync(); break;
                case "5": await EditMenuItemAsync(); break;
                case "6": await DeleteMenuItemAsync(); break;
                case "0": return;
                default: ConsoleUI.ShowError("Invalid option!"); ConsoleUI.Pause(); break;
            }
        }
    }

    private async Task AddMenuItemAsync()
    {
        ConsoleUI.ShowHeader("➕ ADD MENU ITEM");
        ConsoleUI.ShowInfo("Select the day of the week:");
        Console.ForegroundColor = ConsoleColor.Cyan;
        for (int i = 0; i < 7; i++)
            Console.WriteLine($"      [{i}] {(DayOfWeek)i}");
        Console.ResetColor();

        var dayVal = ConsoleUI.ReadInt("Day (0=Sunday ... 6=Saturday)", 0, 6);
        var menu = new MessMenu
        {
            Day = (DayOfWeek)dayVal,
            MealType = ConsoleUI.ReadEnum<MealType>("Meal Type"),
            Items = ConsoleUI.ReadInput("Menu Items (comma separated)")
        };

        await _mess.AddMenuItemAsync(menu);
        await _audit.LogActionAsync("Mess", "Add Menu", _currentUser, $"{menu.Day} - {menu.MealType}: {menu.Items}");
        ConsoleUI.ShowSuccess("Menu item added!");
        ConsoleUI.Pause();
    }

    private async Task ViewTodayMenuAsync()
    {
        ConsoleUI.ShowHeader($"📋 TODAY'S MENU ({DateTime.Now.DayOfWeek})");
        var items = await _mess.GetMenuByDayAsync(DateTime.Now.DayOfWeek);
        ShowMessTable(items);
        ConsoleUI.Pause();
    }

    private async Task ViewWeekMenuAsync()
    {
        ConsoleUI.ShowHeader("📅 FULL WEEK MENU");
        var items = await _mess.GetFullWeekMenuAsync();

        if (items.Count == 0) { ConsoleUI.ShowWarning("No menu items found."); ConsoleUI.Pause(); return; }

        // Group by day
        var grouped = items.GroupBy(m => m.Day).OrderBy(g => g.Key);
        foreach (var dayGroup in grouped)
        {
            ConsoleUI.ShowSubHeader($"📌 {dayGroup.Key}");
            foreach (var item in dayGroup.OrderBy(m => m.MealType))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"      {item.MealType,-12}");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($" → {item.Items}");
            }
        }
        Console.ResetColor();
        ConsoleUI.Pause();
    }

    private async Task ViewMenuByDayAsync()
    {
        ConsoleUI.ShowHeader("🔍 MENU BY DAY");
        var dayVal = ConsoleUI.ReadInt("Day (0=Sunday ... 6=Saturday)", 0, 6);
        var items = await _mess.GetMenuByDayAsync((DayOfWeek)dayVal);
        ShowMessTable(items);
        ConsoleUI.Pause();
    }

    private async Task EditMenuItemAsync()
    {
        ConsoleUI.ShowHeader("✏️ EDIT MENU ITEM");
        var id = ConsoleUI.ReadInt("Menu Item ID");
        var item = await _mess.GetMenuItemAsync(id);
        if (item == null) { ConsoleUI.ShowError("Item not found!"); ConsoleUI.Pause(); return; }

        var items = ConsoleUI.ReadInput($"Items [{item.Items}]");
        if (!string.IsNullOrEmpty(items)) item.Items = items;
        await _mess.UpdateMenuItemAsync(item);
        ConsoleUI.ShowSuccess("Menu item updated!");
        ConsoleUI.Pause();
    }

    private async Task DeleteMenuItemAsync()
    {
        ConsoleUI.ShowHeader("🗑️ DELETE MENU ITEM");
        var id = ConsoleUI.ReadInt("Menu Item ID");
        if (ConsoleUI.ReadConfirm("Delete this item?"))
        {
            await _mess.DeleteMenuItemAsync(id);
            ConsoleUI.ShowSuccess("Menu item deleted!");
        }
        ConsoleUI.Pause();
    }

    private void ShowMessTable(IReadOnlyList<MessMenu> items)
    {
        var rows = items.Select(m => new[] {
            m.Id.ToString(), m.Day.ToString(), m.MealType.ToString(), m.Items
        }).ToList();
        ConsoleUI.ShowTable(new[] { "ID", "Day", "Meal", "Items" }, rows);
    }

    // ═══════════════════════════════════════════════════════════════
    //  NOTICE BOARD
    // ═══════════════════════════════════════════════════════════════
    private async Task NoticeMenuAsync()
    {
        while (true)
        {
            ConsoleUI.ShowMenu("📢 NOTICE BOARD",
                ("1", "Post New Notice", "➕"),
                ("2", "View Active Notices", "📋"),
                ("3", "View All Notices", "📑"),
                ("4", "Edit Notice", "✏️"),
                ("5", "Deactivate Notice", "❌"),
                ("0", "Back to Main Menu", "↩️"));

            var input = Console.ReadLine()?.Trim();
            switch (input)
            {
                case "1": await PostNoticeAsync(); break;
                case "2": await ViewActiveNoticesAsync(); break;
                case "3": await ViewAllNoticesAsync(); break;
                case "4": await EditNoticeAsync(); break;
                case "5": await DeactivateNoticeAsync(); break;
                case "0": return;
                default: ConsoleUI.ShowError("Invalid option!"); ConsoleUI.Pause(); break;
            }
        }
    }

    private async Task PostNoticeAsync()
    {
        ConsoleUI.ShowHeader("➕ POST NEW NOTICE");
        var notice = new Notice
        {
            Title = ConsoleUI.ReadInput("Title"),
            Content = ConsoleUI.ReadInput("Content"),
            PostedBy = _currentUser,
            Priority = ConsoleUI.ReadEnum<NoticePriority>("Priority")
        };

        var daysStr = ConsoleUI.ReadInput("Expires in (days, or Enter for no expiry)");
        if (int.TryParse(daysStr, out var days) && days > 0)
            notice.ExpiresAt = DateTime.Now.AddDays(days);

        if (string.IsNullOrEmpty(notice.Title)) { ConsoleUI.ShowError("Title is required!"); ConsoleUI.Pause(); return; }

        await _notices.PostNoticeAsync(notice);
        await _audit.LogActionAsync("Notice", "Post", _currentUser, $"Notice: {notice.Title}");
        ConsoleUI.ShowSuccess($"Notice posted! (ID: {notice.Id})");
        ConsoleUI.Pause();
    }

    private async Task ViewActiveNoticesAsync()
    {
        ConsoleUI.ShowHeader("📋 ACTIVE NOTICES");
        var notices = await _notices.GetActiveNoticesAsync();

        if (notices.Count == 0) { ConsoleUI.ShowWarning("No active notices."); ConsoleUI.Pause(); return; }

        foreach (var n in notices)
        {
            var priorityColor = n.Priority switch
            {
                NoticePriority.Urgent => ConsoleColor.Red,
                NoticePriority.High => ConsoleColor.Yellow,
                NoticePriority.Medium => ConsoleColor.Cyan,
                _ => ConsoleColor.DarkGray
            };

            Console.ForegroundColor = priorityColor;
            Console.WriteLine($"\n    ┌─── [{n.Priority}] ───────────────────────────────────┐");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"    │ 📢 {n.Title}");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"    │ {n.Content}");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"    │ By: {n.PostedBy} | {n.PostedAt:dd-MMM-yyyy HH:mm}");
            if (n.ExpiresAt.HasValue) Console.Write($" | Expires: {n.ExpiresAt:dd-MMM}");
            Console.WriteLine();
            Console.ForegroundColor = priorityColor;
            Console.WriteLine("    └──────────────────────────────────────────────┘");
        }
        Console.ResetColor();
        ConsoleUI.Pause();
    }

    private async Task ViewAllNoticesAsync()
    {
        ConsoleUI.ShowHeader("📑 ALL NOTICES");
        var notices = await _notices.GetAllNoticesAsync();
        var rows = notices.Select(n => new[] {
            n.Id.ToString(), n.Title, n.PostedBy, n.PostedAt.ToString("dd/MM/yyyy"),
            n.Priority.ToString(), n.IsActive ? "✅" : "❌"
        }).ToList();
        ConsoleUI.ShowTable(new[] { "ID", "Title", "Posted By", "Date", "Priority", "Active" }, rows);
        ConsoleUI.Pause();
    }

    private async Task EditNoticeAsync()
    {
        ConsoleUI.ShowHeader("✏️ EDIT NOTICE");
        var id = ConsoleUI.ReadInt("Notice ID");
        var notices = await _notices.GetAllNoticesAsync();
        var notice = notices.FirstOrDefault(n => n.Id == id);
        if (notice == null) { ConsoleUI.ShowError("Notice not found!"); ConsoleUI.Pause(); return; }

        var title = ConsoleUI.ReadInput($"Title [{notice.Title}]");
        if (!string.IsNullOrEmpty(title)) notice.Title = title;
        var content = ConsoleUI.ReadInput($"Content [{notice.Content}]");
        if (!string.IsNullOrEmpty(content)) notice.Content = content;

        await _notices.UpdateNoticeAsync(notice);
        ConsoleUI.ShowSuccess("Notice updated!");
        ConsoleUI.Pause();
    }

    private async Task DeactivateNoticeAsync()
    {
        ConsoleUI.ShowHeader("❌ DEACTIVATE NOTICE");
        var id = ConsoleUI.ReadInt("Notice ID");
        if (ConsoleUI.ReadConfirm("Deactivate this notice?"))
        {
            try
            {
                await _notices.DeactivateNoticeAsync(id);
                ConsoleUI.ShowSuccess("Notice deactivated.");
            }
            catch (Exception ex) { ConsoleUI.ShowError(ex.Message); }
        }
        ConsoleUI.Pause();
    }
}
