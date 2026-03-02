using System.Text.Json;
using SnoopWpfCLI.Models;
using Xunit;

namespace SnoopWpfCLI.Tests.Services;

/// <summary>
/// Tests for InjectionService response JSON parsing logic.
/// These tests validate that the parse logic correctly handles
/// responses from WpfInspector without requiring actual IPC communication.
/// </summary>
public class InjectionServiceParseTests
{
    // --- FindElement response parsing ---

    [Fact]
    public void ParseFindElementResponse_Success_WithElements()
    {
        var json = """
        {
            "success": true,
            "processId": 1234,
            "message": "Found 2 matching element(s)",
            "matchCount": 2,
            "elements": [
                {
                    "type": "System.Windows.Controls.Button",
                    "hashCode": 11111,
                    "name": "CountButton",
                    "content": "Click Me",
                    "automationId": "BtnCount"
                },
                {
                    "type": "System.Windows.Controls.TextBox",
                    "hashCode": 22222,
                    "name": "InputField",
                    "content": null,
                    "automationId": null
                }
            ]
        }
        """;

        var result = ParseFindElementResponse(json, 1234);

        Assert.True(result.Success);
        Assert.Equal(1234, result.ProcessId);
        Assert.Equal(2, result.MatchCount);
        Assert.Equal(2, result.Elements.Count);

        Assert.Equal("System.Windows.Controls.Button", result.Elements[0].Type);
        Assert.Equal(11111, result.Elements[0].Hashcode);
        Assert.Equal("CountButton", result.Elements[0].Name);
        Assert.Equal("Click Me", result.Elements[0].Content);
        Assert.Equal("BtnCount", result.Elements[0].AutomationId);

        Assert.Equal("System.Windows.Controls.TextBox", result.Elements[1].Type);
        Assert.Equal(22222, result.Elements[1].Hashcode);
        Assert.Equal("InputField", result.Elements[1].Name);
        Assert.Null(result.Elements[1].Content);
        Assert.Null(result.Elements[1].AutomationId);
    }

    [Fact]
    public void ParseFindElementResponse_HashCodeFallback_LowercaseKey()
    {
        // WpfInspector may return "hashCode" (camelCase) but FoundElement model uses "hashcode"
        var json = """
        {
            "success": true,
            "processId": 1234,
            "matchCount": 1,
            "elements": [
                {
                    "type": "System.Windows.Controls.Button",
                    "hashCode": 99999,
                    "name": "Btn"
                }
            ]
        }
        """;

        var result = ParseFindElementResponse(json, 1234);

        Assert.Equal(99999, result.Elements[0].Hashcode);
    }

    [Fact]
    public void ParseFindElementResponse_HashCodeFallback_LowercaseHashcode()
    {
        var json = """
        {
            "success": true,
            "processId": 1234,
            "matchCount": 1,
            "elements": [
                {
                    "type": "System.Windows.Controls.Button",
                    "hashcode": 88888,
                    "name": "Btn"
                }
            ]
        }
        """;

        var result = ParseFindElementResponse(json, 1234);

        Assert.Equal(88888, result.Elements[0].Hashcode);
    }

    [Fact]
    public void ParseFindElementResponse_EmptyElements()
    {
        var json = """
        {
            "success": true,
            "processId": 1234,
            "matchCount": 0,
            "elements": []
        }
        """;

        var result = ParseFindElementResponse(json, 1234);

        Assert.True(result.Success);
        Assert.Equal(0, result.MatchCount);
        Assert.Empty(result.Elements);
    }

    [Fact]
    public void ParseFindElementResponse_ErrorResponse()
    {
        var json = """
        {
            "success": false,
            "error": "Error accessing UI thread: timeout"
        }
        """;

        var result = ParseFindElementResponse(json, 1234);

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
        Assert.Contains("timeout", result.Error);
    }

    // --- ListWindows response parsing ---

    [Fact]
    public void ParseListWindowsResponse_Success_WithWindows()
    {
        var json = """
        {
            "success": true,
            "processId": 5678,
            "windowCount": 2,
            "windows": [
                {
                    "index": 0,
                    "type": "MyApp.MainWindow",
                    "hashCode": 11111,
                    "title": "Main Window",
                    "width": 1024,
                    "height": 768,
                    "isVisible": true,
                    "isActive": true
                },
                {
                    "index": 1,
                    "type": "MyApp.SettingsWindow",
                    "hashCode": 22222,
                    "title": "Settings",
                    "width": 400,
                    "height": 300,
                    "isVisible": true,
                    "isActive": false
                }
            ]
        }
        """;

        var result = ParseListWindowsResponse(json, 5678);

        Assert.True(result.Success);
        Assert.Equal(5678, result.ProcessId);
        Assert.Equal(2, result.WindowCount);
        Assert.Equal(2, result.Windows.Count);

        Assert.Equal(0, result.Windows[0].Index);
        Assert.Equal("MyApp.MainWindow", result.Windows[0].Type);
        Assert.Equal(11111, result.Windows[0].HashCode);
        Assert.Equal("Main Window", result.Windows[0].Title);
        Assert.Equal(1024, result.Windows[0].Width);
        Assert.Equal(768, result.Windows[0].Height);
        Assert.True(result.Windows[0].IsVisible);
        Assert.True(result.Windows[0].IsActive);

        Assert.Equal(1, result.Windows[1].Index);
        Assert.False(result.Windows[1].IsActive);
    }

