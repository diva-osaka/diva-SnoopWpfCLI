using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq;
using SnoopWpfCLI.Commands;
using Xunit;

namespace SnoopWpfCLI.Tests.Commands;

public class GetDataContextCommandTests
{
    [Fact]
    public void Command_HasCorrectName()
    {
        var command = GetDataContextCommand.Create();
        Assert.Equal("get-datacontext", command.Name);
    }

    [Fact]
    public void Command_HasRequiredOptions()
    {
        var command = GetDataContextCommand.Create();
        Assert.NotNull(command.Options.FirstOrDefault(o => o.Name == "--pid"));
        Assert.NotNull(command.Options.FirstOrDefault(o => o.Name == "--type"));
        Assert.NotNull(command.Options.FirstOrDefault(o => o.Name == "--hash"));
    }

    [Fact]
    public void Command_HasOptionalPropertyOption()
    {
        var command = GetDataContextCommand.Create();
        var propertyOption = command.Options.FirstOrDefault(o => o.Name == "--property");
        Assert.NotNull(propertyOption);
    }

    [Fact]
    public void Command_HasFormatOption()
    {
        var command = GetDataContextCommand.Create();
        var formatOption = command.Options.FirstOrDefault(o => o.Name == "--format");
        Assert.NotNull(formatOption);
    }

    [Fact]
    public void Command_HasVerboseOption()
    {
        var command = GetDataContextCommand.Create();
        var verboseOption = command.Options.FirstOrDefault(o => o.Name == "--verbose");
        Assert.NotNull(verboseOption);
    }

    [Fact]
    public void Parse_WithAllRequiredOptions_NoErrors()
    {
        var command = GetDataContextCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("get-datacontext --pid 1234 --type System.Windows.Controls.Button --hash 5678");
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void Parse_WithPropertyOption_NoErrors()
    {
        var command = GetDataContextCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("get-datacontext --pid 1234 --type System.Windows.Controls.Button --hash 5678 --property Title");
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void Parse_WithFormatOption_NoErrors()
    {
        var command = GetDataContextCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("get-datacontext --pid 1234 --type System.Windows.Controls.Button --hash 5678 --format tree");
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void Parse_WithInvalidFormat_HasErrors()
    {
        var command = GetDataContextCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("get-datacontext --pid 1234 --type System.Windows.Controls.Button --hash 5678 --format xml");
        Assert.True(result.Errors.Count > 0);
    }

    [Fact]
    public void Parse_MissingPid_HasErrors()
    {
        var command = GetDataContextCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("get-datacontext --type System.Windows.Controls.Button --hash 5678");
        Assert.True(result.Errors.Count > 0);
    }

    [Fact]
    public void Parse_MissingType_HasErrors()
    {
        var command = GetDataContextCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("get-datacontext --pid 1234 --hash 5678");
        Assert.True(result.Errors.Count > 0);
    }

    [Fact]
    public void Parse_MissingHash_HasErrors()
    {
        var command = GetDataContextCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("get-datacontext --pid 1234 --type System.Windows.Controls.Button");
        Assert.True(result.Errors.Count > 0);
    }

    [Fact]
    public void Parse_WithAllOptions_NoErrors()
    {
        var command = GetDataContextCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("get-datacontext --pid 1234 --type System.Windows.Controls.Button --hash 5678 --property Title --format json --verbose");
        Assert.Equal(0, result.Errors.Count);
    }
}
