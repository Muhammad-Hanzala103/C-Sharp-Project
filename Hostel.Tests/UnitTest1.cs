using Hostel.Core.Entities;
using Hostel.Core.Interfaces;
using Hostel.Core.Services;

namespace Hostel.Tests;

// ═══════════════════════════════════════════════════════════════
//  UNIT TESTS — Hostel Management System
// ═══════════════════════════════════════════════════════════════

public class StudentServiceTests : IDisposable
{
    private readonly string _testDir;
    private readonly StudentService _service;
    private readonly JsonFileRepository<Student> _studentRepo;
    private readonly JsonFileRepository<Room> _roomRepo;
    private readonly JsonFileRepository<Booking> _bookingRepo;

    public StudentServiceTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"hostel_test_{Guid.NewGuid():N}");
        _studentRepo = new JsonFileRepository<Student>(_testDir, "students.json");
        _roomRepo = new JsonFileRepository<Room>(_testDir, "rooms.json");
        _bookingRepo = new JsonFileRepository<Booking>(_testDir, "bookings.json");
        _service = new StudentService(_studentRepo, _roomRepo, _bookingRepo);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir)) Directory.Delete(_testDir, true);
    }

    [Fact]
    public async Task RegisterStudent_SetsActive_And_AssignsId()
    {
        var student = await _service.RegisterStudentAsync(new Student
        {
            FirstName = "Test",
            LastName = "User",
            RegistrationNumber = "FA22-TST-001",
            CNIC = "35202-0000000-0",
            Phone = "0300-0000000",
            Email = "test@uni.edu",
            Department = "CS"
        });

        Assert.True(student.IsActive);
        Assert.True(student.Id > 0);
        Assert.Equal("Test User", student.FullName);
    }

    [Fact]
    public async Task GetActiveStudents_FiltersInactive()
    {
        await _service.RegisterStudentAsync(new Student { FirstName = "Active", LastName = "One", RegistrationNumber = "R1", CNIC = "C1", Phone = "P1", Email = "e1", Department = "CS" });
        var s2 = await _service.RegisterStudentAsync(new Student { FirstName = "Inactive", LastName = "Two", RegistrationNumber = "R2", CNIC = "C2", Phone = "P2", Email = "e2", Department = "CS" });
        await _service.DeactivateStudentAsync(s2.Id);

        var active = await _service.GetActiveStudentsAsync();
        Assert.Single(active);
        Assert.Equal("Active One", active[0].FullName);
    }

    [Fact]
    public async Task AssignRoom_IncrementsOccupancy()
    {
        var room = new Room { RoomNumber = "T-101", Capacity = 2, RoomType = RoomType.Double, MonthlyRent = 5000m, IsActive = true };
        await _roomRepo.AddAsync(room);
        await _roomRepo.SaveChangesAsync();

        var student = await _service.RegisterStudentAsync(new Student { FirstName = "Room", LastName = "Test", RegistrationNumber = "R3", CNIC = "C3", Phone = "P3", Email = "e3", Department = "CS" });
        await _service.AssignRoomAsync(student.Id, room.Id);

        var updated = await _roomRepo.GetByIdAsync(room.Id);
        Assert.Equal(1, updated!.CurrentOccupancy);
        Assert.Equal("T-101", (await _service.GetStudentByIdAsync(student.Id))!.RoomNumber);
    }

    [Fact]
    public async Task SearchStudents_FindsByName()
    {
        await _service.RegisterStudentAsync(new Student { FirstName = "Ahmed", LastName = "Khan", RegistrationNumber = "R4", CNIC = "C4", Phone = "P4", Email = "e4", Department = "CS" });
        await _service.RegisterStudentAsync(new Student { FirstName = "Bilal", LastName = "Ali", RegistrationNumber = "R5", CNIC = "C5", Phone = "P5", Email = "e5", Department = "EE" });

        var results = await _service.SearchStudentsAsync("Ahmed");
        Assert.Single(results);
        Assert.Equal("Ahmed Khan", results[0].FullName);
    }

    [Fact]
    public async Task SwapRooms_Swaps()
    {
        var r1 = new Room { RoomNumber = "X-1", Capacity = 1, RoomType = RoomType.Single, MonthlyRent = 10000m, IsActive = true };
        var r2 = new Room { RoomNumber = "X-2", Capacity = 1, RoomType = RoomType.Single, MonthlyRent = 10000m, IsActive = true };
        await _roomRepo.AddAsync(r1); await _roomRepo.AddAsync(r2);
        await _roomRepo.SaveChangesAsync();

        var s1 = await _service.RegisterStudentAsync(new Student { FirstName = "S", LastName = "1", RegistrationNumber = "R6", CNIC = "C6", Phone = "P6", Email = "e6", Department = "CS" });
        var s2 = await _service.RegisterStudentAsync(new Student { FirstName = "S", LastName = "2", RegistrationNumber = "R7", CNIC = "C7", Phone = "P7", Email = "e7", Department = "CS" });
        await _service.AssignRoomAsync(s1.Id, r1.Id);
        await _service.AssignRoomAsync(s2.Id, r2.Id);
        await _service.SwapRoomsAsync(s1.Id, s2.Id);

        var u1 = await _service.GetStudentByIdAsync(s1.Id);
        var u2 = await _service.GetStudentByIdAsync(s2.Id);
        Assert.Equal("X-2", u1!.RoomNumber);
        Assert.Equal("X-1", u2!.RoomNumber);
    }
}

