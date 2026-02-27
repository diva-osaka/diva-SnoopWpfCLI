using System.CommandLine;
using SnoopWpfCLI.Commands;

var rootCommand = new RootCommand("SnoopWpfCLI - WPF Visual Tree Inspector CLI");

rootCommand.Subcommands.Add(ListProcessesCommand.Create());
rootCommand.Subcommands.Add(PingCommand.Create());
rootCommand.Subcommands.Add(GetTreeCommand.Create());
rootCommand.Subcommands.Add(GetSubtreeCommand.Create());
rootCommand.Subcommands.Add(GetElementCommand.Create());
rootCommand.Subcommands.Add(InvokeCommand.Create());
rootCommand.Subcommands.Add(ScreenshotCommand.Create());

return await rootCommand.Parse(args).InvokeAsync();
