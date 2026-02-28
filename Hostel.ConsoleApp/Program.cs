using Hostel.Core.Entities;
using Hostel.Core.Interfaces;
using Hostel.Core.Services;
using Hostel.ConsoleApp;

// ═══════════════════════════════════════════════════════════════
//  BOOTSTRAP — Wire up JSON-file-based repositories & services
// ═══════════════════════════════════════════════════════════════

var dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "hostel_data");

var studentRepo = new JsonFileRepository<Student>(dataDir, "students.json");
var roomRepo = new JsonFileRepository<Room>(dataDir, "rooms.json");
var bookingRepo = new JsonFileRepository<Booking>(dataDir, "bookings.json");
var paymentRepo = new JsonFileRepository<Payment>(dataDir, "payments.json");
var feeRepo = new JsonFileRepository<FeeStructure>(dataDir, "fees.json");
var complaintRepo = new JsonFileRepository<Complaint>(dataDir, "complaints.json");
var staffRepo = new JsonFileRepository<Staff>(dataDir, "staff.json");
var visitorRepo = new JsonFileRepository<Visitor>(dataDir, "visitors.json");
var attendanceRepo = new JsonFileRepository<Attendance>(dataDir, "attendance.json");
var messRepo = new JsonFileRepository<MessMenu>(dataDir, "mess_menu.json");
var noticeRepo = new JsonFileRepository<Notice>(dataDir, "notices.json");
var auditRepo = new JsonFileRepository<AuditLog>(dataDir, "audit.json");
var adminRepo = new JsonFileRepository<Admin>(dataDir, "admins.json");

var studentService = new StudentService(studentRepo, roomRepo, bookingRepo);
var roomService = new RoomService(roomRepo);
var paymentService = new PaymentService(paymentRepo);
var feeService = new FeeStructureService(feeRepo);
var complaintService = new ComplaintService(complaintRepo);
var staffService = new StaffService(staffRepo);
var visitorService = new VisitorService(visitorRepo);
var attendanceService = new AttendanceService(attendanceRepo);
var messService = new MessMenuService(messRepo);
var noticeService = new NoticeService(noticeRepo);
var auditService = new AuditService(auditRepo);
var adminService = new AdminService(adminRepo);

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ═══════════════════════════════════════════════════════════════
//  DATA SEEDING — Auto-populate demo data on first run
// ═══════════════════════════════════════════════════════════════
var seeder = new DataSeeder(
    studentService, roomService, staffService, feeService,
    messService, noticeService, paymentService, auditService, studentRepo);

if (!await seeder.IsSeededAsync())
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("\n    ⏳ First run detected — seeding demo data...");
    Console.ResetColor();
    await seeder.SeedAsync();
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("    ✅ Demo data loaded! (10 students, 15 rooms, 5 staff, menus & more)");
    Console.ResetColor();
    Console.WriteLine();
}

// ═══════════════════════════════════════════════════════════════
//  LAUNCH APP
// ═══════════════════════════════════════════════════════════════
var app = new HostelApp(
    studentService, roomService, paymentService, feeService,
    complaintService, staffService, visitorService, attendanceService,
    messService, noticeService, auditService, adminService);

await app.RunAsync();
