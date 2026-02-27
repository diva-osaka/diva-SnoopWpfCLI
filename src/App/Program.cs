using System.CommandLine;

var rootCommand = new RootCommand("SnoopWpfCLI - WPF Visual Tree Inspector CLI");

var listProcessesCommand = new Command("list-processes", "List running WPF processes");
var pingCommand = new Command("ping", "Inject DLL and verify communication");
var getTreeCommand = new Command("get-tree", "Get the full visual tree");
var getSubtreeCommand = new Command("get-subtree", "Get a subtree by element hashcode");
var getElementCommand = new Command("get-element", "Get element details by hashcode");
var invokeCommand = new Command("invoke", "Invoke an automation peer action");
var screenshotCommand = new Command("screenshot", "Take a WPF screenshot");

rootCommand.Subcommands.Add(listProcessesCommand);
rootCommand.Subcommands.Add(pingCommand);
rootCommand.Subcommands.Add(getTreeCommand);
rootCommand.Subcommands.Add(getSubtreeCommand);
rootCommand.Subcommands.Add(getElementCommand);
rootCommand.Subcommands.Add(invokeCommand);
rootCommand.Subcommands.Add(screenshotCommand);

return await rootCommand.Parse(args).InvokeAsync();
