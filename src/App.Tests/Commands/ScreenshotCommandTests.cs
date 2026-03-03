using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq;
using SnoopWpfCLI.Commands;
using Xunit;

namespace SnoopWpfCLI.Tests.Commands;

public class ScreenshotCommandTests
{
    [Fact]
    public void Command_HasCorrectName()
    {
        var command = ScreenshotCommand.Create();
        Assert.Equal("screenshot", command.Name);
    }

    [Fact]
    public void Command_HasPidOption()
    {
        var command = ScreenshotCommand.Create();
        Assert.NotNull(command.Options.FirstOrDefault(o => o.Name == "--pid"));
    }

    [Fact]
    public void Command_HasOutputOption()
    {
        var command = ScreenshotCommand.Create();
        Assert.NotNull(command.Options.FirstOrDefault(o => o.Name == "--output"));
    }

    [Fact]
    public void Parse_WithPid_NoErrors()
    {
        var command = ScreenshotCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("screenshot --pid 1234");
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void Parse_WithPidAndOutput_NoErrors()
    {
        var command = ScreenshotCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("screenshot --pid 1234 --output screenshot.png");
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void Parse_WithoutPid_HasErrors()
    {
        var command = ScreenshotCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("screenshot");
        Assert.True(result.Errors.Count > 0);
    }

    [Fact]
    public void Command_HasWindowOption()
    {
        var command = ScreenshotCommand.Create();
        var windowOption = command.Options.FirstOrDefault(o => o.Name == "--window");
        Assert.NotNull(windowOption);
    }

    [Fact]
    public void Parse_WithWindowIndex_NoErrors()
    {
        var command = ScreenshotCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("screenshot --pid 1234 --window 0");
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void Parse_WithWindowIndexAndOutput_NoErrors()
    {
        var command = ScreenshotCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("screenshot --pid 1234 --window 1 --output screenshot.png");
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void Parse_WithLargeWindowIndex_NoErrors()
    {
        var command = ScreenshotCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        // Parsing should succeed; validation happens at runtime
        var result = root.Parse("screenshot --pid 1234 --window 99");
        Assert.Equal(0, result.Errors.Count);
    }
}
