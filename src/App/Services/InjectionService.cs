using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using SnoopWpfCLI.Models;

namespace SnoopWpfCLI.Services;

public class InjectionService
{
    private readonly bool _verbose;
    private readonly Dictionary<int, DateTime> _injectedProcesses = new();

    public InjectionService(bool verbose = false)
    {
        _verbose = verbose;
    }

    private static JsonSerializerOptions GetJsonOptions()
    {
        return new JsonSerializerOptions
        {
            WriteIndented = false,
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
        };
    }

    public async Task<InjectionResult> PingAsync(int processId)
    {
        Log($"Starting injection and ping for process {processId}");

        try
        {
            var process = GetProcess(processId);
            if (process == null)
            {
                return new InjectionResult
                {
                    Success = false,
                    ProcessId = processId,
                    Message = $"Process with ID {processId} not found",
                    Error = ErrorMessages.ProcessNotFound
                };
            }

            Log($"Target process: {process.ProcessName} (PID: {process.Id})");

            var alreadyInjected = await IsAlreadyInjectedAsync(processId);
            if (alreadyInjected)
            {
                Log($"Process {processId} already has WpfInspector injected, attempting direct ping");

                var directPingResponse = await SendPingAsync(processId);
                if (directPingResponse != null)
                {
                    return new InjectionResult
                    {
                        Success = true,
                        ProcessId = processId,
                        Message = "WpfInspector was already injected",
                        Response = directPingResponse,
                        WasAlreadyInjected = true
                    };
                }
                else
                {
                    Log($"Process {processId} was marked as injected but ping failed, re-injecting");
                    _injectedProcesses.Remove(processId);
                }
            }

            Log($"Injecting WpfInspector into process {processId}");
            var injectionSuccess = await InjectWpfInspectorAsync(processId);

            if (!injectionSuccess)
            {
                return new InjectionResult
                {
                    Success = false,
                    ProcessId = processId,
                    Message = "Failed to inject WpfInspector",
                    Error = "Injection failed"
                };
            }

            Log($"WpfInspector injected successfully into process {processId}");
            _injectedProcesses[processId] = DateTime.UtcNow;

            Log("Waiting for pipe server to start...");
            await Task.Delay(2000);

            Log($"Sending ping to process {processId}");
            var response = await SendPingAsync(processId);

            if (response != null)
            {
                return new InjectionResult
                {
                    Success = true,
                    ProcessId = processId,
                    Message = "WpfInspector injected and ping successful",
                    Response = response,
                    WasAlreadyInjected = false
                };
            }
            else
            {
                return new InjectionResult
                {
                    Success = false,
                    ProcessId = processId,
                    Message = "WpfInspector injected but ping failed",
                    Error = "Ping timeout or failed"
                };
            }
        }
        catch (Exception ex)
        {
            Log($"Error during injection and ping for process {processId}: {ex.Message}");
            return new InjectionResult
            {
                Success = false,
                ProcessId = processId,
                Message = "Exception occurred during injection",
                Error = ex.Message
            };
        }
    }

    private async Task<bool> IsAlreadyInjectedAsync(int processId)
    {
        if (_injectedProcesses.ContainsKey(processId))
        {
            var response = await SendPingAsync(processId, TimeSpan.FromSeconds(2));
            if (response != null)
            {
                return true;
            }
            else
            {
                _injectedProcesses.Remove(processId);
            }
        }

        return false;
    }

