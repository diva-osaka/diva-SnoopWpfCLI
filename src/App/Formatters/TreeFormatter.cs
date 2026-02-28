using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace SnoopWpfCLI.Formatters;

public static class TreeFormatter
{
    private const string ColumnSeparator = "  ";

    private static readonly HashSet<string> KeyProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "Name", "Content", "Text", "Title", "Header", "Source", "CommandParameter"
    };

    public static string FormatVisualTree(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return "(empty tree)";
        }

        if (!element.TryGetProperty("type", out _))
        {
            return "(empty tree)";
        }

        var sb = new StringBuilder();
        FormatNode(sb, element, "", true);
        return sb.ToString();
    }

    public static string FormatProcessList(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Array || element.GetArrayLength() == 0)
        {
            return "No WPF processes found.";
        }

        var sb = new StringBuilder();

        // Calculate column widths
        int pidWidth = 5; // "PID" + padding
        int nameWidth = 4; // "Name"
        int titleWidth = 12; // "Window Title"

        foreach (var proc in element.EnumerateArray())
        {
            var pid = proc.TryGetProperty("processId", out var pidProp) ? pidProp.ToString() : "";
            var name = proc.TryGetProperty("processName", out var nameProp) ? nameProp.GetString() ?? "" : "";
            var title = proc.TryGetProperty("mainWindowTitle", out var titleProp) ? titleProp.GetString() ?? "" : "";

            pidWidth = Math.Max(pidWidth, pid.Length);
            nameWidth = Math.Max(nameWidth, name.Length);
            titleWidth = Math.Max(titleWidth, title.Length);
        }

        // Header
        sb.Append($"{"PID".PadRight(pidWidth)}{ColumnSeparator}{"Name".PadRight(nameWidth)}{ColumnSeparator}{"Window Title".PadRight(titleWidth)}");
        sb.Append('\n');
        sb.Append(new string('-', pidWidth + nameWidth + titleWidth + ColumnSeparator.Length * 2));
        sb.Append('\n');

        // Rows
        foreach (var proc in element.EnumerateArray())
        {
            var pid = proc.TryGetProperty("processId", out var pidProp) ? pidProp.ToString() : "";
            var name = proc.TryGetProperty("processName", out var nameProp) ? nameProp.GetString() ?? "" : "";
            var title = proc.TryGetProperty("mainWindowTitle", out var titleProp) ? titleProp.GetString() ?? "" : "";

            sb.Append($"{pid.PadRight(pidWidth)}{ColumnSeparator}{name.PadRight(nameWidth)}{ColumnSeparator}{title}");
            sb.Append('\n');
        }

        return sb.ToString().TrimEnd();
    }

    public static string FormatGenericResult(JsonElement element)
    {
        var sb = new StringBuilder();

        var success = element.TryGetProperty("success", out var successProp) && successProp.GetBoolean();
        sb.Append(success ? "Success" : "Failed");

        if (element.TryGetProperty("processId", out var pidProp))
        {
            sb.Append($" (PID: {pidProp})");
        }

        if (element.TryGetProperty("message", out var msgProp))
        {
            var msg = msgProp.GetString();
            if (!string.IsNullOrEmpty(msg))
            {
                sb.Append($": {msg}");
            }
        }

        if (!success && element.TryGetProperty("error", out var errProp))
        {
            var err = errProp.GetString();
            if (!string.IsNullOrEmpty(err))
            {
                sb.Append($": {err}");
            }
        }

        return sb.ToString();
    }

    private static void FormatNode(StringBuilder sb, JsonElement node, string indent, bool isRoot)
    {
        // Build node display string
        var typeName = GetShortTypeName(node);
        var hashCode = node.TryGetProperty("hashCode", out var hashProp) ? hashProp.GetInt32() : 0;
        var props = GetKeyProperties(node);

        sb.Append($"{typeName} [#{hashCode}]");

        if (props.Count > 0)
        {
            sb.Append(' ');
            sb.Append(string.Join(" ", props));
        }

        // Process children
        if (node.TryGetProperty("children", out var children) && children.ValueKind == JsonValueKind.Array)
        {
            var childArray = new List<JsonElement>();
            foreach (var child in children.EnumerateArray())
            {
                childArray.Add(child);
            }

            for (int i = 0; i < childArray.Count; i++)
            {
                bool isLast = (i == childArray.Count - 1);
                sb.Append('\n');
                sb.Append(indent);
                sb.Append(isLast ? "\u2514\u2500 " : "\u251c\u2500 ");

                var childIndent = indent + (isLast ? "   " : "\u2502  ");
                FormatNode(sb, childArray[i], childIndent, false);
            }
        }
    }

    private static string GetShortTypeName(JsonElement node)
    {
        if (!node.TryGetProperty("type", out var typeProp))
            return "Unknown";

        var fullType = typeProp.GetString() ?? "Unknown";
        var lastDot = fullType.LastIndexOf('.');
        return lastDot >= 0 ? fullType.Substring(lastDot + 1) : fullType;
    }

    private static List<string> GetKeyProperties(JsonElement node)
    {
        var result = new List<string>();

        if (!node.TryGetProperty("properties", out var props) || props.ValueKind != JsonValueKind.Object)
            return result;

        foreach (var prop in props.EnumerateObject())
        {
            if (KeyProperties.Contains(prop.Name) && prop.Value.ValueKind != JsonValueKind.Null)
            {
                var value = prop.Value.ValueKind == JsonValueKind.String
                    ? prop.Value.GetString()
                    : prop.Value.ToString();

                if (!string.IsNullOrEmpty(value))
                {
                    result.Add($"{prop.Name}=\"{value}\"");
                }
            }
        }

        return result;
    }
}
