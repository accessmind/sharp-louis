using AccessMind.SharpLouis;
using AccessMind.SharpLouis.BrailleTranslationTable;
using AwesomeAssertions;
using Xunit;

namespace AccessMind.SharpLouis.Tests;

// Pure metadata-logic tests for the TranslationTable record. No native library involved.
public class TranslationTableTests {
    private static TranslationTable Table(
        IReadOnlyList<string>? tableTypes = null,
        string? contractionType = null,
        string? direction = null,
        int dotsMode = 0,
        IReadOnlyList<string>? languages = null,
        string? region = null,
        string? grade = null) {
        return new TranslationTable {
            FileName = "x.ctb",
            DisplayName = "Display",
            Languages = languages ?? ["en"],
            Region = region,
            TableTypes = tableTypes ?? [],
            ContractionType = contractionType,
            Grade = grade,
            DotsMode = dotsMode,
            Direction = direction,
        };
    }

    [Theory]
    [InlineData("literary", true, false, false)]
    [InlineData("Literary", true, false, false)] // case-insensitive
    [InlineData("computer", false, true, false)]
    [InlineData("math", false, false, true)]
    [InlineData("unknown", false, false, false)]
    public void TypePredicates_ReflectTableTypes(string type, bool literary, bool computer, bool math) {
        var table = Table(tableTypes: [type]);
        table.IsLiteraryBraille().Should().Be(literary);
        table.IsComputerBraille().Should().Be(computer);
        table.IsMathBraille().Should().Be(math);
    }

    [Fact]
    public void TypePredicates_AreAllFalse_WhenNoTypeDeclared() {
        // IPA.utb declares no #+type at all.
        var table = Table(tableTypes: []);
        table.IsLiteraryBraille().Should().BeFalse();
        table.IsComputerBraille().Should().BeFalse();
        table.IsMathBraille().Should().BeFalse();
    }

    [Fact]
    public void TypePredicates_BothTrue_ForADualTypeTable() {
        // The Swedish and Elfdalian 8-dot tables declare "#+type: computer" and "#+type: literary".
        var table = Table(tableTypes: ["computer", "literary"]);
        table.IsComputerBraille().Should().BeTrue();
        table.IsLiteraryBraille().Should().BeTrue();
        table.IsMathBraille().Should().BeFalse();
    }

    [Theory]
    [InlineData("no", true, false, false)]
    [InlineData("partial", false, true, false)]
    [InlineData("PARTIAL", false, true, false)] // case-insensitive
    [InlineData("full", false, false, true)]
    [InlineData(null, false, false, false)]
    public void ContractionPredicates_ReflectContractionType(string? contraction, bool uncontracted, bool partial, bool full) {
        var table = Table(contractionType: contraction);
        table.IsUncontracted().Should().Be(uncontracted);
        table.IsPartiallyContracted().Should().Be(partial);
        table.IsFullyContracted().Should().Be(full);
    }

    [Theory]
    [InlineData("full", true)]
    [InlineData("partial", true)]
    [InlineData("no", false)]
    [InlineData(null, false)]
    public void IsContracted_IsTrueForPartialOrFull(string? contraction, bool expected) {
        Table(contractionType: contraction).IsContracted().Should().Be(expected);
    }

    [Theory]
    [InlineData(null, true, true, true)]      // unspecified direction is treated as bidirectional
    [InlineData("forward", true, false, false)]
    [InlineData("Forward", true, false, false)] // case-insensitive
    [InlineData("backward", false, true, false)]
    [InlineData("both", true, true, true)]
    public void DirectionPredicates_ReflectDirection(string? direction, bool canTranslate, bool canBack, bool canBoth) {
        var table = Table(direction: direction);
        table.CanTranslate().Should().Be(canTranslate);
        table.CanBackTranslate().Should().Be(canBack);
        table.CanTranslateBothWays().Should().Be(canBoth);
    }

    [Theory]
    [InlineData(6, false, true)]
    [InlineData(8, true, false)]
    [InlineData(0, false, false)] // no dots metadata declared
    public void DotsPredicates_ReflectDotsMode(int dotsMode, bool eightDot, bool sixDot) {
        var table = Table(dotsMode: dotsMode);
        table.IsEightDot().Should().Be(eightDot);
        table.IsSixDot().Should().Be(sixDot);
    }

    [Theory]
    [InlineData("1", "1", true)]
    [InlineData("1.5", "1.5", true)] // fractional grades are real: sv-8g1d and friends
    [InlineData("1", "2", false)]
    [InlineData(null, "1", false)]
    public void IsGrade_ComparesTheDeclaredGrade(string? declared, string queried, bool expected) {
        Table(grade: declared).IsGrade(queried).Should().Be(expected);
    }

    [Fact]
    public void MatchesLanguage_FindsEveryDeclaredLanguage() {
        // he-IL.utb (Israeli braille) declares Hebrew, Arabic and English.
        var table = Table(languages: ["he", "ar", "en"]);
        table.MatchesLanguage("he").Should().BeTrue();
        table.MatchesLanguage("ar").Should().BeTrue();
        table.MatchesLanguage("en").Should().BeTrue();
        table.MatchesLanguage("de").Should().BeFalse();
    }

    [Fact]
    public void MatchesLanguage_IsRangeBased_NotStringEquality() {
        Table(languages: ["en"]).MatchesLanguage("en-GB").Should().BeTrue();
        // Asymmetric by design: a table narrowed to a script does not answer to the bare language.
        Table(languages: ["akk-Latn"]).MatchesLanguage("akk").Should().BeFalse();
    }

    [Fact]
    public void MatchesRegion_UsesTheDeclaredRange() {
        var israeli = Table(region: "*-IL");
        israeli.MatchesRegion("he-IL").Should().BeTrue();
        israeli.MatchesRegion("ar-IL").Should().BeTrue();
        israeli.MatchesRegion("he").Should().BeFalse();

        Table(region: null).MatchesRegion("en-US").Should().BeFalse();
    }

    [Fact]
    public void Record_HasValueEquality_IncludingTheListMembers() {
        var a = Table(tableTypes: ["computer", "literary"], contractionType: "full", direction: "both", dotsMode: 6, languages: ["he", "ar"]);
        var b = Table(tableTypes: ["computer", "literary"], contractionType: "full", direction: "both", dotsMode: 6, languages: ["he", "ar"]);

        // The lists are distinct instances: the record-synthesized equality would call these unequal.
        a.Should().Be(b);
        (a == b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Record_IsUnequal_WhenAListMemberDiffers() {
        var a = Table(languages: ["he", "ar"]);
        var b = Table(languages: ["he"]);
        a.Should().NotBe(b);
    }

    [Fact]
    public void IsOfType_AcceptsTheBrailleTypeConstants() {
        var table = Table(tableTypes: [BrailleType.Literary]);
        table.IsOfType(BrailleType.Literary).Should().BeTrue();
        table.IsOfType(BrailleType.Computer).Should().BeFalse();
    }
}
