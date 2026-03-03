using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SnoopWpfCLI.Tests.Integration;

public class TestAppFixture : IAsyncLifetime
{
    public Process? TestAppProcess { get; private set; }
    public int TestAppPid => TestAppProcess?.Id ?? 0;

    public async Task InitializeAsync()
    {
        var testAppPath = IntegrationTestBase.GetTestAppPath();

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

        // Pre-inject via ping with retry (injection may fail if WPF app not fully ready)
        int exitCode = -1;
        string stdout = "", stderr = "";
        for (int i = 0; i < 5; i++)
        {
            (stdout, stderr, exitCode) = await IntegrationTestBase.RunCliAsync(
                $"ping --pid {TestAppPid}", timeoutMs: 60000);
            if (exitCode == 0) break;
            await Task.Delay(2000);
        }

        if (exitCode != 0)
            throw new InvalidOperationException(
                $"Pre-injection ping failed with exit code {exitCode}.\nstdout: {stdout}\nstderr: {stderr}");
    }

    public async Task DisposeAsync()
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

    public Task<(string stdout, string stderr, int exitCode)> RunCliAsync(string arguments, int timeoutMs = 30000)
        => IntegrationTestBase.RunCliAsync(arguments, timeoutMs);
}
