using Cronos;

namespace CronExpressionSimulator;

class Program
{
    const int OccurrencesToShow = 10;

    static readonly (string Expression, string Description)[] Examples =
    {
        ("* * * * *",         "Every minute"),
        ("*/15 * * * *",      "Every 15 minutes"),
        ("0 * * * *",         "Every hour, on the hour"),
        ("0 9-17 * * MON-FRI","Every hour from 9am-5pm, Monday to Friday"),
        ("30 2 * * *",        "Every day at 02:30"),
        ("0 0 1 * *",         "Midnight on the 1st of every month"),
        ("0 0 * * SUN",       "Midnight every Sunday"),
        ("0 0 1 1 *",         "Midnight on January 1st (yearly)"),
        ("@daily",            "Macro for '0 0 * * *'"),
        ("@hourly",           "Macro for '0 * * * *'"),
    };

    static void Main()
    {
        PrintBanner();
        PrintExamples();

        var now = DateTime.UtcNow;

        while (true)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("\ncron> ");
            Console.ResetColor();

            var input = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(input)) continue;
            if (input.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
                input.Equals("quit", StringComparison.OrdinalIgnoreCase)) break;
            if (input.Equals("help", StringComparison.OrdinalIgnoreCase)) { PrintExamples(); continue; }

            try
            {
                var expression = CronExpression.Parse(input, ParseFormat(input));

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\n✔ Parsed OK — {Explain(input)}");
                Console.ResetColor();

                Console.WriteLine($"\nNext {OccurrencesToShow} occurrences (from {now.ToLocalTime():yyyy-MM-dd HH:mm:ss} local):\n");

                var from = now;
                for (var i = 0; i < OccurrencesToShow; i++)
                {
                    var nextUtc = expression.GetNextOccurrence(from, TimeZoneInfo.Local);
                    if (nextUtc is null)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine("  (no further occurrences)");
                        Console.ResetColor();
                        break;
                    }

                    var nextLocal = nextUtc.Value.ToLocalTime();
                    Console.WriteLine($"  {i + 1,2}. {nextLocal:yyyy-MM-dd HH:mm:ss} ({nextLocal:dddd})");
                    from = nextUtc.Value;
                }
            }
            catch (CronFormatException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n✖ Invalid cron expression: {ex.Message}");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n✖ Error: {ex.Message}");
                Console.ResetColor();
            }
        }

        Console.WriteLine("\nGoodbye!");
    }

    // Cronos requires an explicit format when a 6-field (seconds-included) expression is used.
    static CronFormat ParseFormat(string expression) =>
        expression.TrimStart().StartsWith('@')
            ? CronFormat.Standard
            : expression.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length > 5
                ? CronFormat.IncludeSeconds
                : CronFormat.Standard;

    static string Explain(string expression)
    {
        var known = Examples.FirstOrDefault(e => e.Expression.Equals(expression, StringComparison.OrdinalIgnoreCase));
        return known.Description ?? "custom schedule";
    }

    static void PrintBanner()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("╔══════════════════════════════════════════════════╗");
        Console.WriteLine("║           Cron Expression Simulator (C#)         ║");
        Console.WriteLine("║  Type 'help' for examples, 'exit' to quit        ║");
        Console.WriteLine("╚══════════════════════════════════════════════════╝");
        Console.ResetColor();
    }

    static void PrintExamples()
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("\n── Cron field format: minute hour day-of-month month day-of-week ──────────────");
        Console.WriteLine("   (an optional leading seconds field is also supported: sec min hour dom month dow)\n");
        Console.WriteLine("── Examples ─────────────────────────────────────────────────────────────────");
        foreach (var (expression, description) in Examples)
            Console.WriteLine($"    {expression,-22} {description}");
        Console.WriteLine("\n  Commands: help | exit");
        Console.WriteLine("─────────────────────────────────────────────────────────────────────────────");
        Console.ResetColor();
    }
}
