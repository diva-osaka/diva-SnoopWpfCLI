using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace SnoopWpfCLI.Tests.Integration;

public class InvokeIntegrationTests : IntegrationTestBase, IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        await StartTestAppAsync();

        // Pre-inject via ping with retry (injection may fail if WPF app not fully ready)
        int exitCode = -1;
        string stdout = "", stderr = "";
        for (int i = 0; i < 3; i++)
        {
            (stdout, stderr, exitCode) = await RunCliAsync($"ping --pid {TestAppPid}", timeoutMs: 60000);
            if (exitCode == 0) break;
            await Task.Delay(2000);
        }
        Assert.True(exitCode == 0, $"Pre-injection ping failed: exitCode={exitCode}\nstdout: {stdout}\nstderr: {stderr}");
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await CleanupAsync();
    }

    [Fact]
    public async Task InvokeClick_ChangesState()
    {
        // Click the CountButton
        var (stdout, stderr, exitCode) = await RunCliAsync(
            $"invoke --pid {TestAppPid} --name CountButton --action Invoke_Invoke");

        Assert.True(exitCode == 0, $"invoke failed: exitCode={exitCode}\nstdout: {stdout}\nstderr: {stderr}");

        using var doc = JsonDocument.Parse(stdout);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());

        // Verify the click count changed via assert
        var (assertStdout, assertStderr, assertExitCode) = await RunCliAsync(
            $"assert --pid {TestAppPid} --name CountButton --property ClickCount --expected \"1\"");

        Assert.True(assertExitCode == 0,
            $"assert failed: exitCode={assertExitCode}\nstdout: {assertStdout}\nstderr: {assertStderr}");

        using var assertDoc = JsonDocument.Parse(assertStdout);
        Assert.True(assertDoc.RootElement.GetProperty("passed").GetBoolean(),
            "ClickCount should be 1 after one click");
    }

    [Fact]
    public async Task InvokeSetValue_ChangesText()
    {
        var newValue = "Hello E2E";

        // Set value on InputTextBox
        var (stdout, stderr, exitCode) = await RunCliAsync(
            $"invoke --pid {TestAppPid} --name InputTextBox --action Value_Set --params \"{{\\\"value\\\":\\\"{newValue}\\\"}}\"");

        Assert.True(exitCode == 0, $"invoke failed: exitCode={exitCode}\nstdout: {stdout}\nstderr: {stderr}");

        using var doc = JsonDocument.Parse(stdout);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());

        // Verify the text changed via assert
        var (assertStdout, assertStderr, assertExitCode) = await RunCliAsync(
            $"assert --pid {TestAppPid} --name InputTextBox --property InputText --expected \"{newValue}\"");

        Assert.True(assertExitCode == 0,
            $"assert failed: exitCode={assertExitCode}\nstdout: {assertStdout}\nstderr: {assertStderr}");

        using var assertDoc = JsonDocument.Parse(assertStdout);
        Assert.True(assertDoc.RootElement.GetProperty("passed").GetBoolean(),
            $"InputText should be '{newValue}' after Value_Set");
    }
}
