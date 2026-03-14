using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;

namespace ODataQuerySimulator;

// ── Sample domain model ──────────────────────────────────────────────────────

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public double Price { get; set; }
    public int Stock { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public override string ToString() =>
        $"  [{Id}] {Name,-22} | Category: {Category,-12} | Price: {Price,8:F2} | Stock: {Stock,4} | Active: {IsActive} | Created: {CreatedAt:yyyy-MM-dd}";
}

// ── EDM model builder ─────────────────────────────────────────────────────────

static class EdmModelBuilder
{
    public static (IEdmModel Model, IEdmEntityType ProductType) Build()
    {
        var model = new EdmModel();
        var product = new EdmEntityType("NS", "Product");

        product.AddKeys(product.AddStructuralProperty("Id",         EdmPrimitiveTypeKind.Int32));
        product.AddStructuralProperty("Name",       EdmPrimitiveTypeKind.String);
        product.AddStructuralProperty("Category",   EdmPrimitiveTypeKind.String);
        product.AddStructuralProperty("Price",      EdmPrimitiveTypeKind.Double);
        product.AddStructuralProperty("Stock",      EdmPrimitiveTypeKind.Int32);
        product.AddStructuralProperty("IsActive",   EdmPrimitiveTypeKind.Boolean);
        product.AddStructuralProperty("CreatedAt",  EdmPrimitiveTypeKind.DateTimeOffset);

        model.AddElement(product);

        var container = new EdmEntityContainer("NS", "Container");
        container.AddEntitySet("Products", product);
        model.AddElement(container);

        return (model, product);
    }
}

// ── In-memory OData filter evaluator ─────────────────────────────────────────

static class FilterEvaluator
{
    public static bool Evaluate(SingleValueNode node, Product p) => node switch
    {
        BinaryOperatorNode b   => EvalBinary(b, p),
        UnaryOperatorNode u    => EvalUnary(u, p),
        SingleValueFunctionCallNode f => EvalFunction(f, p),
        _                      => throw new NotSupportedException($"Node type '{node.GetType().Name}' is not supported.")
    };

    // ── Binary operators ──────────────────────────────────────────────────────

    static bool EvalBinary(BinaryOperatorNode node, Product p)
    {
        var op = node.OperatorKind;

        // Logical short-circuit
        if (op == BinaryOperatorKind.And)
            return Evaluate(node.Left, p) && Evaluate(node.Right, p);
        if (op == BinaryOperatorKind.Or)
            return Evaluate(node.Left, p) || Evaluate(node.Right, p);

        // Comparison
        var left  = ResolveValue(node.Left,  p);
        var right = ResolveValue(node.Right, p);

        return op switch
        {
            BinaryOperatorKind.Equal              => AreEqual(left, right),
            BinaryOperatorKind.NotEqual           => !AreEqual(left, right),
            BinaryOperatorKind.GreaterThan        => Compare(left, right) > 0,
            BinaryOperatorKind.GreaterThanOrEqual => Compare(left, right) >= 0,
            BinaryOperatorKind.LessThan           => Compare(left, right) < 0,
            BinaryOperatorKind.LessThanOrEqual    => Compare(left, right) <= 0,
            _ => throw new NotSupportedException($"Operator '{op}' is not supported.")
        };
    }

    static bool EvalUnary(UnaryOperatorNode node, Product p)
    {
        if (node.OperatorKind == UnaryOperatorKind.Not)
            return !Evaluate(node.Operand, p);
        throw new NotSupportedException($"Unary operator '{node.OperatorKind}' is not supported.");
    }

    // ── String functions ──────────────────────────────────────────────────────

    static bool EvalFunction(SingleValueFunctionCallNode node, Product p)
    {
        var args = node.Parameters.Select(a => ResolveValue(a, p)).ToArray();

        return node.Name.ToLower() switch
        {
            "contains"   => args[0] is string s0 && args[1] is string s1
                             && s0.Contains(s1, StringComparison.OrdinalIgnoreCase),
            "startswith" => args[0] is string ss0 && args[1] is string ss1
                             && ss0.StartsWith(ss1, StringComparison.OrdinalIgnoreCase),
            "endswith"   => args[0] is string es0 && args[1] is string es1
                             && es0.EndsWith(es1, StringComparison.OrdinalIgnoreCase),
            "tolower"    => throw new NotSupportedException("tolower() used alone as bool — wrap in comparison."),
            _ => throw new NotSupportedException($"Function '{node.Name}' is not supported.")
        };
    }

