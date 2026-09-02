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
/// Matches a language tag against an <em>extended language range</em>, the notation LibLouis uses for
/// the <c>language</c> and <c>region</c> fields of a table's metadata.
/// </summary>
/// <remarks>
/// A range is a language tag whose subtags may be replaced by the wildcard <c>*</c> — LibLouis ships
/// <c>he</c>, <c>akk-Latn</c>, <c>*-IL</c> and <c>*-fonipa</c>, among others. Matching follows the
/// extended filtering algorithm of
/// <see href="https://datatracker.ietf.org/doc/html/rfc4647#section-3.3.2">RFC 4647 §3.3.2</see>, which
/// is what LibLouis itself applies in <c>lou_findTable</c>. Note that the relationship is asymmetric:
/// the range is the pattern and the tag is the thing matched, so the range <c>en</c> matches the tag
/// <c>en-GB</c> but the range <c>en-GB</c> does not match the tag <c>en</c>.
/// </remarks>
public static class LanguageRange {
    private static readonly char[] SubtagSeparator = ['-'];

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="languageTag"/> falls within
    /// <paramref name="range"/> per RFC 4647 extended filtering. Comparison is case-insensitive, as
    /// language tags are. A null or blank range or tag never matches.
    /// </summary>
    /// <param name="range">The extended language range, for example <c>*-IL</c>.</param>
    /// <param name="languageTag">The language tag to test, for example <c>he-IL</c>.</param>
    public static bool Matches(string? range, string? languageTag) {
        if (string.IsNullOrWhiteSpace(range) || string.IsNullOrWhiteSpace(languageTag)) {
            return false;
        }

        string[] rangeSubtags = range.Split(SubtagSeparator, StringSplitOptions.RemoveEmptyEntries);
        string[] tagSubtags = languageTag.Split(SubtagSeparator, StringSplitOptions.RemoveEmptyEntries);
        if (rangeSubtags.Length == 0 || tagSubtags.Length == 0) {
            return false;
        }

        // Step 2: the primary subtags must agree, unless the range wildcards it.
        if (!IsWildcard(rangeSubtags[0]) && !SubtagsEqual(rangeSubtags[0], tagSubtags[0])) {
            return false;
        }

        int r = 1;
        int t = 1;
        while (r < rangeSubtags.Length) {
            // Step 3A: a wildcard consumes nothing and lets the next range subtag float.
            if (IsWildcard(rangeSubtags[r])) {
                r++;
                continue;
            }

            // Step 3B: range subtags left but no tag subtags to match them against.
            if (t >= tagSubtags.Length) {
                return false;
            }

            // Step 3C: a literal hit advances both.
            if (SubtagsEqual(rangeSubtags[r], tagSubtags[t])) {
                r++;
                t++;
                continue;
            }

            // Step 3D: a singleton subtag opens an extension or private-use sequence, which the range
            // is not allowed to skip past.
            if (tagSubtags[t].Length == 1) {
                return false;
            }

            // Step 3E: skip an unmatched tag subtag and try the same range subtag again.
            t++;
        }

        // Step 4: every range subtag was accounted for.
        return true;
    }

    /// <summary>
    /// Returns <see langword="true"/> when any of <paramref name="ranges"/> matches
    /// <paramref name="languageTag"/>.
    /// </summary>
    public static bool MatchesAny(IEnumerable<string>? ranges, string? languageTag) {
        if (ranges is null) {
            return false;
        }

        foreach (string range in ranges) {
            if (Matches(range, languageTag)) {
                return true;
            }
        }

        return false;
    }

    private static bool IsWildcard(string subtag) => subtag.Length == 1 && subtag[0] == '*';

    private static bool SubtagsEqual(string left, string right) =>
        string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
}
