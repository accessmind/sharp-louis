using System;
using System.ComponentModel;
using System.Diagnostics.SymbolStore;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.SqlServer.Server;
using static System.Net.Mime.MediaTypeNames;

namespace SharpLouis;

/// <summary>
/// SharpLouis, .NET wrapper for the LibLouis Braille Translator library
/// Copyright © 2024 AccessMind LLC.
/// Licensed under the Apache License, Version 2.0 (the "License");
/// you may not use this file except in compliance with the License.
/// You may obtain a copy of the License at
/// http://www.apache.org/licenses/LICENSE-2.0
/// Unless required by applicable law or agreed to in writing,
/// software distributed under the License is distributed on an "AS IS" BASIS,
/// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
/// See the License for the specific language governing permissions and limitations under the License.
///
/// The main Wrapper class. Use <c>Wrapper.Create</c> to initialize the wrapper and start working with it
/// </summary>
public class Wrapper: IDisposable {
    /// <summary>
    /// // Counts errors reported from LibLouis dll and is used for checking the Logger Callback mechanism
    /// </summary>
    public static int GlobalLibLouisErrorCount { get; private set; }

    const int TranslationMode = (int)(TranslationModes.NoUndefined | TranslationModes.UnicodeBraille | TranslationModes.DotsInputOutput); // Common for all member functions
    const int BackTranslationMode = 0; // The "mode" parameter is deprecated during backtranslation and must be set to 0 !!

    /// <summary>
    /// Path to be combined with tableName before passing to LibLouis
    /// Must contain the path to the conversion tables, relative to the path of LibLouis.dll.
    /// LibLouis.dll can find the exact absolute path to the tables using this information.
    /// </summary>
    private const string TablesFolder = @"LibLouis\tables";
    private const string LibLouisDll = @"LibLouis\liblouis.dll";

    #region LogCallBack
    private delegate void Func(int level, string message);
    private static void LogCallback(int level, string message) {
        GlobalLibLouisErrorCount++;
        theClient?.OnLibLouisLog(string.Format(": Received callback from LibLouis, describing an error: Level={0} Message={1}", level, message));
    }
    #endregion

