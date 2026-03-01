using System.Text.Json.Serialization;

namespace SnoopWpfCLI.Models;

public class DataContextResult
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("processId")]
    public int ProcessId { get; set; }

    [JsonPropertyName("elementType")]
    public string ElementType { get; set; } = string.Empty;

    [JsonPropertyName("elementHashcode")]
    public int ElementHashcode { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("hasDataContext")]
    public bool HasDataContext { get; set; }

    [JsonPropertyName("dataContext")]
    public object? DataContext { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