    public async Task<AutomationPeerResult> InvokeAutomationPeerAsync(int processId, string type, int hashcode, string action, string? parameters = null)
    {
        Log($"Starting automation peer action for process {processId}, element type: '{type}', hashcode: {hashcode}, action: '{action}'");

        try
        {
            var process = GetProcess(processId);
            if (process == null)
            {
                return new AutomationPeerResult
                {
                    Success = false,
                    ProcessId = processId,
                    Type = type,
                    Hashcode = hashcode,
                    Action = action,
                    Message = $"Process with ID {processId} not found",
                    Error = ErrorMessages.ProcessNotFound
                };
            }

            Log($"Target process: {process.ProcessName} (PID: {process.Id})");

            var alreadyInjected = await IsAlreadyInjectedAsync(processId);

            if (!alreadyInjected)
            {
                Log($"Injecting WpfInspector into process {processId}");
                var injectionSuccess = await InjectWpfInspectorAsync(processId);

                if (!injectionSuccess)
                {
                    return new AutomationPeerResult
                    {
                        Success = false,
                        ProcessId = processId,
                        Type = type,
                        Hashcode = hashcode,
                        Action = action,
                        Message = "Failed to inject WpfInspector",
                        Error = "Injection failed"
                    };
                }

                Log($"Successfully injected WpfInspector into process {processId}");
            }

            var commandData = new
            {
                commandType = "INVOKE_AUTOMATION_PEER",
                type = type,
                hashcode = hashcode,
                action = action
            };

            object finalCommandData;
            if (!string.IsNullOrWhiteSpace(parameters))
            {
                try
                {
                    using var paramsDoc = JsonDocument.Parse(parameters);
                    var paramsDict = new Dictionary<string, object>();

                    paramsDict["commandType"] = "INVOKE_AUTOMATION_PEER";
                    paramsDict["type"] = type;
                    paramsDict["hashcode"] = hashcode;
                    paramsDict["action"] = action;

                    foreach (var prop in paramsDoc.RootElement.EnumerateObject())
                    {
                        paramsDict[prop.Name] = prop.Value.GetRawText().Trim('"');
                    }

                    finalCommandData = paramsDict;
                }
                catch (JsonException)
                {
                    finalCommandData = commandData;
                }
            }
            else
            {
                finalCommandData = commandData;
            }

            Log($"Executing automation peer action on process {processId}");
            var response = await SendRunCommandAsync(processId, finalCommandData);

            if (string.IsNullOrWhiteSpace(response))
            {
                return new AutomationPeerResult
                {
                    Success = false,
                    ProcessId = processId,
                    Type = type,
                    Hashcode = hashcode,
                    Action = action,
                    Message = "No response received from WpfInspector",
                    Error = "Communication timeout or failure"
                };
            }

            using var doc = JsonDocument.Parse(response);
            var root = doc.RootElement;

            var success = root.TryGetProperty("success", out var successElement) ? successElement.GetBoolean() : false;
            var message = root.TryGetProperty("message", out var messageElement) ? messageElement.GetString() ?? "" : "";
            var error = root.TryGetProperty("error", out var errorElement) ? errorElement.GetString() : null;
            var result = root.TryGetProperty("result", out var resultElement) ? resultElement.GetRawText() : null;

            return new AutomationPeerResult
            {
                Success = success,
                ProcessId = processId,
                Type = type,
                Hashcode = hashcode,
                Action = action,
                Message = success ? (message ?? string.Empty) : (error ?? "Unknown error"),
                Error = success ? null : (error ?? "Unknown error"),
                Result = result
            };
        }
        catch (Exception ex)
        {
            Log($"Error during automation peer action for process {processId}: {ex.Message}");

            return new AutomationPeerResult
            {
                Success = false,
                ProcessId = processId,
                Type = type,
                Hashcode = hashcode,
                Action = action,
                Message = "Exception occurred during automation peer action",
                Error = ex.Message
            };
        }
    }

