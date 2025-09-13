using AssetRipper.Primitives;
using GenMatrix.Models.Unity;

namespace GenMatrix.Models;

internal sealed record BuildJobData
{
    public required string Name { get; init; }
    public required string Runner { get; init; }

    public required bool NeedsAndroidSdk { get; init; }
    public required string NeedsAndroidNdk { get; init; }

    public required string UnityVersion { get; init; }
    public required string Modules { get; init; }
    public required BuildTarget BuildTarget { get; init; }
    public required string BuildTargetName { get; init; }
    public required ScriptingImplementation ScriptingImplementation { get; init; }

    public required string ExtraArgs { get; init; }
}
