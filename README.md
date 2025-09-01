# Reisinger IntelliApp V4

**.NET MAUI** App (net9.0) zur Verwaltung und Ansteuerung von **Intellidrive**-Geräten – inkl. **WiFi-Scan**, **lokalem Netzwerkscan**, **Gerätespeicherung**, **Struktur-/Gebäudeverwaltung** und **PDF/Floorplan**-Funktionen.

## Inhalt
- [Funktionen](#funktionen)
- [Architektur](#architektur)
- [Schnellstart](#schnellstart)
- [Wichtige Pfade](#wichtige-pfade)
- [Entwicklung & Tests](#entwicklung--tests)
- [Roadmap](#roadmap)

## Funktionen
- **Start/Tab-Navigation**: MainPage mit 4 Tabs (Structures, Levels, Wifi Dev, Local Dev)
- **WiFi-Scan (Android)**: Netzwerke scannen, verbundenes SSID markieren, Geräte speichern
- **Lokaler Netzwerkscan**: IP-Bereiche scannen, Intellidrive-Geräte erkennen/validieren
- **Gerätespeicher**: Persistente Liste (Preferences/SecureStorage)
- **Struktur-/Gebäudemanagement**: Buildings/Floors speichern & laden
- **PDF → PNG (Android)**: Erste Seite rendern; Zoom/Pan via `PanPinchContainer`

## Architektur
- **MVVM** (CommunityToolkit.MVVM), **DI** in `Helpers/ServiceCollectionExtensions.cs`
- Services: `DeviceService`, `IntellidriveApiService`, `WiFiManagerService`, `PdfConversionService`, `PdfStorageService`, `BuildingStorageService`, `NavigationService`
- **Navigation**: `AppShell → MainPage`; Detailseiten via `Navigation.PushAsync(...)`
- Details siehe **[ARCHITECTURE.md](ARCHITECTURE.md)**

## Schnellstart
### Voraussetzungen
- Windows 11 / macOS (aktuelle Xcode-Version für iOS)
- **Visual Studio 2022 17.10+** mit MAUI-Workload und .NET **9** SDK
- Android SDKs und Emulator bzw. Gerät (für WiFi-Funktionen)

### Build & Run
```bash
dotnet restore
dotnet build -c Debug
# Android
dotnet build -f net9.0-android -c Debug
```

In Visual Studio: Startprojekt `ReisingerIntelliApp_V4`, Ziel `Android` wählen und ausführen.

## Wichtige Pfade
- **Views**: `Views/` (XAML + Code-Behind, keine Business-Logik)
- **ViewModels**: `ViewModels/` (Commands/State)
- **Services**: `Services/`
- **DI**: `Helpers/ServiceCollectionExtensions.cs`
- **Ressourcen**: `Resources/` (Styles, Fonts, Images)
- **Plattform-Code**: `Platforms/Android` (WiFi/PDF-spezifisch)

## Entwicklung & Tests
- **Coding-Guidelines**: MVVM strikt, `x:DataType`, asynchron, Exceptions im Service-Layer abfangen.
- **Tests** (Empfehlung):
  - xUnit + FluentAssertions
  - Fakes für `HttpMessageHandler`
  - ViewModel-Tests (Commands, IsBusy, Validation)
- **Copilot-Workflow**: siehe **[.github/copilot.md](.github/copilot.md)**  
- **Issues**: Feature Template vorhanden → **[.github/ISSUE_TEMPLATE/feature_request.yml](.github/ISSUE_TEMPLATE/feature_request.yml)**

## Roadmap
- Gerätekonfiguration (Parameter lesen/schreiben)
- Erweiterte Fehler-UI & Telemetrie
- Persistenz verbessern (z. B. SQLite)
- CI: GitHub Actions (Build/Test/Artifacts)
