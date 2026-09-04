# CLAUDE.md

This file provides guidance for Claude Code when working with the SharpLouis project.

## Project Overview

SharpLouis is a .NET wrapper for LibLouis, the open-source Braille translator library. It provides P/Invoke bindings to the native LibLouis DLL and includes all translation tables.

## Build Commands

```bash
# Build the solution
dotnet build SharpLouis.sln

# Build in Release mode
dotnet build SharpLouis.sln -c Release

# Create NuGet package
dotnet pack src/SharpLouis/SharpLouis.csproj -c Release

# Run the test suite
dotnet test SharpLouis.sln

# Verify formatting the way CI does (the .NET workflow fails on any diff)
dotnet format --verify-no-changes
```

## Project Structure

- `SharpLouis.sln` - Solution file at repository root
- `README.md` - The public face of the project: badges, a Features section, the full API reference
  and the "Language or region?" guidance. Treat it as user-facing documentation, not notes — when
  the API or the bundled LibLouis version changes, it changes too
- `CHANGELOG.md` - [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) history, one section per
  released version. Packed into the NuGet package alongside the README (see the csproj `docs`
  `PackagePath`), so it must be updated *before* the release tag is pushed, not after
- `CONTRIBUTING.md` - Contribution guidance, linked from the README
- `GitVersion.yml` - Tag-driven versioning config (`ManualDeployment` mode; see the memory note on
  why `branches: main:` stays as it is)
- `src/SharpLouis/` - Main project directory
  - `SharpLouis.csproj` - Project file with all build configuration
  - `BrailleTranslator.cs` - Main translator class with P/Invoke declarations and translation methods
  - `TableCollection.cs` - Fluent API for filtering translation tables (filters are non-destructive: `FindByLanguage`/`FindLiterary` return a new collection and leave the receiver unchanged, so one populated collection can be reused for several independent queries)
  - `TranslationTable.cs` - Translation table metadata (a `sealed record` with hand-written value
    equality, because `Languages` and `TableTypes` are lists and record-synthesized equality would
    compare those by reference)
  - `LanguageRange.cs` - RFC 4647 extended-filtering match for the `language` and `region` metadata,
    which LibLouis declares as extended language ranges (`*-IL`, `akk-Latn`, `*-fonipa`)
  - `TranslationModes.cs` - Translation mode flags enum
  - `TypeForm.cs` - Typeform enum for emphasis styles
  - `NativeFunctions.cs` - Enum for native function selection
  - `LouisException.cs` - Thrown when LibLouis itself fails (a table it cannot compile, a
    translation it rejects). Missing prerequisites deliberately do *not* use it — they surface as
    `DllNotFoundException`, `DirectoryNotFoundException` or `FileNotFoundException`
  - `BrailleTranslationTable/` - Metadata structures
    - `BrailleContraction.cs` - Contraction type constants
    - `BrailleMode.cs` - Dots mode constants (6-dot, 8-dot)
    - `BrailleType.cs` - Braille type constants
    - `TranslationDirection.cs` - Translation direction constants
  - `build/AccessMind.SharpLouis.targets` - MSBuild targets for NuGet package consumers
  - `LibLouis/` - Native assets
    - `liblouis.dll` - Windows x64 native library
    - `tables.json` - Metadata for all translation tables
    - `tables/` - 400+ Braille translation table files (currently 478, from LibLouis 3.39.0)
- `tests/SharpLouis.Tests/` - xUnit test project (`net10.0-windows`)
  - `BrailleTranslatorTests.cs` - End-to-end translation tests against the real native `liblouis.dll`
  - `TableCollectionTests.cs` - `tables.json` parsing and fluent filtering
  - `TranslationTableTests.cs` - Pure metadata-predicate logic
  - `EnumTests.cs` - Guards `TranslationModes`/`TypeForm` values against `liblouis.h`
  - `LanguageRangeTests.cs` - RFC 4647 §3.3.2 matching, including the wildcard and singleton cases
  - The `.csproj` copies `liblouis.dll` (to output root) and `LibLouis/` tables into the test
    output so the translator resolves them exactly as a real consumer would
