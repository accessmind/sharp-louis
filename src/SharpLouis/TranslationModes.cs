namespace SharpLouis;

/// <summary>
/// As defined in liblouis.h
/// </summary>
[Flags]
public enum TranslationModes {
    NoContractions = 1,
    ComputerBrailleAtCursor = 2,
    DotsInputOutput = 4,
    // for historic reasons 8 and 16 are free
    ComputerBrailleLeftFromCursor = 32,
    UnicodeBraille = 64, // In liblouis.h: ucBrl = 64,
    NoUndefined = 128,
    PartialTranslation = 256
}