    public async Task<ScreenshotResult> TakeScreenshotAsync(int processId)
    {
        Log($"Starting screenshot capture for process {processId}");

        try
        {
            var process = GetProcess(processId);
            if (process == null)
            {
                return new ScreenshotResult
                {
                    Success = false,
                    ProcessId = processId,
                    Message = $"Process with ID {processId} not found",
                    Error = ErrorMessages.ProcessNotFound
                };
            }

            Log($"Target process: {process.ProcessName} (PID: {process.Id})");

            var alreadyInjected = await IsAlreadyInjectedAsync(processId);

            if (!alreadyInjected)
            {
                Log($"Injecting WpfInspector into process {processId}");
                var injectionSuccess = await InjectWpfInspectorAsync(processId);

                if (!injectionSuccess)
                {
                    return new ScreenshotResult
                    {
                        Success = false,
                        ProcessId = processId,
                        Message = "Failed to inject WpfInspector",
                        Error = "Injection failed"
                    };
                }

                Log($"WpfInspector injected successfully into process {processId}");
                _injectedProcesses[processId] = DateTime.UtcNow;

                Log("Waiting for pipe server to start...");
                await Task.Delay(2000);
            }

            Log($"Sending screenshot command to process {processId}");
            var commandResult = await SendScreenshotCommandAsync(processId);

            bool success = false;
            string? message = null;
            string? error = null;
            string? windowTitle = null;
            int width = 0;
            int height = 0;
            string? imageData = null;
            string format = "PNG";

            if (commandResult != null)
            {
                var doc = JsonDocument.Parse(commandResult);
                var root = doc.RootElement;

                success = root.TryGetProperty("success", out var successElement) && successElement.GetBoolean();
                message = root.TryGetProperty("message", out var messageElement) ? messageElement.GetString() : null;
                error = root.TryGetProperty("error", out var errorElement) ? errorElement.GetString() : null;
                windowTitle = root.TryGetProperty("windowTitle", out var titleElement) ? titleElement.GetString() : null;

                if (root.TryGetProperty("width", out var widthElement))
                    width = widthElement.GetInt32();
                if (root.TryGetProperty("height", out var heightElement))
                    height = heightElement.GetInt32();

                imageData = root.TryGetProperty("imageData", out var imageElement) ? imageElement.GetString() : null;
                format = root.TryGetProperty("format", out var formatElement) ? formatElement.GetString() ?? "PNG" : "PNG";
            }

            return new ScreenshotResult
            {
                Success = success,
                ProcessId = processId,
                ProcessName = process.ProcessName,
                Message = success ? (message ?? string.Empty) : (error ?? "Unknown error"),
                Error = success ? null : (error ?? "Unknown error"),
                WindowTitle = windowTitle,
                Width = width,
                Height = height,
                ImageData = imageData,
                Format = format
            };
        }
        catch (Exception ex)
        {
            Log($"Error during screenshot capture for process {processId}: {ex.Message}");

            return new ScreenshotResult
            {
                Success = false,
                ProcessId = processId,
                Message = "Exception occurred during screenshot capture",
                Error = ex.Message
            };
        }
    }

    public async Task<VisualTreeResult> GetVisualTreeAsync(int processId)
    {
        Log($"Starting visual tree retrieval for process {processId}");

        try
        {
            var process = GetProcess(processId);
            if (process == null)
            {
                return new VisualTreeResult
                {
                    Success = false,
                    ProcessId = processId,
                    Message = $"Process with ID {processId} not found",
                    Error = ErrorMessages.ProcessNotFound
                };
            }

            Log($"Target process: {process.ProcessName} (PID: {process.Id})");

            var alreadyInjected = await IsAlreadyInjectedAsync(processId);

            if (!alreadyInjected)
            {
                Log($"WpfInspector not yet injected into process {processId}, injecting now...");

                var injectionResult = await PingAsync(processId);
                if (!injectionResult.Success)
                {
                    return new VisualTreeResult
                    {
                        Success = false,
                        ProcessId = processId,
                        Message = $"Failed to inject WpfInspector: {injectionResult.Error}",
                        Error = injectionResult.Error ?? "Injection failed"
                    };
                }
                Log($"Successfully injected WpfInspector into process {processId}");
            }
            else
            {
                Log($"WpfInspector already injected into process {processId}");
            }

            var response = await SendVisualTreeCommandAsync(processId);

            if (string.IsNullOrEmpty(response))
            {
                return new VisualTreeResult
                {
                    Success = false,
                    ProcessId = processId,
                    Message = "No response received from WpfInspector",
                    Error = "No response"
                };
            }

            Log($"Successfully retrieved visual tree for process {processId}");

            return new VisualTreeResult
            {
                Success = true,
                ProcessId = processId,
                Message = "Visual tree retrieved successfully",
                VisualTreeJson = response
            };
        }
        catch (Exception ex)
        {
            Log($"Error during visual tree retrieval for process {processId}: {ex.Message}");

            return new VisualTreeResult
            {
                Success = false,
                ProcessId = processId,
                Message = "Exception occurred during visual tree retrieval",
                Error = ex.Message
            };
        }
    }

