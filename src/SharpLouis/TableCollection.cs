using System.Collections;
using System.Globalization;
using System.Text.Json;

namespace AccessMind.SharpLouis;

// SharpLouis, .NET wrapper for the LibLouis Braille Translator library
// Copyright © 2024–2026 AccessMind LLC.
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing,
// software distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and limitations under the License.

/// <summary>
/// Translation tables collection. Works with the tables.json file provided in this library as part
/// of LibLouis, which is itself a result of tables processing made by the LLJT console utility.
///
/// The primary use is the fluent filtering API (<see cref="PopulateFromJson"/>,
/// <see cref="FindByLanguage"/>, <see cref="FindLiterary"/>, …). The filter methods are
/// <b>non-destructive</b>: each returns a new collection and leaves the receiver unchanged, so a single
/// populated collection can be reused for several independent queries (for example
/// <see cref="ListLanguages"/> after <see cref="FindLiterary"/>). It also implements the full mutable
/// <see cref="ICollection{T}"/> contract (<see cref="Add"/>, <see cref="Remove"/>, <see cref="Clear"/>,
/// enumeration), so a collection can be populated by hand as well as from JSON — and, being
/// enumerable, it composes with LINQ for anything the built-in filters do not cover (say,
/// <c>collection.Where(t =&gt; t.System == "ueb")</c>).
/// </summary>
/// <seealso href="https://github.com/accessmind/liblouis-jsonify-tables"/>
public sealed class TableCollection: ICollection<TranslationTable> {
    private static readonly string TablesJson = Path.Combine(AppContext.BaseDirectory, "LibLouis", "tables.json");
    private List<TranslationTable> tables = [];

    /// <summary>Creates an empty collection, ready to <see cref="PopulateFromJson"/> or be populated by hand.</summary>
    public TableCollection() { }

    /// <summary>Wraps an already-filtered list; used by the non-destructive filter methods.</summary>
    private TableCollection(List<TranslationTable> tables) => this.tables = tables;

    public int Count => tables.Count;

    /// <summary>Always <see langword="false"/>: the collection is mutable.</summary>
    public bool IsReadOnly => false;

    public TableCollection PopulateFromJson() {
        using var file = File.OpenRead(TablesJson);
        this.tables = JsonSerializer.Deserialize<List<TranslationTable>>(file)!;
        return this;
    }

    /// <summary>
    /// Returns a new collection of the tables usable for <paramref name="languageTag"/>; the receiver is
    /// left unchanged.
    /// </summary>
    /// <remarks>
    /// A table declares its languages as RFC 4647 extended language ranges, and this matches
    /// <paramref name="languageTag"/> against them the way LibLouis itself does — so
    /// <c>FindByLanguage("en-GB")</c> finds the tables that declare plain <c>en</c>, and
    /// <c>FindByLanguage("he")</c> finds <c>he-IL.utb</c>. A table that declares several languages is
    /// found under each of them: Israeli braille answers to <c>he</c>, <c>ar</c> and <c>en</c> alike.
    /// Matching is asymmetric, so a bare <c>akk</c> does <em>not</em> find a table declaring only
    /// <c>akk-Latn</c>.
    /// </remarks>
    public TableCollection FindByLanguage(string languageTag) =>
        new(this.tables.FindAll(t => t.MatchesLanguage(languageTag)));

    /// <summary>
    /// Returns a new collection of the tables used in <paramref name="regionTag"/>, matched as an
    /// RFC 4647 extended language range; the receiver is left unchanged. Tables that declare no region
    /// are excluded.
    /// </summary>
    public TableCollection FindByRegion(string regionTag) =>
        new(this.tables.FindAll(t => t.MatchesRegion(regionTag)));

    /// <summary>Returns a new collection of the literary-braille tables; the receiver is left unchanged.</summary>
    public TableCollection FindLiterary() =>
        new(this.tables.FindAll(t => t.IsLiteraryBraille()));

    /// <summary>Returns a new collection of the computer-braille tables; the receiver is left unchanged.</summary>
    public TableCollection FindComputer() =>
        new(this.tables.FindAll(t => t.IsComputerBraille()));

