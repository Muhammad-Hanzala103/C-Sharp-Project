using Hostel.Core.Entities;
using Hostel.Core.InMemory;
using Hostel.Core.Interfaces;

// Very simple DI wiring for console app
IGenericRepository<Student> studentRepo = new InMemoryRepository<Student>();
IGenericRepository<Room> roomRepo = new InMemoryRepository<Room>();
IGenericRepository<Payment> paymentRepo = new InMemoryRepository<Payment>();
IGenericRepository<Complaint> complaintRepo = new InMemoryRepository<Complaint>();

var studentService = new StudentService(studentRepo, roomRepo);
var roomService = new RoomService(roomRepo);
var paymentService = new PaymentService(paymentRepo);
var complaintService = new ComplaintService(complaintRepo);

var app = new HostelConsoleApp(studentService, roomService, paymentService, complaintService);
app.Run();

public class HostelConsoleApp
{
    private readonly IStudentService _students;
    private readonly IRoomService _rooms;
    private readonly IPaymentService _payments;
    private readonly IComplaintService _complaints;

    public HostelConsoleApp(
        IStudentService students,
        IRoomService rooms,
        IPaymentService payments,
        IComplaintService complaints)
    {
        _students = students;
        _rooms = rooms;
        _payments = payments;
        _complaints = complaints;
    }

    public void Run()
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("===== Hostel Management (Console) =====");
            Console.WriteLine("1. Manage Students");
            Console.WriteLine("2. Manage Rooms");
            Console.WriteLine("3. Manage Payments");
            Console.WriteLine("4. Manage Complaints");
            Console.WriteLine("0. Exit");
            Console.Write("Choose option: ");
            var input = Console.ReadLine();

