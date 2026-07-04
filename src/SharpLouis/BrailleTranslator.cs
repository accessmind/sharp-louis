using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace AccessMind.SharpLouis;

/// <summary>
/// SharpLouis, .NET wrapper for the LibLouis Braille Translator library
/// Copyright © 2024–2026 AccessMind LLC.
/// Licensed under the Apache License, Version 2.0 (the "License");
/// you may not use this file except in compliance with the License.
/// You may obtain a copy of the License at
/// http://www.apache.org/licenses/LICENSE-2.0
/// Unless required by applicable law or agreed to in writing,
/// software distributed under the License is distributed on an "AS IS" BASIS,
/// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
/// See the License for the specific language governing permissions and limitations under the License.
///
/// The main Braille translator. Use <see cref="Create"/> (or <see cref="TryCreate"/>) to obtain an
/// instance bound to one or more translation tables.
///
/// A translator is a cheap, name-scoped handle: it owns no unmanaged resource of its own, so there is
/// nothing to dispose. LibLouis keeps a single process-global cache of compiled tables (keyed by the
/// table-list string), so translators for different tables coexist and never interfere. All native
/// calls are serialized internally, so a translator is safe to share across threads. That global cache
/// normally lives for the lifetime of the process — call <see cref="ClearTableCache"/> only to reclaim
/// its memory or to force tables to be recompiled from disk.
/// </summary>
public sealed class BrailleTranslator {
    const int TranslationMode = (int)(TranslationModes.NoUndefined | TranslationModes.UnicodeBraille | TranslationModes.DotsInputOutput); // Common for all member functions
    const int BackTranslationMode = 0; // The "mode" parameter is deprecated during backtranslation and must be set to 0 !!

    /// <summary>
    /// Absolute path to the folder holding the LibLouis translation tables, resolved against the
    /// application base directory (single-file-publish safe). Used for the managed file-existence
    /// checks, which handle Unicode paths natively.
    /// </summary>
    private static readonly string TablesFolder = Path.Combine(AppContext.BaseDirectory, "LibLouis", "tables");

    /// <summary>
    /// ASCII-safe form of <see cref="TablesFolder"/> that is prefixed onto the table name handed to
    /// LibLouis. LibLouis takes the table list as an ANSI <c>char*</c> and opens files with the C
    /// runtime, so a non-ASCII directory (for example a non-Latin user name) would be corrupted; the
    /// 8.3 short form is ASCII and round-trips cleanly. See <see cref="ResolveTableSearchPath"/>.
    /// </summary>
    private static readonly string AsciiTablesFolder = ResolveTableSearchPath(TablesFolder);

    /// <summary>
    /// The native LibLouis library, referenced by bare name so the standard .NET native-library
    /// resolver finds it (shipped as a normal <c>runtimes/win-x64/native</c> NuGet asset, like any
    /// other native dependency). No hard-coded path — works next to the exe and under single-file publish.
    /// </summary>
    private const string LibLouisDll = "liblouis";

    /// <summary>
    /// Serializes every call into LibLouis. LibLouis's compiled-table cache is process-global and is not
    /// safe for concurrent table compilation, so all native entry points below take this lock.
    /// </summary>
    private static readonly Lock NativeLock = new();

    private static readonly char[] NullChars = ['\0'];

    // Process-global native constants, queried once (LibLouis's widechar size depends only on how the
    // native library was built, never on the table). Populated by EnsureNativeInfo before any instance.
    private static int nativeCharSize;
    private static Encoding? nativeEncoding;

