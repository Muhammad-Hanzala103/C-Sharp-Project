using Hostel.Core.Entities;

namespace Hostel.Core.DTOs;

public record StudentDto(
    int Id,
    string FirstName,
    string LastName,
    string RegistrationNumber,
    string Phone,
    string Email,
    int? RoomId,
    string? RoomNumber,
    bool IsActive);

public record RoomDto(
    int Id,
    string RoomNumber,
    int Capacity,
    int CurrentOccupancy,
    RoomType RoomType,
    bool IsActive);

public record PaymentDto(
    int Id,
    int StudentId,
    string StudentName,
    decimal Amount,
    int Month,
    int Year,
    DateTime PaymentDate,
    PaymentStatus Status);

public record ComplaintDto(
    int Id,
    int StudentId,
    string StudentName,
    string Title,
    string Description,
    DateTime CreatedAt,
    ComplaintStatus Status,
    string? HandledBy);

