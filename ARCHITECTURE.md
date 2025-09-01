# ARCHITECTURE.md – ReisingerIntelliApp_V4

## 1. Überblick
Ziel ist eine robuste MAUI‑App (MVVM) zur Verwaltung/Steuerung von IntelliDrive‑Antrieben. Schwerpunkt:
- Netzwerk‑Scans (WLAN & lokal)
- Gerätemanagement (Saved/Local Devices)
- PDF/Floor‑Plan‑Ansicht mit interaktiven Overlays (Geräte‑Pins)
- Dynamische Tabbed‑Settings pro Gerät
- API‑Kommunikation (RS‑485/REST‑Bridge, JSON)

## 2. Schichtenmodell
- **Views (`/Views`)**: XAML‑Pages (z. B. `WifiScanPage.xaml`, `LocalDevicesScanPage.xaml`, `MainPage.xaml`, `StructureEditorPage.xaml`, `DeviceSettingsTabbedPage.xaml`)
- **ViewModels (`/ViewModels`)**: Zustandsverwaltung, Commands, Validierung (z. B. `WifiScanViewModel`, `LocalScanViewModel`, `MainViewModel`, `StructureEditorViewModel`, `DeviceSettingsViewModel`)
- **Services (`/Services`)**:
  - `WifiService`: Scan/Connect/Save
  - `LocalScanService`: IP‑Range‑Scan über aktuelles Subnetz
  - `DeviceService`: CRUD für Geräte, Saved/Local Listen, Dropdown‑Daten
  - `IntellidriveApiService`: HTTP/RS‑485‑Bridge, `/intellidrive/parameters/set`, Status‑Polling
  - `PdfService`: Laden/Rendern (Vitvov.Maui.PDFView) oder PNG‑Konvertierung (Magick.NET)
  - `FloorPlanService`: Positionen/Layouts persistent speichern, Transformierung (Zoom/Pan/Scale)
  - `NavigationService`: Routen/Navi‑Flows, Übergabe von Parametern (z. B. selected Device)
- **Models (`/Models`)**: `Device`, `FloorPlan`, `Building`, `Level`, `DevicePin`, `ApiParameterSet`, etc.
- **Controls (`/Controls`)**: `ZoomableImage`, `FloorPlanCanvas`, ggf. `PinButton`
- **Resources (`/Resources`)**: Styles, Colors, Templates
- **DI**: `MauiProgram.cs` registriert alle Services & ViewModels (Singleton/Scoped je nach Bedarf)

## 3. Navigation
- **Shell** definiert High‑Level Tabs/Pages (z. B. Wifi, Local Devices, Structure, Main)
- **Geräte‑Settings**: `DeviceSettingsTabbedPage` wird per `Navigation.PushAsync()` geöffnet, um flüssige Tab‑Wechsel zu erlauben. Jede Tab‑Child‑Page erhält ihr eigenes ViewModel (z. B. `TimeSettingsViewModel`, `SpeedSettingsViewModel`, `IoSettingsViewModel`, `ProtocolSettingsViewModel`, `DoorFunctionViewModel`).

## 4. Datenflüsse (Beispiele)
### 4.1 Local Device Scan
`LocalDevicesScanPage` → `LocalScanViewModel.ScanCommand` → `LocalScanService.ScanSubnet(startIp, endIp, iface)`  
Ergebnis → `DeviceService.UpsertLocalDevices()` → Anzeige in Liste + persistente Speicherung → Dropdown in `MainPage` via `MainViewModel` aktualisiert.

### 4.2 PDF/Floor‑Plan
`StructureEditorPage` lädt `FloorPlan` (PDF/PNG).  
`FloorPlanService` liefert Geräte‑Pins; `ZoomableImage/FloorPlanCanvas` transformiert Koordinaten mit aktuellem Zoom/Pan.  
Pin‑Interaktion triggert Commands (Open/Close/Config) → `IntellidriveApiService`.

### 4.3 Parameter‑Set übertragen
`DeviceSettings*ViewModel.SaveCommand` sammelt alle Parameterwerte aus allen Tab‑Pages → `IntellidriveApiService.SetParametersAsync(json)` → Validierung & Rückmeldung.  
Fehler werden geloggt und als UI‑State (Toast/Dialog) gemeldet.

## 5. MVVM‑Konventionen
- **Keine Logik** in Code‑Behind, außer UI‑Bindings/Events
- **INotifyPropertyChanged** via BaseViewModel
- **Async Commands** (CancellationToken wo sinnvoll)
- **Validation** vor API‑Calls
- **State immutability** wo möglich; ObservableCollection für Listen

## 6. Naming & Struktur
- Pages: `*Page.xaml` (+ `.xaml.cs`)
- ViewModels: `*ViewModel.cs`
- Services: `*Service.cs`
- Models: `*.cs` (Subfolder je Domain: Device, FloorPlan, Api)
- Commands: `<Verb>Command` (z. B. `ScanCommand`, `SaveCommand`)
- Ressourcen: `Resources/Styles.xaml`, `Resources/Colors.xaml`, `Resources/ControlTemplates.xaml`

## 7. Fehlerbehandlung & Logging
- Zentrales `ILogger<T>` oder `AppTrace`‑Helper
- RS‑485/HTTP‑Kommunikation mit Retry/Timeout
- UI‑Fehler: freundliche Meldungen, Logs ohne sensitive Daten
- Optional: Telemetrie‑Hook (AppCenter/own endpoint)

## 8. Tests
- Unit‑Tests (ViewModels/Services) mit Mocks (HTTP/Storage)
- UI‑Smoke (Starten, Navigation, elementare Aktionen)
- Ziel: deterministische Tests ohne Netzwerkabhängigkeit (Use Fakes)

## 9. Build/CI
- .NET 8
- Windows Runner (MAUI‑Build‑Smoke)
- Pipelines: Restore → Build → Test → (optional) Artifacts

## 10. Erweiterungen (Roadmap‑Ideen)
- Geräte‑Discovery via mDNS/UDP‑Broadcast
- Persistenz via SQLite
- Echte E2E‑UI‑Tests auf Emulator
- Offline‑Queues für API‑Calls
