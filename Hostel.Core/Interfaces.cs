using Hostel.Core.Entities;

namespace Hostel.Core.Interfaces;

public interface IGenericRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IReadOnlyList<T>> GetAllAsync();
    Task AddAsync(T entity);
    void Update(T entity);
    Task DeleteAsync(int id);
    Task SaveChangesAsync();
}

public interface IUnitOfWork : IAsyncDisposable
{
    IGenericRepository<Student> Students { get; }
    IGenericRepository<Room> Rooms { get; }
    IGenericRepository<Payment> Payments { get; }
    IGenericRepository<Complaint> Complaints { get; }
    Task<int> SaveChangesAsync();
}

public interface IStudentService
{
    Task<Student> RegisterStudentAsync(Student student);
    Task AssignRoomAsync(int studentId, int roomId);
    Task<IReadOnlyList<Student>> GetActiveStudentsAsync();
}

public interface IRoomService
{
    Task<Room> CreateRoomAsync(Room room);
    Task<IReadOnlyList<Room>> GetAllRoomsAsync();
    Task<bool> HasCapacityAsync(int roomId);
}

public interface IPaymentService
{
    Task<Payment> RecordPaymentAsync(Payment payment);
    Task<IReadOnlyList<Payment>> GetPaymentsForStudentAsync(int studentId);
}

public interface IComplaintService
{
    Task<Complaint> CreateComplaintAsync(Complaint complaint);
    Task<IReadOnlyList<Complaint>> GetOpenComplaintsAsync();
}

