using AccessMind.SharpLouis;
using AccessMind.SharpLouis.BrailleTranslationTable;
using AwesomeAssertions;
using Xunit;

namespace AccessMind.SharpLouis.Tests;

// Pure metadata-logic tests for the TranslationTable record struct. No native library involved.
public class TranslationTableTests {
    private static TranslationTable Table(
        string? tableType = null,
        string? contractionType = null,
        string? direction = null,
        int dotsMode = 0) {
        return new TranslationTable("x.ctb", "Display", "en", tableType, contractionType, direction, dotsMode);
    }

    [Theory]
    [InlineData("literary", true, false, false)]
    [InlineData("Literary", true, false, false)] // case-insensitive
    [InlineData("computer", false, true, false)]
    [InlineData("math", false, false, true)]
    [InlineData(null, false, false, false)]
    [InlineData("unknown", false, false, false)]
    public void TypePredicates_ReflectTableType(string? type, bool literary, bool computer, bool math) {
        var table = Table(tableType: type);
        table.IsLiteraryBraille().Should().Be(literary);
        table.IsComputerBraille().Should().Be(computer);
        table.IsMathBraille().Should().Be(math);
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

    [Fact]
    public void RecordStruct_HasValueEquality() {
        var a = Table(tableType: "literary", contractionType: "full", direction: "both", dotsMode: 6);
        var b = Table(tableType: "literary", contractionType: "full", direction: "both", dotsMode: 6);
        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }
}
