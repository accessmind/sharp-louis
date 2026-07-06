using System.Collections;
using System.Globalization;
using System.Text.Json;
using AccessMind.SharpLouis.BrailleTranslationTable;

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
/// enumeration), so a collection can be populated by hand as well as from JSON.
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

    /// <summary>Returns a new collection of the tables for <paramref name="language"/>; the receiver is
    /// left unchanged.</summary>
    public TableCollection FindByLanguage(string language) =>
        new(this.tables.FindAll(t => t.Language == language));

    /// <summary>Returns a new collection of the literary-braille tables; the receiver is left unchanged.</summary>
    public TableCollection FindLiterary() =>
        new(this.tables.FindAll(t => t.IsLiteraryBraille()));

    public TranslationTable? FindByFileName(string fileName) {
        // List<T>.Find on a record struct returns default (all-null fields) on a miss, which is
        // indistinguishable from a real entry. Return a nullable so callers can detect "not found".
        foreach (TranslationTable table in this.tables) {
            if (table.FileName == fileName) {
                return table;
            }
        }

        return null;
    }

    public Dictionary<string, string> ListLanguages() {
        var languages = new Dictionary<string, string>();
        foreach (TranslationTable table in this.tables) {
            if (string.IsNullOrEmpty(table.Language) || languages.ContainsKey(table.Language)) {
                continue;
            }

            languages[table.Language] = GetEnglishName(table.Language);
        }

        return languages;
    }

    // English names for ISO 639-2/3 codes that LibLouis ships tables for but no .NET/ICU culture can
    // name (CultureInfo throws CultureNotFoundException). Without these, ListLanguages would surface the
    // bare code (e.g. "ovd") to a UI. Keyed on the primary subtag, case-insensitively.
    private static readonly Dictionary<string, string> KnownLanguageNames = new(StringComparer.OrdinalIgnoreCase) {
        ["dra"] = "Dravidian",
        ["hbo"] = "Classical Hebrew",
        ["mun"] = "Munda",
        ["ovd"] = "Elfdalian",
        ["smi"] = "Sami",
    };

    private static string GetEnglishName(string language) {
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
