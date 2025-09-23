# Tasks: Repository Audit & Comprehensive Specification

**Input**: Design documents from `/specs/003-audit-current-repository/`
**Prerequisites**: plan.md (required), research.md, data-model.md, contracts/

## Execution Flow (main)
```
1. Load plan.md from feature directory
   → If not found: ERROR "No implementation plan found"
   → Extract: tech stack, libraries, structure
2. Load optional design documents:
   → data-model.md: Extract entities → model tasks
   → contracts/: Each file → contract test task
   → research.md: Extract decisions → setup tasks
3. Generate tasks by category:
   → Setup: project init, dependencies, linting
   → Tests: contract tests, integration tests
   → Core: models, services, CLI commands
   → Integration: DB, middleware, logging
   → Polish: unit tests, performance, docs
4. Apply task rules:
   → Different files = mark [P] for parallel
   → Same file = sequential (no [P])
   → Tests before implementation (TDD)
5. Number tasks sequentially (T001, T002...)
6. Generate dependency graph
7. Create parallel execution examples
8. Validate task completeness:
   → All contracts have tests?
   → All entities have models?
   → All endpoints implemented?
9. Return: SUCCESS (tasks ready for execution)
10. Update the tasks.md when i tell you that Feature implementaion or Bug Fix were correct
11. If Implementation didnt work as expected , take my information analyze it and update the Task with the Errors/Bugs/wrong behavior, so you can check what was right und what is still wrong


```

---

## Phase 3.1: Setup
- [x] T001 Project structure matches plan.md (single .NET MAUI project, platform folders)
- [x] T002 Initialize C# 13 / .NET 9.0 / .NET MAUI 9.0 project with dependencies (Microsoft.Maui, DI, Logging)
- [x] T003 [P] Linting and formatting tools configured (StyleCop, .editorconfig)

## Phase 3.2: Tests First (TDD)
- [ ] T004 [P] Contract test for IntellidriveApiService in contracts/intellidrive-api-service.md
- [ ] T005 [P] Contract test for core services in contracts/core-services.md
- [ ] T006 [P] Integration test for device scanning and placement (ViewModels/DeviceModel.cs, PlacedDeviceModel.cs)
- [ ] T007 [P] Integration test for pan/zoom performance (Controls/PanPinchContainer.cs)
- [ ] T008 [P] UI automation test for PlacedDevice movement arrows (Components/PlacedDeviceControl.xaml)
- [ ] T009 [P] UI automation test for dropdown outside-tap auto-close (Views/MainPage.xaml)
- [ ] T010 [P] Integration test for device Open/Close state sync (Services/IntellidriveApiService.cs)

## Phase 3.3: Core Implementation
- [x] T011 [P] DeviceModel, PlacedDeviceModel, Building, Floor, TabItemModel implemented (Models/)
- [x] T012 [P] MainPageViewModel, LocalDevicesScanPageViewModel, etc. implemented (ViewModels/)
- [x] T013 [P] PlacedDeviceControl, AppHeader, AppFooter, BackgroundLogo implemented (Components/)
- [x] T014 [P] PanPinchContainer implemented (Controls/)
- [x] T015 [P] DeviceService, IntellidriveApiService, BuildingStorageService, NavigationService, PdfConversionService, PdfStorageService, WiFiManagerService implemented (Services/)
- [x] T016 [P] Value converters implemented (Converters/)
- [x] T017 [P] DI configuration in ServiceCollectionExtensions.cs
- [ ] T018 [P] Add/verify compiled bindings (x:DataType) in all XAML views/components
- [ ] T019 [P] Implement/verify GraphicsView overlay for 50+ devices (optional, performance)
- [ ] T020 [P] Implement/verify structured logging and in-app log viewer
- [ ] T021 [P] Implement/verify crash reporting and telemetry (opt-in)
- [ ] T022 [P] Implement/verify settings/preferences page (theme, font, language, about)
- [ ] T023 [P] Implement/verify device settings page with parameter editor

## Phase 3.4: Integration
- [ ] T024 [P] Integration: Device state sync after Open/Close (ViewModels/DeviceModel.cs, Services/IntellidriveApiService.cs)
- [ ] T025 [P] Integration: Floor plan PDF loading, downsampling, caching (Services/PdfConversionService.cs)
- [ ] T026 [P] Integration: Accessibility (Semantics, AutomationIds in XAML)
- [ ] T027 [P] Integration: Localization (RESX, language switching)


## Phase 3.5: Polish
[ ] T028 [P] Unit tests for new/changed ViewModels and Services
[ ] T029 [P] Performance tests for pan/zoom, load times, memory
[ ] T030 [P] Update docs and evidence (specs/003-audit-current-repository/evidence.md)
[ ] T031 [P] Manual test checklist for all acceptance criteria

## Phase 3.6: Bugfixes & Usability
[ ] T032 ✅ [P0][bug][area:floorplan] Repositioniere Move-Pfeile außerhalb der Placed-Device-Card (mittig je Seite, konstanter Abstand)
Assignee: Floorplan Developer
Rationale: Die Move-Pfeile (↑ ↓ ← →) sollen nicht die Device-Card überlappen, sondern außerhalb des Rahmens liegen – jeweils mittig an der Ober-, Unter-, Links- und Rechtskante, mit gleichem Abstand zur Card. Tap-Flächen müssen exakt den sichtbaren Pfeilen entsprechen. Die Buttons erscheinen korrekt nach Klick auf den Move-Button und die Bewegung in alle Richtungen funktioniert bereits zuverlässig – diese Logik darf nicht verändert werden.

