using System.Text.Json.Serialization;

namespace GenMatrix.Models;

internal sealed record Matrix<T>
{
    [JsonPropertyName("include")]
    public required IEnumerable<T> Include { get; init; }
}
