using Hostel.Core.Entities;
using Hostel.Core.Interfaces;

namespace Hostel.Core.InMemory;

public class InMemoryRepository<T> : IGenericRepository<T> where T : class
{
    private readonly List<T> _items = new();
    private int _nextId = 1;

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

    public Task DeleteAsync(int id)
    {
        var prop = typeof(T).GetProperty("Id");
        var existing = _items.FirstOrDefault(x => (int)(prop?.GetValue(x) ?? 0) == id);
        if (existing is not null)
        {
            _items.Remove(existing);
        }
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync()
    {
        // In-memory implementation has nothing to persist.
        return Task.CompletedTask;
    }
}

public class StudentService : IStudentService
{
    private readonly IGenericRepository<Student> _students;
    private readonly IGenericRepository<Room> _rooms;

    public StudentService(IGenericRepository<Student> students, IGenericRepository<Room> rooms)
    {
        _students = students;
        _rooms = rooms;
    }

    public async Task<Student> RegisterStudentAsync(Student student)
    {
        student.JoinDate = DateTime.UtcNow;
        student.IsActive = true;
        await _students.AddAsync(student);
        await _students.SaveChangesAsync();
        return student;
    }

    public async Task AssignRoomAsync(int studentId, int roomId)
    {
        var student = await _students.GetByIdAsync(studentId) ?? throw new InvalidOperationException("Student not found");
        var room = await _rooms.GetByIdAsync(roomId) ?? throw new InvalidOperationException("Room not found");

        if (room.CurrentOccupancy >= room.Capacity)
        {
            throw new InvalidOperationException("Room is full");
        }

        room.CurrentOccupancy++;
        student.RoomId = room.Id;
        await _rooms.SaveChangesAsync();
        await _students.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<Student>> GetActiveStudentsAsync()
    {
        var all = await _students.GetAllAsync();
        return all.Where(s => s.IsActive).ToList();
    }
}

public class RoomService : IRoomService
{
    private readonly IGenericRepository<Room> _rooms;

    public RoomService(IGenericRepository<Room> rooms)
    {
        _rooms = rooms;
    }

    public async Task<Room> CreateRoomAsync(Room room)
    {
        await _rooms.AddAsync(room);
        await _rooms.SaveChangesAsync();
        return room;
    }

    public Task<IReadOnlyList<Room>> GetAllRoomsAsync() => _rooms.GetAllAsync();

    public async Task<bool> HasCapacityAsync(int roomId)
    {
        var room = await _rooms.GetByIdAsync(roomId);
        return room is not null && room.CurrentOccupancy < room.Capacity;
    }
}

public class PaymentService : IPaymentService
{
    private readonly IGenericRepository<Payment> _payments;

    public PaymentService(IGenericRepository<Payment> payments)
    {
        _payments = payments;
    }

    public async Task<Payment> RecordPaymentAsync(Payment payment)
    {
        payment.PaymentDate = DateTime.UtcNow;
        await _payments.AddAsync(payment);
        await _payments.SaveChangesAsync();
        return payment;
    }

    public async Task<IReadOnlyList<Payment>> GetPaymentsForStudentAsync(int studentId)
    {
        var all = await _payments.GetAllAsync();
        return all.Where(p => p.StudentId == studentId).ToList();
    }
}

public class ComplaintService : IComplaintService
{
    private readonly IGenericRepository<Complaint> _complaints;

    public ComplaintService(IGenericRepository<Complaint> complaints)
    {
        _complaints = complaints;
    }

    public async Task<Complaint> CreateComplaintAsync(Complaint complaint)
    {
        complaint.CreatedAt = DateTime.UtcNow;
        complaint.Status = ComplaintStatus.Open;
        await _complaints.AddAsync(complaint);
        await _complaints.SaveChangesAsync();
        return complaint;
    }

    public async Task<IReadOnlyList<Complaint>> GetOpenComplaintsAsync()
    {
        var all = await _complaints.GetAllAsync();
        return all.Where(c => c.Status == ComplaintStatus.Open || c.Status == ComplaintStatus.InProgress).ToList();
    }
}

