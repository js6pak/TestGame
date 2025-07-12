using System.Text.Json.Serialization;

namespace GenMatrix.Models;

/// <summary>
/// Stupid trick to go over the matrix job count limit
/// </summary>
internal sealed record Job
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("strategy")]
    public required Strategy<BuildJobData> Strategy { get; init; }
}

internal sealed record Strategy<T>
{
    [JsonPropertyName("matrix")]
    public required Matrix<T> Matrix { get; init; }

    [JsonPropertyName("fail-fast")]
    public required bool FailFast { get; init; }

    [JsonPropertyName("max-parallel")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public required int? MaxParallel { get; init; }
}

internal sealed record Matrix<T>
{
    [JsonPropertyName("include")]
    public required IEnumerable<T> Include { get; init; }
}
