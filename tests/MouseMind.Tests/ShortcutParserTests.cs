using MouseMind.Windows.Actions;

namespace MouseMind.Tests;

public sealed class ShortcutParserTests
{
    [Theory]
    [InlineData("Ctrl+Shift+P", 3)]
    [InlineData("Control + Return", 2)]
    [InlineData("Windows+F24", 2)]
    [InlineData("alt+1", 2)]
    public void TryParse_AcceptsSupportedShortcuts(string shortcut, int expectedCount)
    {
        Assert.True(ShortcutParser.TryParse(shortcut, out var keys, out var error), error);
        Assert.Equal(expectedCount, keys.Count);
    }

    [Theory]
    [InlineData("")]
    [InlineData("F0")]
    [InlineData("F25")]
    [InlineData("Ctrl+Mouse1")]
    [InlineData("中")]
    public void TryParse_RejectsUnsupportedShortcuts(string shortcut) =>
        Assert.False(ShortcutParser.TryParse(shortcut, out _, out _));
}

