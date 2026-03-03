using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SnoopWpfCLI.Models;

namespace SnoopWpfCLI.Services;

public class WpfProcessService
{
    private readonly bool _verbose;
    private static readonly string[] WpfAssemblies = {
        "PresentationFramework",
        "PresentationCore",
        "WindowsBase",
        "System.Windows.Presentation"
    };

    private static readonly Regex WindowClassNameRegex = new(@"^HwndWrapper\[.*;.*;.*\]$", RegexOptions.Compiled);
    private static readonly int CurrentProcessId = Process.GetCurrentProcess().Id;

    public WpfProcessService(bool verbose = false)
    {
        _verbose = verbose;
    }

    public async Task<List<WpfProcessInfo>> GetWpfProcessesAsync()
    {
        Log("Scanning for WPF processes...");
        var wpfProcesses = new List<WpfProcessInfo>();

        try
        {
            var allProcesses = Process.GetProcesses();
            var tasks = allProcesses.Select(async process =>
            {
                try
                {
                    if (IsWpfProcess(process))
                    {
                        var processInfo = await CreateProcessInfoAsync(process);
                        if (processInfo != null)
                        {
                            return processInfo;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log($"Error checking process {process.Id}: {ex.Message}");
                }
                finally
                {
                    process.Dispose();
                }
                return null;
            }).ToArray();

            var results = await Task.WhenAll(tasks);
            wpfProcesses.AddRange(results.Where(p => p != null)!);

            Log($"Found {wpfProcesses.Count} WPF processes");
        }
        catch (Exception ex)
        {
            Log($"Error scanning for WPF processes: {ex.Message}");
        }

        return wpfProcesses;
    }

    private bool IsWpfProcess(Process process)
    {
        try
        {
            if (process.HasExited)
                return false;

            if (process.Id <= 4 || string.IsNullOrEmpty(process.ProcessName))
                return false;

            if (process.Id == CurrentProcessId)
                return false;

            var isWpfByWindowClass = CheckForWpfWindowClasses(process);
            if (isWpfByWindowClass)
            {
                Log($"Process {process.Id} ({process.ProcessName}) identified as WPF by window class");
                return true;
            }

            var isWpfByModules = CheckForWpfGraphicsModules(process);
            if (isWpfByModules)
            {
                Log($"Process {process.Id} ({process.ProcessName}) identified as WPF by graphics modules");
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            Log($"Error checking if process {process.Id} is WPF: {ex.Message}");
            return false;
        }
    }

    private bool CheckForWpfWindowClasses(Process process)
    {
        try
        {
            if (process.MainWindowHandle != IntPtr.Zero)
            {
                var className = GetWindowClassName(process.MainWindowHandle);
                if (!string.IsNullOrEmpty(className) && WindowClassNameRegex.IsMatch(className))
                {
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Log($"Could not check window class for process {process.Id}: {ex.Message}");
        }

        return false;
    }

    private bool CheckForWpfGraphicsModules(Process process)
    {
        try
        {
            var modules = process.Modules;
            foreach (ProcessModule module in modules)
            {
                if (module.ModuleName.StartsWith("wpfgfx_", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                var moduleName = Path.GetFileNameWithoutExtension(module.ModuleName);
                if (WpfAssemblies.Any(wpfAssembly =>
                    string.Equals(moduleName, wpfAssembly, StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Log($"Could not access modules for process {process.Id}: {ex.Message}");
        }

        return false;
    }

    private string GetWindowClassName(IntPtr hwnd)
    {
        try
        {
            const int maxChars = 256;
            var stringBuilder = new StringBuilder(maxChars);
            if (GetClassName(hwnd, stringBuilder, maxChars) > 0)
            {
                return stringBuilder.ToString();
            }
        }
        catch (Exception ex)
        {
            Log($"Could not get window class name: {ex.Message}");
        }

        return string.Empty;
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    private async Task<WpfProcessInfo?> CreateProcessInfoAsync(Process process)
    {
        try
        {
            return await Task.Run(() => new WpfProcessInfo
            {
                ProcessId = process.Id,
                ProcessName = process.ProcessName,
                MainWindowTitle = process.MainWindowTitle ?? string.Empty,
                FileName = GetProcessFileName(process),
                WorkingDirectory = GetProcessWorkingDirectory(process),
                IsWpfApplication = true,
                HasMainWindow = process.MainWindowHandle != IntPtr.Zero,
                StartTime = process.StartTime
            });
        }
        catch (Exception ex)
        {
            Log($"Error creating process info for {process.Id}: {ex.Message}");
            return null;
        }
    }

    private string GetProcessFileName(Process process)
    {
        try
        {
            return process.MainModule?.FileName ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private string GetProcessWorkingDirectory(Process process)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {process.Id}");
            using var objects = searcher.Get();

            foreach (ManagementObject obj in objects)
            {
                var commandLine = obj["CommandLine"]?.ToString();
                if (!string.IsNullOrEmpty(commandLine))
                {
                    var parts = commandLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 0)
                    {
                        var exePath = parts[0].Trim('"');
                        return Path.GetDirectoryName(exePath) ?? string.Empty;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log($"Could not get working directory for process {process.Id}: {ex.Message}");
        }

        return string.Empty;
    }

    private void Log(string message)
    {
        if (_verbose)
        {
            Console.Error.WriteLine(message);
        }
    }
}
