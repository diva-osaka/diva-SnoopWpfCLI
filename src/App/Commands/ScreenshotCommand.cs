using System;
using System.IO;
using System.CommandLine;
using System.Text.Json;
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

        var verboseOption = new Option<bool>("--verbose")
        {
            Description = "Enable verbose output"
        };

        var command = new Command("screenshot", "Take a WPF screenshot");
        command.Options.Add(pidOption);
        command.Options.Add(outputOption);
        command.Options.Add(verboseOption);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var pid = parseResult.GetValue(pidOption);
            var outputPath = parseResult.GetValue(outputOption);
            var verbose = parseResult.GetValue(verboseOption);
            var service = new InjectionService(verbose);

            try
            {
                var result = await service.TakeScreenshotAsync(pid);

                if (result.Success && !string.IsNullOrEmpty(outputPath) && !string.IsNullOrEmpty(result.ImageData))
                {
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

                    var options = new JsonSerializerOptions { WriteIndented = true };
                    Console.WriteLine(JsonSerializer.Serialize(fileResult, options));
                }
                else
                {
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    Console.WriteLine(JsonSerializer.Serialize(result, options));
                }

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
