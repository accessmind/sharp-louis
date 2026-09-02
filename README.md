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
* The bundled LibLouis native library is version 3.39. You can confirm the exact version at runtime with `BrailleTranslator.GetVersion()`;
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
* `static string GetVersion()` — Returns the version string of the underlying native LibLouis library, for example `3.39.0`.

## Translation Tables

A translation table is a way to represent print characters in Braille. As the Braille code consists of only 63 characters in traditional 6-dot Braille plus the space and of 255 characters in Computer 8-dot Braille plus the space, there is no one-on-one correspondence between print and Braille. For example, the character ⠝ (Braille dots 1345) can represent the Latin letter n, Cyrillic н, Hebrew נ/ן, Greek ν and many other letters usually having the value of N, and also the half note C in music. More than that, punctuation and even numbers are sometimes represented differently, depending on the language and the code used. That’s what translation tables are for.  
In SharpLouis, a translation table is represented by a `TranslationTable` record that mirrors translation table metadata from LibLouis.

A word on why two of its properties are lists. LibLouis tables declare their metadata as `#+key: value` lines in the table header, and the LibLouis manual is explicit that _the same key may appear multiple times in a table_. This is not a corner case: `he-IL.utb` (Israeli braille) declares Hebrew, Arabic **and** English, `ancient-languages-us.utb` declares thirty-six languages, and the Swedish and Elfdalian 8-dot tables declare both a `computer` and a `literary` type. `Languages` and `TableTypes` therefore hold every declared value.

`TranslationTable` has the following properties:

* `FileName` — The name of the table file in LibLouis. Example: _en-ueb-g1.ctb_
* `DisplayName` — A human-readable display name for using in user interfaces. Example: _Unified English uncontracted braille_
* `IndexName` — A sort-friendly name written "Language, qualifiers". Example: _English, U.S., computer, 8-dot_. Ordering a table picker by this groups a language's tables together, which `DisplayName` does not. May be `null`.
* `Languages` — Every language the table declares, as a read-only list. These are **RFC 4647 extended language ranges**, not plain codes: alongside _en_ and _he_ you will find _akk-Latn_ and _\*-fonipa_. Use `MatchesLanguage()` rather than comparing strings.
* `Region` — The region the table is used in, also as an extended language range. Example: _en-US_, or _\*-IL_ for "Israel, whatever the language". May be `null`.
* `TableTypes` — The Braille types the table covers, as a read-only list. The values are defined in the `BrailleTranslationTable/BrailleType` struct: literary, computer or math Braille. Usually one entry, occasionally two, and empty when the table declares no type.
* `ContractionType` — Determines the level of contraction the table supports. The values are defined in the `BrailleTranslationTable/BrailleContraction` struct. Can be one of not contracted, partially contracted or fully contracted. May be `null` when the table declares no contraction metadata (for example, computer Braille tables).
* `Grade` — The Braille grade the table implements, as a **string** rather than a number: alongside _0_, _1_, _2_ and _3_, LibLouis ships tables graded _1.2_, _1.3_, _1.4_ and _1.5_. May be `null`.
* `DotsMode` — the "dotness" of the Braille supported by the table. The values are defined in the `BrailleTranslationTable/BrailleMode` struct. Can be 8 (eight-dot Braille) or 6 (six-dot Braille), or 0 when the table declares no dots metadata.
* `Direction` — Translation direction supported by the table. The values are defined in the `BrailleTranslationTable/TranslationDirection` struct. Can be one of forward, backward or both. May be `null` when the table declares no direction metadata, in which case the table is treated as bidirectional.
* `System` — The Braille system or code the table implements. Example: _ueb_ (Unified English Braille), _ebae_, _ddp_. Free-form; may be `null`.
* `Variant` — Distinguishes tables that would otherwise be alike. Example: _detailed_, _compact_, _no-tone_. May be `null`.
* `Version` — The edition of the Braille standard the table follows, usually a year such as _2025_. Unrelated to the LibLouis version. May be `null`.
* `Locale` — The locale whose conventions the table assumes, where that differs from `Languages`: a Greek table transcribed for English-speaking students declares _locale: en_. May be `null`.
* `UnicodeRange` — The character range the table needs, _ucs2_ or _ucs4_. SharpLouis ships a UTF-32 build, so every bundled table is usable. May be `null`.

`TranslationTable` is a record with value semantics: two instances are equal when every field is equal, with `Languages` and `TableTypes` compared element by element.

