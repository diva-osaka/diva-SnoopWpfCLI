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
            Description = "Expected value for --property or --text assertion"
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
        command.Options.Add(existsOption);
        command.Options.Add(propertyOption);
        command.Options.Add(expectedOption);
        command.Options.Add(formatOption);
        command.Options.Add(verboseOption);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var pid = parseResult.GetValue(pidOption);
            var name = parseResult.GetValue(nameOption);
            var text = parseResult.GetValue(textOption);
            var automationId = parseResult.GetValue(automationIdOption);
            var type = parseResult.GetValue(typeOption);
            var hash = parseResult.GetValue(hashOption);
            var exists = parseResult.GetValue(existsOption);
            var property = parseResult.GetValue(propertyOption);
            var expected = parseResult.GetValue(expectedOption);
            var format = parseResult.GetValue(formatOption);
            var verbose = parseResult.GetValue(verboseOption);
            var service = new InjectionService(verbose);

            try
            {
                // Validate: need at least one search criterion
                if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(automationId)
                    && (string.IsNullOrEmpty(type) || !hash.HasValue))
                {
                    CommandHelpers.WriteError(
                        new { success = false, processId = pid, error = "At least --name, --automationid, or both --type and --hash are required" },
                        format);
                    return ExitCodes.GeneralError;
                }

                // Validate: need at least one assertion mode
                if (!exists && string.IsNullOrEmpty(text) && string.IsNullOrEmpty(property))
                {
                    CommandHelpers.WriteError(
                        new { success = false, processId = pid, error = "At least one assertion (--exists, --text, or --property with --expected) is required" },
                        format);
                    return ExitCodes.GeneralError;
                }

                // Mode 1: --exists assertion
                if (exists)
                {
                    return await AssertExists(service, pid, name, text, automationId, type, format);
                }

                // Mode 2: --text assertion (assert element text equals expected)
                if (!string.IsNullOrEmpty(text) && string.IsNullOrEmpty(property))
                {
                    return await AssertText(service, pid, name, text, automationId, type, format);
                }

                // Mode 3: --property + --expected assertion (assert DataContext property value)
                if (!string.IsNullOrEmpty(property) && !string.IsNullOrEmpty(expected))
                {
                    return await AssertProperty(service, pid, name, automationId, type, hash, property, expected, format);
                }

                CommandHelpers.WriteError(
                    new { success = false, processId = pid, error = "--property requires --expected, --text can be used alone or with --name" },
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
        string? automationId, string? type, string? format)
    {
        var findResult = await service.FindElementAsync(pid, name, null, automationId, type);

        var passed = findResult.Success && findResult.MatchCount > 0;
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
        string? automationId, string? type, string? format)
    {
        var findResult = await service.FindElementAsync(pid, name, text, automationId, type);

        var passed = findResult.Success && findResult.MatchCount > 0;
        var actualContent = passed && findResult.Elements.Count > 0
            ? findResult.Elements[0].Content
            : null;

        var result = new AssertResult
        {
            Success = true,
            ProcessId = pid,
            Passed = passed,
            Assertion = "text",
            Expected = text,
            Actual = actualContent,
            Message = passed
                ? $"Element found with matching text"
                : $"No element found with text containing \"{text}\""
        };

        CommandHelpers.WriteResult(result, format);
        return passed ? ExitCodes.Success : ExitCodes.GeneralError;
    }

    private static async Task<int> AssertProperty(
        InjectionService service, int pid, string? name,
        string? automationId, string? type, int? hash,
        string property, string expected, string? format)
    {
        // First find the element
        string elementType;
        int elementHash;

        if (!string.IsNullOrEmpty(type) && hash.HasValue)
        {
            elementType = type;
            elementHash = hash.Value;
        }
        else
        {
            var findResult = await service.FindElementAsync(pid, name, null, automationId, type);
            if (!findResult.Success || findResult.MatchCount == 0)
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
            elementType = findResult.Elements[0].Type;
            elementHash = findResult.Elements[0].Hashcode;
        }

        // Get DataContext and check property
        var dcResult = await service.GetDataContextAsync(pid, elementType, elementHash, property);

        if (!dcResult.Success || !dcResult.HasDataContext || dcResult.DataContext == null)
        {
            var noDcResult = new AssertResult
            {
                Success = true,
                ProcessId = pid,
                Passed = false,
                Assertion = "property",
                Expected = expected,
                Message = dcResult.HasDataContext ? "DataContext has no matching property" : "Element has no DataContext"
            };
            CommandHelpers.WriteResult(noDcResult, format);
            return ExitCodes.GeneralError;
        }

        // Extract property value from DataContext JSON
        var dcJson = JsonSerializer.Serialize(dcResult.DataContext);
        using var dcDoc = JsonDocument.Parse(dcJson);
        string? actualValue = null;

        if (dcDoc.RootElement.TryGetProperty("properties", out var props) && props.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in props.EnumerateObject())
            {
                if (prop.Name.Equals(property, StringComparison.OrdinalIgnoreCase))
                {
                    if (prop.Value.TryGetProperty("value", out var valueProp))
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
}