    #region DllImport
    [DllImport(LibLouisDll, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    private static extern int lou_charSize();

    // Returns a pointer to a static string owned by liblouis. It must NOT be freed, so the return is
    // marshalled as IntPtr (marshalling it directly as string would make the CLR free liblouis's memory).
    [DllImport(LibLouisDll, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    private static extern IntPtr lou_version();

    // Compiles (and caches) a table list, returning a non-null handle on success or NULL on failure.
    // Used to validate and warm a table at Create time. The returned pointer is owned by liblouis.
    [DllImport(LibLouisDll, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    private static extern IntPtr lou_getTable([In][MarshalAs(UnmanagedType.LPStr)] string tableList);

    // Win32: resolves a (possibly non-ASCII) long path to its 8.3 short form, which is ASCII and thus
    // safe to hand to liblouis's ANSI path API. Resolves to GetShortPathNameW via CharSet.Unicode.
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint GetShortPathName(string lpszLongPath, [Out] char[]? lpszShortPath, uint cchBuffer);

    [DllImport(LibLouisDll, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    private static extern int lou_charToDots(
        [In][MarshalAs(UnmanagedType.LPStr)] string tableList,
        [In][MarshalAs(UnmanagedType.LPArray)] byte[] inbuf,
        [Out][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] outbuf,
        [In] int length,
        [In] int mode
    );

    [DllImport(LibLouisDll, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    private static extern int lou_dotsToChar(
    [In][MarshalAs(UnmanagedType.LPStr)] string tableList,
    [In][MarshalAs(UnmanagedType.LPArray)] byte[] inbuf,
    [Out][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] outbuf,
    [In] int length,
    [In] int mode
    );

    [DllImport(LibLouisDll, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    private static extern void lou_free();

    [DllImport(LibLouisDll, CharSet = CharSet.Unicode)]
    private static extern unsafe int lou_translateString(
        [In][MarshalAs(UnmanagedType.LPStr)] string tableList, // const char *tableList
        [In] byte[] inbuf,                                     // const widechar *inbuf
        [In, Out] IntPtr inlen,                                // int *inlen
        [Out] byte[] outbuf,                                   // widechar *outbuf
        [In, Out] IntPtr outlen,                               // int *outlen
        [In] TypeForm[]? typeform,                             // formtype *typeform (may be NULL)
        [MarshalAs(UnmanagedType.LPStr)] string? spacing,      // char *spacing (may be NULL)
        int mode                                               //  int mode
 );

    [DllImport(LibLouisDll, CharSet = CharSet.Unicode)]
    private static extern unsafe int lou_backTranslateString(
        [In][MarshalAs(UnmanagedType.LPStr)] string tableList, // const char *tableList
        [In] byte[] inbuf,                                     // const widechar *inbuf
        [In, Out] IntPtr inlen,                                // int *inlen
        [Out] byte[] outbuf,                                   // widechar *outbuf
        [In, Out] IntPtr outlen,                               // int *outlen
        [In, Out] TypeForm[]? typeform,                        // formtype *typeform (may be NULL)
        [MarshalAs(UnmanagedType.LPStr)] string? spacing,      // char *spacing (may be NULL)
        int mode                                               //  int mode
 );
    #endregion

    // Member state: the table list as the caller gave it (for messages) and the ASCII-safe, folder-
    // prefixed form actually handed to LibLouis.
    private readonly string tableNames;
    private readonly string tablePaths;

    /// <summary>Private constructor. Use <see cref="Create"/> / <see cref="TryCreate"/> from the outside.</summary>
    /// <param name="tableNames">The table list exactly as the caller gave it; kept only for messages.</param>
    /// <param name="normalizedNames">
    /// The comma-separated list of bundled base file names <see cref="Create"/> has already validated.
    /// This is the form actually handed to LibLouis, so it must contain no directory or rooted path.
    /// </param>
    private BrailleTranslator(string tableNames, string normalizedNames) {
        this.tableNames = tableNames;
        // Prefix the (ASCII-safe) tables folder onto the list. Per LibLouis, only the first name needs
        // the folder; the rest of the list and any included tables resolve relative to it. The names are
        // already reduced to bundled base file names, so no rooted path or '..' segment can leak through.
        this.tablePaths = Path.Combine(AsciiTablesFolder, normalizedNames);
    }

    /// <summary>
    /// Translates print text to a dot-pattern string (LibLouis <c>lou_charToDots</c>), one cell per
    /// input character.
    /// </summary>
    public string CharsToDots(string chars) => InvokeNative(NativeFunction.CharsToDots, chars, [], out _);

    /// <summary>Inverse of <see cref="CharsToDots"/> (LibLouis <c>lou_dotsToChar</c>).</summary>
    public string DotsToChars(string dots) => InvokeNative(NativeFunction.DotsToChars, dots, [], out _);

    /// <summary>Translates text to Unicode Braille using the translator's table(s).</summary>
    public string TranslateString(string text) => InvokeNative(NativeFunction.TranslateString, text, [], out _);

    /// <summary>
    /// Translates text to Unicode Braille applying per-character emphasis. <paramref name="typeForms"/>
    /// is indexed like <paramref name="text"/>; shorter arrays leave the remaining characters plain.
    /// </summary>
    public string TranslateStringWithTypeForms(string text, TypeForm[] typeForms) {
        ArgumentNullException.ThrowIfNull(typeForms);
        return InvokeNative(NativeFunction.TranslateStringTfe, text, typeForms, out _);
    }

    /// <summary>Translates Unicode Braille back to text using the translator's table(s).</summary>
    public string BackTranslateString(string braille) => InvokeNative(NativeFunction.BackTranslateString, braille, [], out _);

    /// <summary>
    /// Back-translates Unicode Braille to text and reports the per-character emphasis LibLouis inferred.
    /// </summary>
    /// <returns>The recovered text and a typeform array indexed like that text.</returns>
    public (string Text, TypeForm[] TypeForms) BackTranslateStringWithTypeForms(string braille) {
        string text = InvokeNative(NativeFunction.BackTranslateStringTfe, braille, [], out TypeForm[] typeForms);
        return (text, typeForms);
    }

    /// <summary>
    /// Gets the version string of the underlying native LibLouis library.
    /// </summary>
    /// <returns>The LibLouis version, for example <c>3.38.0</c>.</returns>
    public static string GetVersion() {
        lock (NativeLock) {
            return Marshal.PtrToStringAnsi(lou_version()) ?? string.Empty;
        }
    }

    /// <summary>
    /// Releases LibLouis's <b>process-global</b> cache of compiled translation tables (the native
    /// <c>lou_free</c> call). This affects every translator in the process, not just one instance, so it
    /// is normally unnecessary: the cache is cheap to keep and repopulates automatically on the next
    /// translation. Call it only to reclaim that memory, or to force tables to be recompiled from disk
    /// after their files have changed at runtime.
    /// </summary>
    public static void ClearTableCache() {
        lock (NativeLock) {
            lou_free();
        }
    }

    /// <summary>
    /// Creates a translator bound to <paramref name="tableNames"/> (a single table file name such as
    /// <c>"en-ueb-g1.ctb"</c>, or a comma-separated list). The table is compiled up front so failures
    /// surface here rather than on the first translation.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="tableNames"/> is null, empty or whitespace.</exception>
    /// <exception cref="DllNotFoundException">The native <c>liblouis</c> library cannot be loaded (for example off Windows/x64).</exception>
    /// <exception cref="DirectoryNotFoundException">The bundled tables folder is missing.</exception>
    /// <exception cref="FileNotFoundException">A named table file does not exist.</exception>
    /// <exception cref="LouisException">LibLouis could not compile the requested table(s).</exception>
    public static BrailleTranslator Create(string tableNames) {
        if (string.IsNullOrWhiteSpace(tableNames)) {
            throw new ArgumentException("A table file name (or comma-separated list) is required.", nameof(tableNames));
        }

        // Probe the native library the same way DllImport would, so an absent/unloadable liblouis.dll
        // fails with a clear message instead of a first-call DllNotFoundException/BadImageFormatException.
        if (!NativeLibrary.TryLoad(LibLouisDll, typeof(BrailleTranslator).Assembly, null, out IntPtr libHandle)) {
            throw new DllNotFoundException(
                $"The native '{LibLouisDll}' library could not be loaded. SharpLouis ships it for Windows x64; " +
                "ensure the native asset is present next to your application.");
        }

        // A successful probe adds a load reference. Release it so repeated Create calls don't leak the
        // module: DllImport keeps its own reference for the actual P/Invoke calls, so liblouis stays loaded.
        NativeLibrary.Free(libHandle);

        if (!Directory.Exists(TablesFolder)) {
            throw new DirectoryNotFoundException($"LibLouis tables folder not found: {TablesFolder}");
        }

        // Only bundled tables are supported. Reduce each entry to its base file name (dropping any
        // directory, rooted path or '..' segment) and require that exact file in the tables folder. The
        // very same normalized names are what we hand to LibLouis below, so the managed validation and the
        // native call can never disagree — a rooted or '..' path can no longer bypass the check, and stray
        // whitespace can no longer pass validation only to fail inside LibLouis.
        var shortNames = new List<string>();
        foreach (string name in tableNames.Split(',')) {
            string shortName = Path.GetFileName(name.Trim());
            if (shortName.Length == 0) {
                throw new ArgumentException($"Empty table name in list '{tableNames}'.", nameof(tableNames));
            }

            string fullPath = Path.Combine(TablesFolder, shortName);
            if (!File.Exists(fullPath)) {
                throw new FileNotFoundException($"Translation table not found: {shortName}", fullPath);
            }

            shortNames.Add(shortName);
        }

        EnsureNativeInfo();
        var translator = new BrailleTranslator(tableNames, string.Join(",", shortNames));

        // Compile + cache the table now: fail fast on a broken table, and keep the first real
        // translation as fast as the rest.
        lock (NativeLock) {
            if (lou_getTable(translator.tablePaths) == IntPtr.Zero) {
                throw new LouisException($"LibLouis could not compile the table list '{tableNames}'.");
            }
        }

        return translator;
    }

    /// <summary>
    /// Non-throwing variant of <see cref="Create"/>. Returns <see langword="false"/> (with
    /// <paramref name="translator"/> set to <see langword="null"/>) if the native library, tables folder
    /// or a requested table is missing, or the table fails to compile.
    /// </summary>
    public static bool TryCreate(string tableNames, [NotNullWhen(true)] out BrailleTranslator? translator) {
        try {
            translator = Create(tableNames);
            return true;
        } catch (Exception e) when (e is ArgumentException or DllNotFoundException or BadImageFormatException or IOException or LouisException) {
            translator = null;
            return false;
        }
    }

    private static void EnsureNativeInfo() {
        if (nativeCharSize > 0) {
            return;
        }

        lock (NativeLock) {
            if (nativeCharSize > 0) {
                return;
            }

            int size = lou_charSize();
            nativeEncoding = GetEncoding(size);
            nativeCharSize = size;
        }
    }

    /// <summary>
    /// Returns a form of <paramref name="path"/> that is safe to hand to LibLouis's ANSI (char*) table
    /// path. An already-ASCII path is returned unchanged; otherwise its Windows 8.3 short form (which is
    /// ASCII) is used. Falls back to the original path when no short name is available (8.3 generation
    /// disabled for the volume) — the best a managed wrapper can do in that case.
    /// </summary>
    internal static string ResolveTableSearchPath(string path) {
        if (Ascii.IsValid(path)) {
            return path;
        }

        uint size = GetShortPathName(path, null, 0); // Query the required buffer size (incl. terminator).
        if (size == 0) {
            return path;
        }

        var buffer = new char[size];
        uint written = GetShortPathName(path, buffer, size);
        if (written == 0 || written >= size) {
            return path;
        }

        string shortPath = new(buffer, 0, (int)written);
        return Ascii.IsValid(shortPath) ? shortPath : path;
    }

    /// <summary>
    /// The single place holding all the marshalling and pinning. Runs one of the native functions,
    /// growing the output buffer and retrying if LibLouis reports it filled the buffer (so output is
    /// never silently truncated), and returns the decoded string. Throws <see cref="LouisException"/>
    /// if the native call reports failure.
    /// </summary>
    private string InvokeNative(NativeFunction nativeFunction, string input, TypeForm[] tfeInput, out TypeForm[] tfeOutput) {
        tfeOutput = [];
        if (input.Length == 0) {
            return string.Empty; // Every operation maps the empty string to the empty string.
        }

        Encoding encoding = nativeEncoding!;
        int charSize = nativeCharSize;
        byte[] inBuf = encoding.GetBytes(input);
        int inputLength = input.Length;
        bool lengthKnown = OutputLengthIsKnown(nativeFunction);
        bool usesTypeForms = UsesTypeForms(nativeFunction);

        // Generous starting capacity (back-translation can expand contractions); grown on demand below.
        int capacity = Math.Max((inputLength * 3) + 64, 1024);
        const int MaxCapacity = 1 << 24; // 16M widechars — a runaway guard, far past any real translation.

        while (true) {
            byte[] outBuf = new byte[capacity * charSize];
            TypeForm[] tfeBuf = CreateTypeFormBuffer(usesTypeForms ? capacity : 0, tfeInput);
            int inLen = inputLength;
            int outLen = capacity; // On return, LibLouis overwrites this with the widechars actually written.
            int result;

            unsafe {
                IntPtr inPtr = new(&inLen);
                IntPtr outPtr = new(&outLen);
                lock (NativeLock) {
                    fixed (byte* pInBuf = inBuf, pOutBuf = outBuf)  // Prevent the GC from moving the buffers
                    fixed (TypeForm* pTfeBuf = tfeBuf) {            // during the native call.
                        result = nativeFunction switch {
                            NativeFunction.CharsToDots => lou_charToDots(tablePaths, inBuf, outBuf, inputLength, TranslationMode),
                            NativeFunction.DotsToChars => lou_dotsToChar(tablePaths, inBuf, outBuf, inputLength, BackTranslationMode),
                            NativeFunction.TranslateString => lou_translateString(tablePaths, inBuf, inPtr, outBuf, outPtr, null, null, TranslationMode),
                            NativeFunction.TranslateStringTfe => lou_translateString(tablePaths, inBuf, inPtr, outBuf, outPtr, tfeBuf, null, TranslationMode),
                            NativeFunction.BackTranslateString => lou_backTranslateString(tablePaths, inBuf, inPtr, outBuf, outPtr, null, null, BackTranslationMode),
                            NativeFunction.BackTranslateStringTfe => lou_backTranslateString(tablePaths, inBuf, inPtr, outBuf, outPtr, tfeBuf, null, BackTranslationMode),
                            _ => 0,
                        };
                    }
                }
            }

            if (result != 1) {
                throw new LouisException($"LibLouis '{nativeFunction}' failed for table list '{tableNames}'.");
            }

            if (lengthKnown) {
                // LibLouis treats a too-small buffer as a successful partial translation with
                // outLen == capacity. Grow and retry so the caller always gets the full result.
                if (outLen >= capacity && capacity < MaxCapacity) {
                    capacity = Math.Min(capacity * 2, MaxCapacity);
                    continue;
                }

                if (usesTypeForms) {
                    tfeOutput = new TypeForm[outLen];
                    Array.Copy(tfeBuf, tfeOutput, outLen);
                }

                return encoding.GetString(outBuf, 0, outLen * charSize).TrimEnd(NullChars);
            }

            // charToDots / dotsToChar are 1:1 and report no length: exactly inputLength cells are
            // written, so decode that many and trim any trailing padding nulls.
            return encoding.GetString(outBuf, 0, inputLength * charSize).TrimEnd(NullChars);
        }
    }

    private static TypeForm[] CreateTypeFormBuffer(int length, TypeForm[] tfeInput) {
        // A freshly allocated TypeForm[] is all-zero, i.e. TypeForm.PlainText, the correct default.
        if (length == 0) {
            return [];
        }

        var buffer = new TypeForm[length];
        if (tfeInput.Length > 0) {
            Array.Copy(tfeInput, buffer, Math.Min(tfeInput.Length, length));
        }

        return buffer;
    }

    private static bool OutputLengthIsKnown(NativeFunction nativeFunction) => nativeFunction switch {
        NativeFunction.TranslateString => true,
        NativeFunction.TranslateStringTfe => true,
        NativeFunction.BackTranslateString => true,
        NativeFunction.BackTranslateStringTfe => true,
        _ => false,
    };

    private static bool UsesTypeForms(NativeFunction nativeFunction) =>
        nativeFunction is NativeFunction.TranslateStringTfe or NativeFunction.BackTranslateStringTfe;

    /// <summary>
    /// Gets the encoding based on the character size from LibLouis.
    /// </summary>
    /// <param name="size">Character size, 4 bytes is UTF-32, anything else is UTF-16.</param>
    /// <returns>Character encoding.</returns>
    private static Encoding GetEncoding(int size) {
        if (size == 4) {
            return Encoding.GetEncoding("UTF-32");
        }

        return Encoding.GetEncoding("UTF-16");
    }
}