public class RoomServiceTests : IDisposable
{
    private readonly string _testDir;
    private readonly RoomService _service;
    private readonly JsonFileRepository<Room> _repo;

    public RoomServiceTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"hostel_test_{Guid.NewGuid():N}");
        _repo = new JsonFileRepository<Room>(_testDir, "rooms.json");
        _service = new RoomService(_repo);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir)) Directory.Delete(_testDir, true);
    }

    [Fact]
    public async Task CreateRoom_SetsActive()
    {
        var room = await _service.CreateRoomAsync(new Room { RoomNumber = "A-100", Capacity = 4, RoomType = RoomType.Triple, MonthlyRent = 5000m });
        Assert.True(room.IsActive);
        Assert.Equal(0, room.CurrentOccupancy);
    }

    [Fact]
    public async Task GetAvailableRooms_ExcludesFull()
    {
        var r1 = await _service.CreateRoomAsync(new Room { RoomNumber = "A-1", Capacity = 1, RoomType = RoomType.Single, MonthlyRent = 10000m });
        await _service.CreateRoomAsync(new Room { RoomNumber = "A-2", Capacity = 2, RoomType = RoomType.Double, MonthlyRent = 8000m });
        // Manually fill room 1
        r1.CurrentOccupancy = 1;
        await _service.UpdateRoomAsync(r1);

        var available = await _service.GetAvailableRoomsAsync();
        Assert.Single(available);
        Assert.Equal("A-2", available[0].RoomNumber);
    }

    [Fact]
    public async Task DeleteRoom_ThrowsIfOccupied()
    {
        var room = await _service.CreateRoomAsync(new Room { RoomNumber = "D-1", Capacity = 2, RoomType = RoomType.Double, MonthlyRent = 5000m });
        room.CurrentOccupancy = 1;
        await _service.UpdateRoomAsync(room);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteRoomAsync(room.Id));
    }
}

public class PaymentServiceTests : IDisposable
{
    private readonly string _testDir;
    private readonly PaymentService _service;

