using System;
using System.CommandLine;
using System.Text.Json;
using SnoopWpfCLI.Services;

namespace SnoopWpfCLI.Commands;

public static class GetElementCommand
{
    public static Command Create()
    {
        var pidOption = new Option<int>("--pid")
        {
            Description = "Target process ID",
            Required = true
        };

        var typeOption = new Option<string>("--type")
        {
            Description = "Element type name",
            Required = true
        };

        var hashOption = new Option<int>("--hash")
        {
            Description = "Element hashcode",
            Required = true
        };

        var verboseOption = new Option<bool>("--verbose")
        {
            Description = "Enable verbose output"
        };

        var command = new Command("get-element", "Get element details by hashcode");
        command.Options.Add(pidOption);
        command.Options.Add(typeOption);
        command.Options.Add(hashOption);
        command.Options.Add(verboseOption);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var pid = parseResult.GetValue(pidOption);
            var type = parseResult.GetValue(typeOption)!;
            var hash = parseResult.GetValue(hashOption);
            var verbose = parseResult.GetValue(verboseOption);
            var service = new InjectionService(verbose);

            try
            {
                var result = await service.GetElementByHashcodeAsync(pid, type, hash);

                var options = new JsonSerializerOptions { WriteIndented = true };
                Console.WriteLine(JsonSerializer.Serialize(result, options));
                return result.Success ? 0 : 3;
            }
            catch (Exception ex)
            {
                var error = new { success = false, processId = pid, error = ex.Message };
                Console.Error.WriteLine(JsonSerializer.Serialize(error));
                return 1;
            }
        });

        return command;
    }
}
