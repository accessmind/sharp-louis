# Contributing to SharpLouis

All contributions, big or small, are welcome — thank you for taking the time!

## Before you start

Please [open an issue](https://github.com/accessmind/sharp-louis/issues) before submitting a pull
request, so we can discuss the change and avoid duplicated work. For small, obvious fixes (typos,
documentation wording) a direct pull request is fine.

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later
- Windows x64 — currently the only supported platform, because the bundled native LibLouis library
  and translation tables are Windows x64 only

## Building and testing

```bash
# Build the solution
dotnet build SharpLouis.sln

# Build in Release mode
dotnet build SharpLouis.sln -c Release

# Run the test suite
dotnet test SharpLouis.sln
```

The test project loads the real native `liblouis.dll` and the bundled tables, so tests only run on
Windows x64.

## Coding conventions

- File-scoped namespaces, C# latest language version, nullable reference types and implicit usings
  enabled.
- Match the style of the file you are editing.
- Run `dotnet format` before opening a pull request — CI verifies formatting with
  `dotnet format --verify-no-changes` and will fail on differences.
- Add or update tests in `tests/SharpLouis.Tests` for any behavior change.
- Update `CHANGELOG.md` under the `[Unreleased]` / next-version heading for anything user-visible.
  This project follows [Semantic Versioning](https://semver.org/); note breaking changes explicitly.

## Pull requests

- Keep each pull request focused on a single change.
- Make sure the build and the full test suite pass.
- Reference the issue your pull request addresses.

## License

By contributing, you agree that your contributions are licensed under the
[Apache License, Version 2.0](LICENSE.md), the same license that covers this project.
