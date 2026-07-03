using System.Linq;
using AccessMind.SharpLouis;
using FluentAssertions;
using Xunit;

namespace AccessMind.SharpLouis.Tests;

// Exercises TableCollection against the real tables.json shipped with the package (copied to the
// test output). No native library is loaded here — this is pure JSON metadata handling.
public class TableCollectionTests {
    private static TableCollection Populated() {
        var collection = new TableCollection().PopulateFromJson();
        collection.Should().NotBeNull();
        return collection!;
    }

    [Fact]
    public void PopulateFromJson_LoadsManyTables() {
        Populated().Count.Should().BeGreaterThan(200);
    }

    [Fact]
    public void FindByFileName_KnownTable_ReturnsExpectedMetadata() {
        var table = Populated().FindByFileName("en-ueb-g1.ctb");
        table.Should().NotBeNull();
        table!.Value.DisplayName.Should().Be("Unified English uncontracted braille");
        table.Value.Language.Should().Be("en");
        table.Value.IsLiteraryBraille().Should().BeTrue();
        table.Value.IsUncontracted().Should().BeTrue();
    }

    [Fact]
    public void FindByFileName_UnknownTable_ReturnsNull() {
        Populated().FindByFileName("this-table-does-not-exist.ctb").Should().BeNull();
    }

    [Fact]
    public void FindByLanguage_ReturnsOnlyThatLanguage() {
        var german = Populated().FindByLanguage("de");
        german.Should().NotBeEmpty();
        german.Should().OnlyContain(t => t.Language == "de");
        german.Select(t => t.FileName).Should().Contain("de-g1.ctb");
    }

    [Fact]
    public void FindLiterary_ReturnsOnlyLiteraryTables() {
        var literary = Populated().FindLiterary();
        literary.Should().NotBeEmpty();
        literary.Should().OnlyContain(t => t.IsLiteraryBraille());
    }

    [Fact]
    public void Filters_Chain_Fluently() {
        var germanLiterary = Populated().FindByLanguage("de").FindLiterary();
        germanLiterary.Should().NotBeEmpty();
        germanLiterary.Should().OnlyContain(t => t.Language == "de" && t.IsLiteraryBraille());
    }

    [Fact]
    public void ListLanguages_MapsKnownCodesToEnglishNames() {
        var languages = Populated().ListLanguages();
        languages.Should().ContainKey("en");
        languages["en"].Should().Be("English");
        languages.Should().ContainKey("de");
        languages["de"].Should().Be("German");
    }

    [Fact]
    public void ListLanguages_FallsBackToRawCode_ForUnknownCulture() {
        // A language code that no .NET culture recognizes must fall back to the raw code rather than
        // throwing CultureNotFoundException. Built synthetically so the test does not depend on which
        // obscure bundled codes the current ICU happens to recognize.
        const string bogusCode = "notaculture123";
        var collection = new TableCollection {
            new TranslationTable("bogus.ctb", "Bogus", bogusCode, "literary", "no", "both", 6),
        };
        var languages = collection.ListLanguages();
        languages.Should().ContainKey(bogusCode);
        languages[bogusCode].Should().Be(bogusCode);
    }

    [Fact]
    public void ListLanguages_HasNoEmptyValues() {
        Populated().ListLanguages().Values.Should().OnlyContain(name => !string.IsNullOrEmpty(name));
    }

    [Fact]
    public void ICollection_AddContainsRemoveClear_Behave() {
        var collection = new TableCollection();
        collection.IsReadOnly.Should().BeFalse();
        var entry = new TranslationTable("custom.ctb", "Custom", "xx", "literary", "no", "both", 6);

        collection.Add(entry);
        collection.Count.Should().Be(1);
        collection.Contains(entry).Should().BeTrue();

        var buffer = new TranslationTable[1];
        collection.CopyTo(buffer, 0);
        buffer[0].Should().Be(entry);

        collection.Remove(entry).Should().BeTrue();
        collection.Count.Should().Be(0);

        collection.Add(entry);
        collection.Clear();
        collection.Count.Should().Be(0);
    }

    [Fact]
    public void Enumerator_YieldsAddedItems() {
        var collection = new TableCollection();
        var entry = new TranslationTable("custom.ctb", "Custom", "xx", "literary", "no", "both", 6);
        collection.Add(entry);
        collection.Should().ContainSingle().Which.Should().Be(entry);
    }
}
