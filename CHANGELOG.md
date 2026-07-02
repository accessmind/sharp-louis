# SharpLouis Change Log

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Fixed 🐛

- Fixed native buffer handling in `TranslateString`/`BackTranslateString` that could corrupt memory or crash with an `AccessViolationException` on longer inputs: the output length is now passed to LibLouis as a count of `widechar` elements rather than bytes, and the unused `typeform`/`spacing` arguments are now passed as `NULL` instead of empty buffers.
- `Wrapper.Create()` now reliably returns `null` when the native `liblouis` library is missing or cannot be loaded, instead of throwing.
- `Wrapper.GetVersion()` now returns the native LibLouis version instead of throwing `NotImplementedException` (and no longer frees LibLouis-owned memory).
- `TableCollection` now resolves `tables.json` against the application base directory, so it works regardless of the current working directory.
- `TableCollection.ListLanguages()` no longer throws when a table uses a language code that is not a recognized .NET culture (for example `awa` or `cop`).
- `TableCollection.FindByFileName()` now returns `TranslationTable?` and yields `null` on a miss instead of a default-initialized struct.

### Changed ⚠️

- `TranslationTable.ContractionType`, `TableType`, and `Direction` are now nullable to reflect tables that declare no such metadata; a table with no declared direction is treated as bidirectional.
- Renamed `TranslationTable.isEightDot()` to `IsEightDot()` and `TranslationDirection.both` to `TranslationDirection.Both` for naming consistency (breaking change).
- Removed debug-only scaffolding from the public surface and internals (the `TypeForm.Hex5c5c` member, buffer-pinning checks, and diagnostic helpers).
- The package now ships XML documentation and a symbol package, and enables SourceLink and deterministic builds.

## [1.0.1]

### Fixed 🐛

- [Fix LibLouis bundling](https://github.com/accessmind/sharp-louis/pull/1)

## [1.0.0]

- Initial release.
