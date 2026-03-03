using System;
using System.CommandLine;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SnoopWpfCLI.Formatters;
using SnoopWpfCLI.Models;
using SnoopWpfCLI.Services;


namespace SnoopWpfCLI.Commands;

public static class WaitCommand
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
            Description = "Element name (x:Name) to wait for"
        };

        var textOption = new Option<string?>("--text")
        {
            Description = "Element text/content to wait for (partial match)"
        };

        var automationIdOption = new Option<string?>("--automationid")
        {
            Description = "AutomationId to wait for"
        };

        var typeOption = new Option<string?>("--type")
        {
            Description = "Element type name to filter by"
        };

        var untilOption = new Option<string>("--until")
        {
            Description = "Wait condition: found (default), gone, enabled, disabled",
            DefaultValueFactory = _ => "found"
        };
        untilOption.AcceptOnlyFromAmong("found", "gone", "enabled", "disabled");

        var timeoutOption = new Option<int>("--timeout")
        {
            Description = "Timeout in milliseconds (default: 30000)",
            DefaultValueFactory = _ => 30000
        };

        var intervalOption = new Option<int>("--interval")
        {
            Description = "Polling interval in milliseconds (default: 500)",
            DefaultValueFactory = _ => 500
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

        var interactiveOnlyOption = new Option<bool>("--interactive-only")
        {
            Description = "Filter results to interactive controls only (Button, TextBox, CheckBox, etc.)"
        };

        var command = new Command("wait", "Wait for an element to appear, disappear, or change state");
        command.Options.Add(pidOption);
        command.Options.Add(nameOption);
        command.Options.Add(textOption);
        command.Options.Add(automationIdOption);
        command.Options.Add(typeOption);
        command.Options.Add(untilOption);
        command.Options.Add(timeoutOption);
        command.Options.Add(intervalOption);
        command.Options.Add(formatOption);
        command.Options.Add(verboseOption);
        command.Options.Add(interactiveOnlyOption);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var pid = parseResult.GetValue(pidOption);
            var name = parseResult.GetValue(nameOption);
            var text = parseResult.GetValue(textOption);
            var automationId = parseResult.GetValue(automationIdOption);
            var type = parseResult.GetValue(typeOption);
            var until = parseResult.GetValue(untilOption) ?? "found";
            var timeout = parseResult.GetValue(timeoutOption);
            var interval = parseResult.GetValue(intervalOption);
            var format = parseResult.GetValue(formatOption);
            var verbose = parseResult.GetValue(verboseOption);
            var interactiveOnly = parseResult.GetValue(interactiveOnlyOption);
            var service = new InjectionService(verbose);

            var stopwatch = Stopwatch.StartNew();
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            try
            {
                while (true)
                {
                    linkedCts.Token.ThrowIfCancellationRequested();

                    var findResult = await service.FindElementAsync(pid, name, text, automationId, type);

                    if (interactiveOnly && findResult.Success && findResult.Elements != null)
                    {
                        findResult.Elements = findResult.Elements
                            .Where(e => WpfKnownTypes.InteractiveTypes.Any(t => e.Type != null && e.Type.EndsWith(t)))
                            .ToList();
                        findResult.MatchCount = findResult.Elements.Count;
                    }

                    if (verbose)
                    {
                        Console.Error.WriteLine($"[wait] Poll at {stopwatch.ElapsedMilliseconds}ms: found={findResult.Success}, matchCount={findResult.MatchCount}, condition={until}");
                    }

                    bool conditionMet = false;
                    object? matchedElement = null;

                    switch (until)
                    {
                        case "found":
                            if (findResult.Success && findResult.MatchCount > 0)
                            {
                                conditionMet = true;
                                matchedElement = findResult.Elements[0];
                            }
                            break;

                        case "gone":
                            if (!findResult.Success || findResult.MatchCount == 0)
                            {
                                conditionMet = true;
                            }
                            break;

                        case "enabled":
                        case "disabled":
                            if (findResult.Success && findResult.MatchCount > 0)
                            {
                                var element = findResult.Elements[0];
                                var elementDetail = await service.GetElementByHashcodeAsync(pid, element.Type, element.Hashcode);
                                if (elementDetail.Success && elementDetail.Element != null)
                                {
                                    var elemJson = JsonSerializer.Serialize(elementDetail.Element);
                                    using var elemDoc = JsonDocument.Parse(elemJson);

                                    // WPF default for IsEnabled is true.
                                    // DLL returns only non-default values, so absence means enabled.
                                    bool isEnabled = true;
                                    if (elemDoc.RootElement.TryGetProperty("IsEnabled", out var isEnabledProp))
                                    {
                                        isEnabled = isEnabledProp.ValueKind != JsonValueKind.False &&
                                                    !(isEnabledProp.ValueKind == JsonValueKind.String &&
                                                      isEnabledProp.GetString()?.Equals("false", StringComparison.OrdinalIgnoreCase) == true);
                                    }

                                    if ((until == "enabled" && isEnabled) || (until == "disabled" && !isEnabled))
                                    {
                                        conditionMet = true;
                                        matchedElement = element;
                                    }
                                }
                            }
                            break;
                    }

                    if (conditionMet)
                    {
                        stopwatch.Stop();
                        var result = new WaitResult
                        {
                            Success = true,
                            ProcessId = pid,
                            Condition = until,
                            Message = $"Element {until} within {stopwatch.ElapsedMilliseconds}ms",
                            ElapsedMs = stopwatch.ElapsedMilliseconds,
                            Element = matchedElement
                        };
                        OutputResult(result, format);
                        return ExitCodes.Success;
                    }

                    try
                    {
                        await Task.Delay(interval, linkedCts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        // Will be caught at top of loop or in outer catch
                    }
                }
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
            {
                stopwatch.Stop();
                var result = new WaitResult
                {
                    Success = false,
                    ProcessId = pid,
                    Condition = until,
                    Message = $"Timeout after {stopwatch.ElapsedMilliseconds}ms waiting for element",
                    ElapsedMs = stopwatch.ElapsedMilliseconds,
                    Error = "Timeout"
                };
                OutputResult(result, format);
                return ExitCodes.Timeout;
            }
            catch (Exception ex) when (ex is TimeoutException or TaskCanceledException)
            {
                stopwatch.Stop();
                var result = new WaitResult
                {
                    Success = false,
                    ProcessId = pid,
                    Condition = until,
                    Message = ex.Message,
                    ElapsedMs = stopwatch.ElapsedMilliseconds,
                    Error = "Timeout"
                };
                OutputResult(result, format);
                return ExitCodes.Timeout;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                var result = new WaitResult
                {
                    Success = false,
                    ProcessId = pid,
                    Condition = until,
                    Message = ex.Message,
                    ElapsedMs = stopwatch.ElapsedMilliseconds,
                    Error = ex.Message
                };
                OutputResult(result, format);
                return ExitCodes.GeneralError;
            }
        });

        return command;
    }

    private static void OutputResult(WaitResult result, string? format)
    {
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
    }
}
