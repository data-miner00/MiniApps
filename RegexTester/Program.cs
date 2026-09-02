using System.Text.RegularExpressions;

namespace RegexTester;

class Program
{
    static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(2);

    static readonly (string Pattern, string Description)[] Examples =
    {
        (@"\d+",                                            "One or more digits"),
        (@"^[A-Z][a-z]*$",                                  "A single capitalized word"),
        (@"(?<user>[\w.+-]+)@(?<domain>[\w-]+\.[\w.-]+)",   "Email with named 'user'/'domain' groups"),
        (@"(?i)hello",                                      "Case-insensitive match for 'hello'"),
        (@"\b(\w+)\s+\1\b",                                 "A repeated word (backreference)"),
        (@"colou?r",                                        "Matches 'color' or 'colour'"),
    };

    static void Main()
    {
        PrintBanner();
        PrintHelp();

        Regex? current = null;
        string? currentSource = null;

        while (true)
        {
            if (current is null)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("\npattern> ");
                Console.ResetColor();

                var input = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(input)) continue;
                if (IsExit(input)) break;
                if (IsHelp(input)) { PrintHelp(); continue; }

                try
                {
                    current = new Regex(input, RegexOptions.None, MatchTimeout);
                    currentSource = input;

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("\n✔ Compiled OK — now enter test strings (type 'pattern' to change pattern)");
                    Console.ResetColor();
                }
                catch (ArgumentException ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"\n✖ Invalid regex: {ex.Message}");
                    Console.ResetColor();
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"\ntest [{currentSource}]> ");
                Console.ResetColor();

                var input = Console.ReadLine();
                if (input is null) continue;

                var trimmed = input.Trim();
                if (IsExit(trimmed)) break;
                if (IsHelp(trimmed)) { PrintHelp(); continue; }
                if (trimmed.Equals("pattern", StringComparison.OrdinalIgnoreCase))
                {
                    current = null;
                    currentSource = null;
                    continue;
                }

                EvaluateAndPrint(current, input);
            }
        }

        Console.WriteLine("\nGoodbye!");
    }

    static void EvaluateAndPrint(Regex regex, string input)
    {
        MatchCollection matches;
        try
        {
            matches = regex.Matches(input);
        }
        catch (RegexMatchTimeoutException)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  ✖ Match timed out (possible catastrophic backtracking)");
            Console.ResetColor();
            return;
        }

        if (matches.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("  ✖ No match");
            Console.ResetColor();
            return;
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  ✔ {matches.Count} match{(matches.Count == 1 ? "" : "es")}");
        Console.ResetColor();

        var i = 1;
        foreach (Match match in matches)
        {
            Console.WriteLine($"    {i}. \"{match.Value}\" at [{match.Index}, {match.Index + match.Length})");

            foreach (var name in regex.GetGroupNames())
            {
                if (name == "0") continue;

                var group = match.Groups[name];
                if (!group.Success) continue;

                var label = int.TryParse(name, out _) ? $"group {name}" : $"'{name}'";
                Console.WriteLine($"       {label}: \"{group.Value}\" at [{group.Index}, {group.Index + group.Length})");
            }

            i++;
        }
    }

    static bool IsExit(string s) =>
        s.Equals("exit", StringComparison.OrdinalIgnoreCase) || s.Equals("quit", StringComparison.OrdinalIgnoreCase);

    static bool IsHelp(string s) => s.Equals("help", StringComparison.OrdinalIgnoreCase);

    static void PrintBanner()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("╔══════════════════════════════════════════════════╗");
        Console.WriteLine("║                Regex Tester (C#)                 ║");
        Console.WriteLine("║  Type 'help' for examples, 'exit' to quit        ║");
        Console.WriteLine("╚══════════════════════════════════════════════════╝");
        Console.ResetColor();
    }

    static void PrintHelp()
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("\n── .NET regex syntax; use inline options like (?i), (?m), (?s), (?x) ──────────");
        Console.WriteLine("── Examples ─────────────────────────────────────────────────────────────────");
        foreach (var (pattern, description) in Examples)
            Console.WriteLine($"    {pattern,-52} {description}");
        Console.WriteLine("\n  Commands: help | pattern (change pattern) | exit");
        Console.WriteLine("─────────────────────────────────────────────────────────────────────────────");
        Console.ResetColor();
    }
}
