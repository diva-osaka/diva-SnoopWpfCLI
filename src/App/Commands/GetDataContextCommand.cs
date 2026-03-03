using System;
using System.CommandLine;
using System.Text.Json;
using System.Threading.Tasks;
using SnoopWpfCLI.Formatters;
using SnoopWpfCLI.Services;

namespace SnoopWpfCLI.Commands;

public static class GetDataContextCommand
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
            Description = "Element type name"
        };

        var hashOption = new Option<int?>("--hash")
        {
            Description = "Element hashcode"
        };

        var nameOption = new Option<string?>("--name")
        {
            Description = "Element name (x:Name) - resolves type and hash automatically"
        };

        var textOption = new Option<string?>("--text")
        {
            Description = "Element text/content to search for"
        };

        var bindingPathOption = new Option<string?>("--binding-path")
        {
            Description = "Binding path to search for"
        };

        var propertyOption = new Option<string?>("--property")
        {
            Description = "Specific property name to retrieve (optional, returns all if omitted)"
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

        var command = new Command("get-datacontext", "Get DataContext of an element");
        command.Options.Add(pidOption);
        command.Options.Add(typeOption);
        command.Options.Add(hashOption);
        command.Options.Add(nameOption);
        command.Options.Add(textOption);
        command.Options.Add(bindingPathOption);
        command.Options.Add(propertyOption);
        command.Options.Add(formatOption);
        command.Options.Add(verboseOption);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var pid = parseResult.GetValue(pidOption);
            var type = parseResult.GetValue(typeOption);
            var hashNullable = parseResult.GetValue(hashOption);
            var name = parseResult.GetValue(nameOption);
            var text = parseResult.GetValue(textOption);
            var bindingPath = parseResult.GetValue(bindingPathOption);
            var property = parseResult.GetValue(propertyOption);
            var format = parseResult.GetValue(formatOption);
            var verbose = parseResult.GetValue(verboseOption);
            var service = new InjectionService(verbose);

            try
            {
                // Mutual exclusion: --name, --text, --binding-path, --type/--hash
                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(text))
                {
                    var err = new { success = false, processId = pid, error = "Cannot specify both --name and --text" };
                    CommandHelpers.WriteError(err, format);
                    return ExitCodes.GeneralError;
                }
                if (!string.IsNullOrEmpty(name) && (!string.IsNullOrEmpty(type) || hashNullable.HasValue))
                {
                    var err = new { success = false, processId = pid, error = "Cannot specify --name with --type/--hash" };
                    CommandHelpers.WriteError(err, format);
                    return ExitCodes.GeneralError;
                }
                if (!string.IsNullOrEmpty(text) && (!string.IsNullOrEmpty(type) || hashNullable.HasValue))
                {
                    var err = new { success = false, processId = pid, error = "Cannot specify --text with --type/--hash" };
                    CommandHelpers.WriteError(err, format);
                    return ExitCodes.GeneralError;
                }
                if (!string.IsNullOrEmpty(bindingPath) && !string.IsNullOrEmpty(name))
                {
                    var err = new { success = false, processId = pid, error = "Cannot specify --binding-path with --name" };
                    CommandHelpers.WriteError(err, format);
                    return ExitCodes.GeneralError;
                }
                if (!string.IsNullOrEmpty(bindingPath) && !string.IsNullOrEmpty(text))
                {
                    var err = new { success = false, processId = pid, error = "Cannot specify --binding-path with --text" };
                    CommandHelpers.WriteError(err, format);
                    return ExitCodes.GeneralError;
                }
                if (!string.IsNullOrEmpty(bindingPath) && hashNullable.HasValue)
                {
                    var err = new { success = false, processId = pid, error = "Cannot specify --binding-path with --hash" };
                    CommandHelpers.WriteError(err, format);
                    return ExitCodes.GeneralError;
                }

                // Resolve from name/text/bindingPath if specified
                if (!string.IsNullOrEmpty(name) || !string.IsNullOrEmpty(text) || !string.IsNullOrEmpty(bindingPath))
                {
                    var findResult = await service.FindElementAsync(pid, name, text, null, type, bindingPath);
                    if (!findResult.Success)
                    {
                        var err = new { success = false, processId = pid, error = findResult.Error ?? "Element search failed" };
                        CommandHelpers.WriteError(err, format);
                        return findResult.Error == ErrorMessages.ProcessNotFound
                            ? ExitCodes.ProcessNotFound : ExitCodes.InjectionFailed;
                    }
                    if (findResult.Elements.Count == 0)
                    {
                        var err = new { success = false, processId = pid, error = "Element not found" };
                        CommandHelpers.WriteError(err, format);
                        return ExitCodes.GeneralError;
                    }
                    if (findResult.Elements.Count > 1)
                    {
                        var err = new { success = false, processId = pid, error = $"Multiple elements found ({findResult.MatchCount}). Use --type/--hash to specify." };
                        CommandHelpers.WriteError(err, format);
                        return ExitCodes.GeneralError;
                    }
                    var found = findResult.Elements[0];
                    type = found.Type;
                    hashNullable = found.Hashcode;
                }

                // Validate type + hash are available
                if (string.IsNullOrEmpty(type) || !hashNullable.HasValue)
                {
                    var err = new { success = false, processId = pid, error = "Either --name, --text, --binding-path, or both --type and --hash must be specified" };
                    CommandHelpers.WriteError(err, format);
                    return ExitCodes.GeneralError;
                }

                var hash = hashNullable.Value;
                var result = await service.GetDataContextAsync(pid, type, hash, property);

                CommandHelpers.WriteResult(result, format);
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