    // ── Value resolution ──────────────────────────────────────────────────────

    static object? ResolveValue(QueryNode node, Product p) => node switch
    {
        SingleValuePropertyAccessNode prop => GetPropertyValue(prop.Property.Name, p),
        ConstantNode c                     => c.Value,
        ConvertNode cv                     => ResolveValue(cv.Source, p),
        SingleValueFunctionCallNode fn     => ResolveFunctionValue(fn, p),
        _ => throw new NotSupportedException($"Cannot resolve value from node type '{node.GetType().Name}'.")
    };

    static object? ResolveFunctionValue(SingleValueFunctionCallNode node, Product p)
    {
        var args = node.Parameters.Select(a => ResolveValue(a, p)).ToArray();
        return node.Name.ToLower() switch
        {
            "tolower" => args[0]?.ToString()?.ToLower(),
            "toupper" => args[0]?.ToString()?.ToUpper(),
            "trim"    => args[0]?.ToString()?.Trim(),
            "length"  => (object?)(args[0]?.ToString()?.Length),
            _ => throw new NotSupportedException($"Function '{node.Name}' is not supported as a value.")
        };
    }

    static object? GetPropertyValue(string name, Product p) => name switch
    {
        "Id"        => (object?)p.Id,
        "Name"      => p.Name,
        "Category"  => p.Category,
        "Price"     => p.Price,
        "Stock"     => p.Stock,
        "IsActive"  => p.IsActive,
        "CreatedAt" => p.CreatedAt,
        _           => throw new NotSupportedException($"Property '{name}' not found.")
    };

    // ── Comparison helpers ────────────────────────────────────────────────────

    static bool AreEqual(object? a, object? b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;
        return Normalize(a).Equals(Normalize(b));
    }

    static int Compare(object? a, object? b)
    {
        if (a == null || b == null) throw new InvalidOperationException("Cannot compare null values.");
        var na = Normalize(a);
        var nb = Normalize(b);
        if (na is IComparable ca) return ca.CompareTo(nb);
        throw new NotSupportedException($"Cannot compare type '{a.GetType().Name}'.");
    }

    // Coerce numeric types so e.g. int vs double comparison works
    static object Normalize(object v) => v switch
    {
        int i    => (double)i,
        long l   => (double)l,
        float f  => (double)f,
        _        => v
    };
}

// ── Main program ──────────────────────────────────────────────────────────────

class Program
{
    static readonly List<Product> Products = new()
    {
        new() { Id = 1,  Name = "Laptop Pro 15",    Category = "Electronics", Price = 1299.99, Stock = 25,  IsActive = true,  CreatedAt = new DateTimeOffset(2023, 1, 10, 0,0,0, TimeSpan.Zero) },
        new() { Id = 2,  Name = "Wireless Mouse",   Category = "Electronics", Price = 29.99,   Stock = 150, IsActive = true,  CreatedAt = new DateTimeOffset(2023, 3, 5,  0,0,0, TimeSpan.Zero) },
        new() { Id = 3,  Name = "Office Chair",     Category = "Furniture",   Price = 349.00,  Stock = 10,  IsActive = true,  CreatedAt = new DateTimeOffset(2022, 11, 20,0,0,0, TimeSpan.Zero) },
        new() { Id = 4,  Name = "Standing Desk",    Category = "Furniture",   Price = 749.00,  Stock = 5,   IsActive = false, CreatedAt = new DateTimeOffset(2022, 8, 14, 0,0,0, TimeSpan.Zero) },
        new() { Id = 5,  Name = "USB-C Hub",        Category = "Electronics", Price = 49.99,   Stock = 200, IsActive = true,  CreatedAt = new DateTimeOffset(2023, 6, 1,  0,0,0, TimeSpan.Zero) },
        new() { Id = 6,  Name = "Notebook A5",      Category = "Stationery",  Price = 4.99,    Stock = 500, IsActive = true,  CreatedAt = new DateTimeOffset(2023, 2, 28, 0,0,0, TimeSpan.Zero) },
        new() { Id = 7,  Name = "Ballpoint Pens",   Category = "Stationery",  Price = 2.49,    Stock = 1000,IsActive = true,  CreatedAt = new DateTimeOffset(2023, 2, 28, 0,0,0, TimeSpan.Zero) },
        new() { Id = 8,  Name = "4K Monitor",       Category = "Electronics", Price = 599.00,  Stock = 18,  IsActive = true,  CreatedAt = new DateTimeOffset(2023, 4, 17, 0,0,0, TimeSpan.Zero) },
        new() { Id = 9,  Name = "Mechanical Keyboard",Category="Electronics", Price = 139.99,  Stock = 45,  IsActive = false, CreatedAt = new DateTimeOffset(2022, 12, 1, 0,0,0, TimeSpan.Zero) },
        new() { Id = 10, Name = "Desk Lamp",        Category = "Furniture",   Price = 59.99,   Stock = 0,   IsActive = true,  CreatedAt = new DateTimeOffset(2023, 5, 22, 0,0,0, TimeSpan.Zero) },
    };

