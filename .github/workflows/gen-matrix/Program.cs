using System.Diagnostics;
using System.Text.Json;
using AssetRipper.Primitives;
using GenMatrix;
using GenMatrix.Models;
using GenMatrix.Models.Unity;

var jobs = new List<BuildJobData>();

var unityVersions = ((string[])
[
    "6000.1.11f1",
    // "6000.0.53f1",
    "2023.2.20f1",
    // "2023.1.20f1",
    "2022.3.62f1",
    // "2022.2.21f1",
    // "2022.1.24f1",
    "2021.3.45f1",
    // "2021.2.19f1",
    // "2021.1.28f1",
    "2020.3.48f1",
    // "2020.2.7f1",
    // "2020.1.17f1",
    "2019.4.40f1",
    // "2019.3.15f1",
    // "2019.2.21f1",
    // "2019.1.14f1",
    "2018.4.36f1",
    // "2018.3.14f1",
    // "2018.2.21f1",
    // "2018.1.9f2",
    "2017.4.40f1",
    // "2017.3.1f1",
    // "2017.2.5f1",
    // "2017.1.5f1",
    "5.6.7f1",
]).Select(UnityVersion.Parse);

static bool HasLinuxEditor(UnityVersion unityVersion)
{
    if (unityVersion >= new UnityVersion(2018, 2, 0, UnityVersionType.Beta, 9)) return true;
    if (unityVersion is { Major: 2018, Minor: 1 }) return unityVersion.Build >= 5;
    if (unityVersion is { Major: 2017, Minor: 4 }) return unityVersion.Build >= 6;
    return false;
}


foreach (var unityVersion in unityVersions)
{
    var hasLinuxEditor = HasLinuxEditor(unityVersion);

    var platforms = new[]
    {
        // Platform.Windows,
        // Platform.MacOS,
        // Platform.Linux,
        Platform.Android,
    };

    foreach (var platform in platforms)
    {
        var architectures = new List<PlatformArchitecture>();
        switch (platform)
        {
            case Platform.Windows:
                architectures.AddRange(PlatformArchitecture.X64, PlatformArchitecture.X86);
                if (unityVersion.GreaterThanOrEquals(2023, 1))
                {
                    architectures.Add(PlatformArchitecture.Arm64);
                }

                break;
            case Platform.MacOS:
                architectures.Add(PlatformArchitecture.Universal);
                break;
            case Platform.Linux:
                architectures.Add(PlatformArchitecture.X64);
                if (unityVersion.LessThan(2019, 2))
                {
                    architectures.Add(PlatformArchitecture.X86);
                }

                break;
            case Platform.Android:
                architectures.Add(PlatformArchitecture.Universal);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        foreach (var architecture in architectures)
        {
            var supportsIl2Cpp = platform switch
            {
                Platform.Android => true,
                Platform.Windows or Platform.MacOS => unityVersion >= new UnityVersion(2018, 1, 0, UnityVersionType.Beta, 2),
                Platform.Linux => unityVersion >= new UnityVersion(2019, 3, 0, UnityVersionType.Beta, 4),
                _ => throw new ArgumentOutOfRangeException()
            };

            ReadOnlySpan<ScriptingImplementation> scriptingImplementations = supportsIl2Cpp
                ? [ScriptingImplementation.Mono2x, ScriptingImplementation.IL2CPP]
                : [ScriptingImplementation.Mono2x];

            foreach (var scriptingImplementation in scriptingImplementations)
            {
                var runner = scriptingImplementation switch
                {
                    ScriptingImplementation.Mono2x => hasLinuxEditor ? RunnerOperatingSystem.Linux : RunnerOperatingSystem.Windows,
                    ScriptingImplementation.IL2CPP => platform switch
                    {
                        Platform.Linux => hasLinuxEditor ? RunnerOperatingSystem.Linux : throw new UnreachableException(),
                        Platform.Android => hasLinuxEditor ? RunnerOperatingSystem.Linux : RunnerOperatingSystem.Windows,
                        Platform.Windows => RunnerOperatingSystem.Windows,
                        Platform.MacOS => RunnerOperatingSystem.MacOS,
                        _ => throw new ArgumentOutOfRangeException(),
                    },
                    _ => throw new ArgumentOutOfRangeException(),
                };

                if (GitHubActions.IsAct && runner != RunnerOperatingSystem.Linux)
                {
                    continue;
                }

                var modules = new List<string>();
                if (platform == Platform.Android)
                {
                    modules.Add("android");
                }
                else
                {
                    switch (scriptingImplementation)
                    {
                        case ScriptingImplementation.IL2CPP:
                            modules.Add(platform switch
                            {
                                Platform.Linux => "linux-il2cpp",
                                Platform.Windows => "windows-il2cpp",
                                Platform.MacOS => "mac-il2cpp",
                                _ => throw new ArgumentOutOfRangeException(),
                            });
                            break;

                        case ScriptingImplementation.Mono2x:
                        {
                            if (runner != RunnerOperatingSystem.Linux && platform == Platform.Linux) modules.AddRange("linux-mono", "linux");
                            if (runner != RunnerOperatingSystem.Windows && platform == Platform.Windows) modules.AddRange("windows-mono", "windows");
                            if (runner != RunnerOperatingSystem.MacOS && platform == Platform.MacOS) modules.AddRange("mac-mono", "mac");
                            break;
                        }
                    }
                }

                var buildTarget = (platform, architecture) switch
                {
                    (Platform.Windows, PlatformArchitecture.X86) => BuildTarget.StandaloneWindows,
                    (Platform.Windows, PlatformArchitecture.X64 or PlatformArchitecture.Arm64) => BuildTarget.StandaloneWindows64,

                    (Platform.Linux, PlatformArchitecture.X86) => BuildTarget.StandaloneLinux,
                    (Platform.Linux, PlatformArchitecture.X64) => BuildTarget.StandaloneLinux64,

                    (Platform.MacOS, _) when unityVersion.GreaterThanOrEquals(2017, 3) => BuildTarget.StandaloneOSX,
                    (Platform.MacOS, PlatformArchitecture.X86) => BuildTarget.StandaloneOSXIntel,
                    (Platform.MacOS, PlatformArchitecture.X64) => BuildTarget.StandaloneOSXIntel64,
                    (Platform.MacOS, PlatformArchitecture.Universal) => BuildTarget.StandaloneOSXUniversal,

                    (Platform.Android, _) => BuildTarget.Android,

                    _ => throw new ArgumentOutOfRangeException(),
                };

                var extraArgs = new List<string>();
                if (platform == Platform.Android)
                {
                    extraArgs.AddRange("-targetArchitectures", scriptingImplementation == ScriptingImplementation.Mono2x ? "ARMv7" : "All");
                }
                else
                {
                    extraArgs.AddRange("-arch", (architecture switch
                    {
                        PlatformArchitecture.X64 => OSArchitecture.x64,
                        PlatformArchitecture.X86 => OSArchitecture.x86,
                        PlatformArchitecture.Arm => throw new NotSupportedException(),
                        PlatformArchitecture.Arm64 => OSArchitecture.ARM64,
                        PlatformArchitecture.Universal => OSArchitecture.x64ARM64,
                        _ => throw new ArgumentOutOfRangeException(),
                    }).ToString());
                }


                var id = $"{unityVersion}-" +
                         $"{platform switch
                         {
                             Platform.Windows => "win",
                             Platform.MacOS => "osx",
                             Platform.Linux => "linux",
                             Platform.Android => "android",
                             _ => throw new ArgumentOutOfRangeException(),
                         }}-" +
                         $"{architecture switch
                         {
                             PlatformArchitecture.X64 => "x64",
                             PlatformArchitecture.X86 => "x86",
                             PlatformArchitecture.Arm => "arm",
                             PlatformArchitecture.Arm64 => "arm64",
                             PlatformArchitecture.Universal => "universal",
                             _ => throw new ArgumentOutOfRangeException(),
                         }}-" +
                         scriptingImplementation switch
                         {
                             ScriptingImplementation.Mono2x => "mono",
                             ScriptingImplementation.IL2CPP => "il2cpp",
                             _ => throw new ArgumentOutOfRangeException(),
                         };

                var needsAndroidSdk = platform == Platform.Android && unityVersion.Major < 2019;
                var needsAndroidNdk = needsAndroidSdk && scriptingImplementation == ScriptingImplementation.IL2CPP
                    ? unityVersion.GreaterThanOrEquals(2018, 3)
                        ? "r16b"
                        : unityVersion.GreaterThanOrEquals(2017)
                            ? "r13b"
                            : "r10e"
                    : "";

                jobs.Add(new BuildJobData
                {
                    Name = id,
                    Runner = runner.GetImageLabel(),
                    NeedsAndroidSdk = needsAndroidSdk,
                    NeedsAndroidNdk = needsAndroidNdk,
                    UnityVersion = unityVersion,
                    Modules = string.Join(' ', modules),
                    BuildTarget = buildTarget,
                    ScriptingImplementation = scriptingImplementation,
                    ExtraArgs = string.Join(' ', extraArgs),
                });
            }
        }
    }
}

