using System.Text.Json.Serialization;

namespace SnoopWpfCLI.Models;

public class AssertResult
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("processId")]
    public int ProcessId { get; set; }

    [JsonPropertyName("passed")]
    public bool Passed { get; set; }

    [JsonPropertyName("assertion")]
    public string Assertion { get; set; } = string.Empty;

    [JsonPropertyName("actual")]
    public string? Actual { get; set; }

    [JsonPropertyName("expected")]
    public string? Expected { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
