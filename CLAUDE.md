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
```

## Project Structure

- `SharpLouis.sln` - Solution file at repository root
- `src/SharpLouis/` - Main project directory
  - `SharpLouis.csproj` - Project file with all build configuration
  - `Wrapper.cs` - Main wrapper class with P/Invoke declarations and translation methods
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
    - `tables/` - 945+ Braille translation table files

## Key Architecture Points

### Native Interop
- P/Invoke declarations are in `Wrapper.cs` (lines 40-92)
- DLL path is hardcoded as `LibLouis\liblouis.dll` relative to executing assembly
- Uses `CallingConvention.StdCall` with `CharSet.Unicode`
- Requires `AllowUnsafeBlocks` for pointer operations

### NuGet Packaging
- Current version: 1.0.1
- Native DLL goes to `runtimes/win-x64/native/` in the package
- Tables go to `content/LibLouis/` in the package
- `build/AccessMind.SharpLouis.targets` copies files to consumer's output directory
- Targets are included in both `build/` and `buildTransitive/` for transitive dependency support
- Output paths use custom base paths: `Bin/` and `Obj/` at solution root

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
- .NET 8.0 target framework
