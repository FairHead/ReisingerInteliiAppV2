# STATE – Entwicklungsstand & offene Punkte

> Aktualisiert: 1. September 2025

## Aktueller Stand
- **Architektur** auf MVVM/DI umgestellt, Views/VM/Services sauber getrennt.
- **WiFi-Scan** (Android) funktionsfähig (`WiFiManagerService`), Liste mit Markierung „isConnected“.
- **Lokaler Netzwerkscan** (IP-Bereich) implementiert (`DeviceService`, `LocalDevicesScanPageViewModel`).
- **Intellidrive-Validierung** über `IntellidriveApiService` (JSON-Parsing & Fehlerfälle).
- **Struktur-/Gebäudemanagement** angelegt (`BuildingStorageService`).
- **PDF-Konvertierung** (Android) + **Pan/Pinch** Control vorhanden.
- **DI-Setup** in `Helpers/ServiceCollectionExtensions.cs` vollständig.

## Wichtige Hinweise
- In `MauiProgram.cs` werden in **DEBUG** aktuell **Preferences** geleert und `SecureStorage.Remove("SavedDevices")` ausgeführt – nur für Entwicklung sinnvoll.
- **AppShell** routet nur auf `MainPage`; weitere Navigation erfolgt per `Navigation.PushAsync(...)`.

## Offene Punkte / ToDo
- [ ] **Testsuite** hinzufügen (xUnit):
  - [ ] `IntellidriveApiService` (HTTP/JSON, Timeouts, Fehlerpfade)
  - [ ] `DeviceService` (IP-Range, Parallelität, Cancellation)
  - [ ] ViewModels (Commands/IsBusy/Validation)
- [ ] **Fehler-UI** verbessern (zentrale ErrorView/Toast/Dialog)
- [ ] **Logging** auf `ILogger` migrieren, strukturierte Logs
- [ ] **Persistenz**: Migrationsstrategie für geänderte JSON-Schemata
- [ ] **CI**: GitHub Actions Pipeline
- [ ] **Dokumentation**: Schritt-für-Schritt „Beitragende-Guideline“
- [ ] **DeviceConfig**: UI & API-Calls zum Lesen/Schreiben von Parametern
- [ ] **PDF/Floorplan**: Geräte-Overlays speichern/verschieben (Drag&Drop auf Plan)
