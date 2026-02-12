# Settings Menu - Theme Switching

## Implementierung

### Dark/Light Mode Toggle
Die App unterstützt jetzt einen vollständigen Dark/Light Mode Switcher in den Einstellungen.

### Komponenten

#### 1. SettingsService
- Speichert Theme-Präferenz in `Preferences` API
- Wendet Theme sofort an via `Application.Current.UserAppTheme`
- Lädt gespeichertes Theme beim App-Start

#### 2. SettingsPageViewModel
- Bindet an `IsDarkMode` und `IsLightMode` Properties
- Triggert Theme-Wechsel über SettingsService
- Benachrichtigt UI über PropertyChanged

#### 3. SettingsPage.xaml
- Verwendet `AppThemeBinding` für dynamische Farben
- Zeigt RadioButtons für Theme-Auswahl
- Header mit Back-Button für Navigation

### Theme-Farben

Definiert in `Resources/Styles/Colors.xaml`:

**Light Theme:**
- Background: `#FFFFFF` (Weiß)
- Text: `#000000` (Schwarz)
- Cards: `#F5F5F5` (Hellgrau)
- Borders: `#E0E0E0`

**Dark Theme:**
- Background: `#1E1E1E` (Dunkelgrau)
- Text: `#FFFFFF` (Weiß)
- Cards: `#2D2D2D` (Dunkelgrau)
- Borders: `#404040`

### Verwendung

```xaml
<!-- In XAML: -->
<Label TextColor="{AppThemeBinding Light={StaticResource LightTextColor}, Dark={StaticResource DarkTextColor}}" />

<!-- Oder direkt: -->
<Label>
    <Label.TextColor>
        <AppThemeBinding Light="#000000" Dark="#FFFFFF" />
    </Label.TextColor>
</Label>
```

### Navigation

- **Von MainPage:** Preferences Button (Footer, rechts) ? Settings Page
- **Zurück:** Back Button (Pfeil links oben) oder Hardware-Back-Button

### Persistenz

- Theme wird in `Preferences.Get/Set` gespeichert
- Beim App-Start wird gespeichertes Theme geladen
- Theme-Wechsel erfolgt sofort ohne Neustart

### Testen

1. App starten (startet im gespeicherten Theme, Standard: Dark)
2. Preferences Button klicken
3. Theme wählen (Dark ?? / Light ??)
4. Theme wechselt sofort
5. App neu starten ? Theme bleibt erhalten

## Erweiterung

Weitere Settings können hinzugefügt werden:
- Font-Skalierung
- Sprache
- Benachrichtigungen
- Verbindungs-Timeout
- Auto-Scan beim Start

Alle Einstellungen werden im SettingsService zentral verwaltet.
