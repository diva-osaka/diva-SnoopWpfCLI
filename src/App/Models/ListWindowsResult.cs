using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SnoopWpfCLI.Models;

public class ListWindowsResult
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("processId")]
    public int ProcessId { get; set; }

    [JsonPropertyName("windowCount")]
    public int WindowCount { get; set; }

    [JsonPropertyName("windows")]
    public List<WindowInfo> Windows { get; set; } = new();

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
