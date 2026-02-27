using System;
using System.CommandLine;
using System.Text.Json;
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

        var verboseOption = new Option<bool>("--verbose")
        {
            Description = "Enable verbose output"
        };

        var command = new Command("list-processes", "List running WPF processes");
        command.Options.Add(jsonOption);
        command.Options.Add(verboseOption);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
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

                var options = new JsonSerializerOptions { WriteIndented = true };
                Console.WriteLine(JsonSerializer.Serialize(output, options));
                return 0;
            }
            catch (Exception ex)
            {
                var error = new { success = false, error = ex.Message };
                Console.Error.WriteLine(JsonSerializer.Serialize(error));
                return 1;
            }
        });

        return command;
    }
}
