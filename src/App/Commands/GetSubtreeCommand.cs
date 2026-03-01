using System;
using System.CommandLine;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using SnoopWpfCLI.Formatters;
using SnoopWpfCLI.Services;

namespace SnoopWpfCLI.Commands;

public static class GetSubtreeCommand
{
    public static Command Create()
    {
        var pidOption = new Option<int>("--pid")
        {
            Description = "Target process ID",
            Required = true
        };

        var typeOption = new Option<string?>("--type")
        {
            Description = "Element type name (required unless --name is specified)"
        };

        var hashOption = new Option<int?>("--hash")
        {
            Description = "Element hashcode (required unless --name is specified)"
        };

        var nameOption = new Option<string?>("--name")
        {
            Description = "Element name (x:Name) - resolves type and hash automatically"
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

        var command = new Command("get-subtree", "Get a subtree by element hashcode");
        command.Options.Add(pidOption);
        command.Options.Add(typeOption);
        command.Options.Add(hashOption);
        command.Options.Add(nameOption);
        command.Options.Add(formatOption);
        command.Options.Add(verboseOption);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var pid = parseResult.GetValue(pidOption);
            var type = parseResult.GetValue(typeOption);
            var hashNullable = parseResult.GetValue(hashOption);
            var name = parseResult.GetValue(nameOption);
            var format = parseResult.GetValue(formatOption);
            var verbose = parseResult.GetValue(verboseOption);
            var service = new InjectionService(verbose);

            try
            {
                // Resolve type/hash from name if specified
                if (!string.IsNullOrEmpty(name))
                {
                    var findResult = await service.FindElementAsync(pid, name, null, null, null);
                    if (!findResult.Success || findResult.Elements.Count == 0)
                    {
                        var err = new { success = false, processId = pid, error = $"Element with name '{name}' not found" };
                        Console.Error.WriteLine(format == "tree"
                            ? TreeFormatter.FormatGenericResult(JsonDocument.Parse(JsonSerializer.Serialize(err)).RootElement)
                            : JsonSerializer.Serialize(err));
                        return ExitCodes.GeneralError;
                    }
                    var found = findResult.Elements.First();
                    type = found.Type;
                    hashNullable = found.Hashcode;
                }

                if (string.IsNullOrEmpty(type) || !hashNullable.HasValue)
                {
                    var err = new { success = false, processId = pid, error = "Either --name or both --type and --hash must be specified" };
                    Console.Error.WriteLine(format == "tree"
                        ? TreeFormatter.FormatGenericResult(JsonDocument.Parse(JsonSerializer.Serialize(err)).RootElement)
                        : JsonSerializer.Serialize(err));
                    return ExitCodes.GeneralError;
                }

                var hash = hashNullable.Value;
                var result = await service.GetVisualTreeByHashcodeAsync(pid, type, hash);

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
