using SharpLouis.BrailleTranslationTable;

namespace SharpLouis;

/// <summary>
/// SharpLouis, .NET wrapper for the LibLouis Braille Translator library
/// Copyright © 2024 AccessMind LLC.
/// Licensed under the Apache License, Version 2.0 (the "License");
/// you may not use this file except in compliance with the License.
/// You may obtain a copy of the License at
/// http://www.apache.org/licenses/LICENSE-2.0
/// Unless required by applicable law or agreed to in writing,
/// software distributed under the License is distributed on an "AS IS" BASIS,
/// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
/// See the License for the specific language governing permissions and limitations under the License.
//////
///Struct representing a Braille translation table
/// </summary>
/// <param name="FileName">Translation table file name</param>
/// <param name="DisplayName">Display name of the translation table</param>
/// <param name="Language">Language code of the translation table</param>
/// <param name="TableType">Translation table Braille type, such as literary or computer Braille</param>
/// <param name="ContractionType">Contraction type, such as uncontracted, partially contracted or fully contracted</param>
/// <param name="Direction">Translation direction, can be forward, backward or both</param>
/// <param name="DotsMode">Braille dots mode, either eight-dot or six-dot</param>
public readonly record struct TranslationTable(
    string FileName,
    string DisplayName,
    string Language,
    string TableType,
    string ContractionType,
    string Direction,
    int DotsMode
        ) {
    public bool IsLiteraryBraille() {
        return string.Equals(this.TableType, BrailleType.Literary, StringComparison.OrdinalIgnoreCase);
    }

    public bool IsComputerBraille() {
        return string.Equals(this.TableType, BrailleType.Computer, StringComparison.OrdinalIgnoreCase);
    }

    public bool IsMathBraille() {
        return string.Equals(this.TableType, BrailleType.Math, StringComparison.OrdinalIgnoreCase);
    }

    public bool IsUncontracted() {
        return string.Equals(this.ContractionType, BrailleContraction.Uncontracted, StringComparison.OrdinalIgnoreCase);
    }

    public bool IsPartiallyContracted() {
        return string.Equals(this.ContractionType, BrailleContraction.PartiallyContracted, StringComparison.OrdinalIgnoreCase);
    }

    public bool IsFullyContracted() {
        return string.Equals(this.ContractionType, BrailleContraction.FullyContracted, StringComparison.OrdinalIgnoreCase);
    }

    public bool IsContracted() {
        return this.IsFullyContracted() || this.IsPartiallyContracted();
    }

    public bool CanTranslate() {
        return string.Equals(this.Direction, TranslationDirection.Forward, StringComparison.OrdinalIgnoreCase) || string.Equals(this.Direction, TranslationDirection.both, StringComparison.OrdinalIgnoreCase);
    }

    public bool CanBackTranslate() {
        return string.Equals(this.Direction, TranslationDirection.Backward, StringComparison.OrdinalIgnoreCase) || string.Equals(this.Direction, TranslationDirection.both, StringComparison.OrdinalIgnoreCase);
    }

    public bool CanTranslateBothWays() {
        return string.Equals(this.Direction, TranslationDirection.both, StringComparison.OrdinalIgnoreCase);
    }

    public bool isEightDot() {
        return this.DotsMode == BrailleMode.EightDot;
    }

    public bool IsSixDot() {
        return this.DotsMode == BrailleMode.SixDot;
    }
}
