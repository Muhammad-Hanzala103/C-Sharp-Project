using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Hostel.Core.Entities;
using Hostel.Core.Interfaces;

namespace Hostel.Core.Services;

// ═══════════════════════════════════════════════════════════════
//  JSON FILE-BASED REPOSITORY — Data persists between restarts
// ═══════════════════════════════════════════════════════════════

public class JsonFileRepository<T> : IGenericRepository<T> where T : class
{
    private readonly string _filePath;
    private List<T> _items;
    private int _nextId;

    public JsonFileRepository(string dataDir, string fileName)
    {
        Directory.CreateDirectory(dataDir);
        _filePath = Path.Combine(dataDir, fileName);
        _items = LoadFromFile();
        _nextId = CalculateNextId();
    }

    private List<T> LoadFromFile()
    {
        if (!File.Exists(_filePath)) return new List<T>();
        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>();
        }
        catch { return new List<T>(); }
    }

    private void SaveToFile()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(_items, options);
        File.WriteAllText(_filePath, json);
    }

    private int CalculateNextId()
    {
        if (_items.Count == 0) return 1;
        var prop = typeof(T).GetProperty("Id");
        if (prop == null) return 1;
        return _items.Max(x => (int)(prop.GetValue(x) ?? 0)) + 1;
    }

    public Task<T?> GetByIdAsync(int id)
    {
        var prop = typeof(T).GetProperty("Id");
        var item = _items.FirstOrDefault(x => (int)(prop?.GetValue(x) ?? 0) == id);
        return Task.FromResult(item);
    }

    public Task<IReadOnlyList<T>> GetAllAsync()
    {
        return Task.FromResult((IReadOnlyList<T>)_items.ToList());
    }

    public Task AddAsync(T entity)
    {
        var prop = typeof(T).GetProperty("Id");
        if (prop is not null && (int)(prop.GetValue(entity) ?? 0) == 0)
        {
            prop.SetValue(entity, _nextId++);
        }
        _items.Add(entity);
        return Task.CompletedTask;
    }

    public void Update(T entity)
    {
        var prop = typeof(T).GetProperty("Id");
        if (prop == null) return;
        var id = (int)(prop.GetValue(entity) ?? 0);
        var index = _items.FindIndex(x => (int)(prop.GetValue(x) ?? 0) == id);
        if (index >= 0) _items[index] = entity;
    }

    public Task DeleteAsync(int id)
    {
        var prop = typeof(T).GetProperty("Id");
        var existing = _items.FirstOrDefault(x => (int)(prop?.GetValue(x) ?? 0) == id);
        if (existing is not null) _items.Remove(existing);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync()
    {
        SaveToFile();
        return Task.CompletedTask;
    }
}

// ═══════════════════════════════════════════════════════════════
//  STUDENT SERVICE
// ═══════════════════════════════════════════════════════════════

public class StudentService : IStudentService
{
    private readonly IGenericRepository<Student> _students;
    private readonly IGenericRepository<Room> _rooms;
    private readonly IGenericRepository<Booking> _bookings;

    public StudentService(
        IGenericRepository<Student> students,
        IGenericRepository<Room> rooms,
        IGenericRepository<Booking> bookings)
    {
        _students = students;
        _rooms = rooms;
        _bookings = bookings;
    }

    public async Task<Student> RegisterStudentAsync(Student student)
    {
        student.JoinDate = DateTime.Now;
        student.IsActive = true;
        await _students.AddAsync(student);
        await _students.SaveChangesAsync();
        return student;
    }

    public Task<Student?> GetStudentByIdAsync(int id) => _students.GetByIdAsync(id);

    public async Task UpdateStudentAsync(Student student)
    {
        _students.Update(student);
        await _students.SaveChangesAsync();
    }

    public async Task DeactivateStudentAsync(int studentId)
    {
        var student = await _students.GetByIdAsync(studentId)
            ?? throw new InvalidOperationException("Student not found");
        student.IsActive = false;
        student.LeaveDate = DateTime.Now;
        if (student.RoomId.HasValue)
        {
            var room = await _rooms.GetByIdAsync(student.RoomId.Value);
            if (room != null)
            {
                room.CurrentOccupancy = Math.Max(0, room.CurrentOccupancy - 1);
                _rooms.Update(room);
                await _rooms.SaveChangesAsync();
            }
            student.RoomId = null;
            student.RoomNumber = null;
        }
        _students.Update(student);
        await _students.SaveChangesAsync();
    }