    public async Task<ElementResult> GetElementByHashcodeAsync(int processId, string type, int hashcode)
    {
        Log($"Starting element retrieval for process {processId}, type: '{type}', hashcode: {hashcode}");

        try
        {
            var process = GetProcess(processId);
            if (process == null)
            {
                return new ElementResult
                {
                    Success = false,
                    ProcessId = processId,
                    Type = type,
                    Hashcode = hashcode,
                    Message = $"Process with ID {processId} not found",
                    Error = ErrorMessages.ProcessNotFound
                };
            }

            Log($"Target process: {process.ProcessName} (PID: {process.Id})");

            var alreadyInjected = await IsAlreadyInjectedAsync(processId);

            if (!alreadyInjected)
            {
                Log($"Injecting WpfInspector into process {processId}");
                var injectionSuccess = await InjectWpfInspectorAsync(processId);

                if (!injectionSuccess)
                {
                    return new ElementResult
                    {
                        Success = false,
                        ProcessId = processId,
                        Type = type,
                        Hashcode = hashcode,
                        Message = "Failed to inject WpfInspector",
                        Error = "Injection failed"
                    };
                }

                Log($"Successfully injected WpfInspector into process {processId}");
                _injectedProcesses[processId] = DateTime.UtcNow;

                Log("Waiting for pipe server to start...");
                await Task.Delay(2000);
            }

            var commandData = new
            {
                commandType = "GET_ELEMENT_BY_HASHCODE",
                type = type,
                hashcode = hashcode
            };

            Log($"Executing get element by hashcode on process {processId}");
            var response = await SendRunCommandAsync(processId, commandData);

            if (string.IsNullOrWhiteSpace(response))
            {
                return new ElementResult
                {
                    Success = false,
                    ProcessId = processId,
                    Type = type,
                    Hashcode = hashcode,
                    Message = "No response received from WpfInspector",
                    Error = "Communication timeout or failure"
                };
            }

            using var doc = JsonDocument.Parse(response);
            var root = doc.RootElement;

            var success = root.TryGetProperty("success", out var successElement) ? successElement.GetBoolean() : false;
            var message = root.TryGetProperty("message", out var messageElement) ? messageElement.GetString() ?? "" : "";
            var error = root.TryGetProperty("error", out var errorElement) ? errorElement.GetString() : null;
            var timestamp = root.TryGetProperty("timestamp", out var timestampElement) ? timestampElement.GetString() : null;

            object? element = null;
            if (root.TryGetProperty("element", out var elementElement) && elementElement.ValueKind != JsonValueKind.Null)
            {
                element = JsonSerializer.Deserialize<object>(elementElement.GetRawText());
            }

            object? dataContexts = null;
            if (root.TryGetProperty("dataContexts", out var dataContextsElement) && dataContextsElement.ValueKind != JsonValueKind.Null)
            {
                dataContexts = JsonSerializer.Deserialize<object>(dataContextsElement.GetRawText());
            }

            return new ElementResult
            {
                Success = success,
                ProcessId = processId,
                Type = type,
                Hashcode = hashcode,
                Message = success ? (message ?? string.Empty) : (error ?? "Unknown error"),
                Error = success ? null : (error ?? "Unknown error"),
                Element = element,
                DataContexts = dataContexts,
                Timestamp = timestamp
            };
        }
        catch (Exception ex)
        {
            Log($"Error during element retrieval for process {processId}: {ex.Message}");

            return new ElementResult
            {
                Success = false,
                ProcessId = processId,
                Type = type,
                Hashcode = hashcode,
                Message = "Exception occurred during element retrieval",
                Error = ex.Message
            };
        }
    }

