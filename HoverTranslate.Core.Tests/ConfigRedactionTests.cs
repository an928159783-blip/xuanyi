using Xunit;
using HoverTranslate.Core.Services;

namespace HoverTranslate.Core.Tests;

public class ConfigRedactionTests
{
    [Fact]
    public void RedactApiKeys_masks_values()
    {
        const string raw = """
            {
              "apiProfiles": [
                { "apiKey": "sk-secret123" }
              ]
            }
            """;

        var redacted = ConfigRedaction.RedactApiKeys(raw);

        Assert.Contains("\"***\"", redacted);
        Assert.DoesNotContain("sk-secret123", redacted);
    }
}
