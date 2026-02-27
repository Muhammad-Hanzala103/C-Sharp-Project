using Hostel.Core.Entities;

namespace Hostel.ConsoleApp;

// Student & Room Management Modules
public partial class HostelApp
{
    // ═══════════════════════════════════════════════════════════════
    //  STUDENT MANAGEMENT
    // ═══════════════════════════════════════════════════════════════
    private async Task StudentMenuAsync()
    {
        while (true)
        {
            ConsoleUI.ShowMenu("👨‍🎓 STUDENT MANAGEMENT",
                ("1", "Register New Student", "➕"),
                ("2", "List All Active Students", "📋"),
                ("3", "Search Students", "🔍"),
                ("4", "View Student Details", "👁️"),
                ("5", "Edit Student", "✏️"),
                ("6", "Assign Room to Student", "🏠"),
                ("7", "Unassign Room", "🔓"),
                ("8", "Swap Rooms Between Students", "🔄"),
                ("9", "Students Without Room", "⚠️"),
                ("10", "Deactivate Student", "❌"),
                ("0", "Back to Main Menu", "↩️"));

            var input = Console.ReadLine()?.Trim();
            switch (input)
            {
                case "1": await RegisterStudentAsync(); break;
                case "2": await ListStudentsAsync(); break;
                case "3": await SearchStudentsAsync(); break;
                case "4": await ViewStudentDetailsAsync(); break;
                case "5": await EditStudentAsync(); break;
                case "6": await AssignRoomToStudentAsync(); break;
                case "7": await UnassignRoomFromStudentAsync(); break;
                case "8": await SwapStudentRoomsAsync(); break;
                case "9": await ListStudentsWithoutRoomAsync(); break;
                case "10": await DeactivateStudentAsync(); break;
                case "0": return;
                default: ConsoleUI.ShowError("Invalid option!"); ConsoleUI.Pause(); break;
            }
        }
    }

    private async Task RegisterStudentAsync()
    {
        ConsoleUI.ShowHeader("➕ REGISTER NEW STUDENT");
        var student = new Student
        {
            FirstName = ConsoleUI.ReadInput("First Name"),
            LastName = ConsoleUI.ReadInput("Last Name"),
            RegistrationNumber = ConsoleUI.ReadInput("Registration Number (e.g. FA22-BSE-001)"),
            CNIC = ConsoleUI.ReadInput("CNIC (e.g. 35202-1234567-1)"),
            Phone = ConsoleUI.ReadInput("Phone Number"),
            Email = ConsoleUI.ReadInput("Email Address"),
            Address = ConsoleUI.ReadInput("Home Address"),
            GuardianName = ConsoleUI.ReadInput("Guardian Name"),
            GuardianPhone = ConsoleUI.ReadInput("Guardian Phone"),
            Department = ConsoleUI.ReadInput("Department (e.g. CS, SE, EE)")
        };

        if (string.IsNullOrEmpty(student.FirstName) || string.IsNullOrEmpty(student.RegistrationNumber))
        {
            ConsoleUI.ShowError("Name and Registration Number are required!");
            ConsoleUI.Pause(); return;
        }

        await _students.RegisterStudentAsync(student);
        await _audit.LogActionAsync("Student", "Register", _currentUser, $"Registered {student.FullName} ({student.RegistrationNumber})");
        ConsoleUI.ShowSuccess($"Student '{student.FullName}' registered successfully! (ID: {student.Id})");
        ConsoleUI.Pause();
    }

    private async Task ListStudentsAsync()
    {
        ConsoleUI.ShowHeader("📋 ACTIVE STUDENTS");
        var students = await _students.GetActiveStudentsAsync();
        var rows = students.Select(s => new[] {
            s.Id.ToString(), s.RegistrationNumber, s.FullName, s.Department, s.Phone, s.RoomNumber ?? "—"
        }).ToList();
        ConsoleUI.ShowTable(new[] { "ID", "Reg #", "Name", "Dept", "Phone", "Room" }, rows);
        ConsoleUI.Pause();
    }

    private async Task SearchStudentsAsync()
    {
        ConsoleUI.ShowHeader("🔍 SEARCH STUDENTS");
        var query = ConsoleUI.ReadInput("Search (name, reg#, phone, dept)");
        if (string.IsNullOrEmpty(query)) { ConsoleUI.ShowWarning("Please enter search term"); ConsoleUI.Pause(); return; }
        var students = await _students.SearchStudentsAsync(query);
        var rows = students.Select(s => new[] {
            s.Id.ToString(), s.RegistrationNumber, s.FullName, s.Department, s.Phone, s.RoomNumber ?? "—", s.IsActive ? "Active" : "Inactive"
        }).ToList();
        ConsoleUI.ShowTable(new[] { "ID", "Reg #", "Name", "Dept", "Phone", "Room", "Status" }, rows);
        ConsoleUI.Pause();
    }

