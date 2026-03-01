using System.Text.Json;
using SnoopWpfCLI.Models;
using Xunit;

namespace SnoopWpfCLI.Tests.Models;

public class WindowInfoTests
{
    [Fact]
    public void WindowInfo_DefaultValues()
    {
        var info = new WindowInfo();
        Assert.Equal(0, info.Index);
        Assert.Equal(string.Empty, info.Type);
        Assert.Equal(0, info.HashCode);
        Assert.Equal(string.Empty, info.Title);
        Assert.Equal(0, info.Width);
        Assert.Equal(0, info.Height);
        Assert.False(info.IsVisible);
        Assert.False(info.IsActive);
    }

    [Fact]
    public void WindowInfo_SerializesCorrectly()
    {
        var info = new WindowInfo
        {
            Index = 0,
            Type = "System.Windows.Window",
            HashCode = 111,
            Title = "Main Window",
            Width = 800,
            Height = 600,
            IsVisible = true,
            IsActive = true
        };

        var json = JsonSerializer.Serialize(info);
        var deserialized = JsonSerializer.Deserialize<WindowInfo>(json);

        Assert.NotNull(deserialized);
        Assert.Equal(0, deserialized.Index);
        Assert.Equal("System.Windows.Window", deserialized.Type);
        Assert.Equal(111, deserialized.HashCode);
        Assert.Equal("Main Window", deserialized.Title);
        Assert.Equal(800, deserialized.Width);
        Assert.Equal(600, deserialized.Height);
        Assert.True(deserialized.IsVisible);
        Assert.True(deserialized.IsActive);
    }

    [Fact]
    public void WindowInfo_JsonPropertyNames()
    {
        var info = new WindowInfo
        {
            Index = 1,
            Type = "MyApp.DialogWindow",
            HashCode = 222,
            Title = "Settings",
            Width = 400,
            Height = 300,
            IsVisible = true,
            IsActive = false
        };

        var json = JsonSerializer.Serialize(info);

        Assert.Contains("\"index\":", json);
        Assert.Contains("\"type\":", json);
        Assert.Contains("\"hashCode\":", json);
        Assert.Contains("\"title\":", json);
        Assert.Contains("\"width\":", json);
        Assert.Contains("\"height\":", json);
        Assert.Contains("\"isVisible\":", json);
        Assert.Contains("\"isActive\":", json);
    }
}

public class ListWindowsResultTests
{
    [Fact]
    public void ListWindowsResult_DefaultValues()
    {
        var result = new ListWindowsResult();
        Assert.False(result.Success);
        Assert.Equal(0, result.ProcessId);
        Assert.Equal(0, result.WindowCount);
        Assert.NotNull(result.Windows);
        Assert.Empty(result.Windows);
        Assert.Equal(string.Empty, result.Message);
        Assert.Null(result.Error);
    }

    [Fact]
    public void ListWindowsResult_SerializesCorrectly()
    {
        var result = new ListWindowsResult
        {
            Success = true,
            ProcessId = 1234,
            WindowCount = 2,
            Message = "Windows listed successfully",
            Windows = new()
            {
                new WindowInfo { Index = 0, Type = "System.Windows.Window", Title = "Main" },
                new WindowInfo { Index = 1, Type = "System.Windows.Window", Title = "Dialog" }
            }
        };

        var json = JsonSerializer.Serialize(result);
        var deserialized = JsonSerializer.Deserialize<ListWindowsResult>(json);

        Assert.NotNull(deserialized);
        Assert.True(deserialized.Success);
        Assert.Equal(1234, deserialized.ProcessId);
        Assert.Equal(2, deserialized.WindowCount);
        Assert.Equal(2, deserialized.Windows.Count);
        Assert.Equal("Main", deserialized.Windows[0].Title);
        Assert.Equal("Dialog", deserialized.Windows[1].Title);
    }

    [Fact]
    public void ListWindowsResult_JsonPropertyNames()
    {
        var result = new ListWindowsResult
        {
            Success = true,
            ProcessId = 1234,
            WindowCount = 1
        };

        var json = JsonSerializer.Serialize(result);

        Assert.Contains("\"success\":", json);
        Assert.Contains("\"processId\":", json);
        Assert.Contains("\"windowCount\":", json);
        Assert.Contains("\"windows\":", json);
        Assert.Contains("\"message\":", json);
    }
}
