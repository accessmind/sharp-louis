using System.Linq;
using AccessMind.SharpLouis;
using AwesomeAssertions;
using Xunit;

namespace AccessMind.SharpLouis.Tests;

// Exercises TableCollection against the real tables.json shipped with the package (copied to the
// test output). No native library is loaded here — this is pure JSON metadata handling.
public class TableCollectionTests {
    private static TableCollection Populated() {
        return new TableCollection().PopulateFromJson();
    }

    private static TranslationTable Entry(
        string fileName,
        string displayName,
        IReadOnlyList<string> languages,
        IReadOnlyList<string>? tableTypes = null) {
        return new TranslationTable {
            FileName = fileName,
            DisplayName = displayName,
            Languages = languages,
            TableTypes = tableTypes ?? ["literary"],
            ContractionType = "no",
            Direction = "both",
            DotsMode = 6,
        };
    }

    [Fact]
    public void PopulateFromJson_LoadsManyTables() {
        Populated().Count.Should().BeGreaterThan(200);
    }

    [Fact]
    public void FindByFileName_KnownTable_ReturnsExpectedMetadata() {
        var table = Populated().FindByFileName("en-ueb-g1.ctb");
        table.Should().NotBeNull();
        table!.DisplayName.Should().Be("Unified English uncontracted braille");
        table.Languages.Should().Contain("en");
        table.IsLiteraryBraille().Should().BeTrue();
        table.IsUncontracted().Should().BeTrue();
    }

    [Fact]
    public void FindByFileName_UnknownTable_ReturnsNull() {
        Populated().FindByFileName("this-table-does-not-exist.ctb").Should().BeNull();
    }

    [Fact]
    public void PopulateFromJson_ReadsTheFullMetadataOfAMultiLanguageTable() {
        // he-IL.utb is the case that drove the 3.0 metadata rework: three languages, a wildcard region
        // and a grade, none of which the old single-valued schema could carry.
        var hebrew = Populated().FindByFileName("he-IL.utb");
        hebrew.Should().NotBeNull();
        hebrew!.DisplayName.Should().Be("Israeli braille");
        hebrew.IndexName.Should().Be("Hebrew, modern");
        hebrew.Languages.Should().Equal("he", "ar", "en");
        hebrew.Region.Should().Be("*-IL");
        hebrew.Grade.Should().Be("1");
        hebrew.TableTypes.Should().Equal("literary");
        hebrew.DotsMode.Should().Be(6);
        hebrew.Direction.Should().Be("forward");
    }

    [Fact]
    public void PopulateFromJson_ReadsADualTypeTable() {
        // sv-8g1d.ctb declares both "#+type: computer" and "#+type: literary", plus a variant and a
        // standard version.
        var swedish = Populated().FindByFileName("sv-8g1d.ctb");
        swedish.Should().NotBeNull();
        swedish!.TableTypes.Should().BeEquivalentTo(["computer", "literary"]);
        swedish.IsComputerBraille().Should().BeTrue();
        swedish.IsLiteraryBraille().Should().BeTrue();
        swedish.Variant.Should().Be("detailed");
        swedish.Version.Should().Be("2025");
        swedish.Grade.Should().Be("1");
    }

    [Fact]
    public void PopulateFromJson_IncludesTablesWhoseDisplayNameContainsAFieldName() {
        // Regression: the jsonifier used to pick metadata lines by substring, so the word "language"
        // inside these tables' display names shadowed their real language fields and dropped them from
        // tables.json entirely.
        var collection = Populated();
        var ancient = collection.FindByFileName("ancient-languages-us.utb");
        ancient.Should().NotBeNull();
        ancient!.Languages.Should().HaveCount(36).And.Contain(["akk", "grc", "hbo"]);
        collection.FindByFileName("ancient-languages-borger.utb").Should().NotBeNull();
    }

    [Fact]
    public void PopulateFromJson_ReadsTheRemainingMetadataFields() {
        var american = Populated().FindByFileName("en_US-comp8-ext.tbl");
        american.Should().NotBeNull();
        american!.Region.Should().Be("en-US");
        american.System.Should().Be("ebae");
        american.IndexName.Should().Be("English, U.S., computer, 8-dot");

        var ipa = Populated().FindByFileName("IPA.utb");
        ipa.Should().NotBeNull();
        ipa!.Languages.Should().Equal("*-fonipa");
        ipa.TableTypes.Should().BeEmpty("IPA.utb declares no #+type");

        var ancient = Populated().FindByFileName("ancient-languages-us.utb");
        ancient!.UnicodeRange.Should().Be("ucs4");
    }

    [Fact]
    public void FindByLanguage_ReturnsOnlyTablesThatDeclareThatLanguage() {
        var german = Populated().FindByLanguage("de");
        german.Should().NotBeEmpty();
        german.Should().OnlyContain(t => t.MatchesLanguage("de"));
        german.Select(t => t.FileName).Should().Contain("de-g1.ctb");
    }

    [Fact]
    public void FindByLanguage_FindsEveryLanguageOfAMultiLanguageTable() {
        var collection = Populated();
        collection.FindByLanguage("he").Select(t => t.FileName).Should().Contain("he-IL.utb");
        collection.FindByLanguage("ar").Select(t => t.FileName).Should().Contain("he-IL.utb");
        collection.FindByLanguage("en").Select(t => t.FileName).Should().Contain("he-IL.utb");
    }

    [Fact]
    public void FindByLanguage_MatchesRangesRatherThanExactStrings() {
        // A tag narrower than the declared range still finds the table.
        Populated().FindByLanguage("en-GB").Select(t => t.FileName).Should().Contain("en-ueb-g1.ctb");
    }

