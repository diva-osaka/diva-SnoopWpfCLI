using System.Text.Json;
using SnoopWpfCLI.Formatters;
using Xunit;

namespace SnoopWpfCLI.Tests.Formatters;

public class TreeFormatterTests
{
    [Fact]
    public void FormatVisualTree_EmptyTree_ReturnsEmptyMessage()
    {
        var json = "{}";
        var element = JsonDocument.Parse(json).RootElement;

        var result = TreeFormatter.FormatVisualTree(element);

        Assert.Equal("(empty tree)", result);
    }

    [Fact]
    public void FormatVisualTree_SingleNode_ReturnsNodeLine()
    {
        var json = """
        {
            "type": "System.Windows.Controls.Button",
            "hashCode": 12345,
            "properties": {
                "Content": "Click Me"
            }
        }
        """;
        var element = JsonDocument.Parse(json).RootElement;

        var result = TreeFormatter.FormatVisualTree(element);

        Assert.Equal("Button [#12345] Content=\"Click Me\"", result);
    }

    [Fact]
    public void FormatVisualTree_SingleNode_WithoutProperties_ShowsTypeAndHash()
    {
        var json = """
        {
            "type": "System.Windows.Controls.Grid",
            "hashCode": 23456
        }
        """;
        var element = JsonDocument.Parse(json).RootElement;

        var result = TreeFormatter.FormatVisualTree(element);

        Assert.Equal("Grid [#23456]", result);
    }

    [Fact]
    public void FormatVisualTree_NodeWithChildren_ShowsTreeLines()
    {
        var json = """
        {
            "type": "System.Windows.Controls.Grid",
            "hashCode": 100,
            "children": [
                {
                    "type": "System.Windows.Controls.Button",
                    "hashCode": 200,
                    "properties": {
                        "Content": "OK"
                    }
                },
                {
                    "type": "System.Windows.Controls.TextBox",
                    "hashCode": 300,
                    "properties": {
                        "Text": "Hello"
                    }
                }
            ]
        }
        """;
        var element = JsonDocument.Parse(json).RootElement;

        var result = TreeFormatter.FormatVisualTree(element);

        var expected =
            "Grid [#100]\n" +
            "├─ Button [#200] Content=\"OK\"\n" +
            "└─ TextBox [#300] Text=\"Hello\"";
        Assert.Equal(expected, result);
    }

    [Fact]
    public void FormatVisualTree_NestedChildren_ShowsCorrectIndentation()
    {
        var json = """
        {
            "type": "System.Windows.Window",
            "hashCode": 1,
            "properties": {
                "Title": "Main Window"
            },
            "children": [
                {
                    "type": "System.Windows.Controls.Grid",
                    "hashCode": 2,
                    "children": [
                        {
                            "type": "System.Windows.Controls.StackPanel",
                            "hashCode": 3,
                            "children": [
                                {
                                    "type": "System.Windows.Controls.TextBox",
                                    "hashCode": 4,
                                    "properties": {
                                        "Text": "Hello"
                                    }
                                },
                                {
                                    "type": "System.Windows.Controls.Button",
                                    "hashCode": 5,
                                    "properties": {
                                        "Content": "Click Me"
                                    }
                                }
                            ]
                        },
                        {
                            "type": "System.Windows.Controls.ListBox",
                            "hashCode": 6
                        }
                    ]
                },
                {
                    "type": "System.Windows.Controls.StatusBar",
                    "hashCode": 7
                }
            ]
        }
        """;
        var element = JsonDocument.Parse(json).RootElement;

        var result = TreeFormatter.FormatVisualTree(element);

        var expected =
            "Window [#1] Title=\"Main Window\"\n" +
            "├─ Grid [#2]\n" +
            "│  ├─ StackPanel [#3]\n" +
            "│  │  ├─ TextBox [#4] Text=\"Hello\"\n" +
            "│  │  └─ Button [#5] Content=\"Click Me\"\n" +
            "│  └─ ListBox [#6]\n" +
            "└─ StatusBar [#7]";
        Assert.Equal(expected, result);
    }

    [Fact]
    public void FormatVisualTree_EmptyChildren_TreatedAsLeafNode()
    {
        var json = """
        {
            "type": "System.Windows.Controls.Grid",
            "hashCode": 100,
            "children": []
        }
        """;
        var element = JsonDocument.Parse(json).RootElement;

        var result = TreeFormatter.FormatVisualTree(element);

        Assert.Equal("Grid [#100]", result);
    }