- `samples/SharpLouis.Sample/` - Runnable console demo (pick a table, direction and input string),
  linked from the README's Quick Start. It is part of the solution, so a breaking API change breaks
  the build here too — keep it compiling

## Key Architecture Points

### Native Interop
- P/Invoke declarations are in the `#region DllImport` in `BrailleTranslator.cs`
- The native library is referenced by bare name (`liblouis`) and located by the standard .NET native-library resolver (shipped as a `runtimes/win-x64/native` NuGet asset); it is not a hard-coded path, so it works next to the exe and under single-file publish
- Uses `CallingConvention.StdCall` with `CharSet.Unicode`
- Requires `AllowUnsafeBlocks` for pointer operations

### NuGet Packaging
- The package version is derived from git tags by GitVersion (`GitVersion.yml`), not set in the csproj
- Native DLL goes to `runtimes/win-x64/native/` in the package
- Tables go to `content/LibLouis/` in the package
- `build/AccessMind.SharpLouis.targets` copies files to consumer's output directory
- Targets are included in both `build/` and `buildTransitive/` for transitive dependency support

### Build Configuration (CI-scoped)
Several build settings are deliberately **scoped to CI** via `Condition="'$(GITHUB_ACTIONS)' == 'true'"`,
because CI (full git history, `github.com` remote) is the warning-clean source of truth while local
developer builds stay lenient. Do **not** remove this scoping to make local builds behave like CI.
- `TreatWarningsAsErrors` — on only in CI, so environment-specific warnings don't hard-fail local builds.
- `ContinuousIntegrationBuild` — on only in CI (deterministic/normalized build paths).
- **Symbols & SourceLink** — the package ships `snupkg` symbols (`IncludeSymbols` +
  `SymbolPackageFormat`) with `Microsoft.SourceLink.GitHub` so consumers can step into library source
  from GitHub. `EnableSourceLink` is turned **off** off CI: a developer's git remote may be an SSH host
  alias (e.g. `github:`) that SourceLink's GitHub provider can't resolve, which otherwise emits a benign
  "source control information is not available" warning. SourceLink only matters for the CI-published
  package, so local builds skip it.

### Continuous Integration
`.github/workflows/dotnet.yml` runs on pushes to `master` and on pull requests: restore,
`dotnet format --verify-no-changes`, build, test. The formatting check is a hard gate, so run it
locally before pushing. The README's build-status badge is wired to this file **by name**
(`actions/workflows/dotnet.yml/badge.svg?branch=master`) — renaming the workflow file or the default
branch silently breaks the badge, so update the README in the same commit if you ever do.

### Release Process
Cutting a release is deliberate but fully automatic once started: push a `v*` git tag (GitVersion
resolves the version from it) and the `.github/workflows/release.yml` workflow does everything else,
publishing without further confirmation. **Pushing the tag is the point of no return** — nuget.org
versions are immutable. Two config files are involved — they share the name `release.yml` but are
unrelated:

- `.github/workflows/release.yml` — the GitHub Actions **workflow** (only files under
  `workflows/` run). Two jobs:
  1. `build-test` — runs automatically on the tag: restore, build, test, pack. Its **Show packed
     package** step writes the real `.nupkg` filename (with its GitVersion-resolved version) to the
     run summary, and uploads the package as a build artifact so `publish` reuses the identical bytes.
  2. `publish` — `needs: build-test` and declares `environment: nuget`. It runs straight after
     `build-test` with no approval pause; the `environment:` key stays because the nuget.org
     trusted-publishing policy pins it (removing it breaks the OIDC exchange). It publishes to
     NuGet via **Trusted Publishing** (see below) and creates the GitHub release. It has no
     checkout; `gh` locates the repo via `GH_REPO` and generates notes server-side. A tag with a
     `-` suffix (e.g. `v1.2.3-beta.1`) is marked `--prerelease`.
