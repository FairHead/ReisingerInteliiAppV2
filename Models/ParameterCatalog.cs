namespace ReisingerIntelliApp_V4.Models;

/// <summary>
/// Metadata for a single parameter definition.
/// Source: Betriebsanleitung Kap. 8.1
/// </summary>
public record ParameterMeta
{
    /// <summary>Parameter ID (1-98)</summary>
    public int Id { get; init; }
    
    /// <summary>Display name (Terminal name from documentation)</summary>
    public string Name { get; init; } = string.Empty;
    
    /// <summary>RS232 name (alternative name from documentation)</summary>
    public string Rs232Name { get; init; } = string.Empty;
    
    /// <summary>Minimum value (null if no range or special format)</summary>
    public int? Min { get; init; }
    
    /// <summary>Maximum value (null if variable or no range)</summary>
    public int? Max { get; init; }
    
    /// <summary>Unit (s, mm, mm/s, 1/10s, Grad-2, etc.)</summary>
    public string Unit { get; init; } = string.Empty;
    
    /// <summary>Default value as string (may be "variabel" or empty)</summary>
    public string Default { get; init; } = string.Empty;
    
    /// <summary>Additional notes</summary>
    public string Notes { get; init; } = string.Empty;
    
    /// <summary>Whether max is variable (depends on door width)</summary>
    public bool IsVariableMax { get; init; }
    
    /// <summary>Whether this is a reserved/empty parameter</summary>
    public bool IsReserved { get; init; }
    
    /// <summary>Whether this is read-only (0..0 range or no range)</summary>
    public bool IsReadOnly { get; init; }
    
    /// <summary>Input type for UI</summary>
    public ParameterInputType InputType { get; init; } = ParameterInputType.Numeric;
    
    /// <summary>Special format type for date/time parameters</summary>
    public ParameterFormatType FormatType { get; init; } = ParameterFormatType.None;
    
    /// <summary>Options for Picker input type</summary>
    public Dictionary<int, string>? PickerOptions { get; init; }
    
    /// <summary>Category for UI grouping (None = hidden from UI)</summary>
    public ParameterCategory Category { get; init; } = ParameterCategory.None;
    
    /// <summary>
    /// Gets the range text for display (e.g., "0..200", "50..variabel", "read-only")
    /// </summary>
    public string RangeText
    {
        get
        {
            if (IsReserved) return "reserviert";
            if (IsReadOnly) return "read-only";
            if (FormatType != ParameterFormatType.None) return GetFormatText();
            if (Min == null && Max == null) return "-";
            if (IsVariableMax) return $"{Min}..variabel";
            return $"{Min}..{Max}";
        }
    }
    
    /// <summary>
    /// Gets the default text for display
    /// </summary>
    public string DefaultText => string.IsNullOrEmpty(Default) ? "-" : Default;
    
    private string GetFormatText() => FormatType switch
    {
        ParameterFormatType.Date => "tt:mm:jj",
        ParameterFormatType.Time => "hh:mm:ss",
        ParameterFormatType.TimeWithDot => "hh.mm.ss",
        _ => "-"
    };
}

/// <summary>
/// Input type for parameter UI
/// </summary>
public enum ParameterInputType
{
    /// <summary>Numeric keypad input</summary>
    Numeric,
    
    /// <summary>Toggle/Switch for 0/1 values</summary>
    Toggle,
    
    /// <summary>Picker/Dropdown for small enum ranges</summary>
    Picker,
    
    /// <summary>Special format input (date, time)</summary>
    Format,
    
    /// <summary>Read-only display</summary>
    ReadOnly
}

/// <summary>
/// Special format types for certain parameters
/// </summary>
public enum ParameterFormatType
{
    None,
    Date,       // tt:mm:jj (Parameter 41)
    Time,       // hh:mm:ss (Parameter 42)
    TimeWithDot // hh.mm.ss (Parameter 88, 89)
}

