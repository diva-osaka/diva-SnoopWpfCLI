using System;
using System.CommandLine;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using SnoopWpfCLI.Formatters;
using SnoopWpfCLI.Services;

namespace SnoopWpfCLI.Commands;

public static class FindElementCommand
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
            Description = "Element name (x:Name) to search for"
        };

        var textOption = new Option<string?>("--text")
        {
            Description = "Element text/content to search for (partial match)"
        };

        var automationIdOption = new Option<string?>("--automationid")
        {
            Description = "AutomationId to search for"
        };

        var typeOption = new Option<string?>("--type")
        {
            Description = "Element type name to filter by"
        };

        var bindingPathOption = new Option<string?>("--binding-path")
        {
            Description = "Find elements with a binding to this property path"
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

        var command = new Command("find-element", "Find elements by name, text, or AutomationId");
        command.Options.Add(pidOption);
        command.Options.Add(nameOption);
        command.Options.Add(textOption);
        command.Options.Add(automationIdOption);
        command.Options.Add(typeOption);
        command.Options.Add(bindingPathOption);
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
            var bindingPath = parseResult.GetValue(bindingPathOption);
            var format = parseResult.GetValue(formatOption);
            var verbose = parseResult.GetValue(verboseOption);
            var interactiveOnly = parseResult.GetValue(interactiveOnlyOption);
            var service = new InjectionService(verbose);

            try
            {
                if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(text)
                    && string.IsNullOrEmpty(automationId) && string.IsNullOrEmpty(type)
                    && string.IsNullOrEmpty(bindingPath))
                {
                    CommandHelpers.WriteError(
                        new { success = false, processId = pid, error = "At least one search criterion (--name, --text, --automationid, --type, or --binding-path) is required" },
                        format);
                    return ExitCodes.GeneralError;
                }

                var result = await service.FindElementAsync(pid, name, text, automationId, type, bindingPath);

                if (interactiveOnly && result.Success && result.Elements != null)
                {
                    var interactiveTypes = new[]
                    {
                        "Button", "TextBox", "CheckBox", "ComboBox", "ListBox",
                        "RadioButton", "Slider", "ToggleButton", "PasswordBox",
                        "RichTextBox", "DatePicker", "Expander", "MenuItem",
                        "TabItem", "TreeViewItem", "ListBoxItem", "ComboBoxItem"
                    };
                    result.Elements = result.Elements
                        .Where(e => interactiveTypes.Any(t => e.Type != null && e.Type.EndsWith(t)))
                        .ToList();
                    result.MatchCount = result.Elements.Count;
                }

                if (format == "tree")
                {
                    var jsonStr = JsonSerializer.Serialize(result);
                    using var doc = JsonDocument.Parse(jsonStr);
                    Console.WriteLine(TreeFormatter.FormatFindElementResult(doc.RootElement));
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
