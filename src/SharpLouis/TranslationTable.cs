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
/// The metadata LibLouis declares for one Braille translation table.
/// </summary>
/// <remarks>
/// <para>
/// LibLouis tables carry their metadata as <c>#+key: value</c> (queryable) and <c>#-key: value</c>
/// (informative) lines in the table header, and the LibLouis manual is explicit that <em>the same key
/// may appear multiple times in a table</em>. That is why <see cref="Languages"/> and
/// <see cref="TableTypes"/> are lists rather than single strings: <c>he-IL.utb</c> (Israeli braille)
/// declares Hebrew, Arabic <em>and</em> English, <c>ancient-languages-us.utb</c> declares thirty-six
/// languages, and the Swedish and Elfdalian 8-dot tables declare both a <c>computer</c> and a
/// <c>literary</c> type.
/// </para>
/// <para>
/// This is a reference type with value semantics: two instances are equal when every field is equal,
/// with <see cref="Languages"/> and <see cref="TableTypes"/> compared element by element. (The equality
/// a record synthesizes would compare those two by reference, so both <see cref="Equals(TranslationTable)"/>
/// and <see cref="GetHashCode"/> are written out below.)
/// </para>
/// </remarks>
public sealed record TranslationTable {
    /// <summary>Translation table file name, for example <c>en-ueb-g1.ctb</c>.</summary>
    public required string FileName { get; init; }

    /// <summary>
    /// Human-readable name for a user interface, from <c>#-display-name</c>. Example:
    /// <em>Unified English uncontracted braille</em>.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Sort-friendly name from <c>#-index-name</c>, written "Language, qualifiers" — for example
    /// <em>Hebrew, modern</em> or <em>English, U.S., computer, 8-dot</em>. Ordering a picker by this
    /// groups a language's tables together, which <see cref="DisplayName"/> does not.
    /// </summary>
    public string? IndexName { get; init; }

    /// <summary>
    /// Every language the table declares, as RFC 4647 extended language ranges (so entries such as
    /// <c>akk-Latn</c> and <c>*-fonipa</c> occur). Use <see cref="MatchesLanguage"/> rather than
    /// comparing strings. Never empty for a table loaded from <c>tables.json</c>.
    /// </summary>
    public IReadOnlyList<string> Languages { get; init; } = [];

    /// <summary>
    /// The region the table is used in, as an extended language range — for example <c>en-US</c>,
    /// <c>cmn-CN</c>, or <c>*-IL</c> for "Israel, whatever the language". <see langword="null"/> when
    /// the table declares no region. Use <see cref="MatchesRegion"/> to test it.
    /// </summary>
    public string? Region { get; init; }

    /// <summary>
    /// The Braille types the table covers; the values are defined in <see cref="BrailleType"/>. Usually
    /// a single entry, but a table may be both (the Swedish 8-dot tables are computer <em>and</em>
    /// literary), and it may be empty when the table declares no type.
    /// </summary>
    public IReadOnlyList<string> TableTypes { get; init; } = [];

    /// <summary>
    /// Level of contraction; the values are defined in <see cref="BrailleContraction"/>.
    /// <see langword="null"/> when the table declares no contraction metadata.
    /// </summary>
    public string? ContractionType { get; init; }

    /// <summary>
    /// Braille grade as declared, or <see langword="null"/> when the table declares none. A string
    /// rather than a number on purpose: alongside <c>0</c>, <c>1</c>, <c>2</c> and <c>3</c>, LibLouis
    /// ships tables graded <c>1.2</c>, <c>1.3</c>, <c>1.4</c> and <c>1.5</c>.
    /// </summary>
    public string? Grade { get; init; }

    /// <summary>
    /// The "dotness" of the Braille the table produces; the values are defined in
    /// <see cref="BrailleMode"/>. 8 (eight-dot) or 6 (six-dot), or 0 when the table declares no dots
    /// metadata.
    /// </summary>
    public int DotsMode { get; init; }

    /// <summary>
    /// Translation direction; the values are defined in <see cref="TranslationDirection"/>.
    /// <see langword="null"/> when the table declares no direction, in which case it is treated as
    /// bidirectional.
    /// </summary>
    public string? Direction { get; init; }

    /// <summary>
    /// The Braille system or code the table implements, for example <c>ueb</c> (Unified English
    /// Braille), <c>ebae</c>, <c>ddp</c> or <c>bfu</c>. Free-form; <see langword="null"/> when
    /// undeclared.
    /// </summary>
    public string? System { get; init; }

    /// <summary>
    /// Distinguishes tables that would otherwise be alike, for example <c>detailed</c>, <c>compact</c>
    /// or <c>no-tone</c>. Free-form; <see langword="null"/> when undeclared.
    /// </summary>
    public string? Variant { get; init; }

    /// <summary>
    /// The edition of the Braille standard the table follows, usually a year such as <c>2014</c> or
    /// <c>2025</c>. <see langword="null"/> when undeclared. Unrelated to the LibLouis version.
    /// </summary>
    public string? Version { get; init; }

    /// <summary>
    /// The locale whose conventions the table assumes, where that differs from <see cref="Languages"/>:
    /// a Greek table transcribed for English-speaking students declares <c>locale: en</c>.
    /// <see langword="null"/> when undeclared.
    /// </summary>
    public string? Locale { get; init; }

