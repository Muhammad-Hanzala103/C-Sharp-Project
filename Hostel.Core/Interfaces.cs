using Hostel.Core.Entities;

namespace Hostel.Core.Interfaces;

// ─────────────────────── GENERIC REPOSITORY ───────────────────────
public interface IGenericRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IReadOnlyList<T>> GetAllAsync();
    Task AddAsync(T entity);
    void Update(T entity);
    Task DeleteAsync(int id);
    Task SaveChangesAsync();
}

// ─────────────────────── SERVICE INTERFACES ───────────────────────

public interface IStudentService
{
    Task<Student> RegisterStudentAsync(Student student);
    Task<Student?> GetStudentByIdAsync(int id);
    Task UpdateStudentAsync(Student student);
    Task DeactivateStudentAsync(int studentId);
    Task AssignRoomAsync(int studentId, int roomId);
    Task UnassignRoomAsync(int studentId);
    Task SwapRoomsAsync(int studentId1, int studentId2);
    Task<IReadOnlyList<Student>> GetActiveStudentsAsync();
    Task<IReadOnlyList<Student>> GetAllStudentsAsync();
    Task<IReadOnlyList<Student>> SearchStudentsAsync(string query);
    Task<IReadOnlyList<Student>> GetStudentsByRoomAsync(int roomId);
    Task<IReadOnlyList<Student>> GetStudentsWithoutRoomAsync();
    Task<int> GetActiveCountAsync();
}

public interface IRoomService
{
    Task<Room> CreateRoomAsync(Room room);
    Task<Room?> GetRoomByIdAsync(int id);
    Task UpdateRoomAsync(Room room);
    Task DeleteRoomAsync(int id);
    Task<IReadOnlyList<Room>> GetAllRoomsAsync();
    Task<IReadOnlyList<Room>> GetAvailableRoomsAsync();
    Task<IReadOnlyList<Room>> GetFullRoomsAsync();
    Task<bool> HasCapacityAsync(int roomId);
    Task<int> GetTotalCapacityAsync();
    Task<int> GetTotalOccupancyAsync();
}

public interface IPaymentService
{
    Task<Payment> RecordPaymentAsync(Payment payment);
    Task<IReadOnlyList<Payment>> GetPaymentsForStudentAsync(int studentId);
    Task<IReadOnlyList<Payment>> GetAllPaymentsAsync();
    Task<IReadOnlyList<Payment>> GetPaymentsByMonthAsync(int month, int year);
    Task<IReadOnlyList<Payment>> GetPendingPaymentsAsync();
    Task<decimal> GetTotalRevenueAsync();
    Task<decimal> GetRevenueByMonthAsync(int month, int year);
    Task<string> GenerateReceiptAsync(int paymentId);
}

public interface IFeeStructureService
{
    Task<FeeStructure> CreateFeeStructureAsync(FeeStructure fee);
    Task UpdateFeeStructureAsync(FeeStructure fee);
    Task<IReadOnlyList<FeeStructure>> GetAllFeeStructuresAsync();
    Task<FeeStructure?> GetFeeByRoomTypeAsync(RoomType roomType);
}

public interface IComplaintService
{
    Task<Complaint> CreateComplaintAsync(Complaint complaint);
    Task UpdateComplaintStatusAsync(int complaintId, ComplaintStatus status, string? notes);
    Task AssignComplaintAsync(int complaintId, int staffId);
    Task<IReadOnlyList<Complaint>> GetOpenComplaintsAsync();
    Task<IReadOnlyList<Complaint>> GetAllComplaintsAsync();
    Task<IReadOnlyList<Complaint>> GetComplaintsByStudentAsync(int studentId);
    Task<IReadOnlyList<Complaint>> GetComplaintsByPriorityAsync(ComplaintPriority priority);
    Task<int> GetOpenCountAsync();
}

public interface IStaffService
{
    Task<Staff> AddStaffAsync(Staff staff);
    Task<Staff?> GetStaffByIdAsync(int id);
    Task UpdateStaffAsync(Staff staff);
    Task DeactivateStaffAsync(int staffId);
    Task<IReadOnlyList<Staff>> GetAllStaffAsync();
    Task<IReadOnlyList<Staff>> GetActiveStaffAsync();
    Task<IReadOnlyList<Staff>> GetStaffByRoleAsync(StaffRole role);
    Task<int> GetActiveCountAsync();
}

public interface IVisitorService
{
    Task<Visitor> CheckInVisitorAsync(Visitor visitor);
    Task CheckOutVisitorAsync(int visitorId);
    Task<IReadOnlyList<Visitor>> GetActiveVisitorsAsync();
    Task<IReadOnlyList<Visitor>> GetAllVisitorsAsync();
    Task<IReadOnlyList<Visitor>> GetVisitorsByDateAsync(DateTime date);
    Task<IReadOnlyList<Visitor>> GetVisitorsByStudentAsync(int studentId);
}

public interface IAttendanceService
{
    Task MarkAttendanceAsync(Attendance attendance);
    Task<IReadOnlyList<Attendance>> GetAttendanceByDateAsync(DateTime date);
    Task<IReadOnlyList<Attendance>> GetAttendanceByStudentAsync(int studentId);
    Task<(int present, int absent, int leave)> GetAttendanceStatsAsync(DateTime date);
    Task<double> GetStudentAttendancePercentageAsync(int studentId);
}

public interface IMessMenuService
{
    Task<MessMenu> AddMenuItemAsync(MessMenu menu);
    Task UpdateMenuItemAsync(MessMenu menu);
    Task DeleteMenuItemAsync(int id);
    Task<IReadOnlyList<MessMenu>> GetMenuByDayAsync(DayOfWeek day);
    Task<IReadOnlyList<MessMenu>> GetFullWeekMenuAsync();
    Task<MessMenu?> GetMenuItemAsync(int id);
}

public interface INoticeService
{
    Task<Notice> PostNoticeAsync(Notice notice);
    Task UpdateNoticeAsync(Notice notice);
    Task DeactivateNoticeAsync(int noticeId);
    Task<IReadOnlyList<Notice>> GetActiveNoticesAsync();
    Task<IReadOnlyList<Notice>> GetAllNoticesAsync();
}

public interface IAuditService
{
    Task LogActionAsync(string module, string action, string performedBy, string details);
    Task<IReadOnlyList<AuditLog>> GetRecentLogsAsync(int count = 20);
    Task<IReadOnlyList<AuditLog>> GetLogsByModuleAsync(string module);
    Task<IReadOnlyList<AuditLog>> GetAllLogsAsync();
}

public interface IAdminService
{
    Task<Admin?> AuthenticateAsync(string username, string password);
    Task<Admin> CreateAdminAsync(Admin admin, string password);
    Task ChangePasswordAsync(int adminId, string oldPassword, string newPassword);
    Task<bool> AdminExistsAsync();
}

// ─────────────────────── REPORT MODELS ───────────────────────

public class DashboardStats
{
    public int TotalStudents { get; set; }
    public int ActiveStudents { get; set; }
    public int TotalRooms { get; set; }
    public int OccupiedRooms { get; set; }
    public int AvailableRooms { get; set; }
    public int TotalCapacity { get; set; }
    public int CurrentOccupancy { get; set; }
    public double OccupancyRate { get; set; }
    public int OpenComplaints { get; set; }
    public int ActiveStaff { get; set; }
    public int ActiveVisitors { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public int PendingPayments { get; set; }
}