    public async Task<VisualTreeResult> GetVisualTreeByHashcodeAsync(int processId, string type, int hashcode)
    {
        Log($"Starting visual subtree retrieval for process {processId}, type: '{type}', hashcode: {hashcode}");

        try
        {
            var process = GetProcess(processId);
            if (process == null)
            {
                return new VisualTreeResult
                {
                    Success = false,
                    ProcessId = processId,
                    Message = $"Process with ID {processId} not found",
                    Error = ErrorMessages.ProcessNotFound
                };
            }

            var alreadyInjected = await IsAlreadyInjectedAsync(processId);
            if (!alreadyInjected)
            {
                Log($"Injecting WpfInspector into process {processId} for subtree retrieval");
                var injectionSuccess = await InjectWpfInspectorAsync(processId);
                if (!injectionSuccess)
                {
                    return new VisualTreeResult
                    {
                        Success = false,
                        ProcessId = processId,
                        Message = "Failed to inject WpfInspector",
                        Error = "Injection failed"
                    };
                }
                _injectedProcesses[processId] = DateTime.UtcNow;
                await Task.Delay(2000);
            }

            Log($"Executing GET_VISUAL_TREE_BY_HASHCODE on process {processId}");
            var response = await SendVisualTreeByHashcodeCommandAsync(processId, type, hashcode);
            if (string.IsNullOrWhiteSpace(response))
            {
                return new VisualTreeResult
                {
                    Success = false,
                    ProcessId = processId,
                    Message = "No response received from WpfInspector",
                    Error = "Communication timeout or failure"
                };
            }

            using var doc = JsonDocument.Parse(response);
            var root = doc.RootElement;
            var success = root.TryGetProperty("success", out var successEl) && successEl.GetBoolean();
            var message = root.TryGetProperty("message", out var messageEl) ? messageEl.GetString() : null;
            var error = root.TryGetProperty("error", out var errorEl) ? errorEl.GetString() : null;

            return new VisualTreeResult
            {
                Success = success,
                ProcessId = processId,
                Message = success ? (message ?? "Subtree retrieved successfully") : (error ?? "Unknown error"),
                Error = success ? null : (error ?? "Unknown error"),
                VisualTreeJson = response
            };
        }
        catch (Exception ex)
        {
            Log($"Error during visual subtree retrieval for process {processId}: {ex.Message}");
            return new VisualTreeResult
            {
                Success = false,
                ProcessId = processId,
                Message = "Exception occurred during visual subtree retrieval",
                Error = ex.Message
            };
        }
    }

