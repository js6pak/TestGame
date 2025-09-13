namespace GenMatrix.Models.Unity;

internal enum BuildTarget
{
    // 2017.3+
    StandaloneOSX,

    // <2017.3
    StandaloneOSXUniversal,
    StandaloneOSXIntel,
    StandaloneOSXIntel64,

    StandaloneWindows,
    Android,
    StandaloneLinux,
    StandaloneWindows64,
    StandaloneLinux64,
}
