using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using AssetRipper.Primitives;
using GenMatrix;
using GenMatrix.Models;
using GenMatrix.Models.Unity;

if (args is ["gather-versions"])
{
    using var client = new HttpClient();
    var request = new HttpRequestMessage
    {
        Method = HttpMethod.Post,
        RequestUri = new Uri("https://services.unity.com/graphql"),
        Content = JsonContent.Create(new
        {
            query =
                """
                query GetVersions {
                	getUnityReleases(limit: 10000) {
                		totalCount
                		pageInfo {
                			hasNextPage
                		}
                		edges {
                			node {
                				version
                			}
                		}
                	}
                }
                """,
            operationName = "GetVersions",
        }),
    };

    using var response = await client.SendAsync(request);
    response.EnsureSuccessStatusCode();
    var body = await response.Content.ReadFromJsonAsync<JsonObject>();

    var versions =
        body!["data"]!["getUnityReleases"]!["edges"]!.AsArray()
            .Select(e => UnityVersion.Parse(e!["node"]!["version"]!.ToString()))
            .OrderDescending()
            .GroupBy(v => (v.Major, v.Minor));

    foreach (var group in versions)
    {
        var version = group.First();
        Console.WriteLine($"\"{version}\",");
    }

    return 0;
}

var jobs = new List<BuildJobData>();

var unityVersions = ((string[])
[
    "6000.5.0a3",
    "6000.4.0b2",
    "6000.3.2f1",
    "6000.2.15f1",
    "6000.1.17f1",
    "6000.0.64f1",
    "2023.3.0b10",
    "2023.2.22f1",
    "2023.1.22f1",
    "2022.3.62f3",
    "2022.2.23f1",
    "2022.1.25f1",
    "2021.3.45f2",
    "2021.2.20f1",
    "2021.1.29f1",
    "2020.3.49f1",
    "2020.2.8f1",
    "2020.1.18f1",
    "2019.4.41f2",
    "2019.3.17f1",
    "2019.2.23f1",
    "2019.1.15f1",
    "2018.4.36f1",
    "2018.3.14f1",
    "2018.2.21f1",
    "2018.1.9f2",
    "2017.4.40f1",
    "2017.3.1p4",
    "2017.2.5f1",
    "2017.1.5f1",
    "5.6.7f1",
    "5.5.6f1",
    "5.4.6f3",
    "5.3.8p2",
    "5.2.5f1",
    "5.1.5f1",
    "5.0.4f1",
    "4.7.2",
    "4.6.9",
    "4.5.5",
    // "4.3.4",
    // "4.2.1",
    // "4.1.5",
    // "4.0.1",
]).Select(UnityVersion.Parse);

static bool HasLinuxEditor(UnityVersion unityVersion)
{
    if (unityVersion >= new UnityVersion(2018, 2, 0, UnityVersionType.Beta, 9)) return true;
    if (unityVersion is { Major: 2018, Minor: 1 }) return unityVersion.Build >= 5;
    if (unityVersion is { Major: 2017, Minor: 4 }) return unityVersion.Build >= 6;
    return false;
}

var includes = new List<Regex>();
var excludes = new List<Regex>();

