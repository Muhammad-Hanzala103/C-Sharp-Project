using Hostel.Core.Data;
using Hostel.Core.Entities;
using Hostel.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hostel.Core.EfCore;

public class EfRepository<T> : IGenericRepository<T> where T : class
{
    private readonly HostelDbContext _context;
    private readonly DbSet<T> _set;

    public EfRepository(HostelDbContext context)
    {
        _context = context;
        _set = _context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(int id)
    {
        return await _set.FindAsync(id);
    }

    public async Task<IReadOnlyList<T>> GetAllAsync()
    {
        return await _set.ToListAsync();
    }

    public async Task AddAsync(T entity)
    {
        await _set.AddAsync(entity);
    }

    public void Update(T entity)
    {
        _set.Update(entity);
    }

    public async Task DeleteAsync(int id)
    {
        var prop = typeof(T).GetProperty("Id");
        var key = await _set.FirstOrDefaultAsync(x => (int)(prop?.GetValue(x) ?? 0) == id);
        if (key is not null)
        {
            _set.Remove(key);
        }
    }

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}

public class UnitOfWork : IUnitOfWork
{
    private readonly HostelDbContext _context;

    public UnitOfWork(HostelDbContext context)
    {
        _context = context;
        Students = new EfRepository<Student>(_context);
        Rooms = new EfRepository<Room>(_context);
        Payments = new EfRepository<Payment>(_context);
        Complaints = new EfRepository<Complaint>(_context);
    }

    public IGenericRepository<Student> Students { get; }
    public IGenericRepository<Room> Rooms { get; }
    public IGenericRepository<Payment> Payments { get; }
    public IGenericRepository<Complaint> Complaints { get; }

    public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();

    public ValueTask DisposeAsync()
    {
        return _context.DisposeAsync();
    }
}

