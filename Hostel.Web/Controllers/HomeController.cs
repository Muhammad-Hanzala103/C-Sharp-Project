using Hostel.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hostel.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly IUnitOfWork _uow;

    public HomeController(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IActionResult> Index()
    {
        var students = await _uow.Students.GetAllAsync();
        var rooms = await _uow.Rooms.GetAllAsync();
        var complaints = await _uow.Complaints.GetAllAsync();
        var payments = await _uow.Payments.GetAllAsync();

        var model = new DashboardViewModel
        {
            TotalStudents = students.Count,
            TotalRooms = rooms.Count,
            OccupiedRooms = rooms.Count(r => r.CurrentOccupancy > 0),
            OpenComplaints = complaints.Count(c => c.Status != Core.Entities.ComplaintStatus.Resolved),
            TotalPayments = payments.Count,
            TotalRevenue = payments.Sum(p => p.Amount)
        };

        return View(model);
    }
}

public class DashboardViewModel
{
    public int TotalStudents { get; set; }
    public int TotalRooms { get; set; }
    public int OccupiedRooms { get; set; }
    public int OpenComplaints { get; set; }
    public int TotalPayments { get; set; }
    public decimal TotalRevenue { get; set; }
}