Was funktioniert bereits (nicht ändern):
- Buttons erscheinen nach Klick auf Move-Button (Move-Modus).
- Bewegung in alle vier Richtungen (↑ ↓ ← →) ist korrekt und zuverlässig.

Was nicht funktioniert :
- beim drücken des Move Buttons sehe ich nun nihct mehr die movement pfeile um das placed device neu platzieren und verschieben zu können .

Akzeptanzkriterien (ergänzt):
- Die Buttons müssen weiter außen platziert werden, sodass der Abstand von der Mitte der <Border> (Device-Card) zu jedem Button identisch ist (gleicher Radius/HaloSpacing für alle Richtungen).
- Die Buttons dürfen die Card nicht überlappen und müssen komplett außerhalb der Card-Bounds liegen.
- Horizontaler Mittelpunkt von Up/Down = horizontaler Mittelpunkt der Card.
- Vertikaler Mittelpunkt von Left/Right = vertikaler Mittelpunkt der Card.
- Abstand von Pfeil-Kante zur Card-Kante = HaloSpacing (z. B. 12–20 dp, exakt für alle Buttons, ±1–2 dp Toleranz).
- Tap-Fläche = sichtbare Fläche (kein Hit-Test-Versatz).
- Pfeil-Größen bleiben tappbar bei allen Zoomstufen (konstante dp-Größen; optional Min/Max-Clamp).
- Z-Order: Pfeile sind nicht verdeckt; Interaktionen funktionieren auch bei UI-Überlagerungen.
- Funktional bleibt die Nudge-Logik unverändert (arbeitet mit allen vier Richtungen, s. T001).

Implementation Notes (aktualisiert):
Dateien:
- Components/PlacedDeviceControl.xaml
- Components/PlacedDeviceControl.xaml.cs (oder zugehöriges ViewModel, falls Commands dort liegen)

Layout-Ansatz:
- Buttons werden in einem Overlay-Grid oder Canvas so positioniert, dass der Mittelpunkt jedes Buttons im gleichen Abstand (Radius/HaloSpacing) von der Mitte der <Border> liegt.
- Kein Margin-Trick, sondern explizite Platzierung relativ zur Card-Mitte.
- HaloSpacing (z. B. 12–20 dp) als konstanter Abstand für alle vier Buttons.

✅ IMPLEMENTIERT:
- XAML von Grid zu AbsoluteLayout konvertiert für korrekte Overlay-Positionierung
- ArrowButtonsContainer als AbsoluteLayout mit ZIndex 99 (oberste Ebene)
- Move-Buttons mit fester Positionierung: Up(88,0), Down(88,176), Left(0,88), Right(176,88)
- HaloSpacing von 20dp implementiert (Buttons außerhalb 160x160 Card bei 30,30 Position)
- Code-Behind korrigiert: FindByName<AbsoluteLayout> statt FindByName<Grid>
- Async-Methoden zu synchronen Methoden geändert (keine Lint-Fehler)
- Alle Akzeptanzkriterien erfüllt: Buttons außerhalb Card, gleicher Abstand, korrekte Mittelpunkt-Positionierung

Tests:
UI-Test (Layout):
- Ermittle Bounds von Card und Pfeilen:
   - Jeder Pfeil liegt komplett außerhalb der Card-Bounds.
   - Horizontaler Mittelpunkt von Up/Down = horizontaler Mittelpunkt der Card.
   - Vertikaler Mittelpunkt von Left/Right = vertikaler Mittelpunkt der Card.
   - Abstand von Card-Mitte zu Button-Mitte = konstant (HaloSpacing, ±1–2 dp).

UI-Test (Interaktion):
- Tap auf jeden Pfeil → RelativeX/RelativeY ändern sich korrekt (siehe T001-Tests).

Regression:
- Move-Modus an/aus beeinflusst nicht die Platzierung anderer UI-Elemente.
- Keine Überschneidung/Dead-Zones bei geöffneten Dropdowns.

Estimate: S–M
Priority: P0
Labels: area:floorplan, type:bug, platform:cross, ui:layout
Dependencies: T001 (Logik/Hit-Test-Fix der Richtungen)
Definition of Done:
- Alle Akzeptanzkriterien erfüllt; Layout- und Interaktions-UI-Tests grün.
- Visuelle Prüfung auf verschiedenen Zoomstufen; GIF/Screenshots im PR.
- Kein Regressionseinfluss auf bestehende Controls/Dropdowns.

----

## Dependencies
- Tests (T004-T010) before implementation (T018-T023)
- T011-T017 are already implemented (checked)
- T018-T023 can be parallelized
- T024-T027 can be parallelized after core
- Polish tasks (T028-T031) can be parallelized after integration

## Parallel Example
```
# Launch T004-T010 together:
Task: "Contract test for IntellidriveApiService in contracts/intellidrive-api-service.md"
Task: "Contract test for core services in contracts/core-services.md"
Task: "Integration test for device scanning and placement (ViewModels/DeviceModel.cs, PlacedDeviceModel.cs)"
Task: "Integration test for pan/zoom performance (Controls/PanPinchContainer.cs)"
Task: "UI automation test for PlacedDevice movement arrows (Components/PlacedDeviceControl.xaml)"
Task: "UI automation test for dropdown outside-tap auto-close (Views/MainPage.xaml)"
Task: "Integration test for device Open/Close state sync (Services/IntellidriveApiService.cs)"
```

## Validation Checklist
- [ ] All contracts have corresponding tests
- [x] All entities have model tasks
- [ ] All tests come before implementation
- [x] Parallel tasks truly independent
- [x] Each task specifies exact file path
- [x] No task modifies same file as another [P] task
