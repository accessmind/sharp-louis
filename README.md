# SharpLouis

.NET wrapper for the [LibLouis](https://github.com/liblouis/liblouis) Braille translator and back-translator library.

## Installation

Install via NuGet Package Manager:

```
dotnet add package AccessMind.SharpLouis
```

Or via the Package Manager Console in Visual Studio:

```
Install-Package AccessMind.SharpLouis
```

The package includes the native LibLouis DLL and all translation tables, which are automatically copied to your output directory.

## Quick Start

```csharp
using AccessMind.SharpLouis;

// Create a translator for a translation table. It owns no unmanaged resource, so there is
// nothing to dispose — just create one and use it. Create throws a descriptive exception if
// the native library or the table is missing (use BrailleTranslator.TryCreate for a non-throwing probe).
var translator = BrailleTranslator.Create("en-ueb-g1.ctb");

string braille = translator.TranslateString("Hello World");
Console.WriteLine(braille); // Outputs Unicode Braille: ⠠⠓⠑⠇⠇⠕⠀⠠⠺⠕⠗⠇⠙
```

A translator is cheap and thread-safe: create as many as you like (one per table), share them across threads, and keep them for the lifetime of your app.

For a complete, runnable example see the [`samples/`](samples/) directory: a small console app that translates print text to Braille and back, letting you pick the table, direction, and input string.

## What Is It?

When working with [Braille](https://en.wikipedia.org/wiki/Braille) input and output, one needs to have a tool that ideally would take into account all the particularities and intricacies of the Braille code for various languages and needs (contracted Braille, Unicode Braille, on-the-fly translation and so on). The TL;DR is that there is no straightforward one-to-one way of translating a given message from print to Braille and vice-versa, without knowing the language used, the code variant (known as Braille table) inside that language, sometimes the context and so on, and so forth.

There are several software solutions dealing with this task, but most of them are proprietary and very expensive for use in derived products. The most widely known and used free (LGPL-licensed) open-source solution is [LibLouis](https://github.com/liblouis/liblouis), a library written in C initially for the BRLTTY Linux screen reader but gone far beyond this. Now it is available for all popular operating systems and used in many open-source and proprietary products, including screen readers and Braille translating and embossing software.

However, to this time there was no publicly available open-source wrapper for .NET environment: everyone who wanted to use LibLouis in a .NET-based product had to wrap the C API by oneself. SharpLouis is an attempt to start an initiative that would eventually lead to a robust open-source solution benefitial for every .NET developer wanting to incorporate Braille in their work.

## Limitations and Particularities

Currently SharpLouis is only in the beginning of its life, so there are some known limitations.

* For now, only Windows is supported as we provide LibLouis DLL and tables inside the package. The managed assembly targets plain `net10.0`, so a cross-platform project can reference it without a hard platform block, but any call into it will fail at runtime off Windows x64 (the compiler will also warn via `[SupportedOSPlatform("windows")]`);
* Currently only 64-bit systems are supported (platform is restricted to x64 in the project file);
* The bundled LibLouis native library is version 3.38. You can confirm the exact version at runtime with `BrailleTranslator.GetVersion()`;
* The DLL is built with UTF-32 support (see LibLouis documentation if you don’t know what we are talking about);
* Translation tables are both bundled with the package and listed in a JSON file for displaying and filtering (see the section about translate table collection below). The utility that processes tables is called [LLJT](https://github.com/accessmind/liblouis-jsonify-tables) and is also open-source.
* Translation mode is fixed to `TranslationModes.NoUndefined | TranslationModes.UnicodeBraille | TranslationModes.DotsInputOutput`, i.e., currently SharpLouis works only with Unicode Braille internally and no output for undefined characters is provided.

## The Translator

The main `BrailleTranslator` class exposes several public methods, most of which are directly wrapped C API methods provided by LibLouis. Every translator serializes its native calls internally, so a single instance is safe to share across threads.

* `static BrailleTranslator Create(string tableNames)` — Creates the translator that can be subsequently used. The parameter, although stated in plural, is usually a single table name relative to the path where the translation tables are located, so usually it's something like `"en-ueb-g1.ctb"` (a comma-separated list is also accepted). The table is compiled up front, so a broken or missing table fails here rather than on the first translation: `Create` throws `ArgumentException`, `DllNotFoundException`, `FileNotFoundException`, or `LouisException` as appropriate. A translator owns no unmanaged resource, so it is cheap to create, thread-safe to share, and there is nothing to dispose.
* `static bool TryCreate(string tableNames, out BrailleTranslator? translator)` — Non-throwing form of `Create`: returns `false` (with `translator` set to `null`) instead of throwing when the native library, tables folder, or a requested table is unavailable. Useful for probing availability.
* `string CharsToDots(string chars)` — Equivalent of the `lou_charToDots` function in LibLouis. Accepts characters as a string and returns the corresponding dot patterns. For more details about this and all subsequent methods see the [LibLouis documentation](https://liblouis.io/documentation/liblouis.html). These methods return the result string and throw `LouisException` if the native call fails.
* `string DotsToChars(string dots)` — Inverse of the previous method. Accepts dot patterns and returns characters according to the translation table being used.
* `string TranslateString(string text)` — Translates a string to Unicode Braille according to the translation table selected on translator instantiation.
* `string TranslateStringWithTypeForms(string text, TypeForm[] typeForms)` — Translates a string with emphasis styles. Accepts an array of emphasis typeforms as members of the `TypeForm` enum, indexed like `text`. See the LibLouis documentation for more info on this.
* `string BackTranslateString(string braille)` — Translates a Braille representation back to text according to the translation table selected on translator instantiation. Note! Not every table is capable of back-translating from Braille to text, see below on translation tables filtering.
* `(string Text, TypeForm[] TypeForms) BackTranslateStringWithTypeForms(string braille)` — Same but also reports the per-character emphasis LibLouis inferred.
* `static void ClearTableCache()` — Releases LibLouis's **process-global** cache of compiled tables (the native `lou_free`). This affects every translator in the process, not a single instance, and is normally unnecessary — the cache is cheap to keep and repopulates automatically on the next translation. Call it only to reclaim that memory or to force tables to be recompiled after their files change on disk.
* `static string GetVersion()` — Returns the version string of the underlying native LibLouis library, for example `3.38.0`.

## Translation Tables

A translation table is a way to represent print characters in Braille. As the Braille code consists of only 63 characters in traditional 6-dot Braille plus the space and of 255 characters in Computer 8-dot Braille plus the space, there is no one-on-one correspondence between print and Braille. For example, the character ⠝ (Braille dots 1345) can represent the Latin letter n, Cyrillic н, Hebrew נ/ן, Greek ν and many other letters usually having the value of N, and also the half note C in music. More than that, punctuation and even numbers are sometimes represented differently, depending on the language and the code used. That’s what translation tables are for.  
In SharpLouis, a translation table is represented by a `TranslationTable` structure that mirrors translation table metadata from LibLouis. It has the following properties:

* `FileName` — The name of the table file in LibLouis. Example: _en-ueb-g1.ctb_
* `DisplayName` — A human-readable display name for using in user interfaces. Example: _Unified English uncontracted braille_
* `Language` — The language of the table, usually as two-letter code. Example: _en_ for English
* `TableType` — Type of Braille to translate. The values are defined in the BrailleTranslationTable/BrailleType struct. Currently can be one of literary, computer or math Braille.
* `ContractionType` — Determines the level of contraction the table supports. The values are defined in the `BrailleTranslationTable/BrailleContraction` struct. Can be one of not contracted, partially contracted or fully contracted. May be `null` when the table declares no contraction metadata (for example, computer Braille tables).
* `Direction` — Translation direction supported by the table. The values are defined in the `BrailleTranslationTable/TranslationDirection` struct. Can be one of forward, backward or both. May be `null` when the table declares no direction metadata, in which case the table is treated as bidirectional.
* `DotsMode` — the "dotness" of the Braille supported by the table. The values are defined in the `BrailleTranslationTable/BrailleMode` struct. Can be 8 (eight-dot Braille) or 6 (six-dot Braille), or 0 when the table declares no dots metadata.

This struct also has some helper methods for filtering translation tables:
* `bool IsLiteraryBraille()` — Returns `true` if the current table is a literary Braille table.
* `bool IsComputerBraille()` — Returns `true` if the current table is a computer Braille table.
* `bool IsMathBraille()` — Returns `true` if the current table is a mathematical Braille table.
* `bool IsUncontracted()` — Returns `true` if the current translation table supports no contractions.
* `bool IsPartiallyContracted()` — Returns `true` if the current table supports partially contracted Braille. A good example of this is the German Vollschrift table (de-g1.ctb). This code has contractions for basic letter combinations but no sophisticated whole-word contractions.
* `bool IsFullyContracted()` — Returns `true` when the current table supports contracted Braille, also commonly referred to as grade 2 in many languages.
* `bool IsContracted()` — Returns `true` if the current table supports either fully or partially contracted Braille.
* `bool CanTranslate()` — Returns `true` if the current table can translate print to Braille.
* `bool CanBackTranslate()` — Returns `true` if the current translation table can translate Braille back to print text.
* `bool CanTranslateBothWays()` — Returns `true` only if the current table can translate print text to Braille and Braille to print text.
* `bool IsEightDot()` — Returns `true` if the current table is an eight-dot Braille translation table. Most of them are designed for computer Braille, but not all: there are languages that officially have characters with dots 7 and 8 in their literary Braille.
* `bool IsSixDot()` — Returns `true` if the current translation table is a six-dot Braille table.

## Translation Table Collection

The table collection class helps in filtering translation tables, selecting them and displaying various information about them. It uses the fluent interface. So, for example, to find all literary tables for the French language, you can do:

```csharp
var frenchLiteraryTables = new TableCollection()
    .PopulateFromJson()
    .FindByLanguage("fr")
    .FindLiterary();
```

It has the following methods:

* `TableCollection PopulateFromJson()` — Parses the JSON file provided with the library and returns a table collection instance populated from this file.
* `TableCollection FindByLanguage(string language)` — Filters the table collection and finds all the tables of a given language.
* `TableCollection FindLiterary()` — Filters the collection and finds all the translation tables supporting literary Braille.
* `TranslationTable? FindByFileName(string fileName)` — Accepts a file name and finds the corresponding translation table, or `null` if no table with that file name exists.
* `Dictionary<string, string> ListLanguages()` — Searches all the tables and lists the languages supported by those tables. Returns a dictionary where the key of each element is a language code and the value is its full English name.

## Building from Source

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later
- Windows x64 (currently the only supported platform)

### Clone and Build

```bash
git clone https://github.com/accessmind/sharp-louis.git
cd sharp-louis
dotnet build SharpLouis.sln
```

### Build Configurations

- **Debug**: Full debug symbols, no optimization
- **Release**: Optimized build, no debug symbols

```bash
# Debug build
dotnet build SharpLouis.sln -c Debug

# Release build
dotnet build SharpLouis.sln -c Release
```

### Create NuGet Package

```bash
dotnet pack src/SharpLouis/SharpLouis.csproj -c Release
```

The package will be created in the `src/SharpLouis/bin/Release/` directory.

### Project Structure

```
sharp-louis/
├── SharpLouis.sln              # Solution file
├── src/
│   └── SharpLouis/
│       ├── SharpLouis.csproj   # Project file
│       ├── BrailleTranslator.cs # Main translator class with P/Invoke
│       ├── TableCollection.cs  # Fluent API for filtering tables
│       ├── TranslationTable.cs # Translation table metadata
│       ├── TranslationModes.cs # Translation mode flags
│       ├── TypeForm.cs         # Typeform enum
│       ├── NativeFunctions.cs  # Native function enum
│       ├── BrailleTranslationTable/  # Metadata structures
│       │   ├── BrailleContraction.cs
│       │   ├── BrailleMode.cs
│       │   ├── BrailleType.cs
│       │   └── TranslationDirection.cs
│       ├── build/              # MSBuild targets for NuGet consumers
│       │   └── AccessMind.SharpLouis.targets
│       └── LibLouis/
│           ├── liblouis.dll    # Native library (Windows x64)
│           ├── tables.json     # Table metadata
│           └── tables/         # Translation tables
└── README.md
```

## Contributing

All contributions, big or small, are welcome! Please create an issue before submitting a pull request, thus it will be easier to track everyone's work. Let's improve SharpLouis together!

## License

Copyright © 2024–2026 [André Polykanine](https://github.com/Menelion), [AccessMind LLC.](https://accessmind.io/), and contributors.  
Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.  
You may obtain a copy of the License at [http://www.apache.org/licenses/LICENSE-2.0].  
Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.  
See the License for the specific language governing permissions and limitations under the License.  
Inspired by [LibLouis.NET](https://github.com/LeonarddeR/liblouis.net) by [Leonard de Ruijter](https://github.com/LeonarddeR).  
Heavily based on [LibLouis.CSharpWrapper](https://github.com/JensJensenPublic/liblouis.CSharp.Wrapper) by [Jens Jensen](https://github.com/jensjensenpublic).
