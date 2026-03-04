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

    public static string FormatVisualTree(JsonElement element, bool detail = false)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return "(empty tree)";
        }

        // If the element has a "type" property, it's a direct tree node
        if (element.TryGetProperty("type", out _))
        {
            var sb = new StringBuilder();
            FormatNode(sb, element, "", true, detail);
            return sb.ToString();
        }

        // If the element has a "visualTrees" array, extract and format each root
        if (element.TryGetProperty("visualTrees", out var visualTrees)
            && visualTrees.ValueKind == JsonValueKind.Array
            && visualTrees.GetArrayLength() > 0)
        {
            var sb = new StringBuilder();
            bool first = true;
            foreach (var root in visualTrees.EnumerateArray())
            {
                if (!first)
                    sb.Append('\n');
                first = false;
                FormatNode(sb, root, "", true, detail);
            }
            return sb.ToString();
        }

        return "(empty tree)";
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

    public static string FormatFindElementResult(JsonElement element)
    {
        var sb = new StringBuilder();

        var success = element.TryGetProperty("success", out var successProp) && successProp.GetBoolean();
        var matchCount = element.TryGetProperty("matchCount", out var matchCountProp) ? matchCountProp.GetInt32() : 0;

        if (!success)
        {
            var error = element.TryGetProperty("error", out var errProp) ? errProp.GetString() ?? "Unknown error" : "Unknown error";
            sb.Append($"Failed: {error}");
            return sb.ToString();
        }

        sb.Append($"Found {matchCount} element(s)");

        if (element.TryGetProperty("processId", out var pidProp))
        {
            sb.Append($" (PID: {pidProp})");
        }

        sb.Append('\n');

        if (element.TryGetProperty("elements", out var elements) && elements.ValueKind == JsonValueKind.Array)
        {
            foreach (var elem in elements.EnumerateArray())
            {
                var type = elem.TryGetProperty("type", out var t) ? t.GetString() ?? "Unknown" : "Unknown";
                var lastDot = type.LastIndexOf('.');
                var shortType = lastDot >= 0 ? type.Substring(lastDot + 1) : type;

                var hashCode = elem.TryGetProperty("hashcode", out var h) ? h.GetInt32()
                    : elem.TryGetProperty("hashCode", out var hUpper) ? hUpper.GetInt32() : 0;

                sb.Append($"  {shortType} [#{hashCode}]");

                if (elem.TryGetProperty("name", out var nameProp) && nameProp.ValueKind == JsonValueKind.String)
                {
                    var name = nameProp.GetString();
                    if (!string.IsNullOrEmpty(name))
                        sb.Append($" Name=\"{name}\"");
                }

                if (elem.TryGetProperty("content", out var contentProp) && contentProp.ValueKind == JsonValueKind.String)
                {
                    var content = contentProp.GetString();
                    if (!string.IsNullOrEmpty(content))
                        sb.Append($" Content=\"{content}\"");
                }

                if (elem.TryGetProperty("automationId", out var aidProp) && aidProp.ValueKind == JsonValueKind.String)
                {
                    var automationId = aidProp.GetString();
                    if (!string.IsNullOrEmpty(automationId))
                        sb.Append($" AutomationId=\"{automationId}\"");
                }

                // Show binding paths if present
                if (elem.TryGetProperty("bindings", out var bindingsProp) && bindingsProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var binding in bindingsProp.EnumerateArray())
                    {
                        var property = binding.TryGetProperty("property", out var propName) ? propName.GetString() : null;
                        var path = binding.TryGetProperty("path", out var pathVal) ? pathVal.GetString() : null;
                        if (!string.IsNullOrEmpty(property) && !string.IsNullOrEmpty(path))
                            sb.Append($" {property}={{Binding: {path}}}");
                    }
                }

                sb.Append('\n');
            }
        }

        return sb.ToString().TrimEnd();
    }

    public static string FormatWindowsList(JsonElement element)
    {
        var sb = new StringBuilder();

        var success = element.TryGetProperty("success", out var successProp) && successProp.GetBoolean();
        var windowCount = element.TryGetProperty("windowCount", out var countProp) ? countProp.GetInt32() : 0;

        if (!success)
        {
            var error = element.TryGetProperty("error", out var errProp) ? errProp.GetString() ?? "Unknown error" : "Unknown error";
            sb.Append($"Failed: {error}");
            return sb.ToString();
        }

        sb.Append($"{windowCount} window(s)");

        if (element.TryGetProperty("processId", out var pidProp))
        {
            sb.Append($" (PID: {pidProp})");
        }

        sb.Append('\n');

        if (element.TryGetProperty("windows", out var windows) && windows.ValueKind == JsonValueKind.Array)
        {
            foreach (var win in windows.EnumerateArray())
            {
                var index = win.TryGetProperty("index", out var idx) ? idx.GetInt32() : 0;
                var type = win.TryGetProperty("type", out var t) ? t.GetString() ?? "Unknown" : "Unknown";
                var lastDot = type.LastIndexOf('.');
                var shortType = lastDot >= 0 ? type.Substring(lastDot + 1) : type;

                var title = win.TryGetProperty("title", out var titleProp) ? titleProp.GetString() ?? "" : "";
                var width = win.TryGetProperty("width", out var w) ? w.GetDouble() : 0;
                var height = win.TryGetProperty("height", out var h) ? h.GetDouble() : 0;
                var isVisible = win.TryGetProperty("isVisible", out var vis) && vis.GetBoolean();
                var isActive = win.TryGetProperty("isActive", out var act) && act.GetBoolean();

                sb.Append($"  [{index}] {shortType} \"{title}\" ({width}x{height}) {(isVisible ? "visible" : "hidden")} {(isActive ? "active" : "inactive")}");
                sb.Append('\n');
            }
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

    private static void FormatNode(StringBuilder sb, JsonElement node, string indent, bool isRoot, bool detail = false)
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

        // Show binding details when detail mode is enabled
        if (detail)
        {
            var bindings = GetBindingProperties(node);
            if (bindings.Count > 0)
            {
                sb.Append(' ');
                sb.Append(string.Join(" ", bindings));
            }
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
                FormatNode(sb, childArray[i], childIndent, false, detail);
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

    private static List<string> GetBindingProperties(JsonElement node)
    {
        var result = new List<string>();

        foreach (var prop in node.EnumerateObject())
        {
            // Skip known non-property fields
            if (prop.Name is "type" or "hashCode" or "children" or "childCount"
                or "properties" or "dataContextId" or "automationPeer" or "error")
                continue;

            // Check if value is a binding object
            if (prop.Value.ValueKind == JsonValueKind.Object
                && prop.Value.TryGetProperty("type", out var typeVal)
                && typeVal.GetString() == "binding"
                && prop.Value.TryGetProperty("path", out var pathVal))
            {
                var path = pathVal.GetString();
                if (!string.IsNullOrEmpty(path))
                {
                    result.Add($"{prop.Name}={{Binding: {path}}}");
                }
            }
        }

        return result;
    }
}
