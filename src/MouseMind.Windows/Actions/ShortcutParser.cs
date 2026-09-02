namespace MouseMind.Windows.Actions;

public static class ShortcutParser
{
    private static readonly Dictionary<string, ushort> NamedKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        ["CTRL"] = 0x11, ["CONTROL"] = 0x11, ["SHIFT"] = 0x10,
        ["ALT"] = 0x12, ["WIN"] = 0x5B, ["WINDOWS"] = 0x5B,
        ["ENTER"] = 0x0D, ["RETURN"] = 0x0D, ["ESC"] = 0x1B,
        ["ESCAPE"] = 0x1B, ["TAB"] = 0x09, ["SPACE"] = 0x20,
        ["BACKSPACE"] = 0x08, ["DELETE"] = 0x2E, ["HOME"] = 0x24,
        ["END"] = 0x23, ["LEFT"] = 0x25, ["UP"] = 0x26,
        ["RIGHT"] = 0x27, ["DOWN"] = 0x28
    };

    public static bool TryParse(string shortcut, out IReadOnlyList<ushort> keys, out string error)
    {
        var parsed = new List<ushort>();
        keys = parsed;
        error = "";
        var tokens = shortcut.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0) { error = "快捷键内容为空。"; return false; }

        foreach (var token in tokens)
        {
            if (NamedKeys.TryGetValue(token, out var named)) { parsed.Add(named); continue; }
            if (token.Length == 1 && token[0] <= 127 && char.IsLetterOrDigit(token[0]))
            {
                parsed.Add(char.ToUpperInvariant(token[0]));
                continue;
            }
            if (token.Length is 2 or 3 && token.StartsWith("F", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(token[1..], out var functionKey) && functionKey is >= 1 and <= 24)
            {
                parsed.Add((ushort)(0x70 + functionKey - 1));
                continue;
            }
            error = $"不支持的快捷键：{token}";
            parsed.Clear();
            return false;
        }
        return true;
    }
}

