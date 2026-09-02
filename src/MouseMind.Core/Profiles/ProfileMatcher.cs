using MouseMind.Core.Models;

namespace MouseMind.Core.Profiles;

public sealed class ProfileMatcher
{
    private static readonly char[] Separators = ['/', ',', ';', '|'];

    public MouseProfile? Find(IEnumerable<MouseProfile> profiles, string processName)
    {
        var normalizedProcess = NormalizeProcessName(processName);
        return profiles
            .Where(profile => profile.IsEnabled && Matches(profile.ProcessName, normalizedProcess))
            .OrderByDescending(profile => IsExactMatch(profile.ProcessName, normalizedProcess))
            .ThenByDescending(profile => profile.Priority)
            .FirstOrDefault();
    }

    public static bool Matches(string expression, string processName)
    {
        if (string.IsNullOrWhiteSpace(expression)) return false;
        var normalizedProcess = NormalizeProcessName(processName);
        return expression
            .Split(Separators, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(NormalizeProcessName)
            .Any(candidate => candidate == "*" ||
                candidate.Equals(normalizedProcess, StringComparison.OrdinalIgnoreCase));
    }

    public static string NormalizeProcessName(string value)
    {
        var trimmed = value.Trim();
        return trimmed.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? trimmed[..^4]
            : trimmed;
    }

    private static bool IsExactMatch(string expression, string normalizedProcess) => expression
        .Split(Separators, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
        .Select(NormalizeProcessName)
        .Any(candidate => candidate != "*" &&
            candidate.Equals(normalizedProcess, StringComparison.OrdinalIgnoreCase));
}
