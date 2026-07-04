using System.Text;
using AccessMind.SharpLouis;

// SharpLouis sample — a tiny console app that translates print text to Braille and back again.
//
// Usage:
//   SharpLouis.Sample [--table <file>] [--direction forward|back] [--text <string>]
//
//   --table, -t      Translation table file name (default: en-ueb-g2.ctb). A comma-separated
//                    list is also accepted. See src/SharpLouis/LibLouis/tables for the full set.
//   --direction, -d  "forward" (print -> Braille, the default) or "back" (Braille -> print).
//   --text, -s       The text to translate. In forward mode this is print text; in back mode it
//                    is a Unicode-Braille string (e.g. ⠓⠑⠇⠇⠕). If omitted, a demo string is used.
//   --help, -h       Show this help.
//
// Examples:
//   SharpLouis.Sample
//   SharpLouis.Sample --text "Hello, World!"
//   SharpLouis.Sample -t en-ueb-g1.ctb -s "Braille is fun"
//   SharpLouis.Sample -d back -s "⠓⠑⠇⠇⠕"

// Braille lives in the Unicode Braille Patterns block, so make sure the console can render it.
Console.OutputEncoding = Encoding.UTF8;

CommandLineOptions? options;
try {
    options = CommandLineOptions.Parse(args);
} catch (ArgumentException ex) {
    Console.Error.WriteLine(ex.Message);
    Console.Error.WriteLine();
    CommandLineOptions.PrintUsage();
    return 1;
}

if (options is null) {
    CommandLineOptions.PrintUsage();
    return 0;
}

Console.WriteLine($"LibLouis version: {BrailleTranslator.GetVersion()}");
Console.WriteLine($"Table:            {options.Table}");
Console.WriteLine($"Direction:        {(options.Backward ? "back (Braille -> print)" : "forward (print -> Braille)")}");
Console.WriteLine();

// TryCreate is the non-throwing probe: it returns false (rather than throwing) when the native
// library, the tables folder, or the requested table is missing or fails to compile.
if (!BrailleTranslator.TryCreate(options.Table, out BrailleTranslator? translator)) {
    Console.Error.WriteLine($"Could not load translation table '{options.Table}'.");
    Console.Error.WriteLine("Check the name against the files in LibLouis/tables next to this executable.");
    return 1;
}

try {
    if (options.Backward) {
        // Braille -> print. Not every table can back-translate; a table that cannot will throw.
        string text = translator.BackTranslateString(options.Text);
        Console.WriteLine($"Braille input:  {options.Text}");
        Console.WriteLine($"Back-translated: {text}");
    } else {
        // Print -> Braille, then round-trip back so you can see the translation is reversible.
        string braille = translator.TranslateString(options.Text);
        string roundTrip = translator.BackTranslateString(braille);

        Console.WriteLine($"Print input:    {options.Text}");
        Console.WriteLine($"Braille:        {braille}");
        Console.WriteLine($"Round-tripped:  {roundTrip}");
    }
} catch (LouisException ex) {
    // Thrown when the native call fails — most commonly a table that cannot back-translate.
    Console.Error.WriteLine($"Translation failed: {ex.Message}");
    return 1;
}

return 0;

/// <summary>Parsed command-line options for the sample. Returns <c>null</c> when help was requested.</summary>
internal sealed class CommandLineOptions {
    public string Table { get; private set; } = "en-ueb-g2.ctb";
    public string Text { get; private set; } = "Hello, World!";
    public bool Backward { get; private set; }

    public static CommandLineOptions? Parse(string[] args) {
        var options = new CommandLineOptions();

        for (int i = 0; i < args.Length; i++) {
            string arg = args[i];
            switch (arg) {
                case "--help" or "-h" or "-?" or "/?":
                    return null;
                case "--table" or "-t":
                    options.Table = RequireValue(args, ref i, arg);
                    break;
                case "--text" or "-s":
                    options.Text = RequireValue(args, ref i, arg);
                    break;
                case "--direction" or "-d":
                    string direction = RequireValue(args, ref i, arg);
                    options.Backward = direction.ToLowerInvariant() switch {
                        "back" or "backward" or "b" => true,
                        "forward" or "forwards" or "f" => false,
                        _ => throw new ArgumentException($"Unknown direction '{direction}'. Use 'forward' or 'back'."),
                    };
                    break;
                default:
                    throw new ArgumentException($"Unknown argument '{arg}'. Pass --help for usage.");
            }
        }

        return options;
    }

    private static string RequireValue(string[] args, ref int i, string flag) {
        if (i + 1 >= args.Length) {
            throw new ArgumentException($"Missing value for '{flag}'.");
        }

        return args[++i];
    }

    public static void PrintUsage() {
        Console.WriteLine("""
            SharpLouis sample — translate print text to Braille and back.

            Usage:
              SharpLouis.Sample [--table <file>] [--direction forward|back] [--text <string>]

              --table, -t      Table file name (default: en-ueb-g2.ctb). Comma-separated list allowed.
              --direction, -d  'forward' (print -> Braille, default) or 'back' (Braille -> print).
              --text, -s       Text to translate. Forward: print text. Back: a Unicode-Braille string.
              --help, -h       Show this help.

            Examples:
              SharpLouis.Sample --text "Hello, World!"
              SharpLouis.Sample -t en-ueb-g1.ctb -s "Braille is fun"
              SharpLouis.Sample -d back -s "⠓⠑⠇⠇⠕"
            """);
    }
}