    public async Task AssignRoomAsync(int studentId, int roomId)
    {
        var student = await _students.GetByIdAsync(studentId)
            ?? throw new InvalidOperationException("Student not found");
        var room = await _rooms.GetByIdAsync(roomId)
            ?? throw new InvalidOperationException("Room not found");

        if (room.IsFull)
            throw new InvalidOperationException($"Room {room.RoomNumber} is full ({room.CurrentOccupancy}/{room.Capacity})");

        // Unassign from old room if any
        if (student.RoomId.HasValue)
        {
            var oldRoom = await _rooms.GetByIdAsync(student.RoomId.Value);
            if (oldRoom != null)
            {
                oldRoom.CurrentOccupancy = Math.Max(0, oldRoom.CurrentOccupancy - 1);
                _rooms.Update(oldRoom);
            }
        }

        room.CurrentOccupancy++;
        student.RoomId = room.Id;
        student.RoomNumber = room.RoomNumber;

        // Create booking record
        var booking = new Booking
        {
            StudentId = studentId,
            StudentName = student.FullName,
            RoomId = roomId,
            RoomNumber = room.RoomNumber,
            StartDate = DateTime.Now,
            IsCurrent = true
        };

        _rooms.Update(room);
        _students.Update(student);
        await _bookings.AddAsync(booking);
        await _rooms.SaveChangesAsync();
        await _students.SaveChangesAsync();
        await _bookings.SaveChangesAsync();
    }

    public async Task UnassignRoomAsync(int studentId)
    {
        var student = await _students.GetByIdAsync(studentId)
            ?? throw new InvalidOperationException("Student not found");
        if (!student.RoomId.HasValue) throw new InvalidOperationException("Student has no room assigned");

        var room = await _rooms.GetByIdAsync(student.RoomId.Value);
        if (room != null)
        {
            room.CurrentOccupancy = Math.Max(0, room.CurrentOccupancy - 1);
            _rooms.Update(room);
            await _rooms.SaveChangesAsync();
        }

        student.RoomId = null;
        student.RoomNumber = null;
        _students.Update(student);
        await _students.SaveChangesAsync();
    }

    public async Task SwapRoomsAsync(int studentId1, int studentId2)
    {
        var s1 = await _students.GetByIdAsync(studentId1)
            ?? throw new InvalidOperationException("Student 1 not found");
        var s2 = await _students.GetByIdAsync(studentId2)
            ?? throw new InvalidOperationException("Student 2 not found");

        (s1.RoomId, s2.RoomId) = (s2.RoomId, s1.RoomId);
        (s1.RoomNumber, s2.RoomNumber) = (s2.RoomNumber, s1.RoomNumber);

        _students.Update(s1);
        _students.Update(s2);
        await _students.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<Student>> GetActiveStudentsAsync()
    {
        var all = await _students.GetAllAsync();
        return all.Where(s => s.IsActive).ToList();
    }

    public Task<IReadOnlyList<Student>> GetAllStudentsAsync() => _students.GetAllAsync();

    public async Task<IReadOnlyList<Student>> SearchStudentsAsync(string query)
    {
        var all = await _students.GetAllAsync();
        var q = query.ToLower();
        return all.Where(s =>
            s.FirstName.ToLower().Contains(q) ||
            s.LastName.ToLower().Contains(q) ||
            s.RegistrationNumber.ToLower().Contains(q) ||
            s.Phone.Contains(q) ||
            s.Email.ToLower().Contains(q) ||
            s.Department.ToLower().Contains(q)
        ).ToList();
    }

    public async Task<IReadOnlyList<Student>> GetStudentsByRoomAsync(int roomId)
    {
        var all = await _students.GetAllAsync();
        return all.Where(s => s.RoomId == roomId && s.IsActive).ToList();
    }

    public async Task<IReadOnlyList<Student>> GetStudentsWithoutRoomAsync()
    {
        var all = await _students.GetAllAsync();
        return all.Where(s => !s.RoomId.HasValue && s.IsActive).ToList();
    }

    public async Task<int> GetActiveCountAsync()
    {
        var all = await _students.GetAllAsync();
        return all.Count(s => s.IsActive);
    }
}

// ═══════════════════════════════════════════════════════════════
//  ROOM SERVICE
// ═══════════════════════════════════════════════════════════════

public class RoomService : IRoomService
{
    private readonly IGenericRepository<Room> _rooms;

