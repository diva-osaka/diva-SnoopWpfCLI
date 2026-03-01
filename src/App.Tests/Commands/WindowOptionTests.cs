using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq;
using SnoopWpfCLI.Commands;
using Xunit;

namespace SnoopWpfCLI.Tests.Commands;

public class WindowOptionTests
{
    [Fact]
    public void GetTreeCommand_HasWindowOption()
    {
        var command = GetTreeCommand.Create();
        var windowOption = command.Options.FirstOrDefault(o => o.Name == "--window");
        Assert.NotNull(windowOption);
    }

    [Fact]
    public void GetTreeCommand_ParseWithWindowIndex_NoErrors()
    {
        var command = GetTreeCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("get-tree --pid 1234 --window 1");
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void ScreenshotCommand_HasWindowOption()
    {
        var command = ScreenshotCommand.Create();
        var windowOption = command.Options.FirstOrDefault(o => o.Name == "--window");
        Assert.NotNull(windowOption);
    }

    [Fact]
    public void ScreenshotCommand_ParseWithWindowIndex_NoErrors()
    {
        var command = ScreenshotCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("screenshot --pid 1234 --window 1");
        Assert.Equal(0, result.Errors.Count);
    }
}