    private Process? GetProcess(int processId)
    {
        try
        {
            return Process.GetProcessById(processId);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private async Task<bool> InjectWpfInspectorAsync(int processId)
    {
        try
        {
            var currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var wpfInspectorPath = Path.Combine(currentDirectory!, "WpfInspector.dll");

            if (!File.Exists(wpfInspectorPath))
            {
                Log($"Could not find WpfInspector.dll at {wpfInspectorPath}");
                return false;
            }

            var injectorLauncherPath = Path.Combine(currentDirectory!, "Snoop.InjectorLauncher.x64.exe");

            if (!File.Exists(injectorLauncherPath))
            {
                Log($"Could not find Snoop.InjectorLauncher at {injectorLauncherPath}");
                return false;
            }

            var arguments = $"--targetPID {processId} " +
                          $"--assembly \"{wpfInspectorPath}\" " +
                          $"--className \"SnoopWpfCLI.WpfInspector.Inspector\" " +
                          $"--methodName \"Initialize\"";

            Log($"Executing: {injectorLauncherPath} {arguments}");

            var processStartInfo = new ProcessStartInfo
            {
                FileName = injectorLauncherPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var injectorProcess = Process.Start(processStartInfo);
            if (injectorProcess == null)
            {
                Log("Failed to start injector launcher process");
                return false;
            }

            var output = await injectorProcess.StandardOutput.ReadToEndAsync();
            var error = await injectorProcess.StandardError.ReadToEndAsync();

            await injectorProcess.WaitForExitAsync();

            if (!string.IsNullOrEmpty(output))
            {
                Log($"Injector output: {output}");
            }

            if (!string.IsNullOrEmpty(error))
            {
                Log($"Injector errors: {error}");
            }

            var success = injectorProcess.ExitCode == 0;
            Log($"Injection process completed with exit code: {injectorProcess.ExitCode}");

            return success;
        }
        catch (Exception ex)
        {
            Log($"Exception during injection: {ex.Message}");
            return false;
        }
    }

    private async Task<string?> SendPingAsync(int processId, TimeSpan? timeout = null)
    {
        var actualTimeout = timeout ?? TimeSpan.FromSeconds(10);
        var pipeName = $"WpfInspector_{processId}";

        try
        {
            using var cts = new CancellationTokenSource(actualTimeout);

            Log($"Connecting to named pipe: {pipeName}");

            using var pipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut);

            await pipeClient.ConnectAsync(cts.Token);
            Log("Connected to pipe successfully");

            var message = "PING";
            var messageBytes = Encoding.UTF8.GetBytes(message);
            await pipeClient.WriteAsync(messageBytes, 0, messageBytes.Length, cts.Token);
            await pipeClient.FlushAsync(cts.Token);

            Log($"Sent message: {message}");

            var buffer = new byte[1024];
            var bytesRead = await pipeClient.ReadAsync(buffer, 0, buffer.Length, cts.Token);

            var response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Log($"Received response: {response}");

            return response;
        }
        catch (TimeoutException)
        {
            Log($"Ping timeout after {actualTimeout.TotalSeconds} seconds");
            return null;
        }
        catch (OperationCanceledException)
        {
            Log($"Ping operation cancelled after {actualTimeout.TotalSeconds} seconds");
            return null;
        }
        catch (Exception ex)
        {
            Log($"Error during ping: {ex.Message}");
            return null;
        }
    }

    private async Task<string?> SendScreenshotCommandAsync(int processId, TimeSpan? timeout = null)
    {
        var actualTimeout = timeout ?? TimeSpan.FromSeconds(15);
        var pipeName = $"WpfInspector_{processId}";

        try
        {
            using var cts = new CancellationTokenSource(actualTimeout);

            Log($"Connecting to named pipe: {pipeName}");

            using var pipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut);

            await pipeClient.ConnectAsync(cts.Token);
            Log("Connected to pipe successfully");

            var command = new
            {
                command = "TAKE_SCREENSHOT"
            };

            var commandJson = JsonSerializer.Serialize(command, GetJsonOptions());
            var messageBytes = Encoding.UTF8.GetBytes(commandJson);
            await pipeClient.WriteAsync(messageBytes, 0, messageBytes.Length, cts.Token);
            await pipeClient.FlushAsync(cts.Token);

            Log($"Sent command: {commandJson}");

            var bufferSize = 1024 * 1024; // 1MB buffer for large screenshots
            var buffer = new byte[bufferSize];
            var totalBytesRead = 0;
            var allData = new List<byte>();

            while (pipeClient.IsConnected && !cts.Token.IsCancellationRequested)
            {
                var bytesRead = await pipeClient.ReadAsync(buffer, 0, buffer.Length, cts.Token);
                if (bytesRead == 0) break;

                allData.AddRange(buffer.Take(bytesRead));
                totalBytesRead += bytesRead;

                if (bytesRead < buffer.Length) break;
            }

            var response = Encoding.UTF8.GetString(allData.ToArray());
            Log($"Received response ({totalBytesRead} bytes)");

            return response;
        }
        catch (TimeoutException)
        {
            Log($"Screenshot command timeout after {actualTimeout.TotalSeconds} seconds");
            return null;
        }
        catch (OperationCanceledException)
        {
            Log($"Screenshot command cancelled after {actualTimeout.TotalSeconds} seconds");
            return null;
        }
        catch (Exception ex)
        {
            Log($"Error during screenshot command: {ex.Message}");
            return null;
        }
    }

    private async Task<string?> SendVisualTreeCommandAsync(int processId, TimeSpan? timeout = null)
    {
        var actualTimeout = timeout ?? TimeSpan.FromSeconds(15);
        var pipeName = $"WpfInspector_{processId}";

        try
        {
            using var cts = new CancellationTokenSource(actualTimeout);

            Log($"Connecting to named pipe: {pipeName}");

            using var pipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut);

            await pipeClient.ConnectAsync(cts.Token);
            Log("Connected to pipe successfully");

            var command = new
            {
                command = "GET_VISUAL_TREE"
            };

            var commandJson = JsonSerializer.Serialize(command, GetJsonOptions());
            var messageBytes = Encoding.UTF8.GetBytes(commandJson);
            await pipeClient.WriteAsync(messageBytes, 0, messageBytes.Length, cts.Token);
            await pipeClient.FlushAsync(cts.Token);

            Log($"Sent command: {commandJson}");

            var allData = new List<byte>();
            var buffer = new byte[8192];
            int totalBytesRead = 0;

            while (true)
            {
                var bytesRead = await pipeClient.ReadAsync(buffer, 0, buffer.Length, cts.Token);
                if (bytesRead == 0) break;

                totalBytesRead += bytesRead;
                for (int i = 0; i < bytesRead; i++)
                {
                    allData.Add(buffer[i]);
                }

                var currentData = Encoding.UTF8.GetString(allData.ToArray());
                if (IsCompleteJson(currentData))
                {
                    break;
                }

                if (totalBytesRead > 10 * 1024 * 1024) // 10MB limit
                {
                    Log("Visual tree response exceeded 10MB limit, stopping read");
                    break;
                }
            }

            var response = Encoding.UTF8.GetString(allData.ToArray());
            Log($"Received visual tree response ({totalBytesRead} bytes)");

            return response;
        }
        catch (TimeoutException)
        {
            Log($"Visual tree command timeout after {actualTimeout.TotalSeconds} seconds");
            return null;
        }
        catch (OperationCanceledException)
        {
            Log($"Visual tree command cancelled after {actualTimeout.TotalSeconds} seconds");
            return null;
        }
        catch (Exception ex)
        {
            Log($"Error during visual tree command: {ex.Message}");
            return null;
        }
    }

