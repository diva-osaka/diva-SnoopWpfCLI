using System;
using System.CommandLine;
using System.Text.Json;
using SnoopWpfCLI.Formatters;
using SnoopWpfCLI.Services;

namespace SnoopWpfCLI.Commands;

public static class ListProcessesCommand
{
    public static Command Create()
    {
        var jsonOption = new Option<bool>("--json")
        {
            Description = "Output as JSON (default)",
            DefaultValueFactory = _ => true
        };

        var formatOption = new Option<string>("--format")
        {
            Description = "Output format: json or tree",
            DefaultValueFactory = _ => "json"
        };
        formatOption.AcceptOnlyFromAmong("json", "tree");

        var verboseOption = new Option<bool>("--verbose")
        {
            Description = "Enable verbose output"
        };

        var command = new Command("list-processes", "List running WPF processes");
        command.Options.Add(jsonOption);
        command.Options.Add(formatOption);
        command.Options.Add(verboseOption);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var format = parseResult.GetValue(formatOption);
            var verbose = parseResult.GetValue(verboseOption);
            var service = new WpfProcessService(verbose);

            try
            {
                var processes = await service.GetWpfProcessesAsync();

                var output = new
                {
                    success = true,
                    count = processes.Count,
                    processes = processes
                };

                if (format == "tree")
                {
                    var jsonStr = JsonSerializer.Serialize(processes);
                    using var doc = JsonDocument.Parse(jsonStr);
                    Console.WriteLine(TreeFormatter.FormatProcessList(doc.RootElement));
                }
                else
                {
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    Console.WriteLine(JsonSerializer.Serialize(output, options));
                }
                return ExitCodes.Success;
            }
            catch (Exception ex)
            {
                var error = new { success = false, error = ex.Message };
                if (format == "tree")
                {
                    var jsonStr = JsonSerializer.Serialize(error);
                    using var doc = JsonDocument.Parse(jsonStr);
                    Console.Error.WriteLine(TreeFormatter.FormatGenericResult(doc.RootElement));
                }
                else
                {
                    Console.Error.WriteLine(JsonSerializer.Serialize(error));
                }
                return ExitCodes.GeneralError;
            }
        });

        return command;
    }
}