foreach (var target in args.SelectMany(a => a.Split(" ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)))
{
    if (target.StartsWith('!'))
    {
        var regex = Glob.ToRegex(target.AsSpan(1));
        Console.WriteLine("Excluding: " + regex);
        excludes.Add(regex);
    }
    else
    {
        var regex = Glob.ToRegex(target);
        Console.WriteLine("Including: " + regex);
        includes.Add(regex);
    }
}

if (includes.Count == 0)
{
    Console.WriteLine("No includes specified");
    return 1;
}

foreach (var unityVersion in unityVersions)
{
    var hasLinuxEditor = HasLinuxEditor(unityVersion);

    // Unity 2017.4-2018.4 lightmapping often hangs on Linux
    if (unityVersion.GreaterThanOrEquals(2017, 4) && unityVersion.LessThan(2019))
    {
        hasLinuxEditor = false;
    }

    var platforms = new List<Platform>
    {
        Platform.Windows,
        Platform.MacOS,
        Platform.Linux,
    };

    // .../macos_x64ARM64_player_nondevelopment_mono/UnityPlayer.app/Contents/Frameworks/libmonobdwgc-2.0.dylib could not be retrieved: No such file or directory
    if (unityVersion is { Major: 2021, Minor: 2 })
    {
        platforms.Remove(Platform.MacOS);
    }

    if (
        // There was no buildAndroidPlayer in Unity 4, and we can't build from scripts without Pro there
        unityVersion.Major > 4 &&
        // Unity versions before 5.5 used ancient `android` cli, too much pain to support
        unityVersion.GreaterThanOrEquals(5, 5) &&
        // Unity 2017.1 had broken PlayerSettings.bundleIdentifier
        unityVersion is not { Major: 2017, Minor: 1 } &&
        // Error: JDK not found
        unityVersion is not { Major: 2022, Minor: 1 }
    )
    {
        platforms.Add(Platform.Android);
    }

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
                if (unityVersion.GreaterThanOrEquals(2020, 2))
                {
                    architectures.Add(PlatformArchitecture.X64Arm64);
                }
                else if (unityVersion.GreaterThanOrEquals(2017, 3))
                {
                    architectures.Add(PlatformArchitecture.X64);
                }
                else
                {
                    architectures.Add(PlatformArchitecture.X64X86);
                }

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
                Platform.Android => unityVersion >= new UnityVersion(5, 2, 0, UnityVersionType.Beta, 1),
                Platform.Windows or Platform.MacOS => unityVersion >= new UnityVersion(2018, 1, 0, UnityVersionType.Beta, 2),
                Platform.Linux => unityVersion >= new UnityVersion(2019, 3, 0, UnityVersionType.Beta, 4),
                _ => throw new ArgumentOutOfRangeException()
            };

            // IL2CPP builds break on Unity <=2020 on modern MacOS
            // TODO find a workaround?
            if (platform == Platform.MacOS && (unityVersion.Major <= 2020 || unityVersion is { Major: <= 2023, Minor: < 3 } || unityVersion is { Major: 2023, Minor: 3 }))
            {
                supportsIl2Cpp = false;
            }

            // libil2cpp\os\Win32\Locale.cpp(40): error C2065: 'LC_ALL': undeclared identifier
            if (platform == Platform.Windows && unityVersion is { Major: 2018, Minor: 1 })
            {
                supportsIl2Cpp = false;
            }

            // ld: .../linux64_player_nondevelopment_il2cpp/baselib.a: error adding symbols: file format not recognized
            if (platform == Platform.Linux && unityVersion is { Major: 6000, Minor: 2 })
            {
                supportsIl2Cpp = false;
            }

            //  C++ code builder is unable to build C++ code for Linux: Could not find valid clang executable at clang
            if (platform == Platform.Linux && unityVersion is { Major: 2019, Minor: 3 })
            {
                supportsIl2Cpp = false;
            }

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
                        Platform.Linux or Platform.Android => hasLinuxEditor ? RunnerOperatingSystem.Linux : RunnerOperatingSystem.Windows,
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
                            if (platform == Platform.Linux) modules.AddRange("linux-mono", "linux");
                            if (platform == Platform.Windows) modules.AddRange("windows-mono", "windows");
                            if (platform == Platform.MacOS) modules.AddRange("mac-mono", "mac");
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
                    (Platform.MacOS, PlatformArchitecture.X64X86) => BuildTarget.StandaloneOSXUniversal,

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
                        PlatformArchitecture.X64Arm64 => OSArchitecture.x64ARM64,
                        PlatformArchitecture.X64X86 => (OSArchitecture)2,
                        _ => throw new ArgumentOutOfRangeException(),
                    }).ToString());
                }

                // Workaround BuildPipeline.BuildPlayer requiring Pro before Unity 5
                if (unityVersion.Major < 5)
                {
                    extraArgs.Add("-" + buildTarget switch
                    {
                        BuildTarget.StandaloneWindows => "buildWindowsPlayer",
                        BuildTarget.StandaloneWindows64 => "buildWindows64Player",
                        BuildTarget.StandaloneOSXUniversal => "buildOSXUniversalPlayer",
                        BuildTarget.StandaloneOSXIntel => "buildOSXPlayer",
                        BuildTarget.StandaloneOSXIntel64 => "buildOSX64Player",
                        BuildTarget.StandaloneLinux => "buildLinux32Player",
                        BuildTarget.StandaloneLinux64 => "buildLinux64Player",
                        BuildTarget.Android => throw new NotSupportedException(),
                        _ => throw new ArgumentOutOfRangeException(),
                    });
                    extraArgs.Add($"Builds/{buildTarget}/TestGame" + platform switch
                    {
                        Platform.Windows => ".exe",
                        Platform.MacOS => ".app",
                        Platform.Linux => "",
                        Platform.Android => ".apk",
                        _ => throw new ArgumentOutOfRangeException()
                    });
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
                             PlatformArchitecture.X64X86 => "x64x86",
                             PlatformArchitecture.X64Arm64 => "x64arm64",
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

                if (!includes.Any(t => t.IsMatch(id)))
                {
                    Console.WriteLine($"Not including '{id}', because it didn't match any of the includes");
                    continue;
                }

                if (excludes.Where(t => t.IsMatch(id)).ToArray() is { Length: > 0 } excludedBy)
                {
                    Console.WriteLine($"Not including '{id}', because it was excluded by: {string.Join(", ", excludedBy.Select(r => r.ToString()))}");
                    continue;
                }

                Console.WriteLine($"Including '{id}'");

                jobs.Add(new BuildJobData
                {
                    Name = id,
                    Runner = runner.GetImageLabel(),
                    NeedsAndroidSdk = needsAndroidSdk,
                    NeedsAndroidNdk = needsAndroidNdk,
                    UnityVersion = unityVersion.Major >= 5 ? unityVersion.ToString() : unityVersion.ToStringWithoutType(),
                    Modules = string.Join(' ', modules),
                    BuildTarget = buildTarget,
                    BuildTargetName = unityVersion.Major >= 5
                        ? buildTarget.ToString()
                        : buildTarget switch
                        {
                            BuildTarget.StandaloneOSX
                                or BuildTarget.StandaloneOSXUniversal
                                or BuildTarget.StandaloneOSXIntel
                                or BuildTarget.StandaloneOSXIntel64 => "osx",
                            BuildTarget.StandaloneWindows => "win64",
                            BuildTarget.StandaloneWindows64 => "win32",
                            BuildTarget.StandaloneLinux => "linux32",
                            BuildTarget.StandaloneLinux64 => "linux64",
                            BuildTarget.Android => "android",
                            _ => throw new ArgumentOutOfRangeException(),
                        },
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
    GitHubActions.SetOutput(Constants.JobsVariableName, wrapperJobs.Count == 0 ? "" : JsonSerializer.Serialize(matrices, JsonCtx.Default.MatrixJob));
}
else
{
    var jsonCtx = new JsonCtx(new JsonSerializerOptions(JsonCtx.Default.Options) { WriteIndented = true });
    Console.WriteLine(JsonSerializer.Serialize(matrices, jsonCtx.MatrixJob));

    if (jobs is [var singleJob])
    {
        Console.WriteLine("strategy=" + JsonSerializer.Serialize(new Strategy<BuildJobData>
        {
            Matrix = new Matrix<BuildJobData>
            {
                Include = [singleJob],
            },
            FailFast = false,
            MaxParallel = 1,
        }, JsonCtx.Default.StrategyBuildJobData));
    }
}

return 0;