/// <summary>
/// Category for grouping parameters in the UI.
/// Parameters with Category = None are hidden from the UI but still sent to the device.
/// </summary>
public enum ParameterCategory
{
    /// <summary>Hidden from UI (reserved, internal, or not user-relevant)</summary>
    None,
    
    /// <summary>Zeiten - Time parameters (1-6)</summary>
    Zeiten,
    
    /// <summary>Weiten - Width/distance parameters (10-20, 59)</summary>
    Weiten,
    
    /// <summary>Tempo - Speed/acceleration parameters (21-24, 31-35, 38)</summary>
    Tempo,
    
    /// <summary>I/O - Input/Output and system parameters</summary>
    IO,
    
    /// <summary>Basis - Basic settings</summary>
    Basis
}

/// <summary>
/// Single source of truth for all parameter metadata.
/// Based on Betriebsanleitung Kap. 8.1
/// </summary>
public static class ParameterCatalog
{
    private static readonly Dictionary<int, ParameterMeta> _catalog = new()
    {
        // ===== ZEITEN (Zeit-Parameter) =====
        [1] = new ParameterMeta { Id = 1, Name = "Zeit TEILAUF", Rs232Name = "ZEIT TEILAUF", Min = 0, Max = 200, Unit = "s", Default = "1", Category = ParameterCategory.Zeiten },
        [2] = new ParameterMeta { Id = 2, Name = "Zeit VOLLAUF 1", Rs232Name = "ZEIT VOLLAUF 1", Min = 0, Max = 200, Unit = "s", Default = "12", Category = ParameterCategory.Zeiten },
        [3] = new ParameterMeta { Id = 3, Name = "Zeit BM I", Rs232Name = "Zeit BM I", Min = 0, Max = 200, Unit = "s", Default = "1", Category = ParameterCategory.Zeiten },
        [4] = new ParameterMeta { Id = 4, Name = "Zeit BM A", Rs232Name = "Zeit BM A", Min = 0, Max = 200, Unit = "s", Default = "1", Category = ParameterCategory.Zeiten },
        [5] = new ParameterMeta { Id = 5, Name = "Zeit REV", Rs232Name = "Zeit REV", Min = 0, Max = 200, Unit = "s", Default = "5", Category = ParameterCategory.Zeiten },
        [6] = new ParameterMeta { Id = 6, Name = "Zeit VOLLAUF 2", Rs232Name = "ZEIT VOLLAUF 2", Min = 0, Max = 200, Unit = "s", Default = "12", Category = ParameterCategory.Zeiten },
        
        // E-Riegel (nicht in der Web-UI sichtbar)
        [7] = new ParameterMeta { Id = 7, Name = "E-Riegel V-Zeit", Rs232Name = "E-Riegel V-Zeit", Min = 0, Max = 60, Unit = "s", Default = "0" },
        
        // ===== BASIS =====
        [8] = new ParameterMeta { Id = 8, Name = "autom. Start", Rs232Name = "autom. Start", Min = 1, Max = 3, Default = "2", InputType = ParameterInputType.Picker, 
              PickerOptions = new() { [1] = "1", [2] = "2", [3] = "3" }, Category = ParameterCategory.Basis },
        [9] = new ParameterMeta { Id = 9, Name = "Passwort", Rs232Name = "Passwort", Min = 0, Max = 99999, Unit = "mm", Default = "0", Category = ParameterCategory.Basis },
        
        // ===== WEITEN =====
        [10] = new ParameterMeta { Id = 10, Name = "Weite DAUERAUF", Rs232Name = "Weite DAUERAUF", Min = 50, Max = 1211, Unit = "mm", Default = "variabel", Category = ParameterCategory.Weiten },
        [11] = new ParameterMeta { Id = 11, Name = "Weite TEILAUF", Rs232Name = "Weite TEILAUF", Min = 50, Max = 1211, Unit = "mm", Default = "variabel", Category = ParameterCategory.Weiten },
        [12] = new ParameterMeta { Id = 12, Name = "Weite VOLLAUF 1", Rs232Name = "Weite VOLLAUF 2", Min = 50, Max = 1211, Unit = "mm", Default = "variabel", Category = ParameterCategory.Weiten },
        [13] = new ParameterMeta { Id = 13, Name = "Weite BM I", Rs232Name = "Weite BM I", Min = 50, Max = 1211, Unit = "mm", Default = "variabel", Category = ParameterCategory.Weiten },
        [14] = new ParameterMeta { Id = 14, Name = "Weite BM A", Rs232Name = "Weite BM A", Min = 50, Max = 1211, Unit = "mm", Default = "variabel", Category = ParameterCategory.Weiten },
        [15] = new ParameterMeta { Id = 15, Name = "Weite DTA", Rs232Name = "Weite DTA", Min = 50, Max = 1211, Unit = "mm", Default = "variabel", Category = ParameterCategory.Weiten },
        [16] = new ParameterMeta { Id = 16, Name = "Weite VOLLAUF 2", Rs232Name = "Weite VOLLAUF 3", Min = 50, Max = 1211, Unit = "mm", Default = "variabel", Category = ParameterCategory.Weiten },
        [17] = new ParameterMeta { Id = 17, Name = "Einlaufbereich AUF", Rs232Name = "Einlaufbereich AUF", Min = 5, Max = 200, Unit = "mm", Default = "40", Category = ParameterCategory.Weiten },
        [18] = new ParameterMeta { Id = 18, Name = "Einlaufbereich ZU", Rs232Name = "Einlaufbereich ZU", Min = 5, Max = 200, Unit = "mm", Default = "50", Category = ParameterCategory.Weiten },
        [19] = new ParameterMeta { Id = 19, Name = "Sicherheitsabstand AUF", Rs232Name = "Sicherheitsabstand", Min = 0, Max = 30, Unit = "mm", Default = "10", Category = ParameterCategory.Weiten },
        [20] = new ParameterMeta { Id = 20, Name = "Nullpunkttoleranz", Rs232Name = "Nullpunkttoleranz", Min = 0, Max = 15, Unit = "mm/s", Default = "7", Category = ParameterCategory.Weiten },
        
        // ===== TEMPO =====
        [21] = new ParameterMeta { Id = 21, Name = "Geschwindigkeit AUF", Rs232Name = "Geschwindigkeit AUF", Min = 1, Max = 550, Unit = "mm/s", Default = "300", Category = ParameterCategory.Tempo },
        [22] = new ParameterMeta { Id = 22, Name = "Geschwindigkeit ZU", Rs232Name = "Geschwindigkeit ZU", Min = 1, Max = 500, Unit = "mm/s", Default = "200", Category = ParameterCategory.Tempo },
        [23] = new ParameterMeta { Id = 23, Name = "Geschwindigkeit EINLAUF", Rs232Name = "Geschwindigkeit Einlauf", Min = 1, Max = 50, Unit = "mm/s", Default = "25", Category = ParameterCategory.Tempo },
        [24] = new ParameterMeta { Id = 24, Name = "Geschwindigkeit REF", Rs232Name = "Geschwindigkeit REF", Min = 1, Max = 50, Default = "35", Category = ParameterCategory.Tempo },
        
        // Reserviert (25-26) - versteckt
        [25] = new ParameterMeta { Id = 25, Name = "reserviert", IsReserved = true, IsReadOnly = true, InputType = ParameterInputType.ReadOnly },
        [26] = new ParameterMeta { Id = 26, Name = "reserviert", IsReserved = true, IsReadOnly = true, InputType = ParameterInputType.ReadOnly },
        
        // ===== I/O =====
        [27] = new ParameterMeta { Id = 27, Name = "Zeit Bifunktion", Rs232Name = "Zeit Bifunktion 1/10s", Min = 10, Max = 100, Unit = "1/10s", Default = "10", Category = ParameterCategory.IO },
        [28] = new ParameterMeta { Id = 28, Name = "VOLLAUF 1 Bifunktion", Rs232Name = "VOLLAUF 1 bifunktion", Min = 0, Max = 1, Default = "0", InputType = ParameterInputType.Toggle, Category = ParameterCategory.IO },
        [29] = new ParameterMeta { Id = 29, Name = "WLAN / RS232", Rs232Name = "WLAN / RS233", Min = 0, Max = 2, Default = "0", InputType = ParameterInputType.Picker,
               PickerOptions = new() { [0] = "Aus", [1] = "WLAN", [2] = "RS232" }, Category = ParameterCategory.IO },
        [30] = new ParameterMeta { Id = 30, Name = "Master/Slave", Rs232Name = "Master/Slave", Min = 0, Max = 2, Default = "0", InputType = ParameterInputType.Picker,
               PickerOptions = new() { [0] = "Aus", [1] = "Master", [2] = "Slave" }, Category = ParameterCategory.IO },
        
        // ===== TEMPO (Fortsetzung) =====
        [31] = new ParameterMeta { Id = 31, Name = "REV Empfindlichkeit AUF", Rs232Name = "REV Empfindlichkeit AUF", Min = 100, Max = 1100, Default = "700", Category = ParameterCategory.Tempo },
        [32] = new ParameterMeta { Id = 32, Name = "REV Empfindlichkeit ZU", Rs232Name = "REV Empfindlichkeit ZU", Min = 100, Max = 1100, Default = "500", Category = ParameterCategory.Tempo },
        [33] = new ParameterMeta { Id = 33, Name = "Beschleunigung AUF", Rs232Name = "Beschleunigung AUF", Min = 50, Max = 3000, Default = "1000", Category = ParameterCategory.Tempo },
        [34] = new ParameterMeta { Id = 34, Name = "Bremsrampe AUF", Rs232Name = "Bremsrampe AUF", Min = 10, Max = 2000, Default = "800", Category = ParameterCategory.Tempo },
        [35] = new ParameterMeta { Id = 35, Name = "Bremsrampe ZU", Rs232Name = "Bremsrampe ZU", Min = 10, Max = 2000, Default = "1000", Category = ParameterCategory.Tempo },
        
        // ===== BASIS =====
        [36] = new ParameterMeta { Id = 36, Name = "REV Zyklen", Rs232Name = "REV Zyklen", Min = 1, Max = 9999, Default = "5", Category = ParameterCategory.Basis },
        
        // ===== I/O =====
        [37] = new ParameterMeta { Id = 37, Name = "DTA", Rs232Name = "DTA", Min = 0, Max = 1, Default = "0", InputType = ParameterInputType.Toggle, Category = ParameterCategory.IO },
        
        // ===== TEMPO =====
        [38] = new ParameterMeta { Id = 38, Name = "Beschleunigung ZU", Rs232Name = "Beschleunigung ZU", Min = 50, Max = 3000, Default = "800", Category = ParameterCategory.Tempo },
        
        // ===== BASIS =====
        [39] = new ParameterMeta { Id = 39, Name = "Stop und Zu", Rs232Name = "Stop und zu", Min = 0, Max = 2, Default = "0", InputType = ParameterInputType.Picker,
               PickerOptions = new() { [0] = "Aus", [1] = "Ein", [2] = "Mit Warnung" }, Category = ParameterCategory.Basis },
        [40] = new ParameterMeta { Id = 40, Name = "Sprache", Rs232Name = "Sprache", Min = 1, Max = 2, Default = "1", InputType = ParameterInputType.Picker,
               PickerOptions = new() { [1] = "Deutsch", [2] = "English" }, Category = ParameterCategory.Basis },
        
        // Datum/Uhrzeit (41-42) - versteckt (nicht in Web-UI)
        [41] = new ParameterMeta { Id = 41, Name = "Datum", Rs232Name = "Datum", FormatType = ParameterFormatType.Date, InputType = ParameterInputType.Format },
        [42] = new ParameterMeta { Id = 42, Name = "Uhrzeit", Rs232Name = "Uhrzeit", FormatType = ParameterFormatType.Time, InputType = ParameterInputType.Format },
        [43] = new ParameterMeta { Id = 43, Name = "Sommerzeit", Rs232Name = "Sommerzeit", Min = 0, Max = 1, Default = "0", InputType = ParameterInputType.Toggle },
        
        // ===== I/O =====
        [44] = new ParameterMeta { Id = 44, Name = "DACS", Rs232Name = "DACS", Min = 0, Max = 1, Default = "0", InputType = ParameterInputType.Toggle, Category = ParameterCategory.IO },
        [45] = new ParameterMeta { Id = 45, Name = "Schliess-System", Rs232Name = "Schliess-System", Min = 0, Max = 2, Default = "0", InputType = ParameterInputType.Picker,
               PickerOptions = new() { [0] = "Aus", [1] = "Modus 1", [2] = "Modus 2" }, Category = ParameterCategory.IO },
        
        // ===== BASIS =====
        [46] = new ParameterMeta { Id = 46, Name = "D LOC rt", Rs232Name = "D LOC rt", Min = 1, Max = 64, Default = "64", Category = ParameterCategory.Basis },
        [47] = new ParameterMeta { Id = 47, Name = "D LOC gn", Rs232Name = "D LOC gn", Min = 1, Max = 64, Default = "64", Category = ParameterCategory.Basis },
        [48] = new ParameterMeta { Id = 48, Name = "D AUX", Rs232Name = "D AUX", Min = 0, Max = 64, Default = "64", Category = ParameterCategory.Basis },
        
        // ===== I/O =====
        [49] = new ParameterMeta { Id = 49, Name = "Dauer Strg", Rs232Name = "Dauer Strg", Min = 0, Max = 1, Default = "0", InputType = ParameterInputType.Toggle, Category = ParameterCategory.IO },
        
        // Referenzfahrt (50) - read-only, versteckt
        [50] = new ParameterMeta { Id = 50, Name = "Referenzfahrt", Rs232Name = "Referenzfahrt", Min = 0, Max = 0, IsReadOnly = true, InputType = ParameterInputType.ReadOnly },
        
        // ===== BASIS =====
        [51] = new ParameterMeta { Id = 51, Name = "Sonderfunktion", Rs232Name = "Sonderfunktion", Min = 0, Max = 10, Default = "0", Category = ParameterCategory.Basis },
        
        // Referenzfahrt o. WT (52) - versteckt
        [52] = new ParameterMeta { Id = 52, Name = "Referenzfahrt o. WT", Rs232Name = "Referenzfahrt o. WT", Min = 0, Max = 10 },
        
        // Sommer-/Winterbetrieb (53) - versteckt
        [53] = new ParameterMeta { Id = 53, Name = "Sommer-/Winterbetrieb", Rs232Name = "Sommer-/Winterbetrieb", Min = 0, Max = 1, Default = "0", InputType = ParameterInputType.Toggle },
        
        // ===== BASIS =====
        [54] = new ParameterMeta { Id = 54, Name = "Oeffnungsweite So/Wi", Rs232Name = "Öffnungsweite So/Wi", Min = 40, Max = 100, Default = "80", Category = ParameterCategory.Basis },
        
        // Ladenschluss (55) - versteckt
        [55] = new ParameterMeta { Id = 55, Name = "Ladenschluss", Rs232Name = "Ladenschluss", Min = 0, Max = 1, Default = "0", InputType = ParameterInputType.Toggle },
        
        // ===== I/O =====
        [56] = new ParameterMeta { Id = 56, Name = "DMSS", Rs232Name = "DMSS", Min = 0, Max = 6, Default = "0", Category = ParameterCategory.IO },
        
        // E-Riegel (57) - versteckt
        [57] = new ParameterMeta { Id = 57, Name = "E-Riegel", Rs232Name = "E-Riegel", Min = 0, Max = 1, Default = "0", InputType = ParameterInputType.Toggle },
        
        // ===== I/O =====
        [58] = new ParameterMeta { Id = 58, Name = "Schleuse K1 NO/NC", Rs232Name = "Schleuse K1 NO/NC", Min = 0, Max = 1, Default = "0", InputType = ParameterInputType.Toggle, Category = ParameterCategory.IO },
        
        // ===== WEITEN =====
        [59] = new ParameterMeta { Id = 59, Name = "autom. ZU nach REV", Rs232Name = "autom. ZU nach REV", Min = 0, Max = 2, Default = "1", InputType = ParameterInputType.Picker,
               PickerOptions = new() { [0] = "Aus", [1] = "Ein", [2] = "Verzögert" }, Category = ParameterCategory.Weiten },
        
        // Statusanzeige (60) - read-only, versteckt
        [60] = new ParameterMeta { Id = 60, Name = "Statusanzeige", Rs232Name = "Statusanzeige", Min = 0, Max = 0, IsReadOnly = true, InputType = ParameterInputType.ReadOnly },
        
        // ===== BASIS =====
        [61] = new ParameterMeta { Id = 61, Name = "Programmwahlschalter", Rs232Name = "Programmwahlschalter", Min = 0, Max = 2, Default = "0", InputType = ParameterInputType.Picker,
               PickerOptions = new() { [0] = "Aus", [1] = "Modus 1", [2] = "Modus 2" }, Category = ParameterCategory.Basis },
        
        // Werkeinstellung (62) - read-only, versteckt
        [62] = new ParameterMeta { Id = 62, Name = "Werkeinstellung", Rs232Name = "Werkeinstellung", Min = 0, Max = 0, IsReadOnly = true, InputType = ParameterInputType.ReadOnly },
        
        // Notfall ZU (63) - versteckt
        [63] = new ParameterMeta { Id = 63, Name = "Notfall ZU", Rs232Name = "Notfall ZU", Min = 0, Max = 3, Default = "0" },
        
        // ===== I/O =====
        [64] = new ParameterMeta { Id = 64, Name = "Ausgang AUX", Rs232Name = "Ausgang AUX", Min = 0, Max = 7, Default = "1", Category = ParameterCategory.IO },
        
        // ===== BASIS =====
        [65] = new ParameterMeta { Id = 65, Name = "akustische Signale", Rs232Name = "akustische Signale", Min = 0, Max = 1, Default = "1", InputType = ParameterInputType.Toggle, Category = ParameterCategory.Basis },
        
        // ===== I/O =====
        [66] = new ParameterMeta { Id = 66, Name = "autom. Offenhaltung", Rs232Name = "autom. Offen.h.", Min = 0, Max = 4, Unit = "mm", Default = "0", Category = ParameterCategory.IO },
        
        // ===== BASIS =====
        [67] = new ParameterMeta { Id = 67, Name = "OSS3 aktiv ab", Rs232Name = "OSS3 aktiv ab", Min = 0, Max = 1211, Unit = "mm", Default = "0", Category = ParameterCategory.Basis },
        [68] = new ParameterMeta { Id = 68, Name = "ext. CSS", Rs232Name = "Ext. CSS", Min = 0, Max = 1211, Unit = "mm", Default = "0", Category = ParameterCategory.Basis },
        [69] = new ParameterMeta { Id = 69, Name = "ext. OSS", Rs232Name = "Ext. OSS", Min = 0, Max = 1211, Unit = "Ge2", Default = "0", Category = ParameterCategory.Basis },
        
        // X/Y-Achse (70-71) - versteckt
        [70] = new ParameterMeta { Id = 70, Name = "X-Achse", Rs232Name = "X-Achse", Min = 0, Max = 1000, Unit = "Grad-2" },
        [71] = new ParameterMeta { Id = 71, Name = "Y-Achse", Rs232Name = "Y-Achse", Min = 0, Max = 1000, Unit = "Grad-2" },
        
        // Red. REV (72) - read-only, versteckt
        [72] = new ParameterMeta { Id = 72, Name = "Red. REV AUF", Rs232Name = "Red. REV AUF", Min = 0, Max = 0, IsReadOnly = true, InputType = ParameterInputType.ReadOnly },
        
        // Reed. REV ZU (73) - versteckt
        [73] = new ParameterMeta { Id = 73, Name = "Reed. REV ZU", Rs232Name = "Reed. REV ZU", Min = 20, Max = 1000 },
        
        // XY Haltezeit (74) - versteckt
        [74] = new ParameterMeta { Id = 74, Name = "XY Haltezeit", Rs232Name = "XY Haltezeit", Min = 0, Max = null, Unit = "s" },
        
        // Reserviert (75-79) - versteckt
        [75] = new ParameterMeta { Id = 75, Name = "reserviert", IsReserved = true, IsReadOnly = true, InputType = ParameterInputType.ReadOnly },
        [76] = new ParameterMeta { Id = 76, Name = "reserviert", IsReserved = true, IsReadOnly = true, InputType = ParameterInputType.ReadOnly },
        [77] = new ParameterMeta { Id = 77, Name = "reserviert", IsReserved = true, IsReadOnly = true, InputType = ParameterInputType.ReadOnly },
        [78] = new ParameterMeta { Id = 78, Name = "reserviert", IsReserved = true, IsReadOnly = true, InputType = ParameterInputType.ReadOnly },
        [79] = new ParameterMeta { Id = 79, Name = "reserviert", IsReserved = true, IsReadOnly = true, InputType = ParameterInputType.ReadOnly },
        
        // Sicherheitscheck (80) - read-only, versteckt
        [80] = new ParameterMeta { Id = 80, Name = "Sicherheitscheck", Rs232Name = "Sicherheitscheck", IsReadOnly = true, InputType = ParameterInputType.ReadOnly },
        
        // Sicherheitsintervall (81) - versteckt
        [81] = new ParameterMeta { Id = 81, Name = "Sicherheitsintervall", Rs232Name = "Sicherheitsintervall", Min = 3, Max = 12, Default = "12" },
        
        // ===== I/O =====
        [82] = new ParameterMeta { Id = 82, Name = "Akku Kontrolle", Rs232Name = "Akku Kontrolle", Min = 0, Max = 2, Default = "0", InputType = ParameterInputType.Picker,
               PickerOptions = new() { [0] = "Aus", [1] = "Ein", [2] = "Warnung" }, Category = ParameterCategory.IO },
        [83] = new ParameterMeta { Id = 83, Name = "Pull and Go", Rs232Name = "Pull and GO", Min = 0, Max = 4, Default = "1", Category = ParameterCategory.IO },
        [84] = new ParameterMeta { Id = 84, Name = "Sicherheitselemente", Rs232Name = "Sicherheitselemente", Min = 0, Max = 3, Default = "0", Category = ParameterCategory.IO },
        [85] = new ParameterMeta { Id = 85, Name = "Anzahl Si.-elemente", Rs232Name = "Anzahl Si.-Elemente", Min = 1, Max = 2, Default = "1", InputType = ParameterInputType.Picker,
               PickerOptions = new() { [1] = "1", [2] = "2" }, Category = ParameterCategory.IO },
        [86] = new ParameterMeta { Id = 86, Name = "Si1 Selbstcheck", Rs232Name = "Si1 Selbstcheck", Min = 0, Max = 2, Default = "0", Category = ParameterCategory.IO },
        [87] = new ParameterMeta { Id = 87, Name = "Si2 Selbstcheck", Rs232Name = "Si2 Selbstcheck", Min = 0, Max = 1, Default = "0", InputType = ParameterInputType.Toggle, Category = ParameterCategory.IO },
        
        // Sperrzeit (88-89) - versteckt (Datum/Zeit Format)
        [88] = new ParameterMeta { Id = 88, Name = "Sperrzeit Ta Beginn", Rs232Name = "Sperrzeit Ta Beginn", FormatType = ParameterFormatType.TimeWithDot, InputType = ParameterInputType.Format },
        [89] = new ParameterMeta { Id = 89, Name = "Sperrzeit Ta Ende", Rs232Name = "Sperrzeit Ta Ende", FormatType = ParameterFormatType.TimeWithDot, InputType = ParameterInputType.Format },
        
        // Reserviert (90) - versteckt
        [90] = new ParameterMeta { Id = 90, Name = "reserviert", IsReserved = true, IsReadOnly = true, InputType = ParameterInputType.ReadOnly },
        
        // ===== BASIS =====
        [91] = new ParameterMeta { Id = 91, Name = "Aufrichtung (R-0/L-1)", Rs232Name = "Aufrichtung (R-0,L-1)", Min = 0, Max = 1, InputType = ParameterInputType.Picker,
               PickerOptions = new() { [0] = "Rechts (R)", [1] = "Links (L)" }, Category = ParameterCategory.Basis },
        
        // Reserviert (92-94) - versteckt
        [92] = new ParameterMeta { Id = 92, Name = "reserviert", IsReserved = true, IsReadOnly = true, InputType = ParameterInputType.ReadOnly },
        [93] = new ParameterMeta { Id = 93, Name = "reserviert", IsReserved = true, IsReadOnly = true, InputType = ParameterInputType.ReadOnly },
        [94] = new ParameterMeta { Id = 94, Name = "reserviert", IsReserved = true, IsReadOnly = true, InputType = ParameterInputType.ReadOnly },
        
        // ===== BASIS =====
        [95] = new ParameterMeta { Id = 95, Name = "Module freischalten", Rs232Name = "Module freischalten", Min = 0, Max = 99999, Default = "0", Category = ParameterCategory.Basis },
        
        // Reserviert (96-98) - versteckt
        [96] = new ParameterMeta { Id = 96, Name = "reserviert", IsReserved = true, IsReadOnly = true, InputType = ParameterInputType.ReadOnly },
        [97] = new ParameterMeta { Id = 97, Name = "reserviert", IsReserved = true, IsReadOnly = true, InputType = ParameterInputType.ReadOnly },
        [98] = new ParameterMeta { Id = 98, Name = "reserviert", IsReserved = true, IsReadOnly = true, InputType = ParameterInputType.ReadOnly },
    };
    
