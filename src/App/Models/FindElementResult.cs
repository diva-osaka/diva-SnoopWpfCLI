using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SnoopWpfCLI.Models;

public class FindElementResult
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("processId")]
    public int ProcessId { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("matchCount")]
    public int MatchCount { get; set; }

    [JsonPropertyName("elements")]
    public List<FoundElement> Elements { get; set; } = new();

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

public class FoundElement
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("hashcode")]
    public int Hashcode { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("automationId")]
    public string? AutomationId { get; set; }
}