    private async Task ViewStudentDetailsAsync()
    {
        ConsoleUI.ShowHeader("👁️ STUDENT DETAILS");
        var id = ConsoleUI.ReadInt("Enter Student ID");
        var student = await _students.GetStudentByIdAsync(id);
        if (student == null) { ConsoleUI.ShowError("Student not found!"); ConsoleUI.Pause(); return; }

        ConsoleUI.ShowDetailRow("ID", student.Id.ToString());
        ConsoleUI.ShowDetailRow("Name", student.FullName);
        ConsoleUI.ShowDetailRow("Reg Number", student.RegistrationNumber);
        ConsoleUI.ShowDetailRow("CNIC", student.CNIC);
        ConsoleUI.ShowDetailRow("Phone", student.Phone);
        ConsoleUI.ShowDetailRow("Email", student.Email);
        ConsoleUI.ShowDetailRow("Address", student.Address);
        ConsoleUI.ShowDetailRow("Guardian", $"{student.GuardianName} ({student.GuardianPhone})");
        ConsoleUI.ShowDetailRow("Department", student.Department);
        ConsoleUI.ShowDetailRow("Room", student.RoomNumber ?? "Not Assigned");
        ConsoleUI.ShowDetailRow("Join Date", student.JoinDate.ToString("dd-MMM-yyyy"));
        ConsoleUI.ShowDetailRow("Status", student.IsActive ? "✅ Active" : "❌ Inactive");

        // Show payment history
        var payments = await _payments.GetPaymentsForStudentAsync(id);
        if (payments.Count > 0)
        {
            ConsoleUI.ShowSubHeader("Payment History");
            var pRows = payments.Select(p => new[] {
                p.Id.ToString(), $"Rs. {p.Amount:N0}", $"{p.Month:D2}/{p.Year}", p.Status.ToString()
            }).ToList();
            ConsoleUI.ShowTable(new[] { "ID", "Amount", "Period", "Status" }, pRows);
        }

        // Show attendance percentage
        var attPct = await _attendance.GetStudentAttendancePercentageAsync(id);
        if (attPct > 0)
            ConsoleUI.ShowProgressBar("Attendance", attPct, attPct >= 75 ? ConsoleColor.Green : ConsoleColor.Red);

        ConsoleUI.Pause();
    }

    private async Task EditStudentAsync()
    {
        ConsoleUI.ShowHeader("✏️ EDIT STUDENT");
        var id = ConsoleUI.ReadInt("Enter Student ID");
        var student = await _students.GetStudentByIdAsync(id);
        if (student == null) { ConsoleUI.ShowError("Student not found!"); ConsoleUI.Pause(); return; }

        ConsoleUI.ShowInfo($"Editing: {student.FullName} — press Enter to keep current value");

        var fn = ConsoleUI.ReadInput($"First Name [{student.FirstName}]");
        if (!string.IsNullOrEmpty(fn)) student.FirstName = fn;
        var ln = ConsoleUI.ReadInput($"Last Name [{student.LastName}]");
        if (!string.IsNullOrEmpty(ln)) student.LastName = ln;
        var ph = ConsoleUI.ReadInput($"Phone [{student.Phone}]");
        if (!string.IsNullOrEmpty(ph)) student.Phone = ph;
        var em = ConsoleUI.ReadInput($"Email [{student.Email}]");
        if (!string.IsNullOrEmpty(em)) student.Email = em;
        var dept = ConsoleUI.ReadInput($"Department [{student.Department}]");
        if (!string.IsNullOrEmpty(dept)) student.Department = dept;
        var addr = ConsoleUI.ReadInput($"Address [{student.Address}]");
        if (!string.IsNullOrEmpty(addr)) student.Address = addr;

        await _students.UpdateStudentAsync(student);
        await _audit.LogActionAsync("Student", "Edit", _currentUser, $"Updated {student.FullName}");
        ConsoleUI.ShowSuccess("Student updated successfully!");
        ConsoleUI.Pause();
    }

