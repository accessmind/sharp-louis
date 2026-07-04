# SharpLouis samples

Runnable examples that show how to consume the SharpLouis library.

## SharpLouis.Sample

A small console app that translates print text to Braille and back again, letting you pick the
translation table, the direction, and the input string.

```bash
# From the repository root:
dotnet run --project samples/SharpLouis.Sample

# Translate your own text (forward: print -> Braille, with a round-trip back):
dotnet run --project samples/SharpLouis.Sample -- --text "Hello, World!"

# Use a different table (grade-1 UEB here):
dotnet run --project samples/SharpLouis.Sample -- -t en-ueb-g1.ctb -s "Braille is fun"

# Back-translate a Unicode-Braille string to print:
dotnet run --project samples/SharpLouis.Sample -- -d back -s "⠓⠑⠇⠇⠕"
```

* `--table`, `-t` — Table file name (default `en-ueb-g2.ctb`). A comma-separated list is accepted.
* `--direction`, `-d` — `forward` (print → Braille, default) or `back` (Braille → print).
* `--text`, `-s` — Text to translate. Forward: print text. Back: a Unicode-Braille string.
* `--help`, `-h` — Show usage.

The available table file names are in [`src/SharpLouis/LibLouis/tables`](../src/SharpLouis/LibLouis/tables).
Note that not every table can back-translate — see the table-filtering notes in the top-level
[README](../README.md).

> This sample references the library via a `ProjectReference` and copies the native `liblouis.dll`
> and the translation tables into its output directory (see the comments in its `.csproj`). A real
> application that installs the `AccessMind.SharpLouis` NuGet package gets all of that automatically.