    #region DllImport
    [DllImport(LibLouisDll, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    private static extern int lou_charSize();

    [DllImport(LibLouisDll, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    private static extern string lou_version();

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

    [DllImport(LibLouisDll, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    private static extern void lou_registerLogCallback(Func callback);

    [DllImport(LibLouisDll, CharSet = CharSet.Unicode)]
    private static extern unsafe int lou_translateString(
        [In][MarshalAs(UnmanagedType.LPStr)] string tableList, // const char *tableList
        [In] byte[] inbuf,                                     // const widechar *inbuf
        [In, Out] IntPtr inlen,                                // int *inlen
        [Out] byte[] outbuf,                                   // widechar *outbuf 
        [In, Out] IntPtr outlen,                               // int *outlen  
        [In] TypeForm[] typeform,                          // formtype *typeform 
        [MarshalAs(UnmanagedType.LPStr)] string spacing,       // char *spacing
        int mode                                               //  int mode 
 );

    [DllImport(LibLouisDll, CharSet = CharSet.Unicode)]
    private static extern unsafe int lou_backTranslateString(
        [In][MarshalAs(UnmanagedType.LPStr)] string tableList, // const char *tableList
        [In] byte[] inbuf,                                     // const widechar *inbuf
        [In, Out] IntPtr inlen,                                // int *inlen
        [Out] byte[] outbuf,                                   // widechar *outbuf 
        [In, Out] IntPtr outlen,                               // int *outlen  
        [In, Out] TypeForm[] typeform,                      // formtype *typeform 
        [MarshalAs(UnmanagedType.LPStr)] string spacing,       // char *spacing
        int mode                                               //  int mode 
 );
    #endregion

    TypeForm[] DummyTypeForms; // Dummy parameter

    public bool CharsToDots(string chars, out string dots) {
        return CommonNativeCall(NativeFunction.charsToDots, chars, out dots);
    }

    public bool DotsToChars(string dots, out string chars) {
        return CommonNativeCall(NativeFunction.dotsToChars, dots, out chars);
    }

    public bool TranslateString(string text, out string dots) {
        return CommonNativeCall(NativeFunction.translateString, text, out dots);
    }

    public bool TranslateStringWithTypeForms(string text, out string dots, in TypeForm[] typeForms) {
        return CommonNativeCall(NativeFunction.translateStringTfe, text, out dots, typeForms, out DummyTypeForms);
    }

    public bool BackTranslateString(string dots, out string text) {
        return CommonNativeCall(NativeFunction.backTranslateString, dots, out text);
    }

    public bool BackTranslateStringWithTypeForms(string dots, out string text, out TypeForm[] typeForms) {
        return CommonNativeCall(NativeFunction.backTranslateStringTfe, dots, out text, [], out typeForms);
    }

    public string GetVersion() {
        throw new NotImplementedException();
    }

    private byte[] CreateOutputBuffer(int inBufLength) {
        int outputLength = Math.Max((inBufLength * 2), 1024);  // Always twice the inputbuffer size, but at least 1kB
        byte[] outBuf = new byte[outputLength];
        return outBuf;
    }

    private TypeForm[] CreateTfeBuffer(int inputLength, NativeFunction nativeFunction, TypeForm[] tfeInput) {
        // Developer's note: By initializing "result" to TypeForm.Hex5c5c instead of the default TypeForm.plain_text (0x0000) it is easily verified
        // that lou_backTranslateString() when called with  "out TypeForm[] tfe" where tfe != null initializes the first half of the buffer to the value 0x3030
        // and leaves the last half of the buffer untouched.
        // This suggests some kind of mismatch between the managed code and the native code:  Maybe the native code attempts to initialize the whole buffer to 0x30 ?
        // (The expected behavior would be to use only the values  plain_text = 0x0000, italic = 0x0001, underline = 0x0002 and  bold = 0x0004 )
        int length = GetTfeLength(inputLength, nativeFunction);
        TypeForm[] result = new TypeForm[length];
        for (int i = 0; i < length; i++) {
            result[i] = TypeForm.Hex5c5c;
        } // For debugging only !
        if (tfeInput is not null && tfeInput.Length <= length) {
            Array.Copy(tfeInput, result, tfeInput.Length); // Copy to the common buffer to be passed to native code
        }
        return result;
    }

    private int GetTfeLength(int inputLength, NativeFunction nativeFunction) {
#warning TODO Find out why a smaller defaultBufferSize, for instance "defaultBufferSize =(inputLength * 2)" causes strange crashes !!
        int defaultTfeBufferSize = Math.Max(1024, (inputLength * 2)); // Twice as many Typeform items as input elements, but at least 1024
        switch (nativeFunction) {
            case NativeFunction.translateStringTfe:
                return defaultTfeBufferSize;
            case NativeFunction.backTranslateStringTfe:
                return defaultTfeBufferSize;
        }
        return 0; // No buffer needed i these cases
    }

    /// <summary>
    /// The simple, common signature, used by all functions not using a Typeform parameter
    /// </summary>
    private bool CommonNativeCall(NativeFunction nativeFunction, string input, out string output) {
        if (nativeFunction == NativeFunction.translateStringTfe || nativeFunction == NativeFunction.backTranslateStringTfe) {
            throw new ArgumentException(nativeFunction.ToString());
        }
        return CommonNativeCall(nativeFunction, input, out output, [], out DummyTypeForms);
    }

    /// <summary>
    /// The common, general signature, taking all possible input parameters. 
    /// By using this common signature we only need all the unsafe code and marchalling precautions at one single location
    /// </summary>
    /// <param name="nativeFunction">Identifies the native function to call</param>
    /// <param name="input">Input-string for the native function, either text or Braille</param>
    /// <param name="output">Output-string from the native function, either text or Braille</param>
    /// <param name="tfeInput">Optional TypeForm-input for the native function. May be null</param>
    /// <param name="tfeOutput">Optional TypeForm-output from the native function. May be null</param>
    /// <returns></returns>
    private bool CommonNativeCall(NativeFunction nativeFunction, string input, out string output, in TypeForm[] tfeInput, out TypeForm[] tfeOutput) {
        int result = 0;
        // The following 3 buffers are owned by managed code and passed to native code. They are pinned by the "fixed" clause.
        byte[] inBuf = encoding.GetBytes(input);
        byte[] outBuf = CreateOutputBuffer(inBuf.Length);
        TypeForm[] tfeBuf = CreateTfeBuffer(input.Length, nativeFunction, tfeInput);
        // The following 2 integers are owned by managed code and passed to native code. They don't need pinning, because they are simple stack-variables.
        int inputLength = input.Length;
        int outputLength = outBuf.Length;
        unsafe {
            IntPtr inPtr = new IntPtr(&inputLength);
            IntPtr outPrt = new IntPtr(&outputLength);
            fixed (byte* pInBuf = inBuf, pOutBuf = outBuf) // Prevents GarbageCollector from moving the buffers
            {
                fixed (TypeForm* pTfeBuf = tfeBuf) // Two levels are needed for fixing different types !
                {
                    switch (nativeFunction) {
                        case NativeFunction.charsToDots:
                            result = lou_charToDots(tablePaths, inBuf, outBuf, inputLength, TranslationMode);
                            break;
                        case NativeFunction.dotsToChars:
                            result = lou_dotsToChar(tablePaths, inBuf, outBuf, inputLength, BackTranslationMode);
                            break;
                        case NativeFunction.translateString:
                            result = lou_translateString(tablePaths, inBuf, inPtr, outBuf, outPrt, [], string.Empty, TranslationMode);
                            break;
                        case NativeFunction.translateStringTfe:
                            result = lou_translateString(tablePaths, inBuf, inPtr, outBuf, outPrt, tfeBuf, String.Empty, TranslationMode);
                            break;
                        case NativeFunction.backTranslateString:
                            result = lou_backTranslateString(tablePaths, inBuf, inPtr, outBuf, outPrt, [], string.Empty, BackTranslationMode);
                            break;
                        case NativeFunction.backTranslateStringTfe:
                            result = lou_backTranslateString(tablePaths, inBuf, inPtr, outBuf, outPrt, tfeBuf, string.Empty, BackTranslationMode);
                            break;
                    }
                    fixed (byte* pInBufAfter = inBuf, pOutBufAfter = outBuf) {
                        CheckPinning("InBuf ", (int)pInBuf, (int)pInBufAfter);
                        CheckPinning("OutBuf", (int)pOutBuf, (int)pOutBufAfter);
                    }
                    fixed (TypeForm* pTfeBufAfter = tfeBuf) {
                        CheckPinning("TfeBuf ", (int)pTfeBuf, (int)pTfeBufAfter);
                    }
                }
            }
        }
        output = string.Empty;
        tfeOutput = [];
        if (result != 1) {
            return OnError("Result is not 1");
        }
        if (outBuf is null) {
            return OnError("Output buffer is null");
        }
        if (result == 1 && OutputLengthIsKnown(nativeFunction) && outputLength == outBuf.Length) {
            return OnLengthError(outputLength);
        }
        output = GetOutputString(nativeFunction, outBuf, outputLength, charSize);
        tfeOutput = GetOutputTypeForms(nativeFunction, tfeBuf, outputLength);
        return true;
    }

    /// <summary>
    /// If the length of the outputbuffer received from native code is known we use that information.
    /// Otherwise we just remove any trailing null-characters.
    /// </summary>
    /// <param name="nativeFunction"></param>
    /// <param name="output"></param>
    /// <param name="outputLength"></param>
    /// <param name="charSize"></param>
    /// <returns></returns>
    private string GetOutputString(NativeFunction nativeFunction, byte[] output, int outputLength, int charSize) {
        string s;
        if (OutputLengthIsKnown(nativeFunction)) {
            s = encoding.GetString(output, 0, outputLength * charSize); // Only use the relevant part of the outputbuffer
        } else {
            s = encoding.GetString(output); // The whole outputbuffer
        }
        return s.TrimEnd(new char[] { '\0' }); // Remove all trailing null characters
    }

    private TypeForm[] GetOutputTypeForms(NativeFunction nativeFunction, TypeForm[] tfeBuf, int outputLength) {
        if (!TfeMustBeCopied(nativeFunction)) {
            return new TypeForm[0];
        }
        int length = OutputLengthIsKnown(nativeFunction) ? outputLength : 0;
        TypeForm[] result = new TypeForm[length];
        Array.Copy(tfeBuf, result, length);
        Log(string.Format("(): Tfe.{0}", TfeToString(result)));
        return result;
    }

    private bool OutputLengthIsKnown(NativeFunction nativeFunction) {
        switch (nativeFunction) {
            case NativeFunction.translateString:
                return true;
            case NativeFunction.backTranslateString:
                return true;
            case NativeFunction.translateStringTfe:
                return true;
            case NativeFunction.backTranslateStringTfe:
                return true;
        }
        return false;
    }

    private bool TfeMustBeCopied(NativeFunction nativeFunction) {
        switch (nativeFunction) {
            case NativeFunction.translateStringTfe:
                return true;
            case NativeFunction.backTranslateStringTfe:
                return true;
        }
        return false;
    }

    private string TfeToString(TypeForm[] tfe) {
        if (tfe is null) {
            return "null";
        }
        StringBuilder sb = new StringBuilder();
        foreach (TypeForm t in tfe) {
            // When the buffer used for Typeform information in the call to native code is too small a crash seems to occur around here.
            // For this reason we split up in small steps to illustrate that the crash has to do with the use of native code, not with this method!
            int i = (int)t;
            string s = String.Format("0x{0:x} ", i); // Format as HEX
            sb.Append(s);
        }
        return (string.Format("Length={0} HexValues={1}", tfe.Length, sb.ToString()));
    }

    private void CheckPinning(string id, int pBefore, int pAfter) {
        if (pBefore == pAfter) {
            return;
        }
        string message = string.Format(": The buffer '{0}' changed from {1} to {2} during call to native code - even if it was supposed to be pinned!", id, pBefore, pAfter);
        Log(message);
        throw new Exception(message);
    }

    private bool OnLengthError(int outputLength) {
        // According to footnote 2 in documentation:
        // "When the output buffer is not big enough, lou_translateString returns a partial translation that is more or less accurate
        // up until the returned inlen/outlen, and treats it as a successful translation, i.e. also returns 1."
        return OnError(string.Format(" Result=1 but output may have been truncated to {0} characters to fit size of outputbuffer", outputLength));
    }

    private bool OnError(string s) {
        Log(string.Format(": Error: '{0}'", s));
        return false;
    }

    public void Free() {
        lou_free();
    }

    public void UnregisterCallback() {
        lou_registerLogCallback(null!);
    }

    private void Log(string s) {
        if (theClient is null) {
            return;
        }

        theClient?.OnWrapperLog(s); // Call logging mechanism established by the client 
    }

    /// <summary>
    /// Gets the encoding based on the character size from LibLouis
    /// </summary>
    /// <param name="size">Character size, 4 bytes is UTF-32, anything else is UTF-16</param>
    /// <returns>Character encoding</returns>
    private static Encoding GetEncoding(int size) {
        if (size == 4) {
            return Encoding.GetEncoding("UTF-32");
        }

        return Encoding.GetEncoding("UTF-16");
    }

    // Member variables:
    private readonly int charSize;
    private readonly Encoding encoding;
    private string tablePaths;
    private readonly bool useLogCallback = false;

    /// <summary>
    /// Only for preventing GC from collecting the delegate. MUST BE STATIC to keep the GC away !!
    /// See https://stackoverflow.com/questions/75223488/delegate-getting-gc-even-after-pinning
    /// </summary>
    private static readonly Func loggingCallback = LogCallback;
    private readonly string tableNames;

    /// <summary>
    /// Private constructor. Use Wrapper.Create() from the outside.
    /// </summary>
    private Wrapper(string tableNames) {
        this.tableNames = tableNames;
        this.DummyTypeForms = [];
        Log(string.Format(": TableNames='{0}'", tableNames));
#if DEBUG
        this.useLogCallback = true;
#else
this.useLogCallback = false;
#endif

        if (useLogCallback) {
            Log(string.Format(": Registering LibLouis LogCallback function"));
            lou_registerLogCallback(loggingCallback); // Register the static function LoggingCallback as a callback""
        }
        // string version = GetVersion();
        // Log(string.Format(": LibLouis Version {0}", version));
        charSize = lou_charSize();
        Log(string.Format(": CharSize={0}", charSize));
        encoding = GetEncoding(charSize);  // Get the encoding type based on the lou_charSize.
        Log(string.Format(": Encoding={0}", encoding.ToString()));

        tablePaths = Path.Combine(TablesFolder, tableNames); // According to the documentation only the first name needs to contain the tableBase !! 
        Log(string.Format(": Tables='{0}'", tablePaths));
    }

    /// <summary>
    /// Prevent use of default constructor
    /// </summary>
    private Wrapper() {
        this.DummyTypeForms = [];
        this.encoding = GetEncoding(0);
        this.tableNames = string.Empty;
        this.tablePaths = string.Empty;
    }

    public bool DirectoryExists(string path) {
        if (Directory.Exists(path)) {
            return true;
        }

        return OnMissingItem("Directory", path);
    }

    private bool FileExists(string path) {
        if (File.Exists(path)) {
            return true;
        }
        return OnMissingItem("File", path);
    }

    private bool OnMissingItem(string itemType, string path) {
        Log(string.Format("{0} does not exist: '{1}'", itemType, path));
        return false;
    }

    /// <summary>
    /// Simple code for checking that all directories and files needed by liblouis are found at the right locations
    /// </summary>
    /// <returns></returns>
    private bool CheckInstallation() {
        string executingDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? string.Empty;
        string? libLouisDir = Path.Combine(executingDirectory, "LibLouis");
        if (!DirectoryExists(libLouisDir)) {
            return false;
        }
        string liblouisDll = Path.Combine(libLouisDir, "liblouis.dll");
        if (!FileExists(liblouisDll)) {
            return false;
        }
        string tablesDir = Path.Combine(libLouisDir, "tables");
        if (!DirectoryExists(tablesDir)) {
            return false;
        }

        string[] names = tableNames.Split(',');
        foreach (string name in names) {
            // Only the first name contains the full path !
            string shortName = Path.GetFileName(name);
            string fullPath = (Path.Combine(tablesDir, shortName));
            if (!FileExists(fullPath)) {
                return false;
            }
        }
        Log(string.Format(": All tables in '{0}' were found", tableNames));
        return true;
    }

    private bool disposed = false;

    public void Dispose() {
        if (!disposed) {
            Free();                // Clear all tables
            UnregisterCallback();  // Prevent callbacks to delegate belonging to this object
            disposed = true;       // Handles later async calls from the GC 
        }
    }

    private static IClient? theClient = null;

    public static Wrapper? Create(string tableNames, IClient? client) {
        theClient = client; // Establish logging

        Wrapper wrapper = new Wrapper(tableNames);
        bool ok = wrapper.CheckInstallation();
        return ok ? wrapper : null;
    }
}
