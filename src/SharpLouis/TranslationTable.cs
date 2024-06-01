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
        return this.TableType.ToLower() == BrailleType.Literary;
    }

    public bool IsComputerBraille() {
        return this.TableType.ToLower() == BrailleType.Computer;
    }

    public bool IsMathBraille() {
        return this.TableType.ToLower() == BrailleType.Math;
    }

    public bool IsUncontracted() {
        return this.ContractionType.ToLower() == BrailleContraction.Uncontracted;
    }

    public bool IsPartiallyContracted() {
        return this.ContractionType.ToLower() == BrailleContraction.PartiallyContracted;
    }

    public bool IsFullyContracted() {
        return this.ContractionType.ToLower() == BrailleContraction.FullyContracted;
    }

    public bool IsContracted() {
        return this.IsFullyContracted() || this.IsPartiallyContracted();
    }

    public bool CanTranslate() {
        return this.Direction.ToLower() == TranslationDirection.Forward || this.Direction.ToLower() == TranslationDirection.both;
    }

    public bool CanBackTranslate() {
        return this.Direction.ToLower() == TranslationDirection.Backward || this.Direction.ToLower() == TranslationDirection.both;
    }

    public bool CanTranslateBothWays() {
        return this.Direction.ToLower() == TranslationDirection.both;
    }

    public bool isEightDot() {
        return this.DotsMode == BrailleMode.EightDot;
    }

    public bool IsSixDot() {
        return this.DotsMode == BrailleMode.SixDot;
    }
}