    public RoomService(IGenericRepository<Room> rooms) => _rooms = rooms;

    public async Task<Room> CreateRoomAsync(Room room)
    {
        room.IsActive = true;
        room.CurrentOccupancy = 0;
        await _rooms.AddAsync(room);
        await _rooms.SaveChangesAsync();
        return room;
    }

    public Task<Room?> GetRoomByIdAsync(int id) => _rooms.GetByIdAsync(id);

    public async Task UpdateRoomAsync(Room room)
    {
        _rooms.Update(room);
        await _rooms.SaveChangesAsync();
    }

    public async Task DeleteRoomAsync(int id)
    {
        var room = await _rooms.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Room not found");
        if (room.CurrentOccupancy > 0)
            throw new InvalidOperationException("Cannot delete room with occupants");
        await _rooms.DeleteAsync(id);
        await _rooms.SaveChangesAsync();
    }

    public Task<IReadOnlyList<Room>> GetAllRoomsAsync() => _rooms.GetAllAsync();

    public async Task<IReadOnlyList<Room>> GetAvailableRoomsAsync()
    {
        var all = await _rooms.GetAllAsync();
        return all.Where(r => r.IsActive && !r.IsFull).ToList();
    }

    public async Task<IReadOnlyList<Room>> GetFullRoomsAsync()
    {
        var all = await _rooms.GetAllAsync();
        return all.Where(r => r.IsFull).ToList();
    }

    public async Task<bool> HasCapacityAsync(int roomId)
    {
        var room = await _rooms.GetByIdAsync(roomId);
        return room is not null && !room.IsFull;
    }

    public async Task<int> GetTotalCapacityAsync()
    {
        var all = await _rooms.GetAllAsync();
        return all.Where(r => r.IsActive).Sum(r => r.Capacity);
    }

    public async Task<int> GetTotalOccupancyAsync()
    {
        var all = await _rooms.GetAllAsync();
        return all.Where(r => r.IsActive).Sum(r => r.CurrentOccupancy);
    }
}

// ═══════════════════════════════════════════════════════════════
//  PAYMENT SERVICE
// ═══════════════════════════════════════════════════════════════

public class PaymentService : IPaymentService
{
    private readonly IGenericRepository<Payment> _payments;
    private static int _receiptCounter = 1000;

    public PaymentService(IGenericRepository<Payment> payments) => _payments = payments;

    public async Task<Payment> RecordPaymentAsync(Payment payment)
    {
        payment.PaymentDate = DateTime.Now;
        payment.ReceiptNumber = $"RCP-{DateTime.Now:yyyyMMdd}-{++_receiptCounter}";
        await _payments.AddAsync(payment);
        await _payments.SaveChangesAsync();
        return payment;
    }

    public async Task<IReadOnlyList<Payment>> GetPaymentsForStudentAsync(int studentId)
    {
        var all = await _payments.GetAllAsync();
        return all.Where(p => p.StudentId == studentId).ToList();
    }

    public Task<IReadOnlyList<Payment>> GetAllPaymentsAsync() => _payments.GetAllAsync();

    public async Task<IReadOnlyList<Payment>> GetPaymentsByMonthAsync(int month, int year)
    {
        var all = await _payments.GetAllAsync();
        return all.Where(p => p.Month == month && p.Year == year).ToList();
    }

    public async Task<IReadOnlyList<Payment>> GetPendingPaymentsAsync()
    {
        var all = await _payments.GetAllAsync();
        return all.Where(p => p.Status == PaymentStatus.Pending || p.Status == PaymentStatus.Overdue).ToList();
    }

    public async Task<decimal> GetTotalRevenueAsync()
    {
        var all = await _payments.GetAllAsync();
        return all.Where(p => p.Status == PaymentStatus.Paid).Sum(p => p.Amount);
    }

