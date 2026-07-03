# SharpLouis Change Log

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.0] - Unreleased

This release contains several breaking API changes (see **Changed** below), so it is a major version
bump from 1.1.0.

### Fixed 🐛

- Fixed native buffer handling in `TranslateString`/`BackTranslateString` that could corrupt memory or crash with an `AccessViolationException` on longer inputs: the output length is now passed to LibLouis as a count of `widechar` elements rather than bytes, and the unused `typeform`/`spacing` arguments are now passed as `NULL` instead of empty buffers.
- `Wrapper.GetVersion()` now returns the native LibLouis version instead of throwing `NotImplementedException` (and no longer frees LibLouis-owned memory).
- `Wrapper.Create()` now validates each requested table by its bundled file name only and hands LibLouis exactly the names it validated, so a rooted or relative path can no longer bypass the bundled-table check, and stray whitespace in a comma-separated list no longer passes validation only to fail inside LibLouis.
- The native `liblouis` load probe in `Wrapper.Create()` now releases its load reference, so repeated wrapper creation no longer leaks a module reference.
- `TableCollection` now resolves `tables.json` against the application base directory, so it works regardless of the current working directory.
- `TableCollection.ListLanguages()` no longer throws when a table uses a language code that is not a recognized .NET culture (for example `awa` or `cop`).
- `TableCollection.FindByFileName()` now returns `TranslationTable?` and yields `null` on a miss instead of a default-initialized struct.
- Translation tables are now found even when the application runs from a directory with a non-ASCII path (for example a non-Latin Windows user name). LibLouis takes table paths as an ANSI `char*`, which would corrupt such a path; the wrapper now hands it the ASCII 8.3 short-path form instead.

### Changed ⚠️

- Upgraded the target framework to .NET 10. The library targets plain `net10.0` (so any `net10.0`
  consumer can reference it) and declares its Windows-only native dependency via
  `[assembly: SupportedOSPlatform("windows")]` plus the `runtimes/win-x64/native` asset, which gives
  cross-platform callers a compile-time hint rather than a hard reference block.
- `Wrapper` no longer implements `IDisposable` (breaking change). A wrapper owns no per-instance
  unmanaged resource, so there was nothing to dispose; the previous `Dispose()` called the
  process-global `lou_free`, which could wipe the shared table cache used by other live wrappers.
  Remove `using` from wrapper creation.
- Renamed the static `Wrapper.Free()` to `Wrapper.ClearTableCache()` (breaking change) and documented
  it as the optional, process-global table-cache reset it actually is.
- The translation methods now return the result `string` and throw `LouisException` on a genuine
  native failure, instead of returning `bool` with an `out` parameter and silently swallowing the
  reason (breaking change). `CharsToDots`, `DotsToChars`, `TranslateString`, `BackTranslateString`,
  `TranslateStringWithTypeForms(string, TypeForm[])`, and
  `BackTranslateStringWithTypeForms(string) -> (string Text, TypeForm[] TypeForms)` are affected.
- `Wrapper.Create()` now throws a descriptive exception (`ArgumentException`, `DllNotFoundException`,
  `DirectoryNotFoundException`, `FileNotFoundException`, or `LouisException`) instead of returning a
  bare `null`, and validates/compiles the requested table up front so failures surface at creation
  rather than on the first translation (breaking change). A new `Wrapper.TryCreate(string, out Wrapper?)`
  provides the non-throwing probe.
- All native calls are now serialized internally, so a `Wrapper` is safe to share across threads.
- Output buffers now grow and retry automatically, so long inputs can no longer be silently truncated.
- `TranslationTable.ContractionType`, `TableType`, and `Direction` are now nullable to reflect tables that declare no such metadata; a table with no declared direction is treated as bidirectional.
- Renamed `TranslationTable.isEightDot()` to `IsEightDot()` and `TranslationDirection.both` to `TranslationDirection.Both` for naming consistency (breaking change).
- Removed debug-only scaffolding from the public surface and internals (the `TypeForm.Hex5c5c` member, buffer-pinning checks, and diagnostic helpers).
- The package now ships XML documentation and a symbol package, and enables SourceLink and deterministic builds.
- Updated the bundled LibLouis native library and translation tables to LibLouis 3.38.

### Added ✨

- Added an xUnit test suite (`tests/SharpLouis.Tests`) covering end-to-end native translation,
  table-collection filtering, metadata predicates, and the enum ABI values.

## [1.1.0] - 2025-12-27

### Fixed 🐛

- Corrected how the native library and translation tables are delivered to consumers, via
  [Fix LibLouis bundling](https://github.com/accessmind/sharp-louis/pull/1) and
  [Fix LibLouis NuGet package DLL placement](https://github.com/accessmind/sharp-louis/pull/4).

## [1.0.0] - 2024-06-04

- Initial release.

[2.0.0]: https://github.com/accessmind/sharp-louis/compare/v1.1.0...HEAD
[1.1.0]: https://github.com/accessmind/sharp-louis/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/accessmind/sharp-louis/releases/tag/v1.0.0