    private async Task AssignRoomToStudentAsync()
    {
        ConsoleUI.ShowHeader("🏠 ASSIGN ROOM TO STUDENT");
        // Show available rooms
        var rooms = await _rooms.GetAvailableRoomsAsync();
        if (rooms.Count == 0) { ConsoleUI.ShowWarning("No rooms available!"); ConsoleUI.Pause(); return; }
        var rRows = rooms.Select(r => new[] {
            r.Id.ToString(), r.RoomNumber, r.RoomType.ToString(), $"{r.CurrentOccupancy}/{r.Capacity}", $"Rs. {r.MonthlyRent:N0}"
        }).ToList();
        ConsoleUI.ShowTable(new[] { "ID", "Room #", "Type", "Occupancy", "Rent" }, rRows);

        var studentId = ConsoleUI.ReadInt("Student ID");
        var roomId = ConsoleUI.ReadInt("Room ID");
        try
        {
            await _students.AssignRoomAsync(studentId, roomId);
            await _audit.LogActionAsync("Student", "Assign Room", _currentUser, $"Student {studentId} → Room {roomId}");
            ConsoleUI.ShowSuccess("Room assigned successfully!");
        }
        catch (Exception ex) { ConsoleUI.ShowError(ex.Message); }
        ConsoleUI.Pause();
    }

    private async Task UnassignRoomFromStudentAsync()
    {
        ConsoleUI.ShowHeader("🔓 UNASSIGN ROOM");
        var studentId = ConsoleUI.ReadInt("Student ID");
        try
        {
            await _students.UnassignRoomAsync(studentId);
            await _audit.LogActionAsync("Student", "Unassign Room", _currentUser, $"Student {studentId}");
            ConsoleUI.ShowSuccess("Room unassigned successfully!");
        }
        catch (Exception ex) { ConsoleUI.ShowError(ex.Message); }
        ConsoleUI.Pause();
    }

    private async Task SwapStudentRoomsAsync()
    {
        ConsoleUI.ShowHeader("🔄 SWAP ROOMS");
        var id1 = ConsoleUI.ReadInt("Student 1 ID");
        var id2 = ConsoleUI.ReadInt("Student 2 ID");
        try
        {
            await _students.SwapRoomsAsync(id1, id2);
            await _audit.LogActionAsync("Student", "Swap Rooms", _currentUser, $"Students {id1} ↔ {id2}");
            ConsoleUI.ShowSuccess("Rooms swapped successfully!");
        }
        catch (Exception ex) { ConsoleUI.ShowError(ex.Message); }
        ConsoleUI.Pause();
    }

    private async Task ListStudentsWithoutRoomAsync()
    {
        ConsoleUI.ShowHeader("⚠️ STUDENTS WITHOUT ROOM");
        var students = await _students.GetStudentsWithoutRoomAsync();
        var rows = students.Select(s => new[] {
            s.Id.ToString(), s.RegistrationNumber, s.FullName, s.Department, s.Phone
        }).ToList();
        ConsoleUI.ShowTable(new[] { "ID", "Reg #", "Name", "Dept", "Phone" }, rows);
        ConsoleUI.Pause();
    }

    private async Task DeactivateStudentAsync()
    {
        ConsoleUI.ShowHeader("❌ DEACTIVATE STUDENT");
        var id = ConsoleUI.ReadInt("Student ID");
        var student = await _students.GetStudentByIdAsync(id);
        if (student == null) { ConsoleUI.ShowError("Student not found!"); ConsoleUI.Pause(); return; }

        ConsoleUI.ShowWarning($"This will deactivate: {student.FullName} ({student.RegistrationNumber})");
        if (ConsoleUI.ReadConfirm("Proceed?"))
        {
            await _students.DeactivateStudentAsync(id);
            await _audit.LogActionAsync("Student", "Deactivate", _currentUser, $"Deactivated {student.FullName}");
            ConsoleUI.ShowSuccess("Student deactivated.");
        }
        ConsoleUI.Pause();
    }

    // ═══════════════════════════════════════════════════════════════
    //  ROOM MANAGEMENT
    // ═══════════════════════════════════════════════════════════════
    private async Task RoomMenuAsync()
    {
        while (true)
        {
            ConsoleUI.ShowMenu("🏠 ROOM MANAGEMENT",
                ("1", "Create New Room", "➕"),
                ("2", "List All Rooms", "📋"),
                ("3", "Available Rooms (with vacancy)", "✅"),
                ("4", "Full Rooms", "🔴"),
                ("5", "View Room Details & Occupants", "👁️"),
                ("6", "Edit Room", "✏️"),
                ("7", "Delete Room", "🗑️"),
                ("8", "Occupancy Report", "📊"),
                ("0", "Back to Main Menu", "↩️"));

            var input = Console.ReadLine()?.Trim();
            switch (input)
            {
                case "1": await CreateRoomAsync(); break;
                case "2": await ListAllRoomsAsync(); break;
                case "3": await ListAvailableRoomsAsync(); break;
                case "4": await ListFullRoomsAsync(); break;
                case "5": await ViewRoomDetailsAsync(); break;
                case "6": await EditRoomAsync(); break;
                case "7": await DeleteRoomAsync(); break;
                case "8": await RoomOccupancyReportAsync(); break;
                case "0": return;
                default: ConsoleUI.ShowError("Invalid option!"); ConsoleUI.Pause(); break;
            }
        }
    }

