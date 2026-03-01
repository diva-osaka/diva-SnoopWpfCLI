using System;
using System.Text.Json;
using SnoopWpfCLI.Formatters;

namespace SnoopWpfCLI.Commands;

internal static class CommandHelpers
{
    internal static void WriteResult(object result, string? format)
    {
        if (format == "tree")
        {
            var jsonStr = JsonSerializer.Serialize(result);
            using var doc = JsonDocument.Parse(jsonStr);
            Console.WriteLine(TreeFormatter.FormatGenericResult(doc.RootElement));
        }
        else
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            Console.WriteLine(JsonSerializer.Serialize(result, options));
        }
    }

    internal static void WriteError(object error, string? format)
    {
        if (format == "tree")
        {
            var jsonStr = JsonSerializer.Serialize(error);
            using var doc = JsonDocument.Parse(jsonStr);
            Console.Error.WriteLine(TreeFormatter.FormatGenericResult(doc.RootElement));
        }
        else
        {
            Console.Error.WriteLine(JsonSerializer.Serialize(error));
        }
    }
}