    [Fact]
    public void FormatVisualTree_MultipleProperties_ShowsKeyProperties()
    {
        var json = """
        {
            "type": "System.Windows.Controls.TextBox",
            "hashCode": 500,
            "properties": {
                "Text": "Hello",
                "IsEnabled": true,
                "Width": 200
            }
        }
        """;
        var element = JsonDocument.Parse(json).RootElement;

        var result = TreeFormatter.FormatVisualTree(element);

        // Should show key properties
        Assert.Contains("TextBox [#500]", result);
        Assert.Contains("Text=\"Hello\"", result);
    }

    [Fact]
    public void FormatVisualTree_NullValues_Handled()
    {
        var json = """
        {
            "type": "System.Windows.Controls.TextBlock",
            "hashCode": 600,
            "properties": {
                "Text": null
            }
        }
        """;
        var element = JsonDocument.Parse(json).RootElement;

        var result = TreeFormatter.FormatVisualTree(element);

        Assert.Equal("TextBlock [#600]", result);
    }

    [Fact]
    public void FormatVisualTree_ShortTypeName_ExtractsLastPart()
    {
        var json = """
        {
            "type": "System.Windows.Controls.Primitives.ScrollBar",
            "hashCode": 700
        }
        """;
        var element = JsonDocument.Parse(json).RootElement;

        var result = TreeFormatter.FormatVisualTree(element);

        Assert.Equal("ScrollBar [#700]", result);
    }

    [Fact]
    public void FormatProcessList_EmptyList_ReturnsNoProcessesMessage()
    {
        var json = "[]";
        var element = JsonDocument.Parse(json).RootElement;

        var result = TreeFormatter.FormatProcessList(element);

        Assert.Equal("No WPF processes found.", result);
    }

    [Fact]
    public void FormatProcessList_SingleProcess_ShowsTableWithHeader()
    {
        var json = """
        [
            {
                "processId": 1234,
                "processName": "MyApp",
                "mainWindowTitle": "My Application",
                "isWpfApplication": true
            }
        ]
        """;
        var element = JsonDocument.Parse(json).RootElement;

        var result = TreeFormatter.FormatProcessList(element);

        Assert.Contains("PID", result);
        Assert.Contains("Name", result);
        Assert.Contains("Window Title", result);
        Assert.Contains("1234", result);
        Assert.Contains("MyApp", result);
        Assert.Contains("My Application", result);
    }

    [Fact]
    public void FormatProcessList_MultipleProcesses_ShowsAll()
    {
        var json = """
        [
            {
                "processId": 1234,
                "processName": "App1",
                "mainWindowTitle": "Window 1",
                "isWpfApplication": true
            },
            {
                "processId": 5678,
                "processName": "App2",
                "mainWindowTitle": "Window 2",
                "isWpfApplication": true
            }
        ]
        """;
        var element = JsonDocument.Parse(json).RootElement;

        var result = TreeFormatter.FormatProcessList(element);

        Assert.Contains("1234", result);
        Assert.Contains("App1", result);
        Assert.Contains("5678", result);
        Assert.Contains("App2", result);
    }

    [Fact]
    public void FormatGenericResult_SuccessResult_ShowsSuccessMessage()
    {
        var json = """
        {
            "success": true,
            "processId": 1234,
            "message": "Pong received"
        }
        """;
        var element = JsonDocument.Parse(json).RootElement;

        var result = TreeFormatter.FormatGenericResult(element);

        Assert.Contains("Success", result);
        Assert.Contains("1234", result);
        Assert.Contains("Pong received", result);
    }

    [Fact]
    public void FormatGenericResult_FailureResult_ShowsError()
    {
        var json = """
        {
            "success": false,
            "processId": 1234,
            "error": "Connection failed"
        }
        """;
        var element = JsonDocument.Parse(json).RootElement;

        var result = TreeFormatter.FormatGenericResult(element);

        Assert.Contains("Failed", result);
        Assert.Contains("Connection failed", result);
    }

