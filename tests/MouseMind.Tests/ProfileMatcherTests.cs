using MouseMind.Core.Models;
using MouseMind.Core.Profiles;

namespace MouseMind.Tests;

public sealed class ProfileMatcherTests
{
    private readonly ProfileMatcher _matcher = new();

    [Theory]
    [InlineData("Code", "Code.exe")]
    [InlineData("code.exe", "CODE")]
    [InlineData("chrome / msedge", "msedge.exe")]
    [InlineData("chrome,msedge;firefox|opera", "firefox")]
    public void Matches_NormalizesAndSupportsSeparators(string expression, string process) =>
        Assert.True(ProfileMatcher.Matches(expression, process));

    [Fact]
    public void Find_DoesNotUseSubstringMatching()
    {
        var profiles = new[] { new MouseProfile { Name = "Code", ProcessName = "Code" } };
        Assert.Null(_matcher.Find(profiles, "VisualStudioCodeHelper"));
    }

    [Fact]
    public void Find_ExactProfileBeatsWildcard()
    {
        var profiles = new[]
        {
            new MouseProfile { Name = "Global", ProcessName = "*", Priority = 100 },
            new MouseProfile { Name = "Editor", ProcessName = "Code", Priority = 0 }
        };
        Assert.Equal("Editor", _matcher.Find(profiles, "Code.exe")?.Name);
    }

    [Fact]
    public void Find_IgnoresDisabledProfiles()
    {
        var profiles = new[] { new MouseProfile { ProcessName = "Code", IsEnabled = false } };
        Assert.Null(_matcher.Find(profiles, "Code"));
    }
}

