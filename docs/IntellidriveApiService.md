# IntellidriveApiService Documentation

## Übersicht

Der `IntellidriveApiService` ist ein zentraler Service für die Kommunikation mit Intellidrive-Geräten. Er unterstützt sowohl WiFi-Geräte (die standardmäßig unter `192.168.4.100` erreichbar sind) als auch lokale Netzwerkgeräte mit variablen IP-Adressen.

## Hauptfunktionen

### 1. Geräteerkennung und -test

Der Service bietet verschiedene Methoden zum Testen der Verbindung zu Intellidrive-Geräten:

```csharp
// Test eines Geräts an einer spezifischen IP-Adresse
var (success, response, message) = await _intellidriveApiService.TestIntellidriveConnectionAsync("192.168.1.100");

// Test eines WiFi-Geräts (verwendet automatisch 192.168.4.100)
var (success, response, message) = await _intellidriveApiService.TestWifiIntellidriveConnectionAsync();
```

### 2. Version-Endpoint (`/intellidrive/version`)

Dieser Endpoint ist der Hauptmechanismus zur Identifizierung von Intellidrive-Geräten. Er gibt folgende Informationen zurück:

```json
{
  "DeviceId": "9039fb45-4f49-48c8-aac6-3c179876cb7d",
  "Success": true,
  "Message": "1.9.7",
  "LatestFirmware": true,
  "FirmwareVersion": "",
  "Content": {
    "DEVICE_SERIALNO": "2511111"
  }
}
```

### 3. Modelle

#### IntellidriveVersionResponse
- **DeviceId**: Eindeutige ID des Geräts
- **Success**: Erfolg der Anfrage
- **Message**: Firmware-Version
- **LatestFirmware**: Ob die neueste Firmware installiert ist
- **FirmwareVersion**: Firmware-Version (optional)
- **Content**: Zusätzliche Geräteinformationen

#### IntellidriveVersionContent
- **DeviceSerialNumber**: Seriennummer des Geräts

## Integration in SaveDevicePageViewModel

Der Service ist in das `SaveDevicePageViewModel` integriert und wird in der `TestConnectionAsync`-Methode verwendet:

1. **WiFi-Verbindung**: Das Gerät verbindet sich mit dem ausgewählten WiFi-Netzwerk
2. **Intellidrive-Test**: Der Service testet, ob unter `192.168.4.100` ein Intellidrive-Gerät erreichbar ist
3. **Geräteinformationen**: Bei erfolgreicher Erkennung werden DeviceId, Version und Seriennummer angezeigt
4. **Netzwerk-Wiederherstellung**: Das ursprüngliche Netzwerk wird wiederhergestellt

## Erweiterte Funktionen

Der Service ist darauf ausgelegt, in Zukunft weitere Intellidrive-Endpoints zu unterstützen:

- `/intellidrive/status` - Gerätestatus
- `/intellidrive/control` - Gerätesteuerung
- `/intellidrive/config` - Konfiguration

## Konfiguration

### Service-Registrierung
```csharp
// In MauiProgram.cs / ServiceCollectionExtensions.cs
services.AddSingleton<IntellidriveApiService>();
```

### Timeout-Einstellungen
Der HttpClient ist auf 10 Sekunden Timeout konfiguriert, um schnelle Antworten zu gewährleisten.

## Fehlerbehandlung

Der Service implementiert umfassende Fehlerbehandlung:

- **HTTP-Fehler**: Status-Codes und Fehlermeldungen
- **Timeout-Fehler**: Zeitüberschreitungen werden erkannt
- **JSON-Fehler**: Ungültige Antworten werden abgefangen
- **Netzwerk-Fehler**: Verbindungsprobleme werden behandelt

Alle Fehler werden über Debug.WriteLine geloggt und als Teil der Response zurückgegeben.

## Verwendungsbeispiel

```csharp
public class SaveDevicePageViewModel
{
    private readonly IntellidriveApiService _intellidriveApiService;

    public SaveDevicePageViewModel(IntellidriveApiService intellidriveApiService)
    {
        _intellidriveApiService = intellidriveApiService;
    }

    private async Task TestDeviceAsync()
    {
        var (success, response, message) = await _intellidriveApiService.TestWifiIntellidriveConnectionAsync();
        
        if (success && response != null)
        {
            // Gerät gefunden
            var deviceId = response.DeviceId;
            var version = response.Message;
            var serialNumber = response.Content?.DeviceSerialNumber;
            
            // Geräteinformationen verwenden...
        }
        else
        {
            // Fehler oder kein Intellidrive-Gerät gefunden
            Debug.WriteLine($"Gerät nicht gefunden: {message}");
        }
    }
}
```

## Zukünftige Erweiterungen

Der Service ist so konzipiert, dass er leicht erweitert werden kann:

1. **Zusätzliche Endpoints**: Neue API-Endpunkte können einfach hinzugefügt werden
2. **Authentifizierung**: Support für authentifizierte Anfragen
3. **Batch-Operationen**: Mehrere Geräte gleichzeitig testen
4. **Caching**: Häufig verwendete Geräteinformationen zwischenspeichern
