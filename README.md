# Hostel Management System (C# Console + ASP.NET Core Web)

## Overview

This solution implements a **Hostel Management System** with:

- **Hostel.Core** – class library with entities, services, and EF Core data access.
- **Hostel.ConsoleApp** – console UI for students, rooms, payments, complaints.
- **Hostel.Web** – ASP.NET Core MVC website with admin login and dashboard.

Target framework is **.NET 8** with **SQLite** for persistence.

## Projects

- `Hostel.Core`
  - `Entities` – `Student`, `Room`, `Booking`, `Payment`, `Complaint` and enums.
  - `Interfaces` – `IGenericRepository<T>`, `IUnitOfWork`, service interfaces.
  - `InMemory` – in-memory repository and basic services (used by console app).
  - `Data` – `HostelDbContext` using EF Core.
  - `EfCore` – EF-based repositories and `UnitOfWork` (used by web app).
- `Hostel.ConsoleApp`
  - `Program.cs` wires in-memory services and exposes menus:
    - Students: add, list, assign rooms.
    - Rooms: create, list.
    - Payments: record, list per student.
    - Complaints: create, list open.
- `Hostel.Web`
  - Uses `HostelDbContext` + `UnitOfWork` with SQLite (`hostel.db`).
  - `HomeController` dashboard: total students, rooms, occupied rooms, open complaints, revenue.
  - `AuthController` with cookie-based admin login.
  - Views with Bootstrap for a clean UI.

## How to Run (after installing .NET 8 SDK)

1. Open a terminal in the solution root (`c:\New folder\Projects\C Sharp Project`).
2. (Optional) Restore/build:
   - `dotnet restore`
   - `dotnet build`
3. Run console app:
   - `dotnet run --project Hostel.ConsoleApp/Hostel.ConsoleApp.csproj`
   - Use menus to add rooms, students, payments, and complaints (in-memory for demo).
4. Run web app:
   - `dotnet run --project "Hostel.Web/Hostel.Web.csproj"`
   - Browse to `https://localhost:5001` or the URL shown in the console.
   - Login with `admin / admin123`.
   - View dashboard cards for hostel statistics.

> Note: EF Core is configured for SQLite with `hostel.db`. You can later wire the console app to use `HostelDbContext` instead of in-memory storage if you want shared persistence.

## Demo Script (for presentation)

1. **Explain architecture**
   - Show `Hostel.Core` (entities, services, EF Core) as the shared business layer.
   - Show console and web projects both depending on `Hostel.Core`.
2. **Console demo**
   - Run console app.
   - Create a few rooms and students, assign rooms, record some payments, and add complaints.
   - Highlight menu structure and validations (invalid input handling).
3. **Web demo**
   - Run web app and login (`admin / admin123`).
   - Show dashboard cards and explain each metric.
   - Explain how EF Core + `UnitOfWork` power the statistics.
4. **Technical highlights**
   - Multi-project solution, separation of concerns.
   - Use of interfaces, in-memory repository, and EF Core repository.
   - Cookie-based authentication in `Hostel.Web`.
5. **Future improvements (talk-only)**
   - Connect console app to same SQLite DB.
   - Add full CRUD pages for students/rooms/payments/complaints.
   - Add charts on the dashboard using a JS chart library.

