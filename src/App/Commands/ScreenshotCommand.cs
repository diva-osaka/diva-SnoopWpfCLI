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

        var windowOption = new Option<int?>("--window")
        {
            Description = "Window index (use list-windows to find indices)"
        };

        var command = new Command("screenshot", "Take a WPF screenshot");
        command.Options.Add(pidOption);
        command.Options.Add(outputOption);
        command.Options.Add(formatOption);
        command.Options.Add(verboseOption);
        command.Options.Add(windowOption);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var pid = parseResult.GetValue(pidOption);
            var outputPath = parseResult.GetValue(outputOption);
            var format = parseResult.GetValue(formatOption);
            var verbose = parseResult.GetValue(verboseOption);
            var windowIndex = parseResult.GetValue(windowOption);
            var service = new InjectionService(verbose);

            try
            {
                var result = await service.TakeScreenshotAsync(pid, windowIndex);

                if (result.Success && !string.IsNullOrEmpty(outputPath))
                {
                    if (string.IsNullOrEmpty(result.ImageData))
                    {
                        CommandHelpers.WriteError(new { success = false, processId = pid, error = "Screenshot succeeded but image data is empty" }, format);
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

                    CommandHelpers.WriteResult(fileResult, format);
                }
                else
                {
                    CommandHelpers.WriteResult(result, format);
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
