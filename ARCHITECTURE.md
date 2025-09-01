# Reisinger IntelliApp V4 – Architektur & technische Leitlinien

> Stand: 1. September 2025 • Zielplattformen: **net9.0-android / ios / maccatalyst / windows**  
> Projekt: `ReisingerIntelliApp_V4` (MAUI, MVVM, Dependency Injection, CommunityToolkit.MVVM)

## 1) Übersicht

Die App ist eine .NET MAUI Anwendung mit **MVVM-Pattern**, **Dependency Injection**, eigener **Tab- und Seitenlogik** und Services für **Gerätescans (WiFi/Local Network)**, **Intellidrive-API**, **PDF-Konvertierung/-Speicherung** sowie **Struktur-/Gebäudemanagement**.

### Ziele
- Intuitives UI (Dark Theme mit Header/Footer, Tabs)
- Stabile Netzwerkscans (WiFi + lokales Subnetz)
- Persistenz von Geräten und Gebäudestrukturen
- Konsistente Navigation (AppShell + interne Tabs)
- Klare Erweiterbarkeit (Services, ViewModels, Views)

## 2) Projektstruktur (vereinfacht)

```
ReisingerIntelliApp_V4/
├─ App.xaml, App.xaml.cs
├─ AppShell.xaml, AppShell.xaml.cs
├─ MauiProgram.cs
├─ ReisingerIntelliApp_V4.csproj    # net9.0-… TFMs
│
├─ Components/                      # Wiederverwendbare UI-Bausteine
│  ├─ AppHeader.xaml(.cs)
│  ├─ AppFooter.xaml(.cs)
│  └─ BackgroundLogo.xaml(.cs)
│
├─ Controls/
│  └─ PanPinchContainer.cs          # Gesten/Zoom-Container
│
├─ Converters/                      # XAML Value Converters
│  ├─ BoolToColorConverter.cs
│  ├─ BoolToStatusConverter.cs
│  ├─ BoolToStrokeThicknessConverter.cs
│  ├─ IntToBoolConverter.cs
│  ├─ PercentageToProgressConverter.cs
│  ├─ SavedDeviceOpacityConverter.cs
│  └─ StringToBoolConverter.cs (… WifiConverters.cs)
│
├─ Helpers/
│  └─ ServiceCollectionExtensions.cs # DI-Registrierung (Services/VM/Pages)
│
├─ Models/
│  ├─ DeviceModel.cs
│  ├─ LocalNetworkDeviceModel.cs
│  ├─ NetworkDataModel.cs
│  ├─ IntellidriveApiModels.cs / IntellidriveVersionResponse.cs
│  ├─ Building.cs, Floor.cs
│  └─ TabItemModel.cs
│
├─ Services/
│  ├─ DeviceService.cs               # Scant IP-Ranges, WiFi/LAN Devices
│  ├─ IntellidriveApiService.cs      # REST-Aufrufe zu Intellidrive
│  ├─ WiFiManagerService.cs          # (Android) WLAN-Scan & Status
│  ├─ BuildingStorageService.cs / IBuildingStorageService.cs
│  ├─ PdfConversionService.cs        # (Android) PDF→PNG, sonst Stub
│  ├─ PdfStorageService.cs           # Pfade/Cache-Management
│  ├─ AuthenticationService.cs       # (Platzhalter für Auth)
│  └─ NavigationService.cs           # Optionale Navigationskapsel
│
├─ ViewModels/
│  ├─ BaseViewModel.cs               # IsBusy, Title, Messaging
│  ├─ MainPageViewModel.cs(.Fixed)   # Hauptlogik, Tab-Steuerung
│  ├─ WifiScanPageViewModel.cs(.WifiScanViewModel.cs)
│  ├─ LocalDevicesScanPageViewModel.cs
│  ├─ SaveDevicePageViewModel.cs, SaveLocalDevicePageViewModel.cs
│  ├─ StructuresViewModel.cs, StructureEditorViewModel.cs
│
└─ Views/
   ├─ MainPage.xaml(.cs)             # App Einstieg (AppShell → MainPage)
   ├─ WifiScanPage.xaml(.cs)         # WLAN-Gerätesuche, Speichern
   ├─ LocalDevicesScanPage.xaml(.cs) # IP-Bereich scannen, Speichern
   ├─ SaveDevicePage.xaml(.cs), SaveLocalDevicePage.xaml(.cs)
   ├─ StructureEditorPage.xaml(.cs)
   └─ (weitere: z. B. künftige DeviceConfig-Seiten)
```

## 3) Lebenszyklus & Navigation

- **AppShell** ruft **MainPage** auf (`Route="MainPage"`). AppBar/Tab-Bar werden **custom** in XAML umgesetzt (nicht Shell-Tabs).  
- **Navigation**:
  - Innerhalb von **MainPage** werden Tabs gesteuert (z. B. „Structures“, „Levels“, „Wifi Dev“, „Local Dev“).
  - Für Detailseiten (z. B. `SaveDevicePage`, `StructureEditorPage`) wird via `INavigation`/`Navigation.PushAsync` navigiert (siehe `NavigationService` oder direkt aus ViewModels via DI-injizierte Services).
- **ViewModel-Bindings**: Jede View setzt `x:DataType` für **starke Bindungen**. Business-Logik ist **nicht** im Code-Behind, sondern im zugehörigen ViewModel.