    [Fact]
    public void FormatVisualTree_ArrayInput_ReturnsEmptyMessage()
    {
        var json = "[1, 2, 3]";
        var element = JsonDocument.Parse(json).RootElement;

        var result = TreeFormatter.FormatVisualTree(element);

        Assert.Equal("(empty tree)", result);
    }

    [Fact]
    public void FormatVisualTree_StringInput_ReturnsEmptyMessage()
    {
        var json = "\"hello\"";
        var element = JsonDocument.Parse(json).RootElement;

        var result = TreeFormatter.FormatVisualTree(element);

        Assert.Equal("(empty tree)", result);
    }

    [Fact]
    public void FormatVisualTree_NumberInput_ReturnsEmptyMessage()
    {
        var json = "42";
        var element = JsonDocument.Parse(json).RootElement;

        var result = TreeFormatter.FormatVisualTree(element);

        Assert.Equal("(empty tree)", result);
    }

    [Fact]
    public void FormatVisualTree_NullInput_ReturnsEmptyMessage()
    {
        var json = "null";
        var element = JsonDocument.Parse(json).RootElement;

        var result = TreeFormatter.FormatVisualTree(element);

        Assert.Equal("(empty tree)", result);
    }

    [Fact]
    public void FormatVisualTree_BooleanInput_ReturnsEmptyMessage()
    {
        var json = "true";
        var element = JsonDocument.Parse(json).RootElement;

        var result = TreeFormatter.FormatVisualTree(element);

        Assert.Equal("(empty tree)", result);
    }

    [Fact]
    public void FormatVisualTree_NodeWithNameProperty_ShowsName()
    {
        var json = """
        {
            "type": "System.Windows.Controls.Button",
            "hashCode": 888,
            "properties": {
                "Name": "btnSubmit",
                "Content": "Submit"
            }
        }
        """;
        var element = JsonDocument.Parse(json).RootElement;

        var result = TreeFormatter.FormatVisualTree(element);

        Assert.Contains("Button [#888]", result);
        Assert.Contains("Name=\"btnSubmit\"", result);
        Assert.Contains("Content=\"Submit\"", result);
    }

    [Fact]
    public void FormatVisualTree_WpfInspectorResponse_ExtractsVisualTrees()
    {
        // This is the actual structure returned by WpfInspector
        var json = """
        {
            "success": true,
            "processId": 1234,
            "controlCount": 2,
            "visualTrees": [
                {
                    "type": "System.Windows.Window",
                    "hashCode": 100,
                    "properties": {
                        "Title": "Main Window"
                    },
                    "children": [
                        {
                            "type": "System.Windows.Controls.Grid",
                            "hashCode": 200
                        }
                    ]
                }
            ]
        }
        """;
        var element = JsonDocument.Parse(json).RootElement;

        var result = TreeFormatter.FormatVisualTree(element);

        Assert.NotEqual("(empty tree)", result);
        Assert.Contains("Window [#100]", result);
        Assert.Contains("Title=\"Main Window\"", result);
        Assert.Contains("Grid [#200]", result);
    }

    [Fact]
    public void FormatVisualTree_WpfInspectorResponse_MultipleRoots()
    {
        var json = """
        {
            "success": true,
            "processId": 1234,
            "controlCount": 3,
            "visualTrees": [
                {
                    "type": "System.Windows.Window",
                    "hashCode": 100,
                    "properties": {
                        "Title": "Window 1"
                    }
                },
                {
                    "type": "System.Windows.Window",
                    "hashCode": 200,
                    "properties": {
                        "Title": "Window 2"
                    }
                }
            ]
        }
        """;
        var element = JsonDocument.Parse(json).RootElement;

        var result = TreeFormatter.FormatVisualTree(element);

        Assert.Contains("Window [#100]", result);
        Assert.Contains("Title=\"Window 1\"", result);
        Assert.Contains("Window [#200]", result);
        Assert.Contains("Title=\"Window 2\"", result);
    }

    [Fact]
    public void FormatVisualTree_WpfInspectorResponse_EmptyVisualTrees()
    {
        var json = """
        {
            "success": true,
            "processId": 1234,
            "controlCount": 0,
            "visualTrees": []
        }
        """;
        var element = JsonDocument.Parse(json).RootElement;

        var result = TreeFormatter.FormatVisualTree(element);

        Assert.Equal("(empty tree)", result);
    }
}
