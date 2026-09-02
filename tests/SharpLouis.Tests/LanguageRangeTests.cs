using AccessMind.SharpLouis;
using AwesomeAssertions;
using Xunit;

namespace AccessMind.SharpLouis.Tests;

// RFC 4647 §3.3.2 extended filtering, the rule LibLouis applies to the "language" and "region" fields.
// The range is the pattern and the tag is the thing matched, so the relation is asymmetric.
public class LanguageRangeTests {
    [Theory]
    // Exact and prefix matches.
    [InlineData("en", "en", true)]
    [InlineData("en", "EN", true)]           // language tags are case-insensitive
    [InlineData("en", "en-GB", true)]        // a broader range covers a narrower tag
    [InlineData("en-GB", "en", false)]       // …but not the other way round
    [InlineData("en", "de", false)]
    [InlineData("akk-Latn", "akk-Latn", true)]
    [InlineData("akk-Latn", "akk", false)]
    // Real ranges from the bundled tables.
    [InlineData("*-IL", "he-IL", true)]      // he-IL.utb: "Israel, whatever the language"
    [InlineData("*-IL", "ar-IL", true)]
    [InlineData("*-IL", "he", false)]
    [InlineData("*-fonipa", "en-fonipa", true)]  // IPA.utb
    [InlineData("*-fonipa", "en", false)]
    [InlineData("*-NZ", "en-NZ", true)]
    [InlineData("cmn-CN", "cmn-CN", true)]
    [InlineData("cmn-CN", "cmn-TW", false)]
    // A wildcard skips intervening subtags.
    [InlineData("*", "anything", true)]
    [InlineData("de-*-DE", "de-Latn-DE", true)]
    [InlineData("de-*-DE", "de-DE", true)]
    [InlineData("de-*-DE", "de-Latn-DE-1996", true)]
    [InlineData("de-*-DE", "de-Latf-DE", true)]
    [InlineData("de-*-DE", "de-x-DE", false)]    // a singleton subtag stops the skip
    [InlineData("de-*-DE", "de-Deva", false)]
    public void Matches_FollowsRfc4647ExtendedFiltering(string range, string tag, bool expected) {
        LanguageRange.Matches(range, tag).Should().Be(expected);
    }

    [Theory]
    [InlineData(null, "en")]
    [InlineData("en", null)]
    [InlineData("", "en")]
    [InlineData("en", "  ")]
    public void Matches_NeverMatches_OnMissingInput(string? range, string? tag) {
        LanguageRange.Matches(range, tag).Should().BeFalse();
    }

    [Fact]
    public void MatchesAny_IsTrue_WhenAnyRangeMatches() {
        string[] israeli = ["he", "ar", "en"];
        LanguageRange.MatchesAny(israeli, "ar").Should().BeTrue();
        LanguageRange.MatchesAny(israeli, "en-US").Should().BeTrue();
        LanguageRange.MatchesAny(israeli, "de").Should().BeFalse();
        LanguageRange.MatchesAny([], "en").Should().BeFalse();
        LanguageRange.MatchesAny(null, "en").Should().BeFalse();
    }
}
