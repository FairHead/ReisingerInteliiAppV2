# Splash Screen Konfiguration

## Übersicht
Der App-Startbildschirm zeigt das originale Reisinger-Logo (`reisinger_r_kreis.png`) auf grauem Hintergrund (#1E1E1E).

## Konfiguration

### Datei-Struktur
```
Resources/
  Splash/
    splash.png          # Originales Reisinger-Logo (kopiert von Images/)
  Images/
    reisinger_r_kreis.png  # Original-Logo-Quelle
```

### .csproj Einstellung
```xml
<MauiSplashScreen Include="Resources\Splash\splash.png" 
                  Color="#1E1E1E" 
                  BaseSize="456,456" />
```

## Eigenschaften

- **Logo**: Originales Reisinger R im Kreis (PNG)
- **Hintergrundfarbe**: #1E1E1E (App-Hintergrund grau)
- **Größe**: 456x456px (Original-Größe)

## Testen

Um den neuen Splash Screen zu sehen:
1. **App deinstallieren** (falls bereits installiert)
2. **Neu deployen** auf Gerät/Emulator
3. **App starten** ? Splash Screen erscheint beim Start

## Hinweise

- Das PNG wird automatisch für alle Plattformen (Android, iOS, Windows) optimiert
- Die Hintergrundfarbe (#1E1E1E) entspricht der App-Hauptfarbe für einen nahtlosen Übergang
- Bei Änderungen am Logo muss `splash.png` manuell aus `Images/` aktualisiert werden
