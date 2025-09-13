namespace GenMatrix.Models;

internal enum RunnerOperatingSystem
{
    Linux,
    Windows,
    MacOS,
}

internal static class RunnerOperatingSystemExtensions
{
    public static string GetImageLabel(this RunnerOperatingSystem operatingSystem)
    {
        return operatingSystem switch
        {
            RunnerOperatingSystem.Linux => "ubuntu-24.04",
            RunnerOperatingSystem.Windows => "windows-2025",
            RunnerOperatingSystem.MacOS => "macos-15",
            _ => throw new ArgumentOutOfRangeException(nameof(operatingSystem), operatingSystem, null),
        };
    }
}
