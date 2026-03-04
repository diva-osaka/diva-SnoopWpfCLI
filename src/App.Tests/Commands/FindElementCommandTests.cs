using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq;
using SnoopWpfCLI.Commands;
using Xunit;

namespace SnoopWpfCLI.Tests.Commands;

public class FindElementCommandTests
{
    [Fact]
    public void Command_HasCorrectName()
    {
        var command = FindElementCommand.Create();
        Assert.Equal("find-element", command.Name);
    }

    [Fact]
    public void Command_HasRequiredPidOption()
    {
        var command = FindElementCommand.Create();
        var pidOption = command.Options.FirstOrDefault(o => o.Name == "--pid");
        Assert.NotNull(pidOption);
    }

    [Fact]
    public void Command_HasOptionalNameOption()
    {
        var command = FindElementCommand.Create();
        var nameOption = command.Options.FirstOrDefault(o => o.Name == "--name");
        Assert.NotNull(nameOption);
    }

    [Fact]
    public void Command_HasOptionalTextOption()
    {
        var command = FindElementCommand.Create();
        var textOption = command.Options.FirstOrDefault(o => o.Name == "--text");
        Assert.NotNull(textOption);
    }

    [Fact]
    public void Command_HasOptionalAutomationIdOption()
    {
        var command = FindElementCommand.Create();
        var automationIdOption = command.Options.FirstOrDefault(o => o.Name == "--automationid");
        Assert.NotNull(automationIdOption);
    }

    [Fact]
    public void Command_HasOptionalTypeOption()
    {
        var command = FindElementCommand.Create();
        var typeOption = command.Options.FirstOrDefault(o => o.Name == "--type");
        Assert.NotNull(typeOption);
    }

    [Fact]
    public void Command_HasFormatOption()
    {
        var command = FindElementCommand.Create();
        var formatOption = command.Options.FirstOrDefault(o => o.Name == "--format");
        Assert.NotNull(formatOption);
    }

    [Fact]
    public void Parse_WithPidAndName_NoErrors()
    {
        var command = FindElementCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("find-element --pid 1234 --name CountButton");
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void Parse_WithPidAndText_NoErrors()
    {
        var command = FindElementCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("find-element --pid 1234 --text \"Click Me\"");
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void Parse_WithPidAndAutomationId_NoErrors()
    {
        var command = FindElementCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("find-element --pid 1234 --automationid BtnSubmit");
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void Parse_WithPidAndTypeFilter_NoErrors()
    {
        var command = FindElementCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("find-element --pid 1234 --type System.Windows.Controls.Button");
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void Parse_WithAllOptions_NoErrors()
    {
        var command = FindElementCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("find-element --pid 1234 --name CountButton --text \"Click Me\" --automationid BtnSubmit --type System.Windows.Controls.Button --format json");
        Assert.Equal(0, result.Errors.Count);
    }

    // At least one search criterion (--name, --text, --automationid, --type) is required.
    // --pid only without any search criteria should parse successfully (validation is at runtime).
    [Fact]
    public void Parse_WithPidOnly_NoSearchCriteria_ParsesSuccessfully()
    {
        var command = FindElementCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("find-element --pid 1234");
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void Parse_MissingPid_HasErrors()
    {
        var command = FindElementCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("find-element --name CountButton");
        Assert.True(result.Errors.Count > 0);
    }

    [Fact]
    public void Parse_WithTreeFormat_NoErrors()
    {
        var command = FindElementCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("find-element --pid 1234 --name MyButton --format tree");
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void Parse_WithInvalidFormat_HasErrors()
    {
        var command = FindElementCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("find-element --pid 1234 --name MyButton --format xml");
        Assert.True(result.Errors.Count > 0);
    }

    [Fact]
    public void Command_HasBindingPathOption()
    {
        var command = FindElementCommand.Create();
        var opt = command.Options.FirstOrDefault(o => o.Name == "--binding-path");
        Assert.NotNull(opt);
    }

    [Fact]
    public void Parse_WithBindingPathOnly_NoErrors()
    {
        var command = FindElementCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("find-element --pid 1234 --binding-path LidarIp");
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void Parse_WithBindingPathAndName_NoErrors()
    {
        var command = FindElementCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("find-element --pid 1234 --name InputField --binding-path LidarIp");
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void Command_HasInteractiveOnlyOption()
    {
        var command = FindElementCommand.Create();
        var opt = command.Options.FirstOrDefault(o => o.Name == "--interactive-only");
        Assert.NotNull(opt);
    }

    [Fact]
    public void Parse_WithInteractiveOnly_NoErrors()
    {
        var command = FindElementCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("find-element --pid 1234 --type System.Windows.Controls.Button --interactive-only");
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void Parse_WithInteractiveOnlyAlone_NoErrors()
    {
        var command = FindElementCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("find-element --pid 1234 --interactive-only");
        Assert.Equal(0, result.Errors.Count);
    }
}
