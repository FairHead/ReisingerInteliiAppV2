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
/// Single source of truth for all parameter metadata.
/// Based on Betriebsanleitung Kap. 8.1
/// </summary>
public static class ParameterCatalog
{
    private static readonly Dictionary<int, ParameterMeta> _catalog = new()
    {
        // Zeit-Parameter (1-9)
        [1] = new ParameterMeta { Id = 1, Name = "Zeit TEILAUF", Rs232Name = "ZEIT TEILAUF", Min = 0, Max = 200, Unit = "s", Default = "1" },
        [2] = new ParameterMeta { Id = 2, Name = "ZEIT VOLLAUF 1", Rs232Name = "ZEIT VOLLAUF 1", Min = 0, Max = 200, Unit = "s", Default = "12" },
        [3] = new ParameterMeta { Id = 3, Name = "Zeit BM I", Rs232Name = "Zeit BM I", Min = 0, Max = 200, Unit = "s", Default = "1" },
        [4] = new ParameterMeta { Id = 4, Name = "Zeit BM A", Rs232Name = "Zeit BM A", Min = 0, Max = 200, Unit = "s", Default = "1" },
        [5] = new ParameterMeta { Id = 5, Name = "Zeit REV", Rs232Name = "Zeit REV", Min = 0, Max = 200, Unit = "s", Default = "5" },
        [6] = new ParameterMeta { Id = 6, Name = "ZEIT VOLLAUF 2", Rs232Name = "ZEIT VOLLAUF 2", Min = 0, Max = 200, Unit = "s", Default = "12" },
        [7] = new ParameterMeta { Id = 7, Name = "E-Riegel V-Zeit", Rs232Name = "E-Riegel V-Zeit", Min = 0, Max = 60, Unit = "s", Default = "0" },
        [8] = new ParameterMeta { Id = 8, Name = "autom. Start", Rs232Name = "autom. Start", Min = 1, Max = 3, Default = "2", InputType = ParameterInputType.Picker, 
              PickerOptions = new() { [1] = "1", [2] = "2", [3] = "3" } },
        [9] = new ParameterMeta { Id = 9, Name = "Passwort", Rs232Name = "Passwort", Min = 0, Max = 99999, Default = "0" },
        
        // Weite-Parameter (10-16) - fester Bereich 50-1211
        [10] = new ParameterMeta { Id = 10, Name = "Weite DAUERAUF", Rs232Name = "Weite DAUERAUF", Min = 50, Max = 1211, Unit = "mm", Default = "variabel" },
        [11] = new ParameterMeta { Id = 11, Name = "Weite TEILAUF", Rs232Name = "Weite TEILAUF", Min = 50, Max = 1211, Unit = "mm", Default = "variabel" },
        [12] = new ParameterMeta { Id = 12, Name = "Weite VOLLAUF 1", Rs232Name = "Weite VOLLAUF 2", Min = 50, Max = 1211, Unit = "mm", Default = "variabel", Notes = "RS232 Name in Doku: VOLLAUF 2" },
        [13] = new ParameterMeta { Id = 13, Name = "Weite BM I", Rs232Name = "Weite BM I", Min = 50, Max = 1211, Unit = "mm", Default = "variabel" },
        [14] = new ParameterMeta { Id = 14, Name = "Weite BM A", Rs232Name = "Weite BM A", Min = 50, Max = 1211, Unit = "mm", Default = "variabel" },
        [15] = new ParameterMeta { Id = 15, Name = "Weite DTA", Rs232Name = "Weite DTA", Min = 50, Max = 1211, Unit = "mm", Default = "variabel" },
        [16] = new ParameterMeta { Id = 16, Name = "Weite VOLLAUF 2", Rs232Name = "Weite VOLLAUF 3", Min = 50, Max = 1211, Unit = "mm", Default = "variabel", Notes = "RS232 Name in Doku: VOLLAUF 3" },
        
        // Einlauf/Sicherheit (17-20)
        [17] = new ParameterMeta { Id = 17, Name = "Einlaufbereich AUF", Rs232Name = "Einlaufbereich AUF", Min = 5, Max = 200, Unit = "mm", Default = "40" },
        [18] = new ParameterMeta { Id = 18, Name = "Einlaufbereich ZU", Rs232Name = "Einlaufbereich ZU", Min = 5, Max = 200, Unit = "mm", Default = "50" },
        [19] = new ParameterMeta { Id = 19, Name = "Sicherh. Abstand", Rs232Name = "Sicherheitsabstand", Min = 0, Max = 30, Unit = "mm", Default = "10" },
        [20] = new ParameterMeta { Id = 20, Name = "Nullpunkttol.", Rs232Name = "Nullpunkttoleranz", Min = 0, Max = 15, Unit = "mm", Default = "7" },
        
        // Geschwindigkeit (21-24)
        [21] = new ParameterMeta { Id = 21, Name = "Geschwindigkeit AUF", Rs232Name = "Geschwindigkeit AUF", Min = 1, Max = 500, Default = "300" },
        [22] = new ParameterMeta { Id = 22, Name = "Geschwindigkeit ZU", Rs232Name = "Geschwindigkeit ZU", Min = 1, Max = 500, Default = "200" },
        [23] = new ParameterMeta { Id = 23, Name = "Geschwindigkeit Einlauf", Rs232Name = "Geschwindigkeit Einlauf", Min = 1, Max = 50, Unit = "mm/s", Default = "25" },
        [24] = new ParameterMeta { Id = 24, Name = "Geschwindigkeit REF", Rs232Name = "Geschwindigkeit REF", Min = 1, Max = 50, Unit = "mm/s", Default = "35" },
        
        // Reserviert (25-26)
        [25] = new ParameterMeta { Id = 25, Name = "reserviert", IsReserved = true, IsReadOnly = true, InputType = ParameterInputType.ReadOnly },
        [26] = new ParameterMeta { Id = 26, Name = "reserviert", IsReserved = true, IsReadOnly = true, InputType = ParameterInputType.ReadOnly },
        
        // Bi-Funktion, WLAN, Master/Slave (27-30)
        [27] = new ParameterMeta { Id = 27, Name = "Zeit Bi-Funktion", Rs232Name = "Zeit Bifunktion 1/10s", Min = 10, Max = 100, Unit = "1/10s", Default = "10" },
        [28] = new ParameterMeta { Id = 28, Name = "Bi-Funktion (Voll1)", Rs232Name = "VOLLAUF 1 bifunktion", Min = 0, Max = 1, Default = "0", InputType = ParameterInputType.Toggle },
        [29] = new ParameterMeta { Id = 29, Name = "WLAN / RS232", Rs232Name = "WLAN / RS233", Min = 0, Max = 2, Default = "0", InputType = ParameterInputType.Picker,
               PickerOptions = new() { [0] = "Aus", [1] = "WLAN", [2] = "RS232" } },
        [30] = new ParameterMeta { Id = 30, Name = "Master/Slave", Rs232Name = "Master/Slave", Min = 0, Max = 2, Default = "0", InputType = ParameterInputType.Picker,
               PickerOptions = new() { [0] = "Aus", [1] = "Master", [2] = "Slave" } },
        
        // REV/Beschleunigung/Rampen (31-40)
        [31] = new ParameterMeta { Id = 31, Name = "REV empf. AUF", Rs232Name = "REV Empfindlichkeit AUF", Min = 100, Max = 1100, Default = "700" },
        [32] = new ParameterMeta { Id = 32, Name = "REV empf. ZU", Rs232Name = "REV Empfindlichkeit ZU", Min = 100, Max = 1100, Default = "500" },
        [33] = new ParameterMeta { Id = 33, Name = "Beschleunigung AUF", Rs232Name = "Beschleunigung AUF", Min = 50, Max = 3000, Default = "1000" },
        [34] = new ParameterMeta { Id = 34, Name = "Bremsrampe AUF", Rs232Name = "Bremsrampe AUF", Min = 10, Max = 2000, Default = "800" },
        [35] = new ParameterMeta { Id = 35, Name = "Bremsrampe ZU", Rs232Name = "Bremsrampe ZU", Min = 10, Max = 2000, Default = "1000" },
        [36] = new ParameterMeta { Id = 36, Name = "REV Zyklen", Rs232Name = "REV Zyklen", Min = 1, Max = 9999, Default = "5" },
        [37] = new ParameterMeta { Id = 37, Name = "DTA", Rs232Name = "DTA", Min = 0, Max = 1, Default = "0", InputType = ParameterInputType.Toggle },
        [38] = new ParameterMeta { Id = 38, Name = "Beschleunigung ZU", Rs232Name = "Beschleunigung ZU", Min = 50, Max = 3000, Default = "800" },
        [39] = new ParameterMeta { Id = 39, Name = "Stop und zu", Rs232Name = "Stop und zu", Min = 0, Max = 2, Default = "0", InputType = ParameterInputType.Picker,
               PickerOptions = new() { [0] = "Aus", [1] = "Ein", [2] = "Mit Warnung" } },
        [40] = new ParameterMeta { Id = 40, Name = "Sprache", Rs232Name = "Sprache", Min = 1, Max = 2, Default = "1", InputType = ParameterInputType.Picker,
               PickerOptions = new() { [1] = "Deutsch", [2] = "English" } },
        
        // Datum/Uhrzeit/System (41-50)
        [41] = new ParameterMeta { Id = 41, Name = "Datum", Rs232Name = "Datum", FormatType = ParameterFormatType.Date, InputType = ParameterInputType.Format, Notes = "Format: tt:mm:jj" },
        [42] = new ParameterMeta { Id = 42, Name = "Uhrzeit", Rs232Name = "Uhrzeit", FormatType = ParameterFormatType.Time, InputType = ParameterInputType.Format, Notes = "Format: hh:mm:ss" },
        [43] = new ParameterMeta { Id = 43, Name = "Sommerzeit", Rs232Name = "Sommerzeit", Min = 0, Max = 1, Default = "0", InputType = ParameterInputType.Toggle, Notes = "Doku zeigt tt:mm:jj in Default-Spalte" },
        [44] = new ParameterMeta { Id = 44, Name = "DACS", Rs232Name = "DACS", Min = 0, Max = 1, Default = "0", InputType = ParameterInputType.Toggle },
        [45] = new ParameterMeta { Id = 45, Name = "Schliess-System", Rs232Name = "Schliess-System", Min = 0, Max = 2, Default = "0", InputType = ParameterInputType.Picker,
               PickerOptions = new() { [0] = "Aus", [1] = "Modus 1", [2] = "Modus 2" } },
        [46] = new ParameterMeta { Id = 46, Name = "D LOC rt", Rs232Name = "D LOC rt", Min = 1, Max = 64, Default = "64" },
        [47] = new ParameterMeta { Id = 47, Name = "D LOC gn", Rs232Name = "D LOC gn", Min = 1, Max = 64, Default = "64" },
        [48] = new ParameterMeta { Id = 48, Name = "D AUX", Rs232Name = "D AUX", Min = 0, Max = 64, Default = "64" },
        [49] = new ParameterMeta { Id = 49, Name = "Dauer Strg", Rs232Name = "Dauer Strg", Min = 0, Max = 1, Default = "0", InputType = ParameterInputType.Toggle },
        [50] = new ParameterMeta { Id = 50, Name = "Referenzfahrt", Rs232Name = "Referenzfahrt", Min = 0, Max = 0, IsReadOnly = true, InputType = ParameterInputType.ReadOnly },
        
        // Sonderfunktionen (51-60)
        [51] = new ParameterMeta { Id = 51, Name = "Sonderfunktion", Rs232Name = "Sonderfunktion", Min = 0, Max = 10, Default = "0" },
        [52] = new ParameterMeta { Id = 52, Name = "Referenzfahrt o. WT", Rs232Name = "Referenzfahrt o. WT", Min = 0, Max = 10 },
        [53] = new ParameterMeta { Id = 53, Name = "Sommer-/Winterbetrieb", Rs232Name = "Sommer-/Winterbetrieb", Min = 0, Max = 1, Default = "0", InputType = ParameterInputType.Toggle },
        [54] = new ParameterMeta { Id = 54, Name = "Öffnungsweite So/Wi", Rs232Name = "Öffnungsweite So/Wi", Min = 40, Max = 100, Default = "80" },
        [55] = new ParameterMeta { Id = 55, Name = "Ladenschluss", Rs232Name = "Ladenschluss", Min = 0, Max = 1, Default = "0", InputType = ParameterInputType.Toggle },
        [56] = new ParameterMeta { Id = 56, Name = "DMSS", Rs232Name = "DMSS", Min = 0, Max = 6, Default = "0" },
        [57] = new ParameterMeta { Id = 57, Name = "E-Riegel", Rs232Name = "E-Riegel", Min = 0, Max = 1, Default = "0", InputType = ParameterInputType.Toggle },
        [58] = new ParameterMeta { Id = 58, Name = "Schleuse K1 NO/NC", Rs232Name = "Schleuse K1 NO/NC", Min = 0, Max = 1, Default = "0", InputType = ParameterInputType.Toggle },
        [59] = new ParameterMeta { Id = 59, Name = "autom. ZU nach REV", Rs232Name = "autom. ZU nach REV", Min = 0, Max = 2, Default = "1", InputType = ParameterInputType.Picker,
               PickerOptions = new() { [0] = "Aus", [1] = "Ein", [2] = "Verzögert" } },
        [60] = new ParameterMeta { Id = 60, Name = "Statusanzeige", Rs232Name = "Statusanzeige", Min = 0, Max = 0, IsReadOnly = true, InputType = ParameterInputType.ReadOnly },
        
        // Programmwahl/Einstellungen (61-74)
        [61] = new ParameterMeta { Id = 61, Name = "Programmwahlschalter", Rs232Name = "Programmwahlschalter", Min = 0, Max = 2, Default = "0", InputType = ParameterInputType.Picker,
               PickerOptions = new() { [0] = "Aus", [1] = "Modus 1", [2] = "Modus 2" } },
        [62] = new ParameterMeta { Id = 62, Name = "Werkeinstellung", Rs232Name = "Werkeinstellung", Min = 0, Max = 0, IsReadOnly = true, InputType = ParameterInputType.ReadOnly },
        [63] = new ParameterMeta { Id = 63, Name = "Notfall ZU", Rs232Name = "Notfall ZU", Min = 0, Max = 3, Default = "0" },
        [64] = new ParameterMeta { Id = 64, Name = "Ausgang AUX", Rs232Name = "Ausgang AUX", Min = 0, Max = 7, Default = "1" },
        [65] = new ParameterMeta { Id = 65, Name = "akustische Signale", Rs232Name = "akustische Signale", Min = 0, Max = 1, Default = "1", InputType = ParameterInputType.Toggle },
        [66] = new ParameterMeta { Id = 66, Name = "autom. Offen.h.", Rs232Name = "autom. Offen.h.", Min = 0, Max = 5 },
        [67] = new ParameterMeta { Id = 67, Name = "OSS3 aktiv ab", Rs232Name = "OSS3 aktiv ab", Min = 0, Max = 0, Unit = "mm", Default = "variabel", IsReadOnly = true, InputType = ParameterInputType.ReadOnly },
        [68] = new ParameterMeta { Id = 68, Name = "Ext. CSS", Rs232Name = "Ext. CSS", Min = 0, Max = 0, Unit = "mm", Default = "variabel", IsReadOnly = true, InputType = ParameterInputType.ReadOnly },
        [69] = new ParameterMeta { Id = 69, Name = "Ext. OSS", Rs232Name = "Ext. OSS", Min = 0, Max = 0, Unit = "mm", Default = "variabel", IsReadOnly = true, InputType = ParameterInputType.ReadOnly },
        [70] = new ParameterMeta { Id = 70, Name = "X-Achse", Rs232Name = "X-Achse", Min = 0, Max = 1000, Unit = "Grad-2" },
        [71] = new ParameterMeta { Id = 71, Name = "Y-Achse", Rs232Name = "Y-Achse", Min = 0, Max = 1000, Unit = "Grad-2" },
        [72] = new ParameterMeta { Id = 72, Name = "Red. REV AUF", Rs232Name = "Red. REV AUF", Min = 0, Max = 0, IsReadOnly = true, InputType = ParameterInputType.ReadOnly },
        [73] = new ParameterMeta { Id = 73, Name = "Reed. REV ZU", Rs232Name = "Reed. REV ZU", Min = 20, Max = 1000 },
        [74] = new ParameterMeta { Id = 74, Name = "XY Haltezeit", Rs232Name = "XY Haltezeit", Min = 0, Max = null, Unit = "s" },
        
        // Reserviert (75-79)
        [75] = new ParameterMeta { Id = 75, Name = "reserviert", IsReserved = true, IsReadOnly = true, InputType = ParameterInputType.ReadOnly },
        [76] = new ParameterMeta { Id = 76, Name = "reserviert", IsReserved = true, IsReadOnly = true, InputType = ParameterInputType.ReadOnly },
        [77] = new ParameterMeta { Id = 77, Name = "reserviert", IsReserved = true, IsReadOnly = true, InputType = ParameterInputType.ReadOnly },
        [78] = new ParameterMeta { Id = 78, Name = "reserviert", IsReserved = true, IsReadOnly = true, InputType = ParameterInputType.ReadOnly },
        [79] = new ParameterMeta { Id = 79, Name = "reserviert", IsReserved = true, IsReadOnly = true, InputType = ParameterInputType.ReadOnly },
        
        // Sicherheit (80-89)
        [80] = new ParameterMeta { Id = 80, Name = "Sicherheitscheck", Rs232Name = "Sicherheitscheck", IsReadOnly = true, InputType = ParameterInputType.ReadOnly, Notes = "Kein Range definiert" },
        [81] = new ParameterMeta { Id = 81, Name = "Sicherheitsintervall", Rs232Name = "Sicherheitsintervall", Min = 3, Max = 12, Default = "12" },
        [82] = new ParameterMeta { Id = 82, Name = "Akku Kontrolle", Rs232Name = "Akku Kontrolle", Min = 0, Max = 2, Default = "0", InputType = ParameterInputType.Picker,
               PickerOptions = new() { [0] = "Aus", [1] = "Ein", [2] = "Warnung" } },
        [83] = new ParameterMeta { Id = 83, Name = "Pull and GO", Rs232Name = "Pull and GO", Min = 0, Max = 4, Default = "1" },
        [84] = new ParameterMeta { Id = 84, Name = "Sicherheitselemente", Rs232Name = "Sicherheitselemente", Min = 0, Max = 3, Default = "0" },
        [85] = new ParameterMeta { Id = 85, Name = "Anz. Si.-Elemente", Rs232Name = "Anzahl Si.-Elemente", Min = 1, Max = 2, Default = "1", InputType = ParameterInputType.Picker,
               PickerOptions = new() { [1] = "1", [2] = "2" } },
        [86] = new ParameterMeta { Id = 86, Name = "Si1 Selbstcheck", Rs232Name = "Si1 Selbstcheck", Min = 0, Max = 1, Default = "0", InputType = ParameterInputType.Toggle },
        [87] = new ParameterMeta { Id = 87, Name = "Si2 Selbstcheck", Rs232Name = "Si2 Selbstcheck", Min = 0, Max = 1, Default = "0", InputType = ParameterInputType.Toggle },
        [88] = new ParameterMeta { Id = 88, Name = "Sperrzeit Ta Beginn", Rs232Name = "Sperrzeit Ta Beginn", FormatType = ParameterFormatType.TimeWithDot, InputType = ParameterInputType.Format, Notes = "Format: hh.mm.ss" },
        [89] = new ParameterMeta { Id = 89, Name = "Sperrzeit Ta Ende", Rs232Name = "Sperrzeit Ta Ende", FormatType = ParameterFormatType.TimeWithDot, InputType = ParameterInputType.Format, Notes = "Format: hh.mm.ss" },
        
        // Reserviert (90)
        [90] = new ParameterMeta { Id = 90, Name = "reserviert", IsReserved = true, IsReadOnly = true, InputType = ParameterInputType.ReadOnly },
        
        // Aufrichtung (91)
        [91] = new ParameterMeta { Id = 91, Name = "Aufrichtung (R-0,L-1)", Rs232Name = "Aufrichtung (R-0,L-1)", Min = 0, Max = 1, InputType = ParameterInputType.Picker,
               PickerOptions = new() { [0] = "Rechts (R)", [1] = "Links (L)" } },
        
        // Reserviert (92-94)
        [92] = new ParameterMeta { Id = 92, Name = "reserviert", IsReserved = true, IsReadOnly = true, InputType = ParameterInputType.ReadOnly },
        [93] = new ParameterMeta { Id = 93, Name = "reserviert", IsReserved = true, IsReadOnly = true, InputType = ParameterInputType.ReadOnly },
        [94] = new ParameterMeta { Id = 94, Name = "reserviert", IsReserved = true, IsReadOnly = true, InputType = ParameterInputType.ReadOnly },
        
        // Module (95)
        [95] = new ParameterMeta { Id = 95, Name = "Module", Rs232Name = "Module freischalten", IsReadOnly = true, InputType = ParameterInputType.ReadOnly, Notes = "Kein Range definiert" },
        
        // Reserviert (96-98)
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
}
