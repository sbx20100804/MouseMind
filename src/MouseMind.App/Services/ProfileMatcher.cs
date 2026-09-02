using MouseMind.App.Models;

namespace MouseMind.App.Services;

public sealed class ProfileMatcher
{
    private static readonly char[] Separators = ['/', ',', ';', '|'];

    public MouseProfile? Find(IEnumerable<MouseProfile> profiles, string processName)
    {
        var normalizedProcess = NormalizeProcessName(processName);
        return profiles.FirstOrDefault(profile =>
            profile.IsEnabled && Matches(profile.ProcessName, normalizedProcess));
    }

    internal static bool Matches(string expression, string normalizedProcess)
    {
        if (string.IsNullOrWhiteSpace(expression)) return false;

        return expression
            .Split(Separators, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(NormalizeProcessName)
            .Any(candidate => candidate == "*" ||
                candidate.Equals(normalizedProcess, StringComparison.OrdinalIgnoreCase));
    }

    internal static string NormalizeProcessName(string value)
    {
        var trimmed = value.Trim();
        return trimmed.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? trimmed[..^4]
            : trimmed;
    }
}

