using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AccessMind.SharpLouis;
using FluentAssertions;
using Xunit;

namespace AccessMind.SharpLouis.Tests;

// End-to-end tests that load the real native liblouis.dll and translate against bundled tables.
// All native-touching tests live in this single class so xUnit runs them serially: ClearTableCache
// calls lou_free(), which resets liblouis's process-global table cache.
public class WrapperTests {
    private const string EnglishUncontracted = "en-ueb-g1.ctb";
    private const string GermanPartial = "de-g1.ctb";

    // Every character liblouis emits under the wrapper's fixed UnicodeBraille mode must live in the
    // Unicode Braille Patterns block (U+2800–U+28FF); U+2800 is the blank/space cell.
    private static bool IsAllBraille(string text) => text.All(c => c is >= '⠀' and <= '⣿');

    [Fact]
    public void GetVersion_ReturnsVersionString() {
        var version = Wrapper.GetVersion();
        version.Should().NotBeNullOrWhiteSpace();
        Version.TryParse(version, out _).Should().BeTrue("liblouis reports a dotted version like 3.38.0, got '{0}'", version);
    }

    [Fact]
    public void Create_WithValidTable_ReturnsWrapper() {
        var wrapper = Wrapper.Create(EnglishUncontracted);
        wrapper.Should().NotBeNull();
    }

    [Fact]
    public void Create_WithMissingTableFile_ThrowsFileNotFound() {
        var act = () => Wrapper.Create("this-table-does-not-exist.ctb");
        act.Should().Throw<FileNotFoundException>().WithMessage("*this-table-does-not-exist.ctb*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankName_ThrowsArgumentException(string name) {
        var act = () => Wrapper.Create(name);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TryCreate_ValidTable_ReturnsTrueAndWrapper() {
        Wrapper.TryCreate(EnglishUncontracted, out var wrapper).Should().BeTrue();
        wrapper.Should().NotBeNull();
    }

    [Fact]
    public void TryCreate_MissingTable_ReturnsFalseAndNull() {
        Wrapper.TryCreate("nope.ctb", out var wrapper).Should().BeFalse();
        wrapper.Should().BeNull();
    }

    [Fact]
    public void TranslateString_HelloWorld_ProducesUnifiedEnglishBraille() {
        var wrapper = Wrapper.Create(EnglishUncontracted);
        // Capital indicators (U+2820) precede H and W in Unified English Braille.
        wrapper.TranslateString("Hello World").Should().Be("⠠⠓⠑⠇⠇⠕⠀⠠⠺⠕⠗⠇⠙");
    }

    [Theory]
    [InlineData("Hello World")]
    [InlineData("The quick brown fox")]
    [InlineData("Numbers 123 and symbols!")]
    public void TranslateString_OutputIsAllBrailleCells(string text) {
        var braille = Wrapper.Create(EnglishUncontracted).TranslateString(text);
        braille.Should().NotBeEmpty();
        IsAllBraille(braille).Should().BeTrue("translate output must be Unicode Braille, got '{0}'", braille);
    }

    [Theory]
    [InlineData("Hello World")]
    [InlineData("Braille translation round trip")]
    public void TranslateString_ThenBackTranslate_RecoversOriginal(string text) {
        var wrapper = Wrapper.Create(EnglishUncontracted);
        var braille = wrapper.TranslateString(text);
        wrapper.BackTranslateString(braille).Should().Be(text);
    }

    [Fact]
    public void TranslateString_EmptyInput_ReturnsEmpty() {
        Wrapper.Create(EnglishUncontracted).TranslateString(string.Empty).Should().BeEmpty();
    }

    [Fact]
    public void TranslateString_LongInput_IsNotTruncated() {
        var wrapper = Wrapper.Create(EnglishUncontracted);
        var text = string.Join(" ", Enumerable.Repeat("the quick brown fox jumps over the lazy dog", 400));
        var braille = wrapper.TranslateString(text);
        IsAllBraille(braille).Should().BeTrue();
        // A faithful (non-truncated) translation must back-translate to the original in uncontracted braille.
        wrapper.BackTranslateString(braille).Should().Be(text);
    }

    [Fact]
    public void CharsToDots_ThenDotsToChars_RoundTrips() {
        var wrapper = Wrapper.Create(EnglishUncontracted);
        var dots = wrapper.CharsToDots("abc");
        dots.Should().Be("⠁⠃⠉"); // a=dot1, b=dots12, c=dots14
        wrapper.DotsToChars(dots).Should().Be("abc");
    }

    [Fact]
    public void TranslateStringWithTypeForms_EmphasisedText_ProducesBraille() {
        var wrapper = Wrapper.Create(EnglishUncontracted);
        var forms = new[] { TypeForm.Bold, TypeForm.Bold, TypeForm.Bold, TypeForm.Bold, TypeForm.Bold };
        var braille = wrapper.TranslateStringWithTypeForms("world", forms);
        braille.Should().NotBeEmpty();
        IsAllBraille(braille).Should().BeTrue();
    }

    [Fact]
    public void BackTranslateStringWithTypeForms_ReturnsTextAndTypeForms() {
        var wrapper = Wrapper.Create(EnglishUncontracted);
        var braille = wrapper.TranslateString("hello");
        var (text, typeForms) = wrapper.BackTranslateStringWithTypeForms(braille);
        text.Should().Be("hello");
        typeForms.Should().NotBeNull();
    }

    [Fact]
    public void MultipleWrappers_WithDifferentTables_CoexistIndependently() {
        // Regression guard: wrappers own no per-instance native state, so using several at once (or
        // letting one go out of scope) must not disturb the others.
        var english = Wrapper.Create(EnglishUncontracted);
        var german = Wrapper.Create(GermanPartial);

        IsAllBraille(english.TranslateString("Hello")).Should().BeTrue();
        IsAllBraille(german.TranslateString("Hallo")).Should().BeTrue();

        english.TranslateString("World").Should().NotBeEmpty();
    }

    [Fact]
    public void TranslateString_IsSafeUnderConcurrentUse() {
        var wrapper = Wrapper.Create(EnglishUncontracted);
        var expected = wrapper.TranslateString("Hello World");

        Parallel.For(0, 500, _ => {
            wrapper.TranslateString("Hello World").Should().Be(expected);
        });
    }

    [Fact]
    public void ClearTableCache_ThenTranslate_RepopulatesAndStillWorks() {
        var wrapper = Wrapper.Create(EnglishUncontracted);
        wrapper.TranslateString("first").Should().NotBeEmpty();

        // Clearing the process-global cache is optional and non-destructive: the next translation
        // simply recompiles the table from disk.
        Wrapper.ClearTableCache();

        IsAllBraille(wrapper.TranslateString("second")).Should().BeTrue();
    }
}
