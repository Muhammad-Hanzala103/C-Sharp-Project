using Hostel.Core.Entities;

namespace Hostel.ConsoleApp;

// Payment & Complaint Management Modules
public partial class HostelApp
{
    // ═══════════════════════════════════════════════════════════════
    //  FEE & PAYMENT MANAGEMENT
    // ═══════════════════════════════════════════════════════════════
    private async Task PaymentMenuAsync()
    {
        while (true)
        {
            ConsoleUI.ShowMenu("💰 FEE & PAYMENT MANAGEMENT",
                ("1", "Record Payment", "➕"),
                ("2", "List All Payments", "📋"),
                ("3", "Payments by Student", "👤"),
                ("4", "Payments by Month", "📅"),
                ("5", "Pending/Overdue Payments", "⚠️"),
                ("6", "Generate Receipt", "🧾"),
                ("7", "Fee Structure Setup", "💵"),
                ("8", "View Fee Structure", "👁️"),
                ("9", "Revenue Summary", "📊"),
                ("0", "Back to Main Menu", "↩️"));

            var input = Console.ReadLine()?.Trim();
            switch (input)
            {
                case "1": await RecordPaymentAsync(); break;
                case "2": await ListAllPaymentsAsync(); break;
                case "3": await PaymentsByStudentAsync(); break;
                case "4": await PaymentsByMonthAsync(); break;
                case "5": await PendingPaymentsAsync(); break;
                case "6": await GenerateReceiptAsync(); break;
                case "7": await SetupFeeStructureAsync(); break;
                case "8": await ViewFeeStructureAsync(); break;
                case "9": await RevenueSummaryAsync(); break;
                case "0": return;
                default: ConsoleUI.ShowError("Invalid option!"); ConsoleUI.Pause(); break;
            }
        }
    }

    private async Task RecordPaymentAsync()
    {
        ConsoleUI.ShowHeader("➕ RECORD PAYMENT");
        var studentId = ConsoleUI.ReadInt("Student ID");
        var student = await _students.GetStudentByIdAsync(studentId);
        if (student == null) { ConsoleUI.ShowError("Student not found!"); ConsoleUI.Pause(); return; }

        ConsoleUI.ShowInfo($"Recording payment for: {student.FullName}");

        var payment = new Payment
        {
            StudentId = studentId,
            StudentName = student.FullName,
            Amount = ConsoleUI.ReadDecimal("Amount (Rs.)", 1),
            Month = ConsoleUI.ReadInt("Month (1-12)", 1, 12),
            Year = ConsoleUI.ReadInt("Year", 2020, 2030),
            Method = ConsoleUI.ReadEnum<PaymentMethod>("Payment Method"),
            Status = PaymentStatus.Paid,
            Remarks = ConsoleUI.ReadInput("Remarks (optional)")
        };

        await _payments.RecordPaymentAsync(payment);
        await _audit.LogActionAsync("Payment", "Record", _currentUser,
            $"Rs. {payment.Amount:N0} from {student.FullName} (Receipt: {payment.ReceiptNumber})");
        ConsoleUI.ShowSuccess($"Payment recorded! Receipt: {payment.ReceiptNumber}");
        ConsoleUI.Pause();
    }

    private async Task ListAllPaymentsAsync()
    {
        ConsoleUI.ShowHeader("📋 ALL PAYMENTS");
        var payments = await _payments.GetAllPaymentsAsync();
        var rows = payments.Select(p => new[] {
            p.Id.ToString(), p.ReceiptNumber, p.StudentName,
            $"Rs. {p.Amount:N0}", $"{p.Month:D2}/{p.Year}", p.Method.ToString(),
            ConsoleUI.GetPaymentBadge(p.Status)
        }).ToList();
        ConsoleUI.ShowTable(new[] { "ID", "Receipt", "Student", "Amount", "Period", "Method", "Status" }, rows);
        ConsoleUI.Pause();
    }

    private async Task PaymentsByStudentAsync()
    {
        ConsoleUI.ShowHeader("👤 PAYMENTS BY STUDENT");
        var studentId = ConsoleUI.ReadInt("Student ID");
        var payments = await _payments.GetPaymentsForStudentAsync(studentId);
        var rows = payments.Select(p => new[] {
            p.Id.ToString(), p.ReceiptNumber, $"Rs. {p.Amount:N0}",
            $"{p.Month:D2}/{p.Year}", p.Method.ToString(), ConsoleUI.GetPaymentBadge(p.Status)
        }).ToList();
        ConsoleUI.ShowTable(new[] { "ID", "Receipt", "Amount", "Period", "Method", "Status" }, rows);

        if (payments.Count > 0)
        {
            var total = payments.Where(p => p.Status == PaymentStatus.Paid).Sum(p => p.Amount);
            ConsoleUI.ShowDetailRow("Total Paid", $"Rs. {total:N0}");
        }
        ConsoleUI.Pause();
    }

