# .github/copilot.md – Arbeitsweise für GitHub Copilot

## Ziel
Copilot soll Issues **stabil, zielgerichtet und vollständig** umsetzen – im Kontext der MAUI‑MVVM‑Architektur.

## Pflicht‑Pipeline je Issue/PR
1) **Analyse**
   - Issue lesen, Akzeptanzkriterien extrahieren.
   - `ARCHITECTURE.md` & `STATE.md` berücksichtigen.
   - Betroffene Komponenten bestimmen (Pages, ViewModels, Services, Models).

2) **Plan**
   - Kurzplan im PR‑Beschreibungstext formulieren (Stichpunkte).
   - Datenflüsse, Navigation, DI angeben.

3) **Implement**
   - MVVM: Logik in ViewModels/Services, Views nur Bindings/Events.
   - DI in `MauiProgram.cs` registrieren/erweitern.
   - Namen & Ordner gemäß Konvention.

4) **Tests**
   - Unit‑Tests für betroffene Logik hinzufügen/aktualisieren.
   - Falls UI‑kritisch: minimaler UI‑Smoke (Start/Navi/Command).

5) **Debug & Validation**
   - Lokaler Build: `dotnet build -c Release`
   - `dotnet test`
   - Manuelles Smoke‑Log (Plattformen) in PR abhaken.

6) **Docs**
   - `STATE.md` (Stand/ToDo) bei Bedarf aktualisieren.
   - README/ARCHITECTURE nur bei strukturellen Änderungen.

7) **PR**
   - `pull_request_template.md` vollständig abhaken.
   - `Closes #<nr>` im PR‑Text.

## Stilregeln
- Keine Logik in Code‑Behind (Ausnahmen: UI‑Glue)
- Async/await sauber, CancellationTokens falls sinnvoll
- Kein Copy/Paste von Secrets/Keys
- Logging sinnvoll, keine Chatty‑Logs

## Hinweise für typische App‑Flows
- **Local Scan**: Service‑Aufruf → DeviceService persistieren → ViewModel Liste updaten → UI über ObservableCollection
- **Floor‑Plan Pins**: Koordinaten in „Bild‑Space“ speichern, Transform via Zoom/Pan im Control abbilden
- **Parameters/Set**: Alle Tab‑ViewModels aggregieren → validiertes JSON → `IntellidriveApiService.SetParametersAsync` → Ergebnis UI
