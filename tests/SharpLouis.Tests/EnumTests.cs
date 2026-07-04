using AccessMind.SharpLouis;
using AwesomeAssertions;
using Xunit;

namespace AccessMind.SharpLouis.Tests;

// Guards the numeric enum values, which are an ABI contract with liblouis.h. A silent change here
// would corrupt the mode/typeform integers passed across the P/Invoke boundary.
public class EnumTests {
    [Theory]
    [InlineData(TranslationModes.NoContractions, 1)]
    [InlineData(TranslationModes.ComputerBrailleAtCursor, 2)]
    [InlineData(TranslationModes.DotsInputOutput, 4)]
    [InlineData(TranslationModes.ComputerBrailleLeftFromCursor, 32)]
    [InlineData(TranslationModes.UnicodeBraille, 64)]
    [InlineData(TranslationModes.NoUndefined, 128)]
    [InlineData(TranslationModes.PartialTranslation, 256)]
    public void TranslationModes_MatchLibLouisHeaderValues(TranslationModes mode, int expected) {
        ((int)mode).Should().Be(expected);
    }

    [Fact]
    public void TranslationModes_FixedTranslatorCombination_Is196() {
        // The mode the translator always uses: NoUndefined | UnicodeBraille | DotsInputOutput.
        var combined = TranslationModes.NoUndefined | TranslationModes.UnicodeBraille | TranslationModes.DotsInputOutput;
        ((int)combined).Should().Be(128 + 64 + 4);
    }

    [Theory]
    [InlineData(TypeForm.PlainText, 0x0000)]
    [InlineData(TypeForm.Italic, 0x0001)]
    [InlineData(TypeForm.Underline, 0x0002)]
    [InlineData(TypeForm.Bold, 0x0004)]
    [InlineData(TypeForm.ComputerBraille, 0x0400)]
    [InlineData(TypeForm.NoTranslate, 0x0800)]
    [InlineData(TypeForm.NoContract, 0x1000)]
    public void TypeForm_MatchLibLouisHeaderValues(TypeForm form, int expected) {
        ((int)form).Should().Be(expected);
    }
}