    public PaymentServiceTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"hostel_test_{Guid.NewGuid():N}");
        var repo = new JsonFileRepository<Payment>(_testDir, "payments.json");
        _service = new PaymentService(repo);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir)) Directory.Delete(_testDir, true);
    }

    [Fact]
    public async Task RecordPayment_GeneratesReceipt()
    {
        var payment = await _service.RecordPaymentAsync(new Payment
        {
            StudentId = 1, StudentName = "Test",
            Amount = 8000m, Month = 3, Year = 2026,
            Method = PaymentMethod.Cash, Status = PaymentStatus.Paid
        });

        Assert.StartsWith("RCP-", payment.ReceiptNumber);
        Assert.Equal(8000m, payment.Amount);
    }

    [Fact]
    public async Task GetTotalRevenue_OnlyCountsPaid()
    {
        await _service.RecordPaymentAsync(new Payment { StudentId = 1, StudentName = "A", Amount = 5000m, Month = 1, Year = 2026, Method = PaymentMethod.Cash, Status = PaymentStatus.Paid });
        await _service.RecordPaymentAsync(new Payment { StudentId = 2, StudentName = "B", Amount = 3000m, Month = 1, Year = 2026, Method = PaymentMethod.Cash, Status = PaymentStatus.Pending });

        var total = await _service.GetTotalRevenueAsync();
        Assert.Equal(5000m, total);
    }
}

public class AdminServiceTests : IDisposable
{
    private readonly string _testDir;
    private readonly AdminService _service;

    public AdminServiceTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"hostel_test_{Guid.NewGuid():N}");
        var repo = new JsonFileRepository<Admin>(_testDir, "admins.json");
        _service = new AdminService(repo);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir)) Directory.Delete(_testDir, true);
    }

    [Fact]
    public async Task CreateAdmin_CanAuthenticate()
    {
        await _service.CreateAdminAsync(new Admin { Username = "testadmin", FullName = "Test Admin", Role = "Admin" }, "Pass123");
        var result = await _service.AuthenticateAsync("testadmin", "Pass123");
        Assert.NotNull(result);
        Assert.Equal("Test Admin", result.FullName);
    }

    [Fact]
    public async Task Authenticate_WrongPassword_ReturnsNull()
    {
        await _service.CreateAdminAsync(new Admin { Username = "admin2", FullName = "Admin 2", Role = "Admin" }, "Correct1");
        var result = await _service.AuthenticateAsync("admin2", "WrongPassword");
        Assert.Null(result);
    }

    [Fact]
    public async Task ChangePassword_Works()
    {
        var admin = await _service.CreateAdminAsync(new Admin { Username = "admin3", FullName = "Admin 3", Role = "Admin" }, "Old123");
        await _service.ChangePasswordAsync(admin.Id, "Old123", "New456");

        var result = await _service.AuthenticateAsync("admin3", "New456");
        Assert.NotNull(result);
    }

    [Fact]
    public async Task ValidatePasswordStrength_RejectsWeak()
    {
        Assert.False(await _service.ValidatePasswordStrengthAsync("abc"));
        Assert.False(await _service.ValidatePasswordStrengthAsync("abcdef"));
        Assert.True(await _service.ValidatePasswordStrengthAsync("Abc123"));
    }
}

public class ComplaintServiceTests : IDisposable
{
    private readonly string _testDir;
    private readonly ComplaintService _service;

    public ComplaintServiceTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"hostel_test_{Guid.NewGuid():N}");
        var repo = new JsonFileRepository<Complaint>(_testDir, "complaints.json");
        _service = new ComplaintService(repo);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir)) Directory.Delete(_testDir, true);
    }

    [Fact]
    public async Task CreateComplaint_DefaultsToOpen()
    {
        var c = await _service.CreateComplaintAsync(new Complaint
        {
            StudentId = 1, StudentName = "Test", Title = "Broken Fan",
            Description = "Fan not working", Category = ComplaintCategory.Electrical,
            Priority = ComplaintPriority.High
        });
        Assert.Equal(ComplaintStatus.Open, c.Status);
    }

    [Fact]
    public async Task UpdateStatus_SetsResolvedDate()
    {
        var c = await _service.CreateComplaintAsync(new Complaint
        {
            StudentId = 1, StudentName = "Test", Title = "Leak",
            Category = ComplaintCategory.Plumbing, Priority = ComplaintPriority.Medium
        });

        await _service.UpdateComplaintStatusAsync(c.Id, ComplaintStatus.Resolved, "Fixed by plumber");
        var all = await _service.GetAllComplaintsAsync();
        var updated = all.First(x => x.Id == c.Id);
        Assert.Equal(ComplaintStatus.Resolved, updated.Status);
        Assert.NotNull(updated.ResolvedAt);
    }
}