using System.Text.Json.Serialization;

namespace GenMatrix.Models;

/// <summary>
/// Stupid trick to go over the matrix job count limit
/// </summary>
internal sealed record MatrixWrapper
{
    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("matrix")]
    public required Matrix<Job> Matrix { get; init; }
}
