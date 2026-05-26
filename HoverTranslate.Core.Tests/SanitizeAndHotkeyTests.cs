using HoverTranslate.Core.Services;
using Xunit;

namespace HoverTranslate.Core.Tests;

public class SanitizeAndHotkeyTests
{
    [Fact]
    public void Sanitize_RedactsSkToken()
    {
        var svc = new SanitizeService();
        var (text, had) = svc.Sanitize("key sk-abcdefghijklmnopqrstuvwxyz12345 end");
        Assert.True(had);
        Assert.Contains("[REDACTED]", text);
        Assert.DoesNotContain("sk-abc", text);
    }

    [Fact]
    public void Sanitize_RedactsEmail()
    {
        var svc = new SanitizeService();
        var (text, had) = svc.Sanitize("contact user@example.com please");
        Assert.True(had);
        Assert.Contains("[REDACTED]", text);
    }

    [Fact]
    public void HotkeyParser_Default_IsValid()
    {
        var (mods, vk) = HotkeyParser.Parse(HotkeyParser.DefaultHotkey);
        Assert.NotEqual(0u, mods);
        Assert.NotEqual(0u, vk);
    }

    [Fact]
    public void HotkeyParser_Invalid_Throws()
    {
        Assert.Throws<FormatException>(() => HotkeyParser.Parse("NotAHotkey"));
    }
}