- `.github/release.yml` — **not** a workflow. GitHub reads this exact path to group the
  auto-generated release notes by PR label (`--generate-notes` consumes it). Categories: Breaking
  Changes, New Features (`feature`), Enhancements (`enhancement`), Bug Fixes, Documentation,
  Dependencies, Other. Dependabot PRs are excluded by author, so the Dependencies category only
  collects manual dependency bumps (e.g. a LibLouis native DLL refresh) labeled `dependencies`.

**Trusted Publishing (no stored API key):** the `publish` job uses `NuGet/login@v1` with
`permissions: id-token: write` to exchange a GitHub OIDC token for a short-lived (1 hour,
single-use) nuget.org API key at push time. There is deliberately **no `NUGET_API_KEY` secret** —
nothing long-lived to leak or rotate. The security comes from a trusted-publishing policy on
nuget.org bound to the repo owner ID, and it must be configured before the first tag or the push
step fails.

Required setup (one-time):
- A `nuget` environment (Settings → Environments). It must exist because the trusted-publishing
  policy pins it by name, but it deliberately has **no required reviewers** — releases publish
  unattended. Configure:
  - **Deployment branches and tags** set to *Selected branches and tags* with a **Tag** rule
    `v*` — defense-in-depth so the environment can only ever be entered from a version tag, even if
    a future workflow or trigger change tried to reach `environment: nuget` from a branch. `*` is a
    glob (matches any run of characters except `/`), not regex.
- A trusted-publishing policy on nuget.org (username → Trusted Publishing): Repository Owner,
  Repository `sharp-louis`, Workflow File `release.yml` (filename only, no path), Environment
  `nuget`.
- A `NUGET_USER` **environment secret** on `nuget` holding the nuget.org profile name (not the
  email). It is not a credential — the policy binding is what authorizes publishing — but a secret
  keeps it masked in the public repo's logs rather than a plaintext variable.

NuGet versions are immutable once pushed and nothing stands between the tag and the push, so verify
the version GitVersion will resolve **before** creating the tag. The **Show packed package** summary
is a record of what shipped, not a chance to stop it.

### Translation Tables
- Tables are loaded from `LibLouis\tables\` relative to the DLL
- `tables.json` contains metadata parsed by `TableCollection.PopulateFromJson()`. It is generated by
  the **lljt** tool in the sibling `liblouis-jsonify-tables` repo, so a schema change there and the
  matching change to `TranslationTable` must land together
- LibLouis metadata keys **may repeat within one table** (the manual says so explicitly): 22 bundled
  tables declare several `language:` values (`he-IL.utb` declares he/ar/en; `ancient-languages-us.utb`
  declares 36) and 8 declare two `type:` values. Never model a metadata key as single-valued without
  checking the corpus first
- Table files have extensions: `.ctb`, `.utb`, `.cti`, `.uti`, `.dis`, `.dic`, `.tbl`
- The bundled LibLouis version and the table count are written out in **three** places that must move
  together on an upgrade: this file (Project Structure), the README's **Features** section, and the
  README's **Limitations and Particularities** section. Currently 3.39.0 / 478 tables

## Current Limitations

- Windows x64 only (single `liblouis.dll`)
- Fixed translation mode: `NoUndefined | UnicodeBraille | DotsInputOutput`
- UTF-32 LibLouis build
- Platform is restricted to x64 in project file

## Code Style

- File-scoped namespaces
- C# latest language version
- Nullable reference types enabled
- Implicit usings enabled
- .NET 10.0 target framework (plain `net10.0`, so any net10.0 consumer can reference it). The
  Windows-only native dependency is expressed via the `runtimes/win-x64/native` NuGet asset and an
  assembly-level `[SupportedOSPlatform("windows")]` (declared as an `AssemblyAttribute` item in
  `SharpLouis.csproj`, not in a separate `AssemblyInfo.cs`), which gives cross-platform
  callers a CA1416 hint rather than a hard reference block. The test project stays `net10.0-windows`
  because it exercises the native library and only runs on Windows.
