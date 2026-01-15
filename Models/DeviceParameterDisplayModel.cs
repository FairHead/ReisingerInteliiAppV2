using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json;

namespace ReisingerIntelliApp_V4.Models;

/// <summary>
/// Display model for a device parameter with name and editable value.
/// Used in the DeviceParametersPage to show parameter list.
/// </summary>
public partial class DeviceParameterDisplayModel : ObservableObject
{
    /// <summary>
    /// Parameter ID (1-98)
    /// </summary>
    [ObservableProperty]
    private int _id;

    /// <summary>
    /// Display name for the parameter
    /// </summary>
    [ObservableProperty]
    private string _name = string.Empty;

    /// <summary>
    /// Current value as string (editable in UI)
    /// </summary>
    [ObservableProperty]
    private string _value = string.Empty;

    /// <summary>
    /// Original value from device (for change detection)
    /// </summary>
    [ObservableProperty]
    private string _originalValue = string.Empty;

    /// <summary>
    /// Whether the value is currently being loaded from the device
    /// </summary>
    [ObservableProperty]
    private bool _isValueLoading = true;

    /// <summary>
    /// Whether this parameter has been modified
    /// </summary>
    public bool IsModified => Value != OriginalValue;

    /// <summary>
    /// Creates a placeholder model for immediate display while loading
    /// </summary>
    public static DeviceParameterDisplayModel CreatePlaceholder(int id)
    {
        return new DeviceParameterDisplayModel
        {
            Id = id,
            Name = GetParameterName(id),
            Value = string.Empty,
            OriginalValue = string.Empty,
            IsValueLoading = true
        };
    }

    /// <summary>
    /// Updates this model with the actual value from the API
    /// </summary>
    public void SetValueFromApi(IntellidriveParameterValue apiValue)
    {
        var valueStr = apiValue.V.ValueKind switch
        {
            JsonValueKind.Number => apiValue.V.GetRawText(),
            JsonValueKind.String => apiValue.V.GetString() ?? string.Empty,
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => apiValue.V.GetRawText()
        };

        Value = valueStr;
        OriginalValue = valueStr;
        IsValueLoading = false;
    }

    /// <summary>
    /// Creates a display model from an API parameter value
    /// </summary>
    public static DeviceParameterDisplayModel FromApiValue(IntellidriveParameterValue apiValue)
    {
        var valueStr = apiValue.V.ValueKind switch
        {
            JsonValueKind.Number => apiValue.V.GetRawText(),
            JsonValueKind.String => apiValue.V.GetString() ?? string.Empty,
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => apiValue.V.GetRawText()
        };

        return new DeviceParameterDisplayModel
        {
            Id = apiValue.Id,
            Name = GetParameterName(apiValue.Id),
            Value = valueStr,
            OriginalValue = valueStr,
            IsValueLoading = false
        };
    }

    /// <summary>
    /// Gets a human-readable name for a parameter ID.
    /// TODO: These should come from a config file or API in the future.
    /// </summary>
    private static string GetParameterName(int id)
    {
        return id switch
        {
            1 => "Betriebsmodus",
            2 => "Antriebstyp",
            3 => "Türtyp",
            4 => "Schließrichtung",
            5 => "Automatik Verzögerung",
            6 => "Automatik Zeit",
            7 => "Teilöffnung",
            8 => "Impulsart",
            9 => "Sicherheitsleiste",
            10 => "Geschwindigkeit Auf",
            11 => "Geschwindigkeit Zu",
            12 => "Geschwindigkeit Auf Schleich",
            13 => "Geschwindigkeit Zu Schleich",
            14 => "Geschwindigkeit Referenz",
            15 => "Schleichstrecke",
            16 => "Beschleunigung",
            17 => "Kraft Auf",
            18 => "Kraft Zu",
            19 => "Kraft Schleich",
            20 => "Reversierstrecke",
            21 => "Weg Auf",
            22 => "Weg Zu",
            23 => "Nachlaufzeit Auf",
            24 => "Nachlaufzeit Zu",
            25 => "Voralarm Ein",
            26 => "Voralarm Aus",
            27 => "Lüfter Einschalttemperatur",
            28 => "Motorsteuerung",
            29 => "Eingang 1 Funktion",
            30 => "Eingang 2 Funktion",
            31 => "Totmannzeit",
            32 => "Pausenzeit Reversierer",
            33 => "Maximalzeit Motor",
            34 => "Anzugsstrom",
            35 => "Haltestrom",
            36 => "Strommessung Intervall",
            37 => "Debug Mode",
            38 => "PWM Frequenz",
            39 => "Softstart",
            40 => "Motorfreigabe",
            41 => "Betriebsstunden",
            42 => "Zyklen",
            43 => "Fehlercode",
            44 => "Letzter Fehler",
            45 => "Warnung",
            46 => "Eingang 1 Status",
            47 => "Eingang 2 Status",
            48 => "Eingang 3 Status",
            49 => "Ausgang 1 Status",
            50 => "Ausgang 2 Status",
            51 => "Temperatur",
            52 => "Spannung",
            53 => "Strom",
            54 => "Position",
            55 => "Geschwindigkeit aktuell",
            56 => "Reserve 56",
            57 => "Reserve 57",
            58 => "Reserve 58",
            59 => "Funk Kanal",
            60 => "Funk Adresse",
            61 => "WLAN Modus",
            62 => "WLAN Kanal",
            63 => "Bluetooth",
            64 => "LED Helligkeit",
            65 => "Summer",
            66 => "Reserve 66",
            67 => "Reserve 67",
            68 => "Reserve 68",
            69 => "Reserve 69",
            70 => "Reserve 70",
            71 => "Reserve 71",
            72 => "Reserve 72",
            73 => "Reserve 73",
            74 => "Reserve 74",
            75 => "Reserve 75",
            76 => "Reserve 76",
            77 => "Reserve 77",
            78 => "Reserve 78",
            79 => "Reserve 79",
            80 => "Firmware Version",
            81 => "Hardware Version",
            82 => "Seriennummer Teil 1",
            83 => "Seriennummer Teil 2",
            84 => "Produktionsdatum",
            85 => "Kalibrierung Status",
            86 => "Werkseinstellung",
            87 => "Reserve 87",
            88 => "Reserve 88",
            89 => "Reserve 89",
            90 => "Reserve 90",
            91 => "Reserve 91",
            92 => "Reserve 92",
            93 => "Reserve 93",
            94 => "Reserve 94",
            95 => "Reserve 95",
            96 => "Reserve 96",
            97 => "Reserve 97",
            98 => "Reserve 98",
            _ => $"Parameter {id}"
        };
    }
}
