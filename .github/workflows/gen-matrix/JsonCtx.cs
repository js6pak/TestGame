using System.Text.Json.Serialization;
using GenMatrix.Models;

namespace GenMatrix;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    GenerationMode = JsonSourceGenerationMode.Default,
    UseStringEnumConverter = true
)]
[JsonSerializable(typeof(BuildJobData))]
[JsonSerializable(typeof(Matrix<Job>))]
internal sealed partial class JsonCtx : JsonSerializerContext;