    public async Task<decimal> GetRevenueByMonthAsync(int month, int year)
    {
        var all = await _payments.GetAllAsync();
        return all.Where(p => p.Month == month && p.Year == year && p.Status == PaymentStatus.Paid).Sum(p => p.Amount);
    }

    public async Task<string> GenerateReceiptAsync(int paymentId)
    {
        var payment = await _payments.GetByIdAsync(paymentId);
        if (payment == null) return "Payment not found";

        var sb = new StringBuilder();
        sb.AppendLine("╔══════════════════════════════════════════╗");
        sb.AppendLine("║       HOSTEL MANAGEMENT SYSTEM           ║");
        sb.AppendLine("║           PAYMENT RECEIPT                ║");
        sb.AppendLine("╠══════════════════════════════════════════╣");
        sb.AppendLine($"║ Receipt #  : {payment.ReceiptNumber,-27}║");
        sb.AppendLine($"║ Date       : {payment.PaymentDate:dd-MMM-yyyy HH:mm,-19}║");
        sb.AppendLine($"║ Student ID : {payment.StudentId,-27}║");
        sb.AppendLine($"║ Student    : {payment.StudentName,-27}║");
        sb.AppendLine($"║ Amount     : Rs. {payment.Amount,-23:N0}║");
        sb.AppendLine($"║ Period     : {payment.Month:D2}/{payment.Year,-24}║");
        sb.AppendLine($"║ Method     : {payment.Method,-27}║");
        sb.AppendLine($"║ Status     : {payment.Status,-27}║");
        sb.AppendLine("╠══════════════════════════════════════════╣");
        sb.AppendLine("║          Thank you for payment!          ║");
        sb.AppendLine("╚══════════════════════════════════════════╝");
        return sb.ToString();
    }
}

// ═══════════════════════════════════════════════════════════════
//  FEE STRUCTURE SERVICE
// ═══════════════════════════════════════════════════════════════

public class FeeStructureService : IFeeStructureService
{
    private readonly IGenericRepository<FeeStructure> _fees;

    public FeeStructureService(IGenericRepository<FeeStructure> fees) => _fees = fees;

    public async Task<FeeStructure> CreateFeeStructureAsync(FeeStructure fee)
    {
        fee.IsActive = true;
        await _fees.AddAsync(fee);
        await _fees.SaveChangesAsync();
        return fee;
    }

    public async Task UpdateFeeStructureAsync(FeeStructure fee)
    {
        _fees.Update(fee);
        await _fees.SaveChangesAsync();
    }

    public Task<IReadOnlyList<FeeStructure>> GetAllFeeStructuresAsync() => _fees.GetAllAsync();

    public async Task<FeeStructure?> GetFeeByRoomTypeAsync(RoomType roomType)
    {
        var all = await _fees.GetAllAsync();
        return all.FirstOrDefault(f => f.RoomType == roomType && f.IsActive);
    }
}

// ═══════════════════════════════════════════════════════════════
//  COMPLAINT SERVICE
// ═══════════════════════════════════════════════════════════════

public class ComplaintService : IComplaintService
{
    private readonly IGenericRepository<Complaint> _complaints;

    public ComplaintService(IGenericRepository<Complaint> complaints) => _complaints = complaints;

    public async Task<Complaint> CreateComplaintAsync(Complaint complaint)
    {
        complaint.CreatedAt = DateTime.Now;
        complaint.Status = ComplaintStatus.Open;
        await _complaints.AddAsync(complaint);
        await _complaints.SaveChangesAsync();
        return complaint;
    }

    public async Task UpdateComplaintStatusAsync(int complaintId, ComplaintStatus status, string? notes)
    {
        var complaint = await _complaints.GetByIdAsync(complaintId)
            ?? throw new InvalidOperationException("Complaint not found");
        complaint.Status = status;
        if (status == ComplaintStatus.Resolved || status == ComplaintStatus.Closed)
            complaint.ResolvedAt = DateTime.Now;
        if (!string.IsNullOrEmpty(notes))
            complaint.ResolutionNotes = notes;
        _complaints.Update(complaint);
        await _complaints.SaveChangesAsync();
    }