This record also has some helper methods for filtering translation tables:
* `bool MatchesLanguage(string languageTag)` — Returns `true` if any of the table's declared languages covers `languageTag`, per RFC 4647 extended filtering. Matching is asymmetric: a table declaring _en_ matches the tag _en-GB_, but one declaring _en-GB_ does not match the bare tag _en_.
* `bool MatchesRegion(string regionTag)` — Same, against the table's `Region`. Always `false` for a table that declares no region.
* `bool IsOfType(string tableType)` — Returns `true` if the table declares the given Braille type.
* `bool IsLiteraryBraille()` — Returns `true` if the current table is a literary Braille table.
* `bool IsComputerBraille()` — Returns `true` if the current table is a computer Braille table.
* `bool IsMathBraille()` — Returns `true` if the current table is a mathematical Braille table.
* `bool IsGrade(string grade)` — Returns `true` if the table declares the given Braille grade.
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

The filter methods are non-destructive — each returns a new collection and leaves the receiver unchanged — so a single populated collection can be reused for several independent queries (for example calling `ListLanguages()` on the full set and `FindLiterary()` on the same instance). A collection is also enumerable, so LINQ covers anything the built-in filters do not: `collection.Where(t => t.System == "ueb")`.

It has the following methods:

* `TableCollection PopulateFromJson()` — Parses the JSON file provided with the library and returns a table collection instance populated from this file.
* `TableCollection FindByLanguage(string languageTag)` — Returns a new collection of the tables usable for a given language, leaving the receiver unchanged. Tables declare their languages as RFC 4647 extended language ranges, and this matches `languageTag` against them the way LibLouis itself does: `FindByLanguage("en-GB")` finds the tables that declare plain _en_, and `FindByLanguage("he")` finds _he-IL.utb_. A table declaring several languages is found under each of them, so Israeli braille answers to _he_, _ar_ and _en_ alike.
* `TableCollection FindByRegion(string regionTag)` — Returns a new collection of the tables used in a given region, matched the same way. Tables that declare no region are excluded.
* `TableCollection FindLiterary()` — Returns a new collection of the tables supporting literary Braille, leaving the receiver unchanged.
* `TableCollection FindComputer()` — Same for computer Braille tables.
* `TableCollection FindByGrade(string grade)` — Returns a new collection of the tables of a given Braille grade (`"0"`, `"1"`, `"2"`, `"3"`, and the fractional grades such as `"1.5"`).
* `TranslationTable? FindByFileName(string fileName)` — Accepts a file name and finds the corresponding translation table, or `null` if no table with that file name exists.
* `Dictionary<string, string> ListLanguages()` — Searches all the tables and lists the languages supported by those tables. A table that declares several languages contributes each of them. Returns a dictionary where the key of each element is a language code and the value is its full English name.

Language and region matching is exposed on its own as the static `LanguageRange` class (`Matches(range, tag)` and `MatchesAny(ranges, tag)`), should you need to apply RFC 4647 extended filtering to something other than a table.

### Language or region?

These answer two different questions, and picking the wrong one is the easiest mistake to make with this API.

`FindByLanguage("en-GB")` returns **every English table** — UEB, American, New Zealand, the lot. That is not a bug: no LibLouis table narrows its _language_ to `en-GB`. Every English table declares plain `language: en`, and the country lives in a separate `region` field. So `FindByLanguage` is asking _"which tables can a British reader use?"_, and the honest answer is all of them.

To ask _"which tables are specifically British?"_, filter by region — and narrow to the language too:

```csharp
var british = new TableCollection()
    .PopulateFromJson()
    .FindByLanguage("en")
    .FindByRegion("en-GB");
// en-gb-comp8.ctb, en-gb-g1.utb, en_GB.tbl
```

The `FindByLanguage("en")` step is not redundant. Regions are extended language ranges too, so a bare `FindByRegion("en-GB")` also matches the two _Greek braille as used by English speakers_ tables, whose region is the broader range `en`. They really are used in Britain — they are just not English tables.

One thing to know before you build a country picker: **Unified English Braille declares no region at all**, because it is an international code rather than a national one. A region filter therefore drops `en-ueb-g1.ctb` and `en-ueb-g2.ctb`, which is exactly what the UK has used since 2011. For a UK table list you most likely want the region-`en-GB` tables _plus_ the region-less UEB ones, something like:

```csharp
var all = new TableCollection().PopulateFromJson();
var forTheUk = all.FindByLanguage("en")
    .Where(t => t.MatchesRegion("en-GB") || t.Region is null);
```

The same shape applies elsewhere: `FindByRegion("he-IL")` finds Israeli braille through its `region: *-IL`, and `FindByRegion("en-NZ")` finds the New Zealand tables through their `region: *-NZ`.

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
│       ├── LanguageRange.cs    # RFC 4647 extended language range matching
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
