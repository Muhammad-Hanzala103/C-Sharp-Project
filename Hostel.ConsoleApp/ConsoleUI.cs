using Hostel.Core.Entities;

namespace Hostel.ConsoleApp;

/// <summary>
/// Beautiful console UI helper — colors, boxes, tables, ASCII art, charts
/// </summary>
public static class ConsoleUI
{
    // ─────────────────────── COLOR SCHEME ───────────────────────
    public static readonly ConsoleColor Primary = ConsoleColor.Cyan;
    public static readonly ConsoleColor Secondary = ConsoleColor.Magenta;
    public static readonly ConsoleColor Success = ConsoleColor.Green;
    public static readonly ConsoleColor Warning = ConsoleColor.Yellow;
    public static readonly ConsoleColor Danger = ConsoleColor.Red;
    public static readonly ConsoleColor Info = ConsoleColor.DarkCyan;
    public static readonly ConsoleColor Muted = ConsoleColor.DarkGray;
    public static readonly ConsoleColor Accent = ConsoleColor.White;

    // ─────────────────────── ASCII ART BANNER ───────────────────────
    public static void ShowBanner()
    {
        try { Console.Clear(); } catch { }
        Console.ForegroundColor = Primary;
        Console.WriteLine(@"
    ╔═══════════════════════════════════════════════════════════════════════════╗
    ║                                                                         ║
    ║   ██╗  ██╗ ██████╗ ███████╗████████╗███████╗██╗                         ║
    ║   ██║  ██║██╔═══██╗██╔════╝╚══██╔══╝██╔════╝██║                         ║
    ║   ███████║██║   ██║███████╗   ██║   █████╗  ██║                         ║
    ║   ██╔══██║██║   ██║╚════██║   ██║   ██╔══╝  ██║                         ║
    ║   ██║  ██║╚██████╔╝███████║   ██║   ███████╗███████╗                    ║
    ║   ╚═╝  ╚═╝ ╚═════╝ ╚══════╝   ╚═╝   ╚══════╝╚══════╝                    ║
    ║                                                                         ║
    ║   ███╗   ███╗ █████╗ ███╗   ██╗ █████╗  ██████╗ ███████╗██████╗         ║
    ║   ████╗ ████║██╔══██╗████╗  ██║██╔══██╗██╔════╝ ██╔════╝██╔══██╗        ║
    ║   ██╔████╔██║███████║██╔██╗ ██║███████║██║  ███╗█████╗  ██████╔╝        ║
    ║   ██║╚██╔╝██║██╔══██║██║╚██╗██║██╔══██║██║   ██║██╔══╝  ██╔══██╗        ║
    ║   ██║ ╚═╝ ██║██║  ██║██║ ╚████║██║  ██║╚██████╔╝███████╗██║  ██║        ║
    ║   ╚═╝     ╚═╝╚═╝  ╚═╝╚═╝  ╚═══╝╚═╝  ╚═╝ ╚═════╝ ╚══════╝╚═╝  ╚═╝        ║
    ║                                                                         ║
    ╚═══════════════════════════════════════════════════════════════════════════╝");
        Console.ForegroundColor = Warning;
        Console.WriteLine("               Ultimate Hostel Management System v2.0");
        Console.ForegroundColor = Muted;
        Console.WriteLine("               Powered by .NET 8 | Console Edition");
        Console.ResetColor();
        Console.WriteLine();
    }

    // ─────────────────────── LOGIN SCREEN ───────────────────────
    public static void ShowLoginScreen()
    {
        try { Console.Clear(); } catch { }
        Console.ForegroundColor = Primary;
        Console.WriteLine(@"
    ╔═══════════════════════════════════════════════════╗
    ║                                                   ║
    ║        🔐  ADMIN LOGIN  🔐                        ║
    ║                                                   ║
    ╚═══════════════════════════════════════════════════╝");
        Console.ResetColor();
        Console.WriteLine();
    }

    // ─────────────────────── SECTION HEADERS ───────────────────────
    public static void ShowHeader(string title, ConsoleColor color = ConsoleColor.Cyan)
    {
        try { Console.Clear(); } catch { }
        var line = new string('═', title.Length + 10);
        Console.ForegroundColor = color;
        Console.WriteLine($"    ╔{line}╗");
        Console.WriteLine($"    ║     {title}     ║");
        Console.WriteLine($"    ╚{line}╝");
        Console.ResetColor();
        Console.WriteLine();
    }

    public static void ShowSubHeader(string title)
    {
        Console.ForegroundColor = Secondary;
        Console.WriteLine($"\n  ┌── {title} ──┐");
        Console.ResetColor();
    }

    // ─────────────────────── MENU ───────────────────────
    public static void ShowMenu(string title, params (string key, string label, string icon)[] items)
    {
        ShowHeader(title);
        foreach (var (key, label, icon) in items)
        {
            if (key == "0")
            {
                Console.ForegroundColor = Danger;
                Console.WriteLine($"    [{key}] {icon}  {label}");
            }
            else
            {
                Console.ForegroundColor = Primary;
                Console.Write($"    [{key}]");
                Console.ForegroundColor = Accent;
                Console.WriteLine($" {icon}  {label}");
            }
        }
        Console.ResetColor();
        Console.WriteLine();
        Console.ForegroundColor = Warning;
        Console.Write("    ▸ Choose option: ");
        Console.ResetColor();
    }

    // ─────────────────────── DASHBOARD CARD ───────────────────────
    public static void ShowDashboardCard(string label, string value, ConsoleColor color, int col = 0)
    {
        int xPos = col * 28 + 4;
        Console.ForegroundColor = color;
        Console.SetCursorPosition(xPos, Console.CursorTop);
        Console.Write($"┌────────────────────────┐");
        Console.SetCursorPosition(xPos, Console.CursorTop + 1);
        Console.Write($"│ {value,-22} │");
        Console.SetCursorPosition(xPos, Console.CursorTop + 1);
        Console.Write($"│ {label,-22} │");
        Console.SetCursorPosition(xPos, Console.CursorTop + 1);
        Console.Write($"└────────────────────────┘");
        Console.ResetColor();
    }

    // ─────────────────────── TABLE ───────────────────────
    public static void ShowTable(string[] headers, List<string[]> rows, ConsoleColor headerColor = ConsoleColor.Cyan)
    {
        if (rows.Count == 0)
        {
            ShowWarning("No records found.");
            return;
        }

        // Calculate column widths
        var widths = new int[headers.Length];
        for (int i = 0; i < headers.Length; i++)
            widths[i] = headers[i].Length;
        foreach (var row in rows)
            for (int i = 0; i < Math.Min(row.Length, widths.Length); i++)
                widths[i] = Math.Max(widths[i], (row[i] ?? "").Length);

        // Cap widths
        for (int i = 0; i < widths.Length; i++)
            widths[i] = Math.Min(widths[i], 30);

        // Build separator
        var sep = "    ├" + string.Join("┼", widths.Select(w => new string('─', w + 2))) + "┤";
        var top = "    ┌" + string.Join("┬", widths.Select(w => new string('─', w + 2))) + "┐";
        var bot = "    └" + string.Join("┴", widths.Select(w => new string('─', w + 2))) + "┘";

        // Print top border
        Console.ForegroundColor = Muted;
        Console.WriteLine(top);

        // Print headers
        Console.ForegroundColor = headerColor;
        Console.Write("    │");
        for (int i = 0; i < headers.Length; i++)
            Console.Write($" {headers[i].PadRight(widths[i])} │");
        Console.WriteLine();

        // Print separator
        Console.ForegroundColor = Muted;
        Console.WriteLine(sep);

        // Print rows
        Console.ForegroundColor = Accent;
        foreach (var row in rows)
        {
            Console.Write("    │");
            for (int i = 0; i < headers.Length; i++)
            {
                var val = i < row.Length ? (row[i] ?? "") : "";
                if (val.Length > 30) val = val[..27] + "...";
                Console.Write($" {val.PadRight(widths[i])} │");
            }
            Console.WriteLine();
        }

        // Print bottom border
        Console.ForegroundColor = Muted;
        Console.WriteLine(bot);
        Console.ResetColor();

        Console.ForegroundColor = Info;
        Console.WriteLine($"    Total: {rows.Count} record(s)");
        Console.ResetColor();
    }

    // ─────────────────────── BAR CHART ───────────────────────
    public static void ShowBarChart(string title, Dictionary<string, double> data, ConsoleColor barColor = ConsoleColor.Cyan)
    {
        if (data.Count == 0) return;

        ShowSubHeader(title);
        Console.WriteLine();

        var maxValue = data.Values.Max();
        if (maxValue == 0) maxValue = 1;
        int maxBarWidth = 40;
        int maxLabelWidth = data.Keys.Max(k => k.Length);

        foreach (var kvp in data)
        {
            int barWidth = (int)(kvp.Value / maxValue * maxBarWidth);
            Console.ForegroundColor = Info;
            Console.Write($"    {kvp.Key.PadRight(maxLabelWidth)} │ ");
            Console.ForegroundColor = barColor;
            Console.Write(new string('█', barWidth));
            Console.ForegroundColor = Accent;
            Console.WriteLine($" {kvp.Value:N0}");
        }
        Console.ResetColor();
        Console.WriteLine();
    }

    // ─────────────────────── PROGRESS BAR ───────────────────────
    public static void ShowProgressBar(string label, double percentage, ConsoleColor color = ConsoleColor.Green)
    {
        int width = 30;
        int filled = (int)(percentage / 100 * width);
        Console.ForegroundColor = Info;
        Console.Write($"    {label}: ");
        Console.ForegroundColor = color;
        Console.Write("[");
        Console.Write(new string('█', filled));
        Console.ForegroundColor = Muted;
        Console.Write(new string('░', width - filled));
        Console.ForegroundColor = color;
        Console.Write("]");
        Console.ForegroundColor = Accent;
        Console.WriteLine($" {percentage:F1}%");
        Console.ResetColor();
    }

    // ─────────────────────── MESSAGES ───────────────────────
    public static void ShowSuccess(string message)
    {
        Console.ForegroundColor = Success;
        Console.WriteLine($"\n    ✅ {message}");
        Console.ResetColor();
    }

    public static void ShowError(string message)
    {
        Console.ForegroundColor = Danger;
        Console.WriteLine($"\n    ❌ {message}");
        Console.ResetColor();
    }

    public static void ShowWarning(string message)
    {
        Console.ForegroundColor = Warning;
        Console.WriteLine($"\n    ⚠️  {message}");
        Console.ResetColor();
    }

    public static void ShowInfo(string message)
    {
        Console.ForegroundColor = Info;
        Console.WriteLine($"\n    ℹ️  {message}");
        Console.ResetColor();
    }

    // ─────────────────────── INPUT HELPERS ───────────────────────
    public static string ReadInput(string prompt)
    {
        Console.ForegroundColor = Warning;
        Console.Write($"    ▸ {prompt}: ");
        Console.ResetColor();
        return Console.ReadLine()?.Trim() ?? string.Empty;
    }

    public static int ReadInt(string prompt, int min = int.MinValue, int max = int.MaxValue)
    {
        while (true)
        {
            var input = ReadInput(prompt);
            if (int.TryParse(input, out var value) && value >= min && value <= max)
                return value;
            ShowError($"Please enter a valid number{(min != int.MinValue ? $" ({min}-{max})" : "")}");
        }
    }

    public static decimal ReadDecimal(string prompt, decimal min = 0)
    {
        while (true)
        {
            var input = ReadInput(prompt);
            if (decimal.TryParse(input, out var value) && value >= min)
                return value;
            ShowError($"Please enter a valid amount (minimum {min})");
        }
    }

    public static bool ReadConfirm(string prompt)
    {
        Console.ForegroundColor = Warning;
        Console.Write($"    ▸ {prompt} (y/n): ");
        Console.ResetColor();
        var input = Console.ReadLine()?.Trim().ToLower();
        return input == "y" || input == "yes";
    }

    public static DateTime ReadDate(string prompt)
    {
        while (true)
        {
            var input = ReadInput($"{prompt} (dd-MM-yyyy)");
            if (DateTime.TryParseExact(input, new[] { "dd-MM-yyyy", "d-M-yyyy", "dd/MM/yyyy", "yyyy-MM-dd" },
                null, System.Globalization.DateTimeStyles.None, out var date))
                return date;
            ShowError("Invalid date format. Use dd-MM-yyyy");
        }
    }

    public static T ReadEnum<T>(string prompt) where T : struct, Enum
    {
        var values = Enum.GetValues<T>();
        Console.ForegroundColor = Info;
        Console.WriteLine();
        foreach (var val in values)
        {
            Console.ForegroundColor = Primary;
            Console.Write($"      [{Convert.ToInt32(val)}]");
            Console.ForegroundColor = Accent;
            Console.WriteLine($" {val}");
        }
        Console.ResetColor();

        while (true)
        {
            var input = ReadInput(prompt);
            if (int.TryParse(input, out var num) && Enum.IsDefined(typeof(T), num))
                return (T)(object)num;
            ShowError("Invalid selection. Try again.");
        }
    }

    // ─────────────────────── WAIT / PAUSE ───────────────────────
    public static void Pause()
    {
        Console.ForegroundColor = Muted;
        Console.WriteLine("\n    Press any key to continue...");
        Console.ResetColor();
        Console.ReadKey(true);
    }

    public static void ShowLoading(string message = "Loading")
    {
        Console.ForegroundColor = Info;
        Console.Write($"    {message}");
        for (int i = 0; i < 3; i++)
        {
            Thread.Sleep(200);
            Console.Write(".");
        }
        Console.WriteLine();
        Console.ResetColor();
    }

    // ─────────────────────── STATUS BADGES ───────────────────────
    public static string GetStatusBadge(ComplaintStatus status) => status switch
    {
        ComplaintStatus.Open => "🔴 Open",
        ComplaintStatus.InProgress => "🟡 In Progress",
        ComplaintStatus.Resolved => "🟢 Resolved",
        ComplaintStatus.Closed => "⚫ Closed",
        _ => status.ToString()
    };

    public static string GetPaymentBadge(PaymentStatus status) => status switch
    {
        PaymentStatus.Paid => "✅ Paid",
        PaymentStatus.Pending => "⏳ Pending",
        PaymentStatus.Late => "⚠️ Late",
        PaymentStatus.Overdue => "🔴 Overdue",
        PaymentStatus.Waived => "🔵 Waived",
        _ => status.ToString()
    };

    public static string GetPriorityBadge(ComplaintPriority priority) => priority switch
    {
        ComplaintPriority.Low => "🟢 Low",
        ComplaintPriority.Medium => "🟡 Medium",
        ComplaintPriority.High => "🟠 High",
        ComplaintPriority.Critical => "🔴 Critical",
        _ => priority.ToString()
    };

    // ─────────────────────── DETAIL VIEW ───────────────────────
    public static void ShowDetailRow(string label, string value)
    {
        Console.ForegroundColor = Info;
        Console.Write($"    {label,-20}: ");
        Console.ForegroundColor = Accent;
        Console.WriteLine(value);
        Console.ResetColor();
    }

    public static void ShowSeparator()
    {
        Console.ForegroundColor = Muted;
        Console.WriteLine("    " + new string('─', 60));
        Console.ResetColor();
    }
}