    /// <summary>
    /// Gets the metadata for a parameter by ID.
    /// Returns a default "Unknown" metadata for undefined IDs.
    /// </summary>
    public static ParameterMeta GetMeta(int id)
    {
        if (_catalog.TryGetValue(id, out var meta))
        {
            return meta;
        }
        
        // Unknown parameter
        return new ParameterMeta
        {
            Id = id,
            Name = $"Unknown ({id})",
            IsReadOnly = true,
            InputType = ParameterInputType.ReadOnly,
            Notes = "Parameter nicht in Katalog definiert"
        };
    }
    
    /// <summary>
    /// Gets all defined parameter metadata entries.
    /// </summary>
    public static IReadOnlyDictionary<int, ParameterMeta> All => _catalog;
    
    /// <summary>
    /// Checks if a parameter ID is defined in the catalog.
    /// </summary>
    public static bool IsDefined(int id) => _catalog.ContainsKey(id);
    
    /// <summary>
    /// Gets all parameters that should be visible in the UI (have a category).
    /// </summary>
    public static IEnumerable<ParameterMeta> GetVisibleParameters() 
        => _catalog.Values.Where(p => p.Category != ParameterCategory.None).OrderBy(p => p.Id);
    
    /// <summary>
    /// Gets all parameters for a specific category.
    /// </summary>
    public static IEnumerable<ParameterMeta> GetParametersByCategory(ParameterCategory category)
        => _catalog.Values.Where(p => p.Category == category).OrderBy(p => p.Id);
    
    /// <summary>
    /// Gets all categories that have visible parameters.
    /// </summary>
    public static IEnumerable<ParameterCategory> GetActiveCategories()
        => _catalog.Values
            .Where(p => p.Category != ParameterCategory.None)
            .Select(p => p.Category)
            .Distinct()
            .OrderBy(c => c);
}
