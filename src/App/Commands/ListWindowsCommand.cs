using System;
using System.CommandLine;
using System.Text.Json;
using System.Threading.Tasks;
using SnoopWpfCLI.Formatters;
using SnoopWpfCLI.Services;

namespace SnoopWpfCLI.Commands;

public static class ListWindowsCommand
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

        var command = new Command("list-windows", "List all windows in a WPF application");
        command.Options.Add(pidOption);
        command.Options.Add(formatOption);
        command.Options.Add(verboseOption);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var pid = parseResult.GetValue(pidOption);
            var format = parseResult.GetValue(formatOption);
            var verbose = parseResult.GetValue(verboseOption);
            var service = new InjectionService(verbose);

            try
            {
                var result = await service.ListWindowsAsync(pid);

                if (format == "tree")
                {
                    var jsonStr = JsonSerializer.Serialize(result);
                    using var doc = JsonDocument.Parse(jsonStr);
                    Console.WriteLine(TreeFormatter.FormatGenericResult(doc.RootElement));
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
                var error = new { success = false, processId = pid, error = ex.Message };
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
                return ExitCodes.Timeout;
            }
            catch (Exception ex)
            {
                var error = new { success = false, processId = pid, error = ex.Message };
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