    public async Task AssignComplaintAsync(int complaintId, int staffId)
    {
        var complaint = await _complaints.GetByIdAsync(complaintId)
            ?? throw new InvalidOperationException("Complaint not found");
        complaint.AssignedStaffId = staffId;
        complaint.Status = ComplaintStatus.InProgress;
        _complaints.Update(complaint);
        await _complaints.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<Complaint>> GetOpenComplaintsAsync()
    {
        var all = await _complaints.GetAllAsync();
        return all.Where(c => c.Status == ComplaintStatus.Open || c.Status == ComplaintStatus.InProgress).ToList();
    }

    public Task<IReadOnlyList<Complaint>> GetAllComplaintsAsync() => _complaints.GetAllAsync();

    public async Task<IReadOnlyList<Complaint>> GetComplaintsByStudentAsync(int studentId)
    {
        var all = await _complaints.GetAllAsync();
        return all.Where(c => c.StudentId == studentId).ToList();
    }

    public async Task<IReadOnlyList<Complaint>> GetComplaintsByPriorityAsync(ComplaintPriority priority)
    {
        var all = await _complaints.GetAllAsync();
        return all.Where(c => c.Priority == priority).ToList();
    }

    public async Task<int> GetOpenCountAsync()
    {
        var all = await _complaints.GetAllAsync();
        return all.Count(c => c.Status == ComplaintStatus.Open || c.Status == ComplaintStatus.InProgress);
    }
}

// ═══════════════════════════════════════════════════════════════
//  STAFF SERVICE
// ═══════════════════════════════════════════════════════════════

public class StaffService : IStaffService
{
    private readonly IGenericRepository<Staff> _staff;

    public StaffService(IGenericRepository<Staff> staff) => _staff = staff;

    public async Task<Staff> AddStaffAsync(Staff staff)
    {
        staff.JoinDate = DateTime.Now;
        staff.IsActive = true;
        await _staff.AddAsync(staff);
        await _staff.SaveChangesAsync();
        return staff;
    }

    public Task<Staff?> GetStaffByIdAsync(int id) => _staff.GetByIdAsync(id);

    public async Task UpdateStaffAsync(Staff staff)
    {
        _staff.Update(staff);
        await _staff.SaveChangesAsync();
    }

    public async Task DeactivateStaffAsync(int staffId)
    {
        var staff = await _staff.GetByIdAsync(staffId)
            ?? throw new InvalidOperationException("Staff not found");
        staff.IsActive = false;
        _staff.Update(staff);
        await _staff.SaveChangesAsync();
    }

    public Task<IReadOnlyList<Staff>> GetAllStaffAsync() => _staff.GetAllAsync();

    public async Task<IReadOnlyList<Staff>> GetActiveStaffAsync()
    {
        var all = await _staff.GetAllAsync();
        return all.Where(s => s.IsActive).ToList();
    }

    public async Task<IReadOnlyList<Staff>> GetStaffByRoleAsync(StaffRole role)
    {
        var all = await _staff.GetAllAsync();
        return all.Where(s => s.Role == role && s.IsActive).ToList();
    }

    public async Task<int> GetActiveCountAsync()
    {
        var all = await _staff.GetAllAsync();
        return all.Count(s => s.IsActive);
    }
}

// ═══════════════════════════════════════════════════════════════
//  VISITOR SERVICE
// ═══════════════════════════════════════════════════════════════

public class VisitorService : IVisitorService
{
    private readonly IGenericRepository<Visitor> _visitors;
    private static int _passCounter = 5000;

    public VisitorService(IGenericRepository<Visitor> visitors) => _visitors = visitors;

    public async Task<Visitor> CheckInVisitorAsync(Visitor visitor)
    {
        visitor.CheckInTime = DateTime.Now;
        visitor.Status = VisitorStatus.CheckedIn;
        visitor.PassNumber = $"VP-{DateTime.Now:yyyyMMdd}-{++_passCounter}";
        await _visitors.AddAsync(visitor);
        await _visitors.SaveChangesAsync();
        return visitor;
    }