    private async Task<string?> SendVisualTreeByHashcodeCommandAsync(int processId, string type, int hashcode, TimeSpan? timeout = null)
    {
        var actualTimeout = timeout ?? TimeSpan.FromSeconds(15);
        var pipeName = $"WpfInspector_{processId}";

        try
        {
            using var cts = new CancellationTokenSource(actualTimeout);
            Log($"Connecting to named pipe for subtree: {pipeName}");
            using var pipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut);
            await pipeClient.ConnectAsync(cts.Token);
            Log("Connected to pipe successfully (subtree)");

            var commandObj = new
            {
                commandType = "GET_VISUAL_TREE_BY_HASHCODE",
                type,
                hashcode
            };
            var commandJson = JsonSerializer.Serialize(commandObj, GetJsonOptions());
            var bytes = Encoding.UTF8.GetBytes(commandJson);
            await pipeClient.WriteAsync(bytes, 0, bytes.Length, cts.Token);
            await pipeClient.FlushAsync(cts.Token);
            Log($"Sent subtree command: {commandJson}");

            var allData = new List<byte>();
            var buffer = new byte[8192];
            int totalBytesRead = 0;
            while (true)
            {
                var read = await pipeClient.ReadAsync(buffer, 0, buffer.Length, cts.Token);
                if (read == 0) break;
                totalBytesRead += read;
                for (int i = 0; i < read; i++) allData.Add(buffer[i]);

                var current = Encoding.UTF8.GetString(allData.ToArray());
                if (IsCompleteJson(current)) break;
                if (totalBytesRead > 10 * 1024 * 1024) // 10MB safety
                {
                    Log("Subtree response exceeded 10MB limit, stopping read");
                    break;
                }
            }

            var response = Encoding.UTF8.GetString(allData.ToArray());
            Log($"Received subtree response ({totalBytesRead} bytes)");
            return response;
        }
        catch (TimeoutException)
        {
            Log($"Subtree command timeout after {actualTimeout.TotalSeconds} seconds");
            return null;
        }
        catch (OperationCanceledException)
        {
            Log($"Subtree command cancelled after {actualTimeout.TotalSeconds} seconds");
            return null;
        }
        catch (Exception ex)
        {
            Log($"Error during subtree command: {ex.Message}");
            return null;
        }
    }

    private async Task<string?> SendRunCommandAsync(int processId, object commandData, TimeSpan? timeout = null)
    {
        var actualTimeout = timeout ?? TimeSpan.FromSeconds(15);
        var pipeName = $"WpfInspector_{processId}";

        try
        {
            using var cts = new CancellationTokenSource(actualTimeout);

            Log($"Connecting to named pipe: {pipeName}");

            using var pipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut);

            await pipeClient.ConnectAsync(cts.Token);
            Log("Connected to pipe successfully");

            var commandJson = JsonSerializer.Serialize(commandData, GetJsonOptions());
            var messageBytes = Encoding.UTF8.GetBytes(commandJson);
            await pipeClient.WriteAsync(messageBytes, 0, messageBytes.Length, cts.Token);
            await pipeClient.FlushAsync(cts.Token);

            Log($"Sent command: {commandJson}");

            var buffer = new byte[4096];
            var bytesRead = await pipeClient.ReadAsync(buffer, 0, buffer.Length, cts.Token);

            var response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Log($"Received response: {response}");

            return response;
        }
        catch (TimeoutException)
        {
            Log($"Run command timeout after {actualTimeout.TotalSeconds} seconds");
            return null;
        }
        catch (OperationCanceledException)
        {
            Log($"Run command cancelled after {actualTimeout.TotalSeconds} seconds");
            return null;
        }
        catch (Exception ex)
        {
            Log($"Error during run command: {ex.Message}");
            return null;
        }
    }

    private bool IsCompleteJson(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void Log(string message)
    {
        if (_verbose)
        {
            Console.Error.WriteLine(message);
        }
    }
}