Console.WriteLine($"{jobs.Count} jobs");

var macJobs = jobs.Where(x => x.Runner.StartsWith("macos-")).ToList();
jobs.RemoveAll(x => macJobs.Contains(x));

var chunks = jobs.Chunk(Constants.MaxJobCountPerMatrix).ToArray();

var wrapperJobs = chunks.Index().Select(x => new Job
{
    Name = chunks.Length == 1 ? "Build" : $"Build {x.Index + 1}/{chunks.Length}",
    Strategy = new Strategy<BuildJobData>
    {
        Matrix = new Matrix<BuildJobData>
        {
            Include = x.Item,
        },
        FailFast = false,
        MaxParallel = null,
    },
}).ToList();

if (macJobs.Count != 0)
{
    if (macJobs.Count > Constants.MaxJobCountPerMatrix) throw new NotImplementedException();
    wrapperJobs.Add(new Job
    {
        Name = "Build (MacOS)",
        Strategy = new Strategy<BuildJobData>
        {
            Matrix = new Matrix<BuildJobData>
            {
                Include = macJobs,
            },
            FailFast = false,
            MaxParallel = 1,
        },
    });
}

var matrices = new Matrix<Job>
{
    Include = wrapperJobs,
};

if (GitHubActions.IsRunning)
{
    GitHubActions.SetOutput(Constants.JobsVariableName, JsonSerializer.Serialize(matrices, JsonCtx.Default.MatrixJob));
}
else
{
    var jsonCtx = new JsonCtx(new JsonSerializerOptions(JsonCtx.Default.Options) { WriteIndented = true });
    Console.WriteLine(JsonSerializer.Serialize(matrices, jsonCtx.MatrixJob));
}
