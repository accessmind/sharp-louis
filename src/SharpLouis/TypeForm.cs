namespace SharpLouis;

/// <summary>
/// SharpLouis, .NET wrapper for the LibLouis Braille Translator library
/// Copyright  2024 AccessMind LLC.
/// Licensed under the Apache License, Version 2.0 (the "License");
/// you may not use this file except in compliance with the License.
/// You may obtain a copy of the License at
/// http://www.apache.org/licenses/LICENSE-2.0
/// Unless required by applicable law or agreed to in writing,
/// software distributed under the License is distributed on an "AS IS" BASIS,
/// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
/// See the License for the specific language governing permissions and limitations under the License.
/// 
/// Corresponds to values defined in liblouis.h
/// </summary>
public enum TypeForm : int {
    PlainText = 0x0000,
    Italic = 0x0001,
    Underline = 0x0002,
    Bold = 0x0004,
    Emphasis4 = 0x0008,
    Emphasis5 = 0x0010,
    Emphasis6 = 0x0020,
    Emphasis7 = 0x0040,
    Emphasis8 = 0x0080,
    Emphasis9 = 0x0100,
    Emphasis10 = 0x0200,
    ComputerBraille = 0x0400,
    NoTranslate = 0x0800,
    NoContract = 0x1000,
    Hex5c5c = 0x5c5c // NOTE: For debugging only !!
}