    /// <summary>
    /// Returns a new collection of the tables of the given Braille grade (<c>"0"</c>, <c>"1"</c>,
    /// <c>"2"</c>, <c>"3"</c>, and the fractional grades such as <c>"1.5"</c>); the receiver is left
    /// unchanged.
    /// </summary>
    public TableCollection FindByGrade(string grade) =>
        new(this.tables.FindAll(t => t.IsGrade(grade)));

    /// <summary>
    /// Finds the table with the given file name, or <see langword="null"/> when the collection holds no
    /// such table.
    /// </summary>
    public TranslationTable? FindByFileName(string fileName) =>
        this.tables.Find(t => t.FileName == fileName);

    /// <summary>
    /// Lists every language the collection's tables declare, as a map of language code to English name.
    /// A table that declares several languages contributes each of them.
    /// </summary>
    public Dictionary<string, string> ListLanguages() {
        var languages = new Dictionary<string, string>();
        foreach (TranslationTable table in this.tables) {
            foreach (string language in table.Languages) {
                if (string.IsNullOrEmpty(language) || languages.ContainsKey(language)) {
                    continue;
                }

                languages[language] = GetEnglishName(language);
            }
        }

        return languages;
    }

    // English names for the language ranges LibLouis ships tables for that .NET cannot name on its own.
    // Depending on the ICU data on the machine, CultureInfo either throws CultureNotFoundException or —
    // more often — hands back the bare code as the "English name", so a language picker would show
    // "ovd" instead of "Elfdalian". Codes ICU does resolve today (Akkadian, Sumerian, …) are listed too,
    // so the names do not depend on which ICU version the host happens to carry. Looked up
    // case-insensitively, first by the whole range — which is how the wildcard ranges are named — and
    // then by the primary subtag.
    private static readonly Dictionary<string, string> KnownLanguageNames = new(StringComparer.OrdinalIgnoreCase) {
        ["*-fonipa"] = "International Phonetic Alphabet",
        ["akk"] = "Akkadian",
        ["dra"] = "Dravidian",
        ["elx"] = "Elamite",
        ["hbo"] = "Classical Hebrew",
        ["hit"] = "Hittite",
        ["jpa"] = "Jewish Palestinian Aramaic",
        ["mun"] = "Munda",
        ["oar"] = "Old Aramaic",
        ["obm"] = "Moabite",
        ["ovd"] = "Elfdalian",
        ["peo"] = "Old Persian",
        ["smi"] = "Sami",
        ["sux"] = "Sumerian",
        ["syc"] = "Classical Syriac",
        ["tlg"] = "Tagalog",
        ["uga"] = "Ugaritic",
        ["xdm"] = "Edomite",
        ["xeb"] = "Eblaite",
        ["xhu"] = "Hurrian",
        ["xlu"] = "Luwian",
        ["xur"] = "Urartian",
    };

    private static string GetEnglishName(string language) {
        if (KnownLanguageNames.TryGetValue(language, out var knownRange)) {
            return knownRange;
        }

        var primary = language.Split('-')[0];
        if (KnownLanguageNames.TryGetValue(primary, out var known)) {
            return known;
        }

        try {
            return new CultureInfo(primary).EnglishName;
        } catch (CultureNotFoundException) {
            // A code no installed .NET/ICU culture recognizes and that isn't in KnownLanguageNames:
            // fall back to the raw code rather than throwing.
            return language;
        }
    }

    public void Add(TranslationTable item) => ((ICollection<TranslationTable>)tables).Add(item);
    public void Clear() => ((ICollection<TranslationTable>)tables).Clear();
    public bool Contains(TranslationTable item) => ((ICollection<TranslationTable>)tables).Contains(item);
    public void CopyTo(TranslationTable[] array, int arrayIndex) => ((ICollection<TranslationTable>)tables).CopyTo(array, arrayIndex);
    public bool Remove(TranslationTable item) => ((ICollection<TranslationTable>)tables).Remove(item);
    public IEnumerator<TranslationTable> GetEnumerator() => ((IEnumerable<TranslationTable>)tables).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)tables).GetEnumerator();
}