    /// <summary>
    /// The character range the table needs: <c>ucs2</c> or <c>ucs4</c>. A <c>ucs4</c> table needs a
    /// UTF-32 LibLouis build, which is what SharpLouis ships, so every bundled table is usable.
    /// <see langword="null"/> when undeclared.
    /// </summary>
    public string? UnicodeRange { get; init; }

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="languageTag"/> falls within any of the
    /// table's declared <see cref="Languages"/>, per RFC 4647 extended filtering. Matching is
    /// asymmetric: a table declaring <c>en</c> matches the tag <c>en-GB</c>, but one declaring
    /// <c>en-GB</c> does not match the bare tag <c>en</c>.
    /// </summary>
    public bool MatchesLanguage(string languageTag) => LanguageRange.MatchesAny(this.Languages, languageTag);

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="regionTag"/> falls within the table's
    /// declared <see cref="Region"/>, per RFC 4647 extended filtering. Always <see langword="false"/>
    /// for a table that declares no region.
    /// </summary>
    public bool MatchesRegion(string regionTag) => LanguageRange.Matches(this.Region, regionTag);

    /// <summary>Returns <see langword="true"/> when the table declares the given Braille type.</summary>
    public bool IsOfType(string tableType) => this.TableTypes.Contains(tableType, StringComparer.OrdinalIgnoreCase);

    public bool IsLiteraryBraille() => this.IsOfType(BrailleType.Literary);

    public bool IsComputerBraille() => this.IsOfType(BrailleType.Computer);

    public bool IsMathBraille() => this.IsOfType(BrailleType.Math);

    public bool IsUncontracted() =>
        string.Equals(this.ContractionType, BrailleContraction.Uncontracted, StringComparison.OrdinalIgnoreCase);

    public bool IsPartiallyContracted() =>
        string.Equals(this.ContractionType, BrailleContraction.PartiallyContracted, StringComparison.OrdinalIgnoreCase);

    public bool IsFullyContracted() =>
        string.Equals(this.ContractionType, BrailleContraction.FullyContracted, StringComparison.OrdinalIgnoreCase);

    public bool IsContracted() => this.IsFullyContracted() || this.IsPartiallyContracted();

    /// <summary>Returns <see langword="true"/> when the table declares the given <see cref="Grade"/>.</summary>
    public bool IsGrade(string grade) => string.Equals(this.Grade, grade, StringComparison.OrdinalIgnoreCase);

    public bool CanTranslate() {
        // In liblouis an absent "direction" field declares no restriction, so the table is usable
        // both ways. Treat unspecified direction as translatable rather than misreporting it as false.
        return this.Direction is null
            || string.Equals(this.Direction, TranslationDirection.Forward, StringComparison.OrdinalIgnoreCase)
            || string.Equals(this.Direction, TranslationDirection.Both, StringComparison.OrdinalIgnoreCase);
    }

    public bool CanBackTranslate() {
        return this.Direction is null
            || string.Equals(this.Direction, TranslationDirection.Backward, StringComparison.OrdinalIgnoreCase)
            || string.Equals(this.Direction, TranslationDirection.Both, StringComparison.OrdinalIgnoreCase);
    }

    public bool CanTranslateBothWays() {
        return this.Direction is null
            || string.Equals(this.Direction, TranslationDirection.Both, StringComparison.OrdinalIgnoreCase);
    }

    public bool IsEightDot() => this.DotsMode == BrailleMode.EightDot;

    public bool IsSixDot() => this.DotsMode == BrailleMode.SixDot;

    /// <summary>
    /// Value equality over every field, with the two list-valued fields compared element by element.
    /// The record-synthesized version compares those lists by reference, so two separately built but
    /// identical descriptions of the same table would come out unequal.
    /// </summary>
    public bool Equals(TranslationTable? other) {
        return other is not null
            && this.FileName == other.FileName
            && this.DisplayName == other.DisplayName
            && this.IndexName == other.IndexName
            && this.Languages.SequenceEqual(other.Languages, StringComparer.Ordinal)
            && this.Region == other.Region
            && this.TableTypes.SequenceEqual(other.TableTypes, StringComparer.Ordinal)
            && this.ContractionType == other.ContractionType
            && this.Grade == other.Grade
            && this.DotsMode == other.DotsMode
            && this.Direction == other.Direction
            && this.System == other.System
            && this.Variant == other.Variant
            && this.Version == other.Version
            && this.Locale == other.Locale
            && this.UnicodeRange == other.UnicodeRange;
    }

    /// <inheritdoc/>
    public override int GetHashCode() {
        var hash = new HashCode();
        hash.Add(this.FileName);
        hash.Add(this.DisplayName);
        hash.Add(this.IndexName);
        foreach (string language in this.Languages) {
            hash.Add(language);
        }

        hash.Add(this.Region);
        foreach (string tableType in this.TableTypes) {
            hash.Add(tableType);
        }

        hash.Add(this.ContractionType);
        hash.Add(this.Grade);
        hash.Add(this.DotsMode);
        hash.Add(this.Direction);
        hash.Add(this.System);
        hash.Add(this.Variant);
        hash.Add(this.Version);
        hash.Add(this.Locale);
        hash.Add(this.UnicodeRange);
        return hash.ToHashCode();
    }
}
