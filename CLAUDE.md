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
```

## Project Structure

- `SharpLouis.sln` - Solution file at repository root
- `src/SharpLouis/` - Main project directory
  - `SharpLouis.csproj` - Project file with all build configuration
  - `BrailleTranslator.cs` - Main translator class with P/Invoke declarations and translation methods
  - `TableCollection.cs` - Fluent API for filtering translation tables
  - `TranslationTable.cs` - Translation table metadata structure
  - `TranslationModes.cs` - Translation mode flags enum
  - `TypeForm.cs` - Typeform enum for emphasis styles
  - `NativeFunctions.cs` - Enum for native function selection
  - `BrailleTranslationTable/` - Metadata structures
    - `BrailleContraction.cs` - Contraction type constants
    - `BrailleMode.cs` - Dots mode constants (6-dot, 8-dot)
    - `BrailleType.cs` - Braille type constants
    - `TranslationDirection.cs` - Translation direction constants
  - `build/AccessMind.SharpLouis.targets` - MSBuild targets for NuGet package consumers
  - `LibLouis/` - Native assets
    - `liblouis.dll` - Windows x64 native library
    - `tables.json` - Metadata for all translation tables
    - `tables/` - 400+ Braille translation table files (currently 472)
- `tests/SharpLouis.Tests/` - xUnit test project (`net10.0-windows`)
  - `BrailleTranslatorTests.cs` - End-to-end translation tests against the real native `liblouis.dll`
  - `TableCollectionTests.cs` - `tables.json` parsing and fluent filtering
  - `TranslationTableTests.cs` - Pure metadata-predicate logic
  - `EnumTests.cs` - Guards `TranslationModes`/`TypeForm` values against `liblouis.h`
  - The `.csproj` copies `liblouis.dll` (to output root) and `LibLouis/` tables into the test
    output so the translator resolves them exactly as a real consumer would

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

### Release Process
Cutting a release is deliberate and human-gated: push a `v*` git tag (GitVersion resolves the
version from it) and the `.github/workflows/release.yml` workflow does the rest. Two config files
are involved — they share the name `release.yml` but are unrelated:

- `.github/workflows/release.yml` — the GitHub Actions **workflow** (only files under
  `workflows/` run). Two jobs:
  1. `build-test` — runs automatically on the tag: restore, build, test, pack. Its **Show packed
     package** step writes the real `.nupkg` filename (with its GitVersion-resolved version) to the
     run summary, and uploads the package as a build artifact so `publish` reuses the identical bytes.
  2. `publish` — `needs: build-test` and declares `environment: nuget`, so the run **pauses for a
     manual approval** before anything ships. On approval it publishes to NuGet via **Trusted
     Publishing** (see below) and creates the GitHub release. It has no checkout; `gh` locates the
     repo via `GH_REPO` and generates notes server-side. A tag with a `-` suffix (e.g.
     `v1.2.3-beta.1`) is marked `--prerelease`.
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
- A `nuget` environment (Settings → Environments) with:
  - **Required reviewers** enabled — this enforces the approval pause (free on this public repo). It
    also scopes the OIDC exchange: because the trusted-publishing policy pins the `nuget`
    environment, a temporary key can only be minted from the gated, post-approval job.
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

NuGet versions are immutable once pushed; the summary + approval gate exist to catch a wrong version
before it becomes permanent.

### Translation Tables
- Tables are loaded from `LibLouis\tables\` relative to the DLL
- `tables.json` contains metadata parsed by `TableCollection.PopulateFromJson()`
- Table files have extensions: `.ctb`, `.utb`, `.cti`, `.uti`, `.dis`, `.dic`, `.tbl`

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
  assembly-level `[SupportedOSPlatform("windows")]` (see `AssemblyInfo.cs`), which gives cross-platform
  callers a CA1416 hint rather than a hard reference block. The test project stays `net10.0-windows`
  because it exercises the native library and only runs on Windows.
