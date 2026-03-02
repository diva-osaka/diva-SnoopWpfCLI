using SnoopWpfCLI.Services;
using Xunit;

namespace SnoopWpfCLI.Tests.Services;

/// <summary>
/// Tests for ResponseParser which handles JSON response parsing
/// from WpfInspector. Tests the production parsing logic directly.
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

        var result = ResponseParser.ParseFindElementResponse(json, 1234);

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
    public void ParseFindElementResponse_HashCodeFallback_CamelCaseKey()
    {
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

        var result = ResponseParser.ParseFindElementResponse(json, 1234);

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

        var result = ResponseParser.ParseFindElementResponse(json, 1234);

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

        var result = ResponseParser.ParseFindElementResponse(json, 1234);

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

        var result = ResponseParser.ParseFindElementResponse(json, 1234);

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

        var result = ResponseParser.ParseListWindowsResponse(json, 5678);

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

        var result = ResponseParser.ParseListWindowsResponse(json, 1234);

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

        var result = ResponseParser.ParseListWindowsResponse(json, 1234);

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

        var result = ResponseParser.ParseGetDataContextResponse(json, 1234, "System.Windows.Controls.Button", 99999);

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

        var result = ResponseParser.ParseGetDataContextResponse(json, 1234, "System.Windows.Controls.Grid", 12345);

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

        var result = ResponseParser.ParseGetDataContextResponse(json, 1234, "System.Windows.Controls.Button", 99999);

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
        Assert.Contains("not found", result.Error);
    }
}
