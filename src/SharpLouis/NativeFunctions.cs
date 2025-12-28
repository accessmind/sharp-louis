namespace AccessMind.SharpLouis;

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
/// Helper for calling functions from LibLouis.dll
/// </summary>
internal enum NativeFunction {
    charsToDots,
    dotsToChars,
    translateString,       // Do NOT Use the TypeForm parameter
    translateStringTfe,    // Use the TypeForm parameter
    backTranslateString,   // Do NOT Use the TypeForm parameter
    backTranslateStringTfe // Use the TypeForm parameter
}
