using System;
using System.CommandLine;
using System.Text;
using SnoopWpfCLI.Commands;

Console.OutputEncoding = Encoding.UTF8;

var rootCommand = new RootCommand("SnoopWpfCLI - WPF Visual Tree Inspector CLI");

rootCommand.Subcommands.Add(ListProcessesCommand.Create());
rootCommand.Subcommands.Add(PingCommand.Create());
rootCommand.Subcommands.Add(GetTreeCommand.Create());
rootCommand.Subcommands.Add(GetSubtreeCommand.Create());
rootCommand.Subcommands.Add(GetElementCommand.Create());
rootCommand.Subcommands.Add(InvokeCommand.Create());
rootCommand.Subcommands.Add(ScreenshotCommand.Create());
rootCommand.Subcommands.Add(ListWindowsCommand.Create());
rootCommand.Subcommands.Add(FindElementCommand.Create());
rootCommand.Subcommands.Add(GetDataContextCommand.Create());
rootCommand.Subcommands.Add(WaitCommand.Create());
rootCommand.Subcommands.Add(AssertCommand.Create());

return await rootCommand.Parse(args).InvokeAsync();
