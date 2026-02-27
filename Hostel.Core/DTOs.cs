using Hostel.Core.Entities;

namespace Hostel.Core.DTOs;

public record StudentDto(
    int Id, string FirstName, string LastName, string RegistrationNumber,
    string CNIC, string Phone, string Email, string Address,
    string GuardianName, string GuardianPhone, string Department,
    int? RoomId, string? RoomNumber, bool IsActive, DateTime JoinDate);

public record RoomDto(
    int Id, string RoomNumber, int Floor, int Capacity, int CurrentOccupancy,
    RoomType RoomType, decimal MonthlyRent, bool HasAC, bool HasAttachedBath, bool IsActive);

public record PaymentDto(
    int Id, int StudentId, string StudentName, decimal Amount,
    int Month, int Year, DateTime PaymentDate, PaymentMethod Method,
    string ReceiptNumber, PaymentStatus Status, string Remarks);

public record FeeStructureDto(
    int Id, RoomType RoomType, decimal MonthlyRent, decimal MessFee,
    decimal UtilityCharges, decimal SecurityDeposit, decimal LaundryFee,
    decimal TotalMonthly, string Description);

public record ComplaintDto(
    int Id, int StudentId, string StudentName, string Title, string Description,
    ComplaintCategory Category, ComplaintPriority Priority,
    DateTime CreatedAt, DateTime? ResolvedAt, ComplaintStatus Status,
    int? AssignedStaffId, string? AssignedStaffName, string? ResolutionNotes);

public record StaffDto(
    int Id, string FullName, string Phone, string Email, string CNIC,
    StaffRole Role, decimal Salary, string Shift, DateTime JoinDate, bool IsActive);

public record VisitorDto(
    int Id, string VisitorName, string CNIC, string Phone, string Relationship,
    int StudentId, string StudentName, string Purpose,
    DateTime CheckInTime, DateTime? CheckOutTime, VisitorStatus Status, string PassNumber);

public record AttendanceDto(
    int Id, int StudentId, string StudentName,
    DateTime Date, AttendanceStatus Status, string Remarks);

public record MessMenuDto(
    int Id, DayOfWeek Day, MealType MealType, string Items, bool IsActive);

public record NoticeDto(
    int Id, string Title, string Content, string PostedBy,
    DateTime PostedAt, DateTime? ExpiresAt, NoticePriority Priority, bool IsActive);

public record AuditLogDto(
    int Id, string Action, string Module, string PerformedBy,
    DateTime Timestamp, string Details);