    private async Task PaymentsByMonthAsync()
    {
        ConsoleUI.ShowHeader("📅 PAYMENTS BY MONTH");
        var month = ConsoleUI.ReadInt("Month (1-12)", 1, 12);
        var year = ConsoleUI.ReadInt("Year", 2020, 2030);
        var payments = await _payments.GetPaymentsByMonthAsync(month, year);
        var rows = payments.Select(p => new[] {
            p.Id.ToString(), p.StudentName, $"Rs. {p.Amount:N0}", p.Method.ToString(), ConsoleUI.GetPaymentBadge(p.Status)
        }).ToList();
        ConsoleUI.ShowTable(new[] { "ID", "Student", "Amount", "Method", "Status" }, rows);

        var total = payments.Where(p => p.Status == PaymentStatus.Paid).Sum(p => p.Amount);
        ConsoleUI.ShowDetailRow("Month Revenue", $"Rs. {total:N0}");
        ConsoleUI.Pause();
    }

    private async Task PendingPaymentsAsync()
    {
        ConsoleUI.ShowHeader("⚠️ PENDING / OVERDUE PAYMENTS");
        var payments = await _payments.GetPendingPaymentsAsync();
        var rows = payments.Select(p => new[] {
            p.Id.ToString(), p.StudentName, $"Rs. {p.Amount:N0}",
            $"{p.Month:D2}/{p.Year}", ConsoleUI.GetPaymentBadge(p.Status)
        }).ToList();
        ConsoleUI.ShowTable(new[] { "ID", "Student", "Amount", "Period", "Status" }, rows);
        ConsoleUI.Pause();
    }

    private async Task GenerateReceiptAsync()
    {
        ConsoleUI.ShowHeader("🧾 GENERATE RECEIPT");
        var paymentId = ConsoleUI.ReadInt("Payment ID");
        var receipt = await _payments.GenerateReceiptAsync(paymentId);
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(receipt);
        Console.ResetColor();
        ConsoleUI.Pause();
    }

    private async Task SetupFeeStructureAsync()
    {
        ConsoleUI.ShowHeader("💵 SETUP FEE STRUCTURE");
        var fee = new FeeStructure
        {
            RoomType = ConsoleUI.ReadEnum<RoomType>("Room Type"),
            MonthlyRent = ConsoleUI.ReadDecimal("Monthly Rent (Rs.)"),
            MessFee = ConsoleUI.ReadDecimal("Mess Fee (Rs.)"),
            UtilityCharges = ConsoleUI.ReadDecimal("Utility Charges (Rs.)"),
            SecurityDeposit = ConsoleUI.ReadDecimal("Security Deposit (Rs.)"),
            LaundryFee = ConsoleUI.ReadDecimal("Laundry Fee (Rs.)"),
            Description = ConsoleUI.ReadInput("Description")
        };

        await _fees.CreateFeeStructureAsync(fee);
        await _audit.LogActionAsync("Fee", "Setup", _currentUser, $"Fee structure for {fee.RoomType}: Rs. {fee.TotalMonthly:N0}/month");
        ConsoleUI.ShowSuccess($"Fee structure created! Total Monthly: Rs. {fee.TotalMonthly:N0}");
        ConsoleUI.Pause();
    }

    private async Task ViewFeeStructureAsync()
    {
        ConsoleUI.ShowHeader("👁️ FEE STRUCTURE");
        var fees = await _fees.GetAllFeeStructuresAsync();
        var rows = fees.Select(f => new[] {
            f.Id.ToString(), f.RoomType.ToString(), $"Rs. {f.MonthlyRent:N0}",
            $"Rs. {f.MessFee:N0}", $"Rs. {f.UtilityCharges:N0}", $"Rs. {f.LaundryFee:N0}",
            $"Rs. {f.TotalMonthly:N0}", $"Rs. {f.SecurityDeposit:N0}"
        }).ToList();
        ConsoleUI.ShowTable(new[] { "ID", "Room Type", "Rent", "Mess", "Utility", "Laundry", "Total/Mo", "Deposit" }, rows);
        ConsoleUI.Pause();
    }