    private async Task CreateRoomAsync()
    {
        ConsoleUI.ShowHeader("➕ CREATE NEW ROOM");
        var room = new Room
        {
            RoomNumber = ConsoleUI.ReadInput("Room Number (e.g. A-101)"),
            Floor = ConsoleUI.ReadInt("Floor Number", 0, 20),
            Capacity = ConsoleUI.ReadInt("Capacity (beds)", 1, 20),
            RoomType = ConsoleUI.ReadEnum<RoomType>("Room Type"),
            MonthlyRent = ConsoleUI.ReadDecimal("Monthly Rent (Rs.)"),
            HasAC = ConsoleUI.ReadConfirm("Has AC?"),
            HasAttachedBath = ConsoleUI.ReadConfirm("Has Attached Bathroom?")
        };

        if (string.IsNullOrEmpty(room.RoomNumber)) { ConsoleUI.ShowError("Room number required!"); ConsoleUI.Pause(); return; }

        await _rooms.CreateRoomAsync(room);
        await _audit.LogActionAsync("Room", "Create", _currentUser, $"Room {room.RoomNumber} created");
        ConsoleUI.ShowSuccess($"Room '{room.RoomNumber}' created! (ID: {room.Id})");
        ConsoleUI.Pause();
    }

    private async Task ListAllRoomsAsync()
    {
        ConsoleUI.ShowHeader("📋 ALL ROOMS");
        var rooms = await _rooms.GetAllRoomsAsync();
        var rows = rooms.Select(r => new[] {
            r.Id.ToString(), r.RoomNumber, $"Floor {r.Floor}", r.RoomType.ToString(),
            $"{r.CurrentOccupancy}/{r.Capacity}", $"Rs. {r.MonthlyRent:N0}",
            r.HasAC ? "✅" : "❌", r.HasAttachedBath ? "✅" : "❌",
            r.IsFull ? "🔴 Full" : "🟢 Open"
        }).ToList();
        ConsoleUI.ShowTable(new[] { "ID", "Room", "Floor", "Type", "Occ", "Rent", "AC", "Bath", "Status" }, rows);
        ConsoleUI.Pause();
    }

    private async Task ListAvailableRoomsAsync()
    {
        ConsoleUI.ShowHeader("✅ AVAILABLE ROOMS");
        var rooms = await _rooms.GetAvailableRoomsAsync();
        var rows = rooms.Select(r => new[] {
            r.Id.ToString(), r.RoomNumber, r.RoomType.ToString(),
            $"{r.CurrentOccupancy}/{r.Capacity}", $"Rs. {r.MonthlyRent:N0}"
        }).ToList();
        ConsoleUI.ShowTable(new[] { "ID", "Room", "Type", "Occupancy", "Rent" }, rows);
        ConsoleUI.Pause();
    }

    private async Task ListFullRoomsAsync()
    {
        ConsoleUI.ShowHeader("🔴 FULL ROOMS");
        var rooms = await _rooms.GetFullRoomsAsync();
        var rows = rooms.Select(r => new[] {
            r.Id.ToString(), r.RoomNumber, r.RoomType.ToString(), $"{r.Capacity}/{r.Capacity}"
        }).ToList();
        ConsoleUI.ShowTable(new[] { "ID", "Room", "Type", "Occupancy" }, rows);
        ConsoleUI.Pause();
    }

