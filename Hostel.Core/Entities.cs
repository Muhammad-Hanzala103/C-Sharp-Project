namespace Hostel.Core.Entities;

// ─────────────────────── STUDENT ───────────────────────
public class Student
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;
    public string CNIC { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string GuardianName { get; set; } = string.Empty;
    public string GuardianPhone { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public int? RoomId { get; set; }
    public string? RoomNumber { get; set; }
    public DateTime JoinDate { get; set; }
    public DateTime? LeaveDate { get; set; }
    public bool IsActive { get; set; } = true;
    public string FullName => $"{FirstName} {LastName}";
}

// ─────────────────────── ROOM ───────────────────────
public class Room
{
    public int Id { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public int Floor { get; set; }
    public int Capacity { get; set; }
    public int CurrentOccupancy { get; set; }
    public RoomType RoomType { get; set; }
    public decimal MonthlyRent { get; set; }
    public bool HasAC { get; set; }
    public bool HasAttachedBath { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsFull => CurrentOccupancy >= Capacity;
}

// ─────────────────────── BOOKING ───────────────────────
public class Booking
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsCurrent { get; set; }
}

// ─────────────────────── PAYMENT ───────────────────────
public class Payment
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public DateTime PaymentDate { get; set; }
    public PaymentMethod Method { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; }
    public string Remarks { get; set; } = string.Empty;
}

// ─────────────────────── FEE STRUCTURE ───────────────────────
public class FeeStructure
{
    public int Id { get; set; }
    public RoomType RoomType { get; set; }
    public decimal MonthlyRent { get; set; }
    public decimal MessFee { get; set; }
    public decimal UtilityCharges { get; set; }
    public decimal SecurityDeposit { get; set; }
    public decimal LaundryFee { get; set; }
    public decimal TotalMonthly => MonthlyRent + MessFee + UtilityCharges + LaundryFee;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

// ─────────────────────── COMPLAINT ───────────────────────
public class Complaint
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ComplaintCategory Category { get; set; }
    public ComplaintPriority Priority { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public ComplaintStatus Status { get; set; }
    public int? AssignedStaffId { get; set; }
    public string? AssignedStaffName { get; set; }
    public string? ResolutionNotes { get; set; }
}

// ─────────────────────── STAFF ───────────────────────
public class Staff
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string CNIC { get; set; } = string.Empty;
    public StaffRole Role { get; set; }
    public decimal Salary { get; set; }
    public string Shift { get; set; } = "Day"; // Day, Night, Rotating
    public DateTime JoinDate { get; set; }
    public bool IsActive { get; set; } = true;
}

// ─────────────────────── VISITOR ───────────────────────
public class Visitor
{
    public int Id { get; set; }
    public string VisitorName { get; set; } = string.Empty;
    public string CNIC { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public DateTime CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public VisitorStatus Status { get; set; }
    public string PassNumber { get; set; } = string.Empty;
}

// ─────────────────────── ATTENDANCE ───────────────────────
public class Attendance
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public AttendanceStatus Status { get; set; }
    public string Remarks { get; set; } = string.Empty;
}

// ─────────────────────── MESS MENU ───────────────────────
public class MessMenu
{
    public int Id { get; set; }
    public DayOfWeek Day { get; set; }
    public MealType MealType { get; set; }
    public string Items { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

// ─────────────────────── NOTICE ───────────────────────
public class Notice
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string PostedBy { get; set; } = "Admin";
    public DateTime PostedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public NoticePriority Priority { get; set; }
    public bool IsActive { get; set; } = true;
}

// ─────────────────────── AUDIT LOG ───────────────────────
public class AuditLog
{
    public int Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string PerformedBy { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Details { get; set; } = string.Empty;
}

// ─────────────────────── ADMIN ───────────────────────
public class Admin
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = "Admin";
    public DateTime LastLogin { get; set; }
    public bool IsActive { get; set; } = true;
}

// ═══════════════════════ ENUMS ═══════════════════════

public enum RoomType
{
    Single = 1,
    Double = 2,
    Triple = 3,
    Dormitory = 4
}

public enum ComplaintStatus
{
    Open = 1,
    InProgress = 2,
    Resolved = 3,
    Closed = 4
}

public enum PaymentStatus
{
    Pending = 1,
    Paid = 2,
    Late = 3,
    Overdue = 4,
    Waived = 5
}

public enum PaymentMethod
{
    Cash = 1,
    BankTransfer = 2,
    OnlineBanking = 3,
    Cheque = 4,
    JazzCash = 5,
    EasyPaisa = 6
}

public enum StaffRole
{
    Warden = 1,
    Accountant = 2,
    Maintenance = 3,
    Cook = 4,
    Guard = 5,
    Cleaner = 6,
    Manager = 7
}

public enum MealType
{
    Breakfast = 1,
    Lunch = 2,
    Dinner = 3
}

public enum AttendanceStatus
{
    Present = 1,
    Absent = 2,
    Leave = 3,
    Late = 4
}

public enum ComplaintCategory
{
    Maintenance = 1,
    Electrical = 2,
    Plumbing = 3,
    Cleanliness = 4,
    Food = 5,
    Security = 6,
    RoomIssue = 7,
    Other = 8
}

public enum ComplaintPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public enum NoticePriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Urgent = 4
}

public enum VisitorStatus
{
    CheckedIn = 1,
    CheckedOut = 2
}
