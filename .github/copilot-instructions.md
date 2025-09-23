# Copilot Instructions – Reisinger IntelliApp V4 (.NET MAUI)

Dieses Dokument legt fest, wie Copilot in diesem Projekt zu arbeiten hat.  
Es ergänzt `.github/copilot.md` und macht klar, wie **TODO-Kommentare** auszuwerten sind.

---

## 1) Projektkontext

- **Framework:** .NET MAUI 9, C# 13 (via .NET SDK 9), MVVM-Pattern
- **Struktur:**
  - `Views/` – reine UI (XAML Pages)
  - `ViewModels/` – State + Business-Logik
  - `Services/` – API, Geräteverwaltung, PDF, WiFi, etc.
  - `Models/` – DTOs, PlacedDevice, Building/Floor
  - `Components/` – wiederverwendbare UI (PlacedDeviceControl, Header/Footer)
  - `Controls/` – Low-Level (PanPinchContainer)
  - `Helpers/`, `Converters/`, `Platforms/`
- **No-Touch Zonen:** AppShell.xaml, Core-Zoom/Pan, bestehende DI-Struktur (außer explizit gefordert)
- **Ground Truth:** ARCHITECTURE.md, specs/000-baseline/spec.md, memory/constitution.md

---

## 2) Copilot Arbeitsweise

### Workflow (immer einhalten)
1. **/specify** → erzeugt `spec.md` mit User Stories, Akzeptanzkriterien, Gaps, TODOs
2. **/plan** → erzeugt `plan.md` mit Architekturfolgen, betroffenen Dateien, DI-Wiring, Rollback
3. **/tasks** → erzeugt `tasks.md` mit atomaren Issues, Tests, Labels, Dependencies
4. **Implementierung** → Tasks nacheinander abarbeiten
5. **Evidence & PR** → Screens, Logs, Tests in `evidence.md`, PR referenziert `spec.md`

### Prinzipien
- Striktes MVVM, DI-first
- Erweiterung statt Umbau
- Kleine, nachvollziehbare PRs
- Performance Budgets einhalten
- Tests (Unit + UI) verpflichtend

---

## 3) TODO-Kommentare (verbindlich)

**Copilot MUSS TODO-Kommentare in Code und XAML als offizielle Work Items behandeln.**

### Gültige Muster
- `// TODO ...`
- `/// TODO ...`
- `# TODO ...`
- `<!--- TODO ... -->` (XAML)

### Erweiterte Syntax
- `<!--- TODO T[032], Now when i press the Move Device Button, the Move Buttons should be visible and interactible, but in the ui you dont see the Move Buttons -->`  
  - `T[032]` = Task-ID (vom Dev vergeben, optional)  
  - Rest = Beschreibung des Fehlers oder Feature-Gap

### Regeln
1. TODO-Kommentare gelten als **Developer Knowledge** → Copilot darf sie nicht ignorieren.  
2. Jeder TODO wird in die Artefakte übernommen:  
   - **spec.md:** unter *Gaps & Planned Features* aufführen  
   - **plan.md:** als Teil von Milestones/Epics aufnehmen  
   - **tasks.md:** als Issue mit ACs, DoD, Tests konkretisieren  
3. TODOs sind **High Priority** → Copilot muss sie wie P0/P1-Issues behandeln.  
4. TODOs bleiben im Code bestehen, bis ein PR sie **explizit schließt** (mit Tests + Evidence).  
5. Format möglichst standardisiert:  
   ```csharp
   // TODO T[045]: Fix Z-Order of Move Buttons – they are not visible above PlacedDeviceControl
   ```

---

## 4) Mapping TODO → Artefakte

| Ort im Code         | Beispiel-Kommentar                                                                                         | Artefakt                     |
|---------------------|----------------------------------------------------------------------------------------------------------|------------------------------|
| `Components/PlacedDeviceControl.xaml` | `<!--- TODO T[032], Move Buttons sichtbar und klickbar machen -->`                                 | tasks.md → Bugfix-Task       |
| `ViewModels/DeviceViewModel.cs`       | `// TODO: Validate min/max API params before save`                                                | spec.md → Gap, tasks.md Task |
| `Services/IntellidriveApiService.cs`  | `// TODO: Handle 401 Unauthorized response gracefully`                                            | plan.md → Error Handling     |
| `Controls/PanPinchContainer.cs`       | `// TODO: Reduce allocations in gesture loop`                                                     | plan.md → Perf Task          |

---

## 5) Output-Regeln für Copilot

- TODOs **immer referenzieren** mit Dateiname + Zeilennummer (wenn möglich).  
- In `spec.md` unter *Gaps & Planned Features* → TODOs als Bullet Points aufnehmen.  
- In `plan.md` → TODOs gruppieren (z. B. *Milestone M1 – Stability & UX*).  
- In `tasks.md` → jede TODO wird Issue mit:  
  - Titel `[P0][bug]` oder `[P1][feature]` + Kurzbeschreibung  
  - Rationale (TODO-Kommentar wörtlich zitieren)  
  - ACs/DoD (ausformuliert)  
  - Tests (Unit/UI)  
  - Labels (area, type, priority)  

---

## 6) Beispiel Transformation

**Code-Kommentar:**
```xml
<!--- TODO T[032], Now when i press the Move Device Button, 
the Move Buttons should be visible and interactible, 
but in the ui you dont see the Move Buttons -->
```

**Wird in tasks.md zu:**
```markdown
### T032 [P0][bug][area:floorplan] Move Buttons not visible/interactable

**Rationale:** TODO-Kommentar in `Components/PlacedDeviceControl.xaml`:  
"Now when i press the Move Device Button, the Move Buttons should be visible and interactible, but in the UI you don’t see the Move Buttons."

**Acceptance Criteria:**
- Move Buttons erscheinen beim Klick auf "Move Device Button"
- Buttons sind außerhalb der Card sichtbar (Halo)
- Buttons sind klickbar (Hit-Test stimmt)
- UI-Test prüft Sichtbarkeit & Interaktion

**DoD:** Sichtbar, klickbar, Test grün, PR schließt TODO.
```

---

## 7) Labels, Branches & Commits

- **Labels:** `area:floorplan`, `type:bug`, `priority:P0`, `platform:cross`
- **Branches:** `fix/<slug>`, `feat/<slug>`
- **Commit:**  
  ```
  fix(floorplan): make Move Buttons visible & interactible (closes TODO T[032])
  ```

---

## 8) Definition of Done (global)

- ACs erfüllt (Tests grün)  
- TODO-Kommentare im Code entweder entfernt oder in Folge-Issue überführt  
- Evidence in `evidence.md` gepflegt  
- Keine Verstöße gegen MVVM/DI/No-Touch  
- Build & Tests grün

---

**Kurz:** Copilot muss alle TODOs wie offizielle Work Items behandeln, in Spez/Plan/Tasks einfließen lassen und im PR nachweisbar schließen.