    private async Task ViewRoomDetailsAsync()
    {
        ConsoleUI.ShowHeader("👁️ ROOM DETAILS");
        var id = ConsoleUI.ReadInt("Room ID");
        var room = await _rooms.GetRoomByIdAsync(id);
        if (room == null) { ConsoleUI.ShowError("Room not found!"); ConsoleUI.Pause(); return; }

        ConsoleUI.ShowDetailRow("Room Number", room.RoomNumber);
        ConsoleUI.ShowDetailRow("Floor", room.Floor.ToString());
        ConsoleUI.ShowDetailRow("Type", room.RoomType.ToString());
        ConsoleUI.ShowDetailRow("Capacity", room.Capacity.ToString());
        ConsoleUI.ShowDetailRow("Occupancy", $"{room.CurrentOccupancy}/{room.Capacity}");
        ConsoleUI.ShowDetailRow("Rent", $"Rs. {room.MonthlyRent:N0}/month");
        ConsoleUI.ShowDetailRow("AC", room.HasAC ? "Yes ✅" : "No ❌");
        ConsoleUI.ShowDetailRow("Attached Bath", room.HasAttachedBath ? "Yes ✅" : "No ❌");
        ConsoleUI.ShowDetailRow("Status", room.IsFull ? "🔴 Full" : "🟢 Available");

        ConsoleUI.ShowProgressBar("Occupancy", room.Capacity > 0 ? (double)room.CurrentOccupancy / room.Capacity * 100 : 0);

        // Show occupants
        var occupants = await _students.GetStudentsByRoomAsync(id);
        if (occupants.Count > 0)
        {
            ConsoleUI.ShowSubHeader("Current Occupants");
            var rows = occupants.Select(s => new[] { s.Id.ToString(), s.FullName, s.RegistrationNumber, s.Phone }).ToList();
            ConsoleUI.ShowTable(new[] { "ID", "Name", "Reg #", "Phone" }, rows);
        }
        ConsoleUI.Pause();
    }

    private async Task EditRoomAsync()
    {
        ConsoleUI.ShowHeader("✏️ EDIT ROOM");
        var id = ConsoleUI.ReadInt("Room ID");
        var room = await _rooms.GetRoomByIdAsync(id);
        if (room == null) { ConsoleUI.ShowError("Room not found!"); ConsoleUI.Pause(); return; }

        ConsoleUI.ShowInfo($"Editing: Room {room.RoomNumber} — press Enter to keep current value");
        var num = ConsoleUI.ReadInput($"Room Number [{room.RoomNumber}]");
        if (!string.IsNullOrEmpty(num)) room.RoomNumber = num;
        var rentStr = ConsoleUI.ReadInput($"Monthly Rent [{room.MonthlyRent:N0}]");
        if (decimal.TryParse(rentStr, out var rent)) room.MonthlyRent = rent;
        var capStr = ConsoleUI.ReadInput($"Capacity [{room.Capacity}]");
        if (int.TryParse(capStr, out var cap) && cap >= room.CurrentOccupancy) room.Capacity = cap;

        await _rooms.UpdateRoomAsync(room);
        await _audit.LogActionAsync("Room", "Edit", _currentUser, $"Updated room {room.RoomNumber}");
        ConsoleUI.ShowSuccess("Room updated!");
        ConsoleUI.Pause();
    }

    private async Task DeleteRoomAsync()
    {
        ConsoleUI.ShowHeader("🗑️ DELETE ROOM");
        var id = ConsoleUI.ReadInt("Room ID");
        var room = await _rooms.GetRoomByIdAsync(id);
        if (room == null) { ConsoleUI.ShowError("Room not found!"); ConsoleUI.Pause(); return; }
        if (room.CurrentOccupancy > 0) { ConsoleUI.ShowError("Cannot delete occupied room!"); ConsoleUI.Pause(); return; }

        if (ConsoleUI.ReadConfirm($"Delete room {room.RoomNumber}?"))
        {
            await _rooms.DeleteRoomAsync(id);
            await _audit.LogActionAsync("Room", "Delete", _currentUser, $"Deleted room {room.RoomNumber}");
            ConsoleUI.ShowSuccess("Room deleted.");
        }
        ConsoleUI.Pause();
    }

    private async Task RoomOccupancyReportAsync()
    {
        ConsoleUI.ShowHeader("📊 ROOM OCCUPANCY REPORT");
        var rooms = await _rooms.GetAllRoomsAsync();

        var chartData = new Dictionary<string, double>();
        foreach (var r in rooms.Where(r => r.IsActive))
            chartData[$"{r.RoomNumber} ({r.RoomType})"] = r.CurrentOccupancy;

        ConsoleUI.ShowBarChart("Room Occupancy", chartData, ConsoleColor.Cyan);

        var totalCap = rooms.Where(r => r.IsActive).Sum(r => r.Capacity);
        var totalOcc = rooms.Where(r => r.IsActive).Sum(r => r.CurrentOccupancy);
        var rate = totalCap > 0 ? (double)totalOcc / totalCap * 100 : 0;

        ConsoleUI.ShowSeparator();
        ConsoleUI.ShowDetailRow("Total Capacity", totalCap.ToString());
        ConsoleUI.ShowDetailRow("Total Occupied", totalOcc.ToString());
        ConsoleUI.ShowDetailRow("Available Beds", (totalCap - totalOcc).ToString());
        ConsoleUI.ShowProgressBar("Overall Occupancy", rate);
        ConsoleUI.Pause();
    }
}