            switch (input)
            {
                case "1":
                    StudentMenu();
                    break;
                case "2":
                    RoomMenu();
                    break;
                case "3":
                    PaymentMenu();
                    break;
                case "4":
                    ComplaintMenu();
                    break;
                case "0":
                    return;
                default:
                    Console.WriteLine("Invalid option. Press any key...");
                    Console.ReadKey();
                    break;
            }
        }
    }

    private async void StudentMenu()
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("===== Students =====");
            Console.WriteLine("1. Add new student");
            Console.WriteLine("2. List active students");
            Console.WriteLine("3. Assign room");
            Console.WriteLine("0. Back");
            Console.Write("Choose option: ");
            var input = Console.ReadLine();

            switch (input)
            {
                case "1":
                    await AddStudent();
                    break;
                case "2":
                    await ListStudents();
                    break;
                case "3":
                    await AssignRoom();
                    break;
                case "0":
                    return;
                default:
                    Console.WriteLine("Invalid option. Press any key...");
                    Console.ReadKey();
                    break;
            }
        }
    }

    private async Task AddStudent()
    {
        Console.Write("First name: ");
        var firstName = Console.ReadLine() ?? string.Empty;
        Console.Write("Last name: ");
        var lastName = Console.ReadLine() ?? string.Empty;
        Console.Write("Registration no: ");
        var regNo = Console.ReadLine() ?? string.Empty;
        Console.Write("Phone: ");
        var phone = Console.ReadLine() ?? string.Empty;
        Console.Write("Email: ");
        var email = Console.ReadLine() ?? string.Empty;

        var student = new Student
        {
            FirstName = firstName,
            LastName = lastName,
            RegistrationNumber = regNo,
            Phone = phone,
            Email = email
        };

        await _students.RegisterStudentAsync(student);
        Console.WriteLine($"Student created with Id={student.Id}. Press any key...");
        Console.ReadKey();
    }

    private async Task ListStudents()
    {
        var students = await _students.GetActiveStudentsAsync();
        Console.WriteLine($"Total active: {students.Count}");
        foreach (var s in students)
        {
            Console.WriteLine($"{s.Id} - {s.FirstName} {s.LastName} ({s.RegistrationNumber})");
        }
        Console.WriteLine("Press any key...");
        Console.ReadKey();
    }

    private async Task AssignRoom()
    {
        Console.Write("Student Id: ");
        var sIdStr = Console.ReadLine();
        Console.Write("Room Id: ");
        var rIdStr = Console.ReadLine();
        if (!int.TryParse(sIdStr, out var studentId) || !int.TryParse(rIdStr, out var roomId))
        {
            Console.WriteLine("Invalid ids. Press any key...");
            Console.ReadKey();
            return;
        }

        try
        {
            await _students.AssignRoomAsync(studentId, roomId);
            Console.WriteLine("Room assigned. Press any key...");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        Console.ReadKey();
    }

    private async void RoomMenu()
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("===== Rooms =====");
            Console.WriteLine("1. Create room");
            Console.WriteLine("2. List rooms");
            Console.WriteLine("0. Back");
            Console.Write("Choose option: ");
            var input = Console.ReadLine();

            switch (input)
            {
                case "1":
                    await CreateRoom();
                    break;
                case "2":
                    await ListRooms();
                    break;
                case "0":
                    return;
                default:
                    Console.WriteLine("Invalid option. Press any key...");
                    Console.ReadKey();
                    break;
            }
        }
    }

    private async Task CreateRoom()
    {
        Console.Write("Room number: ");
        var number = Console.ReadLine() ?? string.Empty;
        Console.Write("Capacity: ");
        var capStr = Console.ReadLine();
        if (!int.TryParse(capStr, out var capacity))
        {
            Console.WriteLine("Invalid capacity. Press any key...");
            Console.ReadKey();
            return;
        }

        Console.Write("Room type (1=Single,2=Double,3=Dormitory): ");
        var typeStr = Console.ReadLine();
        if (!int.TryParse(typeStr, out var typeVal) || typeVal < 1 || typeVal > 3)
        {
            Console.WriteLine("Invalid type. Press any key...");
            Console.ReadKey();
            return;
        }

        var room = new Room
        {
            RoomNumber = number,
            Capacity = capacity,
            RoomType = (RoomType)typeVal
        };

        await _rooms.CreateRoomAsync(room);
        Console.WriteLine($"Room created with Id={room.Id}. Press any key...");
        Console.ReadKey();
    }

    private async Task ListRooms()
    {
        var rooms = await _rooms.GetAllRoomsAsync();
        Console.WriteLine($"Total rooms: {rooms.Count}");
        foreach (var r in rooms)
        {
            Console.WriteLine($"{r.Id} - {r.RoomNumber} | Cap: {r.Capacity} | Curr: {r.CurrentOccupancy} | Type: {r.RoomType}");
        }
        Console.WriteLine("Press any key...");
        Console.ReadKey();
    }

    private async void PaymentMenu()
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("===== Payments =====");
            Console.WriteLine("1. Record payment");
            Console.WriteLine("2. List payments for student");
            Console.WriteLine("0. Back");
            Console.Write("Choose option: ");
            var input = Console.ReadLine();

            switch (input)
            {
                case "1":
                    await RecordPayment();
                    break;
                case "2":
                    await ListPaymentsForStudent();
                    break;
                case "0":
                    return;
                default:
                    Console.WriteLine("Invalid option. Press any key...");
                    Console.ReadKey();
                    break;
            }
        }
    }

    private async Task RecordPayment()
    {
        Console.Write("Student Id: ");
        var sIdStr = Console.ReadLine();
        Console.Write("Amount: ");
        var amtStr = Console.ReadLine();
        Console.Write("Month (1-12): ");
        var monthStr = Console.ReadLine();
        Console.Write("Year (e.g. 2026): ");
        var yearStr = Console.ReadLine();

        if (!int.TryParse(sIdStr, out var studentId) ||
            !decimal.TryParse(amtStr, out var amount) ||
            !int.TryParse(monthStr, out var month) ||
            !int.TryParse(yearStr, out var year))
        {
            Console.WriteLine("Invalid input. Press any key...");
            Console.ReadKey();
            return;
        }

        var payment = new Payment
        {
            StudentId = studentId,
            Amount = amount,
            Month = month,
            Year = year,
            Status = PaymentStatus.Paid
        };

        await _payments.RecordPaymentAsync(payment);
        Console.WriteLine($"Payment recorded with Id={payment.Id}. Press any key...");
        Console.ReadKey();
    }

    private async Task ListPaymentsForStudent()
    {
        Console.Write("Student Id: ");
        var sIdStr = Console.ReadLine();
        if (!int.TryParse(sIdStr, out var studentId))
        {
            Console.WriteLine("Invalid id. Press any key...");
            Console.ReadKey();
            return;
        }

        var payments = await _payments.GetPaymentsForStudentAsync(studentId);
        Console.WriteLine($"Payments for {studentId}: {payments.Count}");
        foreach (var p in payments)
        {
            Console.WriteLine($"{p.Id} | {p.Amount} | {p.Month}/{p.Year} | {p.Status}");
        }
        Console.WriteLine("Press any key...");
        Console.ReadKey();
    }

    private async void ComplaintMenu()
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("===== Complaints =====");
            Console.WriteLine("1. Create complaint");
            Console.WriteLine("2. List open complaints");
            Console.WriteLine("0. Back");
            Console.Write("Choose option: ");
            var input = Console.ReadLine();

            switch (input)
            {
                case "1":
                    await CreateComplaint();
                    break;
                case "2":
                    await ListOpenComplaints();
                    break;
                case "0":
                    return;
                default:
                    Console.WriteLine("Invalid option. Press any key...");
                    Console.ReadKey();
                    break;
            }
        }
    }

    private async Task CreateComplaint()
    {
        Console.Write("Student Id: ");
        var sIdStr = Console.ReadLine();
        if (!int.TryParse(sIdStr, out var studentId))
        {
            Console.WriteLine("Invalid id. Press any key...");
            Console.ReadKey();
            return;
        }

        Console.Write("Title: ");
        var title = Console.ReadLine() ?? string.Empty;
        Console.Write("Description: ");
        var desc = Console.ReadLine() ?? string.Empty;

        var complaint = new Complaint
        {
            StudentId = studentId,
            Title = title,
            Description = desc
        };

        await _complaints.CreateComplaintAsync(complaint);
        Console.WriteLine($"Complaint created with Id={complaint.Id}. Press any key...");
        Console.ReadKey();
    }

    private async Task ListOpenComplaints()
    {
        var complaints = await _complaints.GetOpenComplaintsAsync();
        Console.WriteLine($"Open complaints: {complaints.Count}");
        foreach (var c in complaints)
        {
            Console.WriteLine($"{c.Id} | Student: {c.StudentId} | {c.Title} | {c.Status}");
        }
        Console.WriteLine("Press any key...");
        Console.ReadKey();
    }
}

