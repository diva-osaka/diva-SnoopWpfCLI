using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace SnoopWpfCLI.Tests.Integration;

[Collection("TestApp")]
public class ScreenshotIntegrationTests
{
    private readonly TestAppFixture _fixture;

    public ScreenshotIntegrationTests(TestAppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Screenshot_ReturnsBase64()
    {
        var (stdout, stderr, exitCode) = await _fixture.RunCliAsync(
            $"screenshot --pid {_fixture.TestAppPid}");

        Assert.True(exitCode == 0, $"screenshot failed: exitCode={exitCode}\nstdout: {stdout}\nstderr: {stderr}");

        using var doc = JsonDocument.Parse(stdout);
        var root = doc.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("imageData").GetString()),
            "imageData should not be empty");
        Assert.True(root.GetProperty("width").GetInt32() > 0);
        Assert.True(root.GetProperty("height").GetInt32() > 0);
    }

    [Fact]
    public async Task Screenshot_SaveToFile()
    {
        var tmpFile = Path.Combine(Path.GetTempPath(), $"snoopwpfcli_test_{Guid.NewGuid()}.png");

        try
        {
            var (stdout, stderr, exitCode) = await _fixture.RunCliAsync(
                $"screenshot --pid {_fixture.TestAppPid} --output \"{tmpFile}\"");

            Assert.True(exitCode == 0, $"screenshot failed: exitCode={exitCode}\nstdout: {stdout}\nstderr: {stderr}");

            using var doc = JsonDocument.Parse(stdout);
            var root = doc.RootElement;

            Assert.True(root.GetProperty("success").GetBoolean());

            // Verify file exists
            Assert.True(File.Exists(tmpFile), $"Screenshot file should exist at {tmpFile}");

            // Verify PNG header (first 8 bytes: 137 80 78 71 13 10 26 10)
            var header = new byte[8];
            using (var fs = File.OpenRead(tmpFile))
            {
                await fs.ReadExactlyAsync(header, 0, 8);
            }
            Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, header);
        }
        finally
        {
            if (File.Exists(tmpFile))
                File.Delete(tmpFile);
        }
    }
}