    public async Task CheckOutVisitorAsync(int visitorId)
    {
        var visitor = await _visitors.GetByIdAsync(visitorId)
            ?? throw new InvalidOperationException("Visitor not found");
        visitor.CheckOutTime = DateTime.Now;
        visitor.Status = VisitorStatus.CheckedOut;
        _visitors.Update(visitor);
        await _visitors.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<Visitor>> GetActiveVisitorsAsync()
    {
        var all = await _visitors.GetAllAsync();
        return all.Where(v => v.Status == VisitorStatus.CheckedIn).ToList();
    }

    public Task<IReadOnlyList<Visitor>> GetAllVisitorsAsync() => _visitors.GetAllAsync();

    public async Task<IReadOnlyList<Visitor>> GetVisitorsByDateAsync(DateTime date)
    {
        var all = await _visitors.GetAllAsync();
        return all.Where(v => v.CheckInTime.Date == date.Date).ToList();
    }

    public async Task<IReadOnlyList<Visitor>> GetVisitorsByStudentAsync(int studentId)
    {
        var all = await _visitors.GetAllAsync();
        return all.Where(v => v.StudentId == studentId).ToList();
    }
}

// ═══════════════════════════════════════════════════════════════
//  ATTENDANCE SERVICE
// ═══════════════════════════════════════════════════════════════

public class AttendanceService : IAttendanceService
{
    private readonly IGenericRepository<Attendance> _attendance;

    public AttendanceService(IGenericRepository<Attendance> attendance) => _attendance = attendance;

    public async Task MarkAttendanceAsync(Attendance attendance)
    {
        attendance.Date = attendance.Date.Date; // normalize to date only
        await _attendance.AddAsync(attendance);
        await _attendance.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<Attendance>> GetAttendanceByDateAsync(DateTime date)
    {
        var all = await _attendance.GetAllAsync();
        return all.Where(a => a.Date.Date == date.Date).ToList();
    }

    public async Task<IReadOnlyList<Attendance>> GetAttendanceByStudentAsync(int studentId)
    {
        var all = await _attendance.GetAllAsync();
        return all.Where(a => a.StudentId == studentId).ToList();
    }

    public async Task<(int present, int absent, int leave)> GetAttendanceStatsAsync(DateTime date)
    {
        var records = await GetAttendanceByDateAsync(date);
        return (
            records.Count(a => a.Status == AttendanceStatus.Present),
            records.Count(a => a.Status == AttendanceStatus.Absent),
            records.Count(a => a.Status == AttendanceStatus.Leave)
        );
    }

    public async Task<double> GetStudentAttendancePercentageAsync(int studentId)
    {
        var records = await GetAttendanceByStudentAsync(studentId);
        if (records.Count == 0) return 0;
        var present = records.Count(a => a.Status == AttendanceStatus.Present);
        return (double)present / records.Count * 100;
    }
}

// ═══════════════════════════════════════════════════════════════
//  MESS MENU SERVICE
// ═══════════════════════════════════════════════════════════════

public class MessMenuService : IMessMenuService
{
    private readonly IGenericRepository<MessMenu> _menus;

    public MessMenuService(IGenericRepository<MessMenu> menus) => _menus = menus;

    public async Task<MessMenu> AddMenuItemAsync(MessMenu menu)
    {
        menu.IsActive = true;
        await _menus.AddAsync(menu);
        await _menus.SaveChangesAsync();
        return menu;
    }

    public async Task UpdateMenuItemAsync(MessMenu menu)
    {
        _menus.Update(menu);
        await _menus.SaveChangesAsync();
    }

    public async Task DeleteMenuItemAsync(int id)
    {
        await _menus.DeleteAsync(id);
        await _menus.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<MessMenu>> GetMenuByDayAsync(DayOfWeek day)
    {
        var all = await _menus.GetAllAsync();
        return all.Where(m => m.Day == day && m.IsActive).OrderBy(m => m.MealType).ToList();
    }

    public async Task<IReadOnlyList<MessMenu>> GetFullWeekMenuAsync()
    {
        var all = await _menus.GetAllAsync();
        return all.Where(m => m.IsActive).OrderBy(m => m.Day).ThenBy(m => m.MealType).ToList();
    }

    public Task<MessMenu?> GetMenuItemAsync(int id) => _menus.GetByIdAsync(id);
}

// ═══════════════════════════════════════════════════════════════
//  NOTICE SERVICE
// ═══════════════════════════════════════════════════════════════

public class NoticeService : INoticeService
{
    private readonly IGenericRepository<Notice> _notices;

