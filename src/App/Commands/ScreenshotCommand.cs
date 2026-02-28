using System;
using System.IO;
using System.CommandLine;
using System.Text.Json;
using System.Threading.Tasks;
using SnoopWpfCLI.Formatters;
using SnoopWpfCLI.Services;

namespace SnoopWpfCLI.Commands;

public static class ScreenshotCommand
{
    public static Command Create()
    {
        var pidOption = new Option<int>("--pid")
        {
            Description = "Target process ID",
            Required = true
        };

        var outputOption = new Option<string?>("--output")
        {
            Description = "Output file path (PNG). If omitted, outputs base64 JSON."
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

        var command = new Command("screenshot", "Take a WPF screenshot");
        command.Options.Add(pidOption);
        command.Options.Add(outputOption);
        command.Options.Add(formatOption);
        command.Options.Add(verboseOption);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var pid = parseResult.GetValue(pidOption);
            var outputPath = parseResult.GetValue(outputOption);
            var format = parseResult.GetValue(formatOption);
            var verbose = parseResult.GetValue(verboseOption);
            var service = new InjectionService(verbose);

            try
            {
                var result = await service.TakeScreenshotAsync(pid);

                if (result.Success && !string.IsNullOrEmpty(outputPath))
                {
                    if (string.IsNullOrEmpty(result.ImageData))
                    {
                        var emptyError = new { success = false, processId = pid, error = "Screenshot succeeded but image data is empty" };
                        if (format == "tree")
                        {
                            var jsonStr = JsonSerializer.Serialize(emptyError);
                            using var doc = JsonDocument.Parse(jsonStr);
                            Console.Error.WriteLine(TreeFormatter.FormatGenericResult(doc.RootElement));
                        }
                        else
                        {
                            Console.Error.WriteLine(JsonSerializer.Serialize(emptyError));
                        }
                        return ExitCodes.GeneralError;
                    }

                    var imageBytes = Convert.FromBase64String(result.ImageData);
                    await File.WriteAllBytesAsync(outputPath, imageBytes, cancellationToken);

                    var fileResult = new
                    {
                        success = true,
                        processId = result.ProcessId,
                        processName = result.ProcessName,
                        message = $"Screenshot saved to {outputPath}",
                        windowTitle = result.WindowTitle,
                        width = result.Width,
                        height = result.Height,
                        filePath = Path.GetFullPath(outputPath),
                        format = result.Format
                    };

                    if (format == "tree")
                    {
                        var jsonStr = JsonSerializer.Serialize(fileResult);
                        using var doc = JsonDocument.Parse(jsonStr);
                        Console.WriteLine(TreeFormatter.FormatGenericResult(doc.RootElement));
                    }
                    else
                    {
                        var options = new JsonSerializerOptions { WriteIndented = true };
                        Console.WriteLine(JsonSerializer.Serialize(fileResult, options));
                    }
                }
                else
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
