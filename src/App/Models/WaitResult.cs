using System.Text.Json.Serialization;

namespace SnoopWpfCLI.Models;

public class WaitResult
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("processId")]
    public int ProcessId { get; set; }

    [JsonPropertyName("condition")]
    public string Condition { get; set; } = "";

    [JsonPropertyName("message")]
    public string Message { get; set; } = "";

    [JsonPropertyName("elapsedMs")]
    public long ElapsedMs { get; set; }

    [JsonPropertyName("element")]
    public object? Element { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
