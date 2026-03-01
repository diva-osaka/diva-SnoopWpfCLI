using System;
using System.CommandLine;
using System.Text.Json;
using System.Threading.Tasks;
using SnoopWpfCLI.Formatters;
using SnoopWpfCLI.Services;

namespace SnoopWpfCLI.Commands;

public static class GetTreeCommand
{
    public static Command Create()
    {
        var pidOption = new Option<int>("--pid")
        {
            Description = "Target process ID",
            Required = true
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

        var windowOption = new Option<int?>("--window")
        {
            Description = "Window index (use list-windows to find indices)"
        };

        var command = new Command("get-tree", "Get the full visual tree");
        command.Options.Add(pidOption);
        command.Options.Add(formatOption);
        command.Options.Add(verboseOption);
        command.Options.Add(windowOption);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var pid = parseResult.GetValue(pidOption);
            var format = parseResult.GetValue(formatOption);
            var verbose = parseResult.GetValue(verboseOption);
            var windowIndex = parseResult.GetValue(windowOption);
            var service = new InjectionService(verbose);

            try
            {
                var result = await service.GetVisualTreeAsync(pid, windowIndex);

                if (format == "tree" && result.Success && !string.IsNullOrEmpty(result.VisualTreeJson))
                {
                    using var doc = JsonDocument.Parse(result.VisualTreeJson);
                    Console.WriteLine(TreeFormatter.FormatVisualTree(doc.RootElement));
                }
                else
                {
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    Console.WriteLine(JsonSerializer.Serialize(result, options));
                }
                if (result.Success)
                    return ExitCodes.Success;
                return result.Error == ErrorMessages.ProcessNotFound ? ExitCodes.ProcessNotFound : ExitCodes.InjectionFailed;
            }
            catch (Exception ex) when (ex is TimeoutException or OperationCanceledException or TaskCanceledException)
            {
                CommandHelpers.WriteError(new { success = false, processId = pid, error = ex.Message }, format);
                return ExitCodes.Timeout;
            }
            catch (Exception ex)
            {
                CommandHelpers.WriteError(new { success = false, processId = pid, error = ex.Message }, format);
                return ExitCodes.GeneralError;
            }
        });

        return command;
    }
}
