namespace GenMatrix;

internal static class GitHubActions
{
    public static bool IsRunning { get; } = string.Equals(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"), "true", StringComparison.OrdinalIgnoreCase);
    public static bool IsAct { get; } = string.Equals(Environment.GetEnvironmentVariable("ACT"), "true", StringComparison.OrdinalIgnoreCase);

    private static string? GitHubOutput { get; } = Environment.GetEnvironmentVariable("GITHUB_OUTPUT");

    public static void SetOutput(string name, string value)
    {
        ArgumentNullException.ThrowIfNull(GitHubOutput);

        IssueFileCommand(GitHubOutput, PrepareKeyValueMessage(name, value));
    }

    private static void IssueFileCommand(string filePath, string message)
    {
        File.AppendAllText(filePath, message + Environment.NewLine);
    }

    // https://github.com/actions/toolkit/blob/683703c1149439530dcee7b8c5dbbfeec4104368/packages/core/src/file-command.ts#L27-L47
    private static string PrepareKeyValueMessage(string key, string value)
    {
        var delimiter = $"ghadelimiter_{Guid.NewGuid()}";

        // These should realistically never happen, but just in case someone finds a
        // way to exploit uuid generation let's not allow keys or values that contain
        // the delimiter.
        if (key.Contains(delimiter))
        {
            throw new ArgumentException($"Unexpected input: name should not contain the delimiter \"{delimiter}\"");
        }

        if (value.Contains(delimiter))
        {
            throw new ArgumentException($"Unexpected input: value should not contain the delimiter \"{delimiter}\"");
        }

        return $"{key}<<{delimiter}{Environment.NewLine}{value}{Environment.NewLine}{delimiter}";
    }
}
