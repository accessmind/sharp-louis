﻿namespace SharpLouis.BrailleTranslationTable;

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
///
/// Braille types for filtering translation tables
/// </summary>
public readonly struct BrailleType {
    public const string Literary = "literary";
    public const string Computer = "computer";
    public const string Math = "math";
}