## 4) MVVM-Datenfluss

1. **View** (XAML) bindet an **ViewModel** (DI-registriert).  
2. ViewModel nutzt **Services** (HTTP, Storage, Scans).  
3. **Models** repräsentieren Daten (z. B. `DeviceModel`, `Building`, `Floor`).  
4. **Converters** formatieren Werte für die View.  
5. **Commands** (CommunityToolkit.MVVM `[RelayCommand]`) triggern Aktionen, berücksichtigen **CancellationToken** & **IsBusy**.

**Beispiel Flows**

- **WiFi-Scan**: `WifiScanPageViewModel` → `WiFiManagerService` (Android-Only) → `DeviceService` (Mapping/Erweiterung) → Anzeige/Save.
- **Lokaler Netzwerkscan**: `LocalDevicesScanPageViewModel` generiert IP-Liste → `DeviceService.ScanForLocalNetworkDevicesAsync` → parallelisierte Probe → `IntellidriveApiService.VerifyDeviceAsync` → Ergebnisliste + Persistenz.
- **PDF/Floor-Plan**: `PdfStorageService` verwaltet Pfade; `PdfConversionService` rendert erste Seite als PNG (Android via `PdfRenderer`). UI nutzt `PanPinchContainer` für Zoom/Pan.

## 5) Services & Querschnittsthemen

- **IntellidriveApiService**
  - Basiskommunikation (HTTP, JSON, Fehlerbehandlung, Timeouts).
  - Default IP **192.168.4.100** für WiFi-Devices; Local-LAN-IPs variabel.
  - Validierung via `IntellidriveVersionResponse`.
- **DeviceService**
  - Erzeugt IP-Ranges (Start/Ende).
  - Führt parallele Pings/HTTP-Probes aus (Throttling beachten).
  - Mapt Antworten → `LocalNetworkDeviceModel` / `DeviceModel`.
  - Persistiert (Preferences/SecureStorage) via JSON (siehe `GetSavedDevicesAsync`, `SaveDeviceAsync`, …).
- **BuildingStorageService**
  - Serialisierung von `Building`/`Floor`-Strukturen in App-Storage.
- **WiFiManagerService** *(nur Android)*
  - Scant Netzwerke, markiert **aktuelles SSID** als verbunden.
- **PdfConversionService**
  - Android: echte Konvertierung; andere Plattformen: Stub, liefert Zielpfad zurück.

## 6) Fehlerbehandlung & Logging

- Verwenden von `try/catch` im Service-Layer (z. B. `HttpRequestException`, `JsonException`).  
- `Debug.WriteLine` als Baselogging (in DEBUG via `builder.Logging.AddDebug()` aktiv).  
- ViewModels setzen `HasValidationError`, `ValidationMessage`, `ScanStatusMessage` etc.

## 7) Konfiguration & DI

- **`Helpers/ServiceCollectionExtensions.cs`** registriert:
  - **HttpClient** (ggf. mit Handler für Timeouts/Headers).
  - Alle **Services** (Transient/Singleton sinnvoll wählen).
  - **ViewModels** und **Views**.
- **`MauiProgram.cs`**: ruft `RegisterServices()` auf und aktiviert Debug-Logging.
  - Hinweis: Aktuell werden `Preferences.Default.Clear()` und `SecureStorage.Remove("SavedDevices")` im Startup ausgeführt (nur in DEV sinnvoll).

## 8) Persistenz

- **Preferences/SecureStorage** für leichtgewichtige Daten (z. B. gespeicherte Geräte, zuletzt verbundene SSID).  
- **Dateisystem** (Cache/AppData) für PDFs & abgeleitete PNGs.  
- Erweiterbar Richtung SQLite/EF Core bei wachsender Komplexität.

## 9) UI-/UX-Richtlinien

- **Dark Theme** als Standard, Typografie: *SpaceMono*.
- **Header/Footer** + **Tab-Bar** (4 Tabs).
- Feedback über **Status-Labels**, **Progress**, **Badges**.
- **x:DataType** in XAML für compile-time Binding Checks.
- **Keine Business-Logik** im Code-Behind.
- **Asynchronität**: UI immer responsive halten (IsBusy, Cancellation).

## 10) Tests (Empfehlung)

- **Unit Tests** (xUnit + FluentAssertions):
  - `IntellidriveApiService` mit **Fake `HttpMessageHandler`**.
  - `DeviceService` IP-Range-Generator & Parallel-Scan-Logik (Throttling, Cancellation).
  - `BuildingStorageService` Serialisierung/Deserialisierung.
- **ViewModel-Tests**: Commands/State-Transitions (über DI gemockte Services).
- **UI-Tests** (optional): .NET MAUI UITest/Playwright für kritische Flows.

## 11) Build & CI (Empfehlung)

- .NET 9 SDK, VS 2022 17.10+.
- Pipeline (GitHub Actions) mit:
  - `dotnet restore/build`
  - `dotnet test`
  - Lint/format (z. B. `dotnet format`)
  - Artifacts (Android .aab/.apk in Release)

---

**Erweiterungen**: Device-Konfiguration, strukturierte Logs (ILogger), robuste Fehler-UI, echte Auth, Offline-Cache, Diagnostics/Telemetry.
