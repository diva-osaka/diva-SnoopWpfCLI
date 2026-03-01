using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace SnoopWpfCLI.Tests.Integration;

public class IntegrationTestBase : IAsyncDisposable
{
    protected Process? TestAppProcess { get; private set; }
    protected int TestAppPid => TestAppProcess?.Id ?? 0;

    protected static string GetTestAppPath()
    {
        var testAssemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        // Navigate from src/App.Tests/bin/Debug/net10.0-windows/ to tests/TestApp/bin/Debug/net10.0-windows/
        var solutionRoot = Path.GetFullPath(Path.Combine(testAssemblyDir, "..", "..", "..", "..", ".."));
        var testAppPath = Path.Combine(solutionRoot, "tests", "TestApp", "bin", "Debug", "net10.0-windows", "TestApp.exe");

        if (!File.Exists(testAppPath))
        {
            // Try alternative path for dotnet run output
            testAppPath = Path.Combine(solutionRoot, "tests", "TestApp", "bin", "Debug", "net10.0-windows", "TestApp.dll");
        }

        return testAppPath;
    }

    protected static string GetCliPath()
    {
        var testAssemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var solutionRoot = Path.GetFullPath(Path.Combine(testAssemblyDir, "..", "..", "..", "..", ".."));
        return Path.Combine(solutionRoot, "src", "App", "bin", "Debug", "net10.0-windows", "snoopwpfcli.exe");
    }

    protected async Task<Process> StartTestAppAsync()
    {
        var testAppPath = GetTestAppPath();

        ProcessStartInfo psi;
        if (testAppPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"\"{testAppPath}\"",
                UseShellExecute = false,
                CreateNoWindow = false
            };
        }
        else
        {
            psi = new ProcessStartInfo
            {
                FileName = testAppPath,
                UseShellExecute = false,
                CreateNoWindow = false
            };
        }

        var process = Process.Start(psi);
        if (process == null)
            throw new InvalidOperationException("Failed to start TestApp");

        TestAppProcess = process;

        // Wait for the process to initialize and create its window
        await Task.Delay(3000);

        if (process.HasExited)
            throw new InvalidOperationException($"TestApp exited immediately with code {process.ExitCode}");

        return process;
    }

    protected async Task<(string stdout, string stderr, int exitCode)> RunCliAsync(string arguments, int timeoutMs = 30000)
    {
        var cliPath = GetCliPath();

        var psi = new ProcessStartInfo
        {
            FileName = cliPath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        if (process == null)
            throw new InvalidOperationException("Failed to start CLI");

        using var cts = new CancellationTokenSource(timeoutMs);

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cts.Token);
        var stderrTask = process.StandardError.ReadToEndAsync(cts.Token);

        await process.WaitForExitAsync(cts.Token);

        return (await stdoutTask, await stderrTask, process.ExitCode);
    }

    public async Task CleanupAsync()
    {
        if (TestAppProcess != null && !TestAppProcess.HasExited)
        {
            try
            {
                TestAppProcess.Kill(entireProcessTree: true);
                await TestAppProcess.WaitForExitAsync();
            }
            catch
            {
                // Best effort cleanup
            }
            finally
            {
                TestAppProcess.Dispose();
                TestAppProcess = null;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await CleanupAsync();
    }
}
