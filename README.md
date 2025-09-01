# ReisingerIntelliApp_V4 – .NET MAUI (MVVM)

ReisingerIntelliApp_V4 ist eine .NET MAUI App im MVVM‑Stil zur Konfiguration und Steuerung von IntelliDrive‑Geräten (RS‑485‑basierte Antriebe). 
Sie bietet u. a. WLAN‑Scan, lokalen Gerätescan, Struktur-/Gebäudeeditor, PDF‑/Bauplan‑Integration, sowie eine dynamische Tab‑Navigation für Geräteeinstellungen.

## Features (Auszug)
- **WifiScanPage**: WLANs scannen, verbinden, speichern
- **LocalDevicesScanPage**: lokales Subnetz scannen (Start/End‑IP), Geräte speichern
- **MainPage**: Auswahl gespeicherter Geräte (Dropdown „Local Devices“ & „Saved Devices“)
- **StructureEditorPage**: Gebäude/Stockwerke/Zuordnung von PDF‑Plänen
- **PDF‑Integration**: PDFs anzeigen (Vitvov.Maui.PDFView) oder PNG‑Konvertierung (Magick.NET) für Overlays/Buttons
- **Floor‑Plan‑Overlay**: Geräte auf Plan positionieren/verschieben/speichern, Open/Close‑Aktionen
- **DeviceSettingsTabbedPage**: dynamische Tabs (Time/Speed/IO/Protocol/DoorFunction) mit sauberem Tab‑Lifecycle
- **Intellidrive API**: `/intellidrive/parameters/set` u. a. Endpunkte, JSON‑basierte Konfiguration

## Architektur (Kurzüberblick)
- **Views**: `*Page.xaml` + minimale Code‑Behind (UI‑Glue)
- **ViewModels**: `*ViewModel.cs` (INotifyPropertyChanged, Commands, State)
- **Services**: `DeviceService`, `NavigationService`, `IntellidriveApiService`, `PdfService`, `FloorPlanService`, `WifiService`, `LocalScanService`
- **Controls**: Custom Controls (z. B. ZoomableImage/FloorPlanCanvas)
- **DI**: Registrierung in `MauiProgram.cs`
- **Navigation**: Shell + gezielte `Navigation.PushAsync()` für Tabbed‑Flows
- **Persistence**: Preferences/Local DB (z. B. für gespeicherte Geräte/Strukturen)

Siehe **ARCHITECTURE.md** für detaillierte Struktur, Naming‑Konventionen und Datenflüsse.

## Entwicklung
```bash
dotnet restore
dotnet build -c Debug
dotnet test
```

MAUI‑Projekt(e) kompilieren (Smoke‑Build):
```bash
dotnet build -c Release
```

## Ordnerstruktur (empfohlen)
```
/ReisingerIntelliApp_V4
  /Resources
  /Views
  /ViewModels
  /Services
  /Models
  /Controls
  /Platforms
  /Assets
  /docs
  ARCHITECTURE.md
  STATE.md
  README.md
  .github/
```

## Definition of Done (DoD)
- Feature vollständig (Navigation, Bindings, DI)
- CI grün (Build + Tests)
- Manuelles Smoke‑Testing (Android/iOS/Windows) dokumentiert
- Doku aktualisiert (README/STATE/ARCHITECTURE falls nötig)

## Lizenz / Sicherheit
Keine Secrets ins Repo. App‑Keys lokal verwalten.