    private async Task RevenueSummaryAsync()
    {
        ConsoleUI.ShowHeader("📊 REVENUE SUMMARY");
        var totalRevenue = await _payments.GetTotalRevenueAsync();
        var payments = await _payments.GetAllPaymentsAsync();

        ConsoleUI.ShowDetailRow("Total Revenue (All Time)", $"Rs. {totalRevenue:N0}");
        ConsoleUI.ShowDetailRow("Total Transactions", payments.Count.ToString());
        ConsoleUI.ShowSeparator();

        // Monthly chart for current year
        var chartData = new Dictionary<string, double>();
        var monthNames = new[] { "", "Jan", "Feb", "Mar", "Apr", "May", "Jun",
                                     "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
        for (int m = 1; m <= 12; m++)
        {
            var rev = await _payments.GetRevenueByMonthAsync(m, DateTime.Now.Year);
            if (rev > 0) chartData[$"{monthNames[m]} {DateTime.Now.Year}"] = (double)rev;
        }
        if (chartData.Count > 0)
            ConsoleUI.ShowBarChart($"Monthly Revenue ({DateTime.Now.Year})", chartData, ConsoleColor.Green);

        // Payment method breakdown
        var methodData = new Dictionary<string, double>();
        foreach (PaymentMethod pm in Enum.GetValues<PaymentMethod>())
        {
            var count = payments.Count(p => p.Method == pm);
            if (count > 0) methodData[pm.ToString()] = count;
        }
        if (methodData.Count > 0)
            ConsoleUI.ShowBarChart("By Payment Method", methodData, ConsoleColor.Yellow);

        ConsoleUI.Pause();
    }

    // ═══════════════════════════════════════════════════════════════
    //  COMPLAINT MANAGEMENT
    // ═══════════════════════════════════════════════════════════════
    private async Task ComplaintMenuAsync()
    {
        while (true)
        {
            ConsoleUI.ShowMenu("📋 COMPLAINT MANAGEMENT",
                ("1", "Create Complaint", "➕"),
                ("2", "List Open Complaints", "🔴"),
                ("3", "List All Complaints", "📋"),
                ("4", "Complaints by Student", "👤"),
                ("5", "Complaints by Priority", "⚡"),
                ("6", "Assign to Staff", "👥"),
                ("7", "Update Status", "🔄"),
                ("8", "Complaint Analytics", "📊"),
                ("0", "Back to Main Menu", "↩️"));

            var input = Console.ReadLine()?.Trim();
            switch (input)
            {
                case "1": await CreateComplaintAsync(); break;
                case "2": await ListOpenComplaintsAsync(); break;
                case "3": await ListAllComplaintsAsync(); break;
                case "4": await ComplaintsByStudentAsync(); break;
                case "5": await ComplaintsByPriorityAsync(); break;
                case "6": await AssignComplaintAsync(); break;
                case "7": await UpdateComplaintStatusAsync(); break;
                case "8": await ComplaintAnalyticsAsync(); break;
                case "0": return;
                default: ConsoleUI.ShowError("Invalid option!"); ConsoleUI.Pause(); break;
            }
        }
    }

    private async Task CreateComplaintAsync()
    {
        ConsoleUI.ShowHeader("➕ CREATE COMPLAINT");
        var studentId = ConsoleUI.ReadInt("Student ID");
        var student = await _students.GetStudentByIdAsync(studentId);
        if (student == null) { ConsoleUI.ShowError("Student not found!"); ConsoleUI.Pause(); return; }

        var complaint = new Complaint
        {
            StudentId = studentId,
            StudentName = student.FullName,
            Title = ConsoleUI.ReadInput("Title"),
            Description = ConsoleUI.ReadInput("Description"),
            Category = ConsoleUI.ReadEnum<ComplaintCategory>("Category"),
            Priority = ConsoleUI.ReadEnum<ComplaintPriority>("Priority")
        };

        await _complaints.CreateComplaintAsync(complaint);
        await _audit.LogActionAsync("Complaint", "Create", _currentUser,
            $"Complaint #{complaint.Id}: {complaint.Title} by {student.FullName}");
        ConsoleUI.ShowSuccess($"Complaint created! (ID: {complaint.Id})");
        ConsoleUI.Pause();
    }

    private async Task ListOpenComplaintsAsync()
    {
        ConsoleUI.ShowHeader("🔴 OPEN COMPLAINTS");
        var complaints = await _complaints.GetOpenComplaintsAsync();
        ShowComplaintTable(complaints);
        ConsoleUI.Pause();
    }

    private async Task ListAllComplaintsAsync()
    {
        ConsoleUI.ShowHeader("📋 ALL COMPLAINTS");
        var complaints = await _complaints.GetAllComplaintsAsync();
        ShowComplaintTable(complaints);
        ConsoleUI.Pause();
    }

    private async Task ComplaintsByStudentAsync()
    {
        ConsoleUI.ShowHeader("👤 COMPLAINTS BY STUDENT");
        var studentId = ConsoleUI.ReadInt("Student ID");
        var complaints = await _complaints.GetComplaintsByStudentAsync(studentId);
        ShowComplaintTable(complaints);
        ConsoleUI.Pause();
    }

    private async Task ComplaintsByPriorityAsync()
    {
        ConsoleUI.ShowHeader("⚡ COMPLAINTS BY PRIORITY");
        var priority = ConsoleUI.ReadEnum<ComplaintPriority>("Select Priority");
        var complaints = await _complaints.GetComplaintsByPriorityAsync(priority);
        ShowComplaintTable(complaints);
        ConsoleUI.Pause();
    }

    private void ShowComplaintTable(IReadOnlyList<Complaint> complaints)
    {
        var rows = complaints.Select(c => new[] {
            c.Id.ToString(), c.StudentName, c.Title, c.Category.ToString(),
            ConsoleUI.GetPriorityBadge(c.Priority), ConsoleUI.GetStatusBadge(c.Status),
            c.AssignedStaffName ?? "—"
        }).ToList();
        ConsoleUI.ShowTable(new[] { "ID", "Student", "Title", "Category", "Priority", "Status", "Assigned To" }, rows);
    }

    private async Task AssignComplaintAsync()
    {
        ConsoleUI.ShowHeader("👥 ASSIGN COMPLAINT TO STAFF");
        var complaintId = ConsoleUI.ReadInt("Complaint ID");

        // Show available staff
        var staff = await _staff.GetActiveStaffAsync();
        var sRows = staff.Select(s => new[] { s.Id.ToString(), s.FullName, s.Role.ToString() }).ToList();
        ConsoleUI.ShowTable(new[] { "ID", "Name", "Role" }, sRows);

        var staffId = ConsoleUI.ReadInt("Staff ID to assign");
        var staffMember = await _staff.GetStaffByIdAsync(staffId);

        try
        {
            await _complaints.AssignComplaintAsync(complaintId, staffId);
            // Update complaint's assigned staff name
            var allComplaints = await _complaints.GetAllComplaintsAsync();
            var complaint = allComplaints.FirstOrDefault(c => c.Id == complaintId);
            if (complaint != null && staffMember != null)
            {
                complaint.AssignedStaffName = staffMember.FullName;
            }
            await _audit.LogActionAsync("Complaint", "Assign", _currentUser,
                $"Complaint #{complaintId} → {staffMember?.FullName ?? "Unknown"}");
            ConsoleUI.ShowSuccess("Complaint assigned!");
        }
        catch (Exception ex) { ConsoleUI.ShowError(ex.Message); }
        ConsoleUI.Pause();
    }

    private async Task UpdateComplaintStatusAsync()
    {
        ConsoleUI.ShowHeader("🔄 UPDATE COMPLAINT STATUS");
        var complaintId = ConsoleUI.ReadInt("Complaint ID");
        var status = ConsoleUI.ReadEnum<ComplaintStatus>("New Status");
        var notes = ConsoleUI.ReadInput("Resolution Notes (optional)");
        try
        {
            await _complaints.UpdateComplaintStatusAsync(complaintId, status, notes);
            await _audit.LogActionAsync("Complaint", "Status Update", _currentUser,
                $"Complaint #{complaintId} → {status}");
            ConsoleUI.ShowSuccess($"Status updated to {status}!");
        }
        catch (Exception ex) { ConsoleUI.ShowError(ex.Message); }
        ConsoleUI.Pause();
    }

    private async Task ComplaintAnalyticsAsync()
    {
        ConsoleUI.ShowHeader("📊 COMPLAINT ANALYTICS");
        var all = await _complaints.GetAllComplaintsAsync();

        ConsoleUI.ShowDetailRow("Total Complaints", all.Count.ToString());
        ConsoleUI.ShowDetailRow("Open", all.Count(c => c.Status == ComplaintStatus.Open).ToString());
        ConsoleUI.ShowDetailRow("In Progress", all.Count(c => c.Status == ComplaintStatus.InProgress).ToString());
        ConsoleUI.ShowDetailRow("Resolved", all.Count(c => c.Status == ComplaintStatus.Resolved).ToString());
        ConsoleUI.ShowDetailRow("Closed", all.Count(c => c.Status == ComplaintStatus.Closed).ToString());

        // By category chart
        var catData = new Dictionary<string, double>();
        foreach (ComplaintCategory cat in Enum.GetValues<ComplaintCategory>())
        {
            var count = all.Count(c => c.Category == cat);
            if (count > 0) catData[cat.ToString()] = count;
        }
        if (catData.Count > 0)
            ConsoleUI.ShowBarChart("By Category", catData, ConsoleColor.Yellow);

        // By priority chart
        var prioData = new Dictionary<string, double>();
        foreach (ComplaintPriority p in Enum.GetValues<ComplaintPriority>())
        {
            var count = all.Count(c => c.Priority == p);
            if (count > 0) prioData[p.ToString()] = count;
        }
        if (prioData.Count > 0)
            ConsoleUI.ShowBarChart("By Priority", prioData, ConsoleColor.Red);

        ConsoleUI.Pause();
    }
}