    [Fact]
    public void ParseListWindowsResponse_EmptyWindows()
    {
        var json = """
        {
            "success": true,
            "processId": 1234,
            "windowCount": 0,
            "windows": []
        }
        """;

        var result = ParseListWindowsResponse(json, 1234);

        Assert.True(result.Success);
        Assert.Equal(0, result.WindowCount);
        Assert.Empty(result.Windows);
    }

    [Fact]
    public void ParseListWindowsResponse_ErrorResponse()
    {
        var json = """
        {
            "success": false,
            "error": "Error listing windows: access denied"
        }
        """;

        var result = ParseListWindowsResponse(json, 1234);

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    // --- GetDataContext response parsing ---

    [Fact]
    public void ParseGetDataContextResponse_Success_WithDataContext()
    {
        var json = """
        {
            "success": true,
            "message": "DataContext retrieved successfully",
            "processId": 1234,
            "elementType": "System.Windows.Controls.Button",
            "elementHashcode": 99999,
            "hasDataContext": true,
            "dataContext": {
                "type": "MyApp.ViewModel",
                "hashCode": 55555,
                "properties": {
                    "Name": { "propertyType": "String", "value": "Test", "isReadOnly": false }
                }
            }
        }
        """;

        var result = ParseGetDataContextResponse(json, 1234, "System.Windows.Controls.Button", 99999);

        Assert.True(result.Success);
        Assert.Equal(1234, result.ProcessId);
        Assert.Equal("System.Windows.Controls.Button", result.ElementType);
        Assert.Equal(99999, result.ElementHashcode);
        Assert.True(result.HasDataContext);
        Assert.NotNull(result.DataContext);
    }

    [Fact]
    public void ParseGetDataContextResponse_NoDataContext()
    {
        var json = """
        {
            "success": true,
            "message": "Element has no DataContext",
            "processId": 1234,
            "elementType": "System.Windows.Controls.Grid",
            "elementHashcode": 12345,
            "hasDataContext": false
        }
        """;

        var result = ParseGetDataContextResponse(json, 1234, "System.Windows.Controls.Grid", 12345);

        Assert.True(result.Success);
        Assert.False(result.HasDataContext);
        Assert.Null(result.DataContext);
    }

    [Fact]
    public void ParseGetDataContextResponse_ElementNotFound()
    {
        var json = """
        {
            "success": false,
            "error": "System.Windows.Controls.Button with hashcode 99999 not found in any window"
        }
        """;

        var result = ParseGetDataContextResponse(json, 1234, "System.Windows.Controls.Button", 99999);

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
        Assert.Contains("not found", result.Error);
    }

    // --- Helper methods that replicate InjectionService parsing logic ---

    private static FindElementResult ParseFindElementResponse(string response, int processId)
    {
        using var doc = JsonDocument.Parse(response);
        var root = doc.RootElement;

        var success = root.TryGetProperty("success", out var successElement) ? successElement.GetBoolean() : false;
        var message = root.TryGetProperty("message", out var messageElement) ? messageElement.GetString() ?? "" : "";
        var error = root.TryGetProperty("error", out var errorElement) ? errorElement.GetString() : null;
        var matchCount = root.TryGetProperty("matchCount", out var matchCountElement) ? matchCountElement.GetInt32() : 0;

        var elements = new System.Collections.Generic.List<FoundElement>();
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

    private static ListWindowsResult ParseListWindowsResponse(string response, int processId)
    {
        using var doc = JsonDocument.Parse(response);
        var root = doc.RootElement;

        var success = root.TryGetProperty("success", out var successElement) && successElement.GetBoolean();
        var message = root.TryGetProperty("message", out var messageElement) ? messageElement.GetString() ?? "" : "";
        var error = root.TryGetProperty("error", out var errorElement) ? errorElement.GetString() : null;
        var windowCount = root.TryGetProperty("windowCount", out var countElement) ? countElement.GetInt32() : 0;

        var windows = new System.Collections.Generic.List<WindowInfo>();
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

    private static DataContextResult ParseGetDataContextResponse(string response, int processId, string type, int hashcode)
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