    static void Main()
    {
        var (model, productType) = EdmModelBuilder.Build();

        PrintBanner();
        PrintSampleData();
        PrintExamples();

        while (true)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("\n$filter=");
            Console.ResetColor();

            var input = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(input)) continue;
            if (input.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
                input.Equals("quit", StringComparison.OrdinalIgnoreCase)) break;
            if (input.Equals("help", StringComparison.OrdinalIgnoreCase)) { PrintExamples(); continue; }
            if (input.Equals("data", StringComparison.OrdinalIgnoreCase)) { PrintSampleData(); continue; }

            try
            {
                var parser = new ODataQueryOptionParser(
                    model,
                    model.FindType("NS.Product") as IEdmType,
                    model.EntityContainer.FindEntitySet("Products"),
                    new Dictionary<string, string> { ["$filter"] = input });

                var filterClause = parser.ParseFilter();
                var results = Products.Where(p => FilterEvaluator.Evaluate(filterClause.Expression, p)).ToList();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\n✔ {results.Count} result(s) matched:\n");
                Console.ResetColor();

                if (results.Count == 0)
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine("  (no products matched the filter)");
                    Console.ResetColor();
                }
                else
                {
                    foreach (var r in results)
                        Console.WriteLine(r);
                }
            }
            catch (ODataException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n✖ OData parse error: {ex.Message}");
                Console.ResetColor();
            }
            catch (NotSupportedException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n✖ Not supported: {ex.Message}");
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

    static void PrintBanner()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("╔══════════════════════════════════════════════════╗");
        Console.WriteLine("║        OData Filter Query Simulator (C#)         ║");
        Console.WriteLine("║  Type 'help' for examples, 'exit' to quit        ║");
        Console.WriteLine("╚══════════════════════════════════════════════════╝");
        Console.ResetColor();
    }

    static void PrintSampleData()
    {
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine("\n── Sample Products ───────────────────────────────────────────────────────────────────────");
        Console.ResetColor();
        foreach (var p in Products) Console.WriteLine(p);
        Console.WriteLine("──────────────────────────────────────────────────────────────────────────────────────────");
    }

    static void PrintExamples()
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(@"
── Supported OData filter examples ──────────────────────────────────────────

  Equality / Inequality:
    Category eq 'Electronics'
    Price ne 29.99

  Comparison:
    Price gt 100
    Price ge 49.99
    Stock lt 20
    Stock le 10

  Logical:
    Category eq 'Electronics' and Price lt 200
    Category eq 'Furniture' or Category eq 'Stationery'
    not (IsActive eq true)

  String functions:
    contains(Name, 'Pro')
    startswith(Name, 'Desk')
    endswith(Name, 'Hub')

  Boolean:
    IsActive eq true
    IsActive eq false

  Date:
    CreatedAt gt 2023-01-01T00:00:00Z

  Combined:
    Category eq 'Electronics' and IsActive eq true and Price lt 500
    contains(Name, 'Desk') or contains(Name, 'Chair')

  Commands: help | data | exit
──────────────────────────────────────────────────────────────────────────────");
        Console.ResetColor();
    }
}
