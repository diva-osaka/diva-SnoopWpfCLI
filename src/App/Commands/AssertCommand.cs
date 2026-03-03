using System;
using System.CommandLine;
using System.Text.Json;
using System.Threading.Tasks;
using SnoopWpfCLI.Models;
using SnoopWpfCLI.Services;

namespace SnoopWpfCLI.Commands;

public static class AssertCommand
{
    public static Command Create()
    {
        var pidOption = new Option<int>("--pid")
        {
            Description = "Target process ID",
            Required = true
        };

        var nameOption = new Option<string?>("--name")
        {
            Description = "Element name (x:Name) to assert on"
        };

        var textOption = new Option<string?>("--text")
        {
            Description = "Assert element text/content equals this value"
        };

        var automationIdOption = new Option<string?>("--automationid")
        {
            Description = "AutomationId to search for"
        };

        var typeOption = new Option<string?>("--type")
        {
            Description = "Element type name to filter by"
        };

        var hashOption = new Option<int?>("--hash")
        {
            Description = "Element hashcode (use with --type for specific element)"
        };

        var bindingPathOption = new Option<string?>("--binding-path")
        {
            Description = "Binding path to search for"
        };

        var existsOption = new Option<bool>("--exists")
        {
            Description = "Assert that the element exists"
        };

        var propertyOption = new Option<string?>("--property")
        {
            Description = "DataContext property name to assert on"
        };

        var expectedOption = new Option<string?>("--expected")
        {
            Description = "Expected value for --property assertion"
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

        var command = new Command("assert", "Assert element state or property values");
        command.Options.Add(pidOption);
        command.Options.Add(nameOption);
        command.Options.Add(textOption);
        command.Options.Add(automationIdOption);
        command.Options.Add(typeOption);
        command.Options.Add(hashOption);
        command.Options.Add(bindingPathOption);
        command.Options.Add(existsOption);
        command.Options.Add(propertyOption);
        command.Options.Add(expectedOption);
        command.Options.Add(formatOption);
        command.Options.Add(verboseOption);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var pid = parseResult.GetValue(pidOption);
            var name = parseResult.GetValue(nameOption)?.Trim() is { Length: > 0 } n ? n : null;
            var text = parseResult.GetValue(textOption)?.Trim() is { Length: > 0 } tx ? tx : null;
            var automationId = parseResult.GetValue(automationIdOption)?.Trim() is { Length: > 0 } aid ? aid : null;
            var type = parseResult.GetValue(typeOption)?.Trim() is { Length: > 0 } tp ? tp : null;
            var hash = parseResult.GetValue(hashOption);
            var bindingPath = parseResult.GetValue(bindingPathOption)?.Trim() is { Length: > 0 } bp ? bp : null;
            var exists = parseResult.GetValue(existsOption);
            var property = parseResult.GetValue(propertyOption);
            var expected = parseResult.GetValue(expectedOption);
            var format = parseResult.GetValue(formatOption);
            var verbose = parseResult.GetValue(verboseOption);
            var service = new InjectionService(verbose);

            try
            {
                // Validate: need at least one search criterion
                if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(text)
                    && string.IsNullOrWhiteSpace(automationId) && string.IsNullOrWhiteSpace(bindingPath)
                    && (string.IsNullOrWhiteSpace(type) || !hash.HasValue))
                {
                    CommandHelpers.WriteError(
                        new { success = false, processId = pid, error = "At least --name, --text, --automationid, --binding-path, or both --type and --hash are required" },
                        format);
                    return ExitCodes.GeneralError;
                }

                // Validate: need at least one assertion mode
                if (!exists && string.IsNullOrWhiteSpace(text) && string.IsNullOrWhiteSpace(property))
                {
                    CommandHelpers.WriteError(
                        new { success = false, processId = pid, error = "At least one assertion (--exists, --text, or --property with --expected) is required" },
                        format);
                    return ExitCodes.GeneralError;
                }

                // Validate: --exists and --property are mutually exclusive
                if (exists && !string.IsNullOrWhiteSpace(property))
                {
                    CommandHelpers.WriteError(
                        new { success = false, processId = pid, error = "--exists and --property are mutually exclusive. Use --exists to check element presence, or --property with --expected to check a DataContext value." },
                        format);
                    return ExitCodes.GeneralError;
                }

                // Mode 1: --exists assertion
                if (exists)
                {
                    return await AssertExists(service, pid, name, text, automationId, type, bindingPath, format);
                }

                // Mode 2: --text assertion (assert element text equals expected)
                if (!string.IsNullOrWhiteSpace(text) && string.IsNullOrWhiteSpace(property))
                {
                    return await AssertText(service, pid, name, text, automationId, type, bindingPath, format);
                }

                // Mode 3: --property + --expected assertion (assert DataContext property value)
                if (!string.IsNullOrWhiteSpace(property) && expected is not null)
                {
                    return await AssertProperty(service, pid, name, text, automationId, type, hash, bindingPath, property, expected, format);
                }

                CommandHelpers.WriteError(
                    new { success = false, processId = pid, error = "--property requires --expected" },
                    format);
                return ExitCodes.GeneralError;
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

    private static async Task<int> AssertExists(
        InjectionService service, int pid, string? name, string? text,
        string? automationId, string? type, string? bindingPath, string? format)
    {
        var findResult = await service.FindElementAsync(pid, name, text, automationId, type, bindingPath);

        if (!findResult.Success)
        {
            CommandHelpers.WriteError(
                new { success = false, processId = pid, error = findResult.Error ?? findResult.Message },
                format);
            return MapFailureExitCode(findResult.Error, findResult.Message);
        }

        var passed = findResult.MatchCount > 0;
        var result = new AssertResult
        {
            Success = true,
            ProcessId = pid,
            Passed = passed,
            Assertion = "exists",
            Actual = findResult.MatchCount.ToString(),
            Message = passed
                ? $"Element found ({findResult.MatchCount} match(es))"
                : "Element not found"
        };

        CommandHelpers.WriteResult(result, format);
        return passed ? ExitCodes.Success : ExitCodes.GeneralError;
    }

    private static async Task<int> AssertText(
        InjectionService service, int pid, string? name, string text,
        string? automationId, string? type, string? bindingPath, string? format)
    {
        var findResult = await service.FindElementAsync(pid, name, text, automationId, type, bindingPath);

        if (!findResult.Success)
        {
            CommandHelpers.WriteError(
                new { success = false, processId = pid, error = findResult.Error ?? findResult.Message },
                format);
            return MapFailureExitCode(findResult.Error, findResult.Message);
        }

        string? actualContent = null;
        var passed = false;
        if (findResult.Elements.Count > 0)
        {
            foreach (var element in findResult.Elements)
            {
                if (string.Equals(element.Content, text, StringComparison.Ordinal))
                {
                    actualContent = element.Content;
                    passed = true;
                    break;
                }
            }
            actualContent ??= findResult.Elements[0].Content;
        }

        var result = new AssertResult
        {
            Success = true,
            ProcessId = pid,
            Passed = passed,
            Assertion = "text",
            Expected = text,
            Actual = actualContent,
            Message = passed
                ? $"Element found with exact matching text"
                : $"No element found with text exactly \"{text}\""
        };

        CommandHelpers.WriteResult(result, format);
        return passed ? ExitCodes.Success : ExitCodes.GeneralError;
    }

    private static async Task<int> AssertProperty(
        InjectionService service, int pid, string? name, string? text,
        string? automationId, string? type, int? hash, string? bindingPath,
        string property, string expected, string? format)
    {
        // First find the element
        string elementType;
        int elementHash;

        if (!string.IsNullOrWhiteSpace(type) && hash.HasValue)
        {
            elementType = type;
            elementHash = hash.Value;
        }
        else
        {
            var findResult = await service.FindElementAsync(pid, name, text, automationId, type, bindingPath);
            if (!findResult.Success)
            {
                CommandHelpers.WriteError(
                    new { success = false, processId = pid, error = findResult.Error ?? findResult.Message },
                    format);
                return MapFailureExitCode(findResult.Error, findResult.Message);
            }
            if (findResult.MatchCount == 0)
            {
                var notFoundResult = new AssertResult
                {
                    Success = true,
                    ProcessId = pid,
                    Passed = false,
                    Assertion = "property",
                    Expected = expected,
                    Message = "Element not found for property assertion"
                };
                CommandHelpers.WriteResult(notFoundResult, format);
                return ExitCodes.GeneralError;
            }
            if (findResult.MatchCount > 1)
            {
                CommandHelpers.WriteError(
                    new { success = false, processId = pid, error = $"Multiple elements matched ({findResult.MatchCount}). Refine selector with --name/--automationid/--type+--hash." },
                    format);
                return ExitCodes.GeneralError;
            }
            elementType = findResult.Elements[0].Type;
            elementHash = findResult.Elements[0].Hashcode;
        }

        // Get DataContext and check property
        var dcResult = await service.GetDataContextAsync(pid, elementType, elementHash, property);

        if (!dcResult.Success)
        {
            CommandHelpers.WriteError(
                new { success = false, processId = pid, error = dcResult.Error ?? dcResult.Message },
                format);
            return MapFailureExitCode(dcResult.Error, dcResult.Message);
        }

        if (!dcResult.HasDataContext || dcResult.DataContext == null)
        {
            var noDcResult = new AssertResult
            {
                Success = true,
                ProcessId = pid,
                Passed = false,
                Assertion = "property",
                Expected = expected,
                Message = "Element has no DataContext"
            };
            CommandHelpers.WriteResult(noDcResult, format);
            return ExitCodes.GeneralError;
        }

        // Extract property value from DataContext JSON
        string? actualValue = null;

        if (dcResult.DataContext is JsonElement dataContextElement
            && dataContextElement.ValueKind == JsonValueKind.Object
            && dataContextElement.TryGetProperty("properties", out var props)
            && props.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in props.EnumerateObject())
            {
                if (prop.Name.Equals(property, StringComparison.OrdinalIgnoreCase))
                {
                    if (prop.Value.ValueKind == JsonValueKind.Object
                        && prop.Value.TryGetProperty("value", out var valueProp))
                    {
                        actualValue = valueProp.ValueKind == JsonValueKind.String
                            ? valueProp.GetString()
                            : valueProp.GetRawText();
                    }
                    break;
                }
            }
        }

        var passed = string.Equals(actualValue, expected, StringComparison.OrdinalIgnoreCase);
        var result = new AssertResult
        {
            Success = true,
            ProcessId = pid,
            Passed = passed,
            Assertion = "property",
            Expected = expected,
            Actual = actualValue,
            Message = passed
                ? $"Property \"{property}\" equals \"{expected}\""
                : $"Property \"{property}\" expected \"{expected}\" but got \"{actualValue}\""
        };

        CommandHelpers.WriteResult(result, format);
        return passed ? ExitCodes.Success : ExitCodes.GeneralError;
    }

    private static int MapFailureExitCode(string? error, string? message)
    {
        if (string.Equals(error, ErrorMessages.ProcessNotFound, StringComparison.Ordinal))
            return ExitCodes.ProcessNotFound;

        var merged = $"{error} {message}";
        if (merged.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
            merged.Contains("timed out", StringComparison.OrdinalIgnoreCase))
            return ExitCodes.Timeout;

        return ExitCodes.InjectionFailed;
    }
}
