using System;
using System.Collections.Generic;
using System.Text.Json;
using SnoopWpfCLI.Models;

namespace SnoopWpfCLI.Services;

internal static class ResponseParser
{
    internal static FindElementResult ParseFindElementResponse(string response, int processId)
    {
        using var doc = JsonDocument.Parse(response);
        var root = doc.RootElement;

        var success = root.TryGetProperty("success", out var successElement) ? successElement.GetBoolean() : false;
        var message = root.TryGetProperty("message", out var messageElement) ? messageElement.GetString() ?? "" : "";
        var error = root.TryGetProperty("error", out var errorElement) ? errorElement.GetString() : null;
        var matchCount = root.TryGetProperty("matchCount", out var matchCountElement) ? matchCountElement.GetInt32() : 0;

        var elements = new List<FoundElement>();
        if (root.TryGetProperty("elements", out var elementsElement) && elementsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var elem in elementsElement.EnumerateArray())
            {
                var found = new FoundElement
                {
                    Type = elem.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "",
                    Hashcode = elem.TryGetProperty("hashCode", out var h) ? h.GetInt32()
                        : elem.TryGetProperty("hashcode", out var hLower) ? hLower.GetInt32() : 0,
                    Name = elem.TryGetProperty("name", out var n) ? n.GetString() : null,
                    Content = elem.TryGetProperty("content", out var c) ? c.GetString() : null,
                    AutomationId = elem.TryGetProperty("automationId", out var a) ? a.GetString() : null
                };
                elements.Add(found);
            }
        }

        return new FindElementResult
        {
            Success = success,
            ProcessId = processId,
            Message = success ? (message ?? string.Empty) : (error ?? "Unknown error"),
            Error = success ? null : (error ?? "Unknown error"),
            MatchCount = matchCount,
            Elements = elements
        };
    }

    internal static ListWindowsResult ParseListWindowsResponse(string response, int processId)
    {
        using var doc = JsonDocument.Parse(response);
        var root = doc.RootElement;

        var success = root.TryGetProperty("success", out var successElement) && successElement.GetBoolean();
        var message = root.TryGetProperty("message", out var messageElement) ? messageElement.GetString() ?? "" : "";
        var error = root.TryGetProperty("error", out var errorElement) ? errorElement.GetString() : null;
        var windowCount = root.TryGetProperty("windowCount", out var countElement) ? countElement.GetInt32() : 0;

        var windows = new List<WindowInfo>();
        if (root.TryGetProperty("windows", out var windowsElement) && windowsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var windowElement in windowsElement.EnumerateArray())
            {
                var windowInfo = new WindowInfo
                {
                    Index = windowElement.TryGetProperty("index", out var idx) ? idx.GetInt32() : 0,
                    Type = windowElement.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "",
                    HashCode = windowElement.TryGetProperty("hashCode", out var hc) ? hc.GetInt32() : 0,
                    Title = windowElement.TryGetProperty("title", out var title) ? title.GetString() ?? "" : "",
                    Width = windowElement.TryGetProperty("width", out var w) ? w.GetDouble() : 0,
                    Height = windowElement.TryGetProperty("height", out var h) ? h.GetDouble() : 0,
                    IsVisible = windowElement.TryGetProperty("isVisible", out var vis) && vis.GetBoolean(),
                    IsActive = windowElement.TryGetProperty("isActive", out var act) && act.GetBoolean()
                };
                windows.Add(windowInfo);
            }
        }

        return new ListWindowsResult
        {
            Success = success,
            ProcessId = processId,
            WindowCount = windowCount,
            Windows = windows,
            Message = success ? (message ?? string.Empty) : (error ?? "Unknown error"),
            Error = success ? null : (error ?? "Unknown error")
        };
    }

    internal static AutomationPeerResult ParseAutomationPeerResponse(string response, int processId, string type, int hashcode, string action)
    {
        using var doc = JsonDocument.Parse(response);
        var root = doc.RootElement;

        var success = root.TryGetProperty("success", out var successElement) ? successElement.GetBoolean() : false;
        var message = root.TryGetProperty("message", out var messageElement) ? messageElement.GetString() ?? "" : "";
        var error = root.TryGetProperty("error", out var errorElement) ? errorElement.GetString() : null;

        Dictionary<string, object?>? resultData = null;
        foreach (var prop in root.EnumerateObject())
        {
            if (prop.Name is "success" or "message" or "error") continue;
            resultData ??= new();
            resultData[prop.Name] = prop.Value.ValueKind switch
            {
                JsonValueKind.String => prop.Value.GetString(),
                JsonValueKind.Number => prop.Value.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => prop.Value.GetRawText()
            };
        }

        return new AutomationPeerResult
        {
            Success = success,
            ProcessId = processId,
            Type = type,
            Hashcode = hashcode,
            Action = action,
            Message = success ? (message ?? string.Empty) : (error ?? "Unknown error"),
            Error = success ? null : (error ?? "Unknown error"),
            Result = resultData
        };
    }

    internal static DataContextResult ParseGetDataContextResponse(string response, int processId, string type, int hashcode)
    {
        using var doc = JsonDocument.Parse(response);
        var root = doc.RootElement;

        var success = root.TryGetProperty("success", out var successElement) ? successElement.GetBoolean() : false;
        var message = root.TryGetProperty("message", out var messageElement) ? messageElement.GetString() ?? "" : "";
        var error = root.TryGetProperty("error", out var errorElement) ? errorElement.GetString() : null;
        var hasDataContext = root.TryGetProperty("hasDataContext", out var hasDataContextElement) && hasDataContextElement.GetBoolean();

        object? dataContext = null;
        if (root.TryGetProperty("dataContext", out var dataContextElement) && dataContextElement.ValueKind != JsonValueKind.Null)
        {
            dataContext = JsonSerializer.Deserialize<object>(dataContextElement.GetRawText());
        }

        return new DataContextResult
        {
            Success = success,
            ProcessId = processId,
            ElementType = type,
            ElementHashcode = hashcode,
            Message = success ? (message ?? string.Empty) : (error ?? "Unknown error"),
            Error = success ? null : (error ?? "Unknown error"),
            HasDataContext = hasDataContext,
            DataContext = dataContext
        };
    }
}