    public NoticeService(IGenericRepository<Notice> notices) => _notices = notices;

    public async Task<Notice> PostNoticeAsync(Notice notice)
    {
        notice.PostedAt = DateTime.Now;
        notice.IsActive = true;
        await _notices.AddAsync(notice);
        await _notices.SaveChangesAsync();
        return notice;
    }

    public async Task UpdateNoticeAsync(Notice notice)
    {
        _notices.Update(notice);
        await _notices.SaveChangesAsync();
    }

    public async Task DeactivateNoticeAsync(int noticeId)
    {
        var notice = await _notices.GetByIdAsync(noticeId)
            ?? throw new InvalidOperationException("Notice not found");
        notice.IsActive = false;
        _notices.Update(notice);
        await _notices.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<Notice>> GetActiveNoticesAsync()
    {
        var all = await _notices.GetAllAsync();
        return all.Where(n => n.IsActive &&
            (!n.ExpiresAt.HasValue || n.ExpiresAt.Value > DateTime.Now))
            .OrderByDescending(n => n.Priority)
            .ThenByDescending(n => n.PostedAt)
            .ToList();
    }

    public Task<IReadOnlyList<Notice>> GetAllNoticesAsync() => _notices.GetAllAsync();
}

// ═══════════════════════════════════════════════════════════════
//  AUDIT SERVICE
// ═══════════════════════════════════════════════════════════════

public class AuditService : IAuditService
{
    private readonly IGenericRepository<AuditLog> _logs;

    public AuditService(IGenericRepository<AuditLog> logs) => _logs = logs;

    public async Task LogActionAsync(string module, string action, string performedBy, string details)
    {
        var log = new AuditLog
        {
            Module = module,
            Action = action,
            PerformedBy = performedBy,
            Timestamp = DateTime.Now,
            Details = details
        };
        await _logs.AddAsync(log);
        await _logs.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<AuditLog>> GetRecentLogsAsync(int count = 20)
    {
        var all = await _logs.GetAllAsync();
        return all.OrderByDescending(l => l.Timestamp).Take(count).ToList();
    }

    public async Task<IReadOnlyList<AuditLog>> GetLogsByModuleAsync(string module)
    {
        var all = await _logs.GetAllAsync();
        return all.Where(l => l.Module.Equals(module, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public Task<IReadOnlyList<AuditLog>> GetAllLogsAsync() => _logs.GetAllAsync();
}

// ═══════════════════════════════════════════════════════════════
//  ADMIN SERVICE
// ═══════════════════════════════════════════════════════════════

public class AdminService : IAdminService
{
    private readonly IGenericRepository<Admin> _admins;

    public AdminService(IGenericRepository<Admin> admins) => _admins = admins;

    public async Task<Admin?> AuthenticateAsync(string username, string password)
    {
        var all = await _admins.GetAllAsync();
        var hash = HashPassword(password);
        var admin = all.FirstOrDefault(a =>
            a.Username.Equals(username, StringComparison.OrdinalIgnoreCase) &&
            a.PasswordHash == hash &&
            a.IsActive);

        if (admin != null)
        {
            admin.LastLogin = DateTime.Now;
            _admins.Update(admin);
            await _admins.SaveChangesAsync();
        }
        return admin;
    }

    public async Task<Admin> CreateAdminAsync(Admin admin, string password)
    {
        admin.PasswordHash = HashPassword(password);
        admin.IsActive = true;
        await _admins.AddAsync(admin);
        await _admins.SaveChangesAsync();
        return admin;
    }

    public async Task ChangePasswordAsync(int adminId, string oldPassword, string newPassword)
    {
        var admin = await _admins.GetByIdAsync(adminId)
            ?? throw new InvalidOperationException("Admin not found");

        if (admin.PasswordHash != HashPassword(oldPassword))
            throw new InvalidOperationException("Incorrect current password");

        admin.PasswordHash = HashPassword(newPassword);
        _admins.Update(admin);
        await _admins.SaveChangesAsync();
    }

    public async Task<bool> AdminExistsAsync()
    {
        var all = await _admins.GetAllAsync();
        return all.Any(a => a.IsActive);
    }

    private static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password + "HostelSalt2026"));
        return Convert.ToBase64String(bytes);
    }
}