    [Fact]
    public void FindByRegion_ReturnsTablesForThatRegion() {
        var israel = Populated().FindByRegion("he-IL");
        israel.Select(t => t.FileName).Should().Contain("he-IL.utb");

        var american = Populated().FindByRegion("en-US");
        american.Should().NotBeEmpty();
        american.Should().OnlyContain(t => t.MatchesRegion("en-US"));
    }

    [Fact]
    public void FindByGrade_ReturnsTablesOfThatGrade() {
        var gradeTwo = Populated().FindByGrade("2");
        gradeTwo.Should().NotBeEmpty();
        gradeTwo.Should().OnlyContain(t => t.Grade == "2");
    }

    [Fact]
    public void FindLiterary_ReturnsOnlyLiteraryTables() {
        var literary = Populated().FindLiterary();
        literary.Should().NotBeEmpty();
        literary.Should().OnlyContain(t => t.IsLiteraryBraille());
    }

    [Fact]
    public void FindComputer_ReturnsOnlyComputerTables() {
        var computer = Populated().FindComputer();
        computer.Should().NotBeEmpty();
        computer.Should().OnlyContain(t => t.IsComputerBraille());
    }

    [Fact]
    public void Filters_Chain_Fluently() {
        var germanLiterary = Populated().FindByLanguage("de").FindLiterary();
        germanLiterary.Should().NotBeEmpty();
        germanLiterary.Should().OnlyContain(t => t.MatchesLanguage("de") && t.IsLiteraryBraille());
    }

    [Fact]
    public void Filters_AreNonDestructive_LeaveSourceUnchanged() {
        var all = Populated();
        var total = all.Count;

        var literary = all.FindLiterary();

        literary.Count.Should().BeLessThan(total, "filtering should narrow the result");
        all.Count.Should().Be(total, "the source collection must not be mutated by a filter");
    }

    [Fact]
    public void Populated_CanBeReusedForIndependentQueries() {
        // Regression: FindLiterary used to mutate in place, so a later ListLanguages saw only the
        // literary subset and dropped languages whose only tables are non-literary.
        var collection = Populated();

        var literaryCount = collection.FindLiterary().Count;
        var germanCount = collection.FindByLanguage("de").Count;
        var languages = collection.ListLanguages();

        literaryCount.Should().BeGreaterThan(0);
        germanCount.Should().BeGreaterThan(0);
        // ListLanguages ran on the full set, unaffected by the two filters above.
        languages.Count.Should().BeGreaterThan(germanCount);
        languages.Should().ContainKey("en");
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
    public void ListLanguages_IncludesEverySecondaryLanguageOfATable() {
        // Arabic reaches the list only through he-IL.utb, which declares it alongside Hebrew.
        Populated().ListLanguages().Should().ContainKey("ar");
    }

    [Fact]
    public void ListLanguages_FallsBackToRawCode_ForUnknownCulture() {
        // A language code that no .NET culture recognizes must fall back to the raw code rather than
        // throwing CultureNotFoundException. Built synthetically so the test does not depend on which
        // obscure bundled codes the current ICU happens to recognize.
        const string bogusCode = "notaculture123";
        var collection = new TableCollection {
            Entry("bogus.ctb", "Bogus", [bogusCode]),
        };
        var languages = collection.ListLanguages();
        languages.Should().ContainKey(bogusCode);
        languages[bogusCode].Should().Be(bogusCode);
    }

    [Fact]
    public void ListLanguages_UsesCuratedName_ForCodesIcuCannotName() {
        // ISO 639-2/3 codes LibLouis ships tables for but no .NET culture can name; ListLanguages must
        // surface a real name rather than the bare code.
        var collection = new TableCollection {
            Entry("ovd.utb", "Elfdalian 6-dot braille", ["ovd"]),
            Entry("smi.utb", "Sami 6-dot braille", ["smi"]),
            Entry("hbo.utb", "Classical Hebrew braille", ["hbo"]),
            Entry("IPA.utb", "International Phonetic Alphabet braille", ["*-fonipa"]),
        };
        var languages = collection.ListLanguages();
        languages["ovd"].Should().Be("Elfdalian");
        languages["smi"].Should().Be("Sami");
        languages["hbo"].Should().Be("Classical Hebrew");
        languages["*-fonipa"].Should().Be("International Phonetic Alphabet");
    }

    [Fact]
    public void ListLanguages_HasNoEmptyValues() {
        Populated().ListLanguages().Values.Should().OnlyContain(name => !string.IsNullOrEmpty(name));
    }

    [Fact]
    public void ListLanguages_NamesEveryBundledLanguage() {
        // Every language any bundled table declares must come back with a real name, not the bare code.
        // A LibLouis upgrade that introduces an exotic code fails here, which is the cue to add it to
        // TableCollection.KnownLanguageNames rather than let a picker show "ovd" or "xdm".
        var unnamed = Populated().ListLanguages()
            .Where(pair => pair.Value == pair.Key || pair.Value == pair.Key.Split('-')[0])
            .Select(pair => pair.Key);
        unnamed.Should().BeEmpty();
    }

    [Fact]
    public void ICollection_AddContainsRemoveClear_Behave() {
        var collection = new TableCollection();
        collection.IsReadOnly.Should().BeFalse();
        var entry = Entry("custom.ctb", "Custom", ["xx"]);

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
        var entry = Entry("custom.ctb", "Custom", ["xx"]);
        collection.Add(entry);
        collection.Should().ContainSingle().Which.Should().Be(entry);
    }
}
