using AssetRipper.Primitives;
using GenMatrix.Models.Unity;

namespace GenMatrix.Models;

internal sealed record Job
{
    public required string Title { get; init; }
    public required string Runner { get; init; }

    public required UnityVersion UnityVersion { get; init; }
    public required string Modules { get; init; }
    public required BuildTarget BuildTarget { get; init; }
    public required ScriptingImplementation ScriptingImplementation { get; init; }

    public required string ExtraArgs { get; init; }
}
