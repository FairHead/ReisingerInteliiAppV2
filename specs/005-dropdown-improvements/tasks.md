# Tasks: Dropdown Logic Improvements & UI Enhancements

## T001 [P1][feature][area:navigation] Implement Empty State for Structures Dropdown

**Rationale:** User braucht Guidance wenn keine Structures vorhanden sind und einfachen Weg um erste Structure zu erstellen.

**Acceptance Criteria:**
- Empty state card zeigt "Nothing to display yet" message
- "Add Structure" label und button sichtbar
- Button führt zur StructureEditor Seite (gleiche Navigation wie Plus-Button)
- Card styling konsistent mit bestehenden Info-Cards(schon vorhanden , an den dropdown einträgen und deren styling nichts ändern)
- Nur sichtbar wenn `StructuresDropdownItems.Count == 0`

**Implementation:**
- Add `ShowStructuresEmptyState` property zu MainPageViewModel
- Add empty state card in MainPage.xaml Structures area  
- Bind button zu existing navigation command
- Unit test für empty state logic

**DoD:** Empty state card funktioniert, führt zu StructureEditor, verschwindet wenn Structures vorhanden

---

## T002 [P1][feature][area:navigation] Implement Level Dropdown Access Control

**Rationale:** Level Dropdown soll nur zugänglich sein wenn Structure ausgewählt, da Levels immer zu Structures gehören.

**Acceptance Criteria:**
- Level Dropdown deaktiviert wenn `SelectedBuildingName == null`
- Visual feedback (opacity/grayed out) für disabled state
- Keine Funktionalität wenn disabled
- Automatische Aktivierung wenn Structure gewählt wird

**Implementation:**
- Add `IsLevelDropdownEnabled` property zu MainPageViewModel
- Logic: `SelectedBuildingName != null → enabled`
- Bind Level dropdown container IsEnabled property
- Add BoolToOpacityConverter für visual feedback

**DoD:** Level Dropdown disabled ohne Structure, enabled mit Structure, visual feedback korrekt

---

## T003 [P1][feature][area:navigation] Implement Auto Level Navigation

**Rationale:** User soll automatisch zur Level Dropdown wechseln wenn Structure ausgewählt wird für besseren Flow.

**Acceptance Criteria:**
- Structure Auswahl → automatischer Wechsel zu "Levels" tab
- Erstes verfügbares Level wird automatisch ausgewählt
- Entsprechender Bauplan wird angezeigt
- User kann manuell anderes Level wählen (override default)
-Letztes angewähltes Level soll gemerkt werden ,sodass bei erneutem öffnen der Eintrag, dessen bauplan gerade angeziegt wird wenn vorhanden , in der Dropdown gewählt angezeigt wird 

**Implementation:**
- Enhance `OnStructureSelected()` method in MainPageViewModel
- Add auto-switch: `CurrentActiveTab = "Levels"`
- Add auto-select first level logic
- Trigger bauplan update für selected level
- Unit tests für navigation flow

**DoD:** Structure select → auto navigation → first level selected → bauplan displayed

---

## T004 [P1][feature][area:ui] Implement Level Selection Synchronization

**Rationale:** Level Selection muss immer mit angezeigtem Bauplan synchron sein damit User weiß welcher Bauplan aktiv ist.

**Acceptance Criteria:**
- Aktuell angezeigter Bauplan → entsprechender Level highlighted in dropdown
- Level Dropdown öffnen → korrekter Eintrag visuell markiert
- Bauplan programmatisch wechseln → Level selection update
- Bidirectional synchronization funktioniert

**Implementation:**
- Method `SynchronizeLevelSelection()` in MainPageViewModel
- Watch `StructuresVM.SelectedLevel` PropertyChanged
- Update `SelectedLevelName` entsprechend
- Bidirectional binding logic
- Integration tests für sync behavior

**DoD:** Level selection immer synchron mit angezeigtem Bauplan, bidirectional sync funktioniert

---

## T005 [P2][feature][area:ui] Implement Dropdown Background Overlay

**Rationale:** Visual feedback für offene Dropdowns verbessert UX und zeigt aktiven Bereich.

**Acceptance Criteria:**
- Offene Dropdown → leicht dunkelgrauer, fast transparenter Hintergrund
- Hintergrund volle Breite, Höhe = Anzahl Dropdown-Einträge
- Nur für aktiv geöffnete Dropdowns sichtbar
- Smooth fade-in/out animations
- Performance: keine UI lag
- Drauf achten das nachdem schließen die INteraktion mit dem bauplan nicht blokiert wird.

**Implementation:**
- Add dropdown state properties: `IsStructuresDropdownOpen`, `IsLevelsDropdownOpen`
- Add background overlay BoxViews in MainPage.xaml
- Bind visibility zu dropdown state properties
- Dynamic height calculation basierend auf item count
- CSS animations für smooth transitions

**DoD:** Background overlays erscheinen/verschwinden korrekt, richtige Größe, smooth animations

---

## T006 [P1][feature][area:ui] Implement Native Dropdown Close Behavior

**Rationale:** Users erwarten native Dropdown-Verhalten: außerhalb klicken schließt Dropdown.

**Acceptance Criteria:**
- Klick/Tap außerhalb Dropdown → automatisches Schließen
- Erneuter Klick auf aktive Tab → Dropdown schließen (bestehend beibehalten)
- Funktioniert für alle Dropdowns (Structures, Levels, Devices)
- Touch events außerhalb Background → Dropdown schließen
- Keine Interferenz mit bestehenden touch events

**Implementation:**
- Background overlay tap events → close dropdown
- Method `CloseAllDropdowns()` in MainPage.xaml.cs
- Integration mit existing tab click logic
- Touch event handling für outside-tap detection
- Preserve existing dropdown toggle behavior

**DoD:** Outside-tap schließt Dropdowns, bestehende Tab-toggle funktioniert, keine touch conflicts

---

## T007 [P2][feature][area:navigation] Add Structure Navigation Command Integration

**Rationale:** Empty state button soll gleiche Navigation wie Plus-Button nutzen für Konsistenz.

**Acceptance Criteria:**
- Empty state button verwendet existing navigation command
- Führt zur gleichen StructureEditor Seite wie Plus-Button
- Gleiche Parameter und Verhalten
- Keine Code-Duplikation

**Implementation:**
- Identify existing Plus-Button navigation command
- Reuse command in empty state button binding
- Verify navigation parameters consistency
- Unit test für command integration

**DoD:** Empty state navigation identisch mit Plus-Button, keine Duplikation, funktioniert korrekt

---

## T008 [P3][test][area:ui] UI Integration Tests for Dropdown Improvements

**Rationale:** Sicherstellen dass neue Features keine Regressionen in bestehender UI verursachen.

**Acceptance Criteria:**
- Bestehende Card styles unverändert
- Dropdown-Einträge Design unverändert  
- Keine visuellen Regressionen
- Performance Tests für neue Features
- Touch event Tests

**Implementation:**
- UI regression tests für existing cards
- Visual comparison tests
- Performance benchmarks für dropdown operations
- Touch event integration tests
- Accessibility tests

**DoD:** Alle Tests grün, keine UI-Regressionen, Performance acceptable

---

## T009 [P3][refactor][area:code] Code Cleanup and Documentation

**Rationale:** Code sauber strukturieren und dokumentieren für Wartbarkeit.

**Acceptance Criteria:**
- Neue Properties gut dokumentiert
- Method documentation für neue Navigation logic
- Code comments für komplexe sync logic
- README update mit neuen Features

**Implementation:**
- XML documentation für neue properties/methods
- Inline comments für business logic
- Update ARCHITECTURE.md wenn nötig
- Feature documentation

**DoD:** Code gut dokumentiert, Architektur-Docs aktuell

---

## Dependencies

```
T001 → T007 (Empty state needs navigation command)
T002 → T003 (Access control before auto navigation)
T003 → T004 (Auto navigation before sync)
T005 → T006 (Overlay before close behavior)
T008 depends on T001-T006 (Tests after implementation)
T009 depends on all (Cleanup after features)
```

## Milestone Planning

### M1: Core Navigation (Sprint 1)
- T001: Empty State
- T002: Access Control  
- T003: Auto Navigation
- T007: Command Integration

### M2: UI Enhancements (Sprint 2)
- T004: Selection Sync
- T005: Background Overlay
- T006: Close Behavior

### M3: Quality Assurance (Sprint 3)
- T008: Integration Tests
- T009: Documentation

## Risk Mitigation

### High Risk: UI Regression
- **Mitigation:** Extensive visual testing, feature flags
- **Fallback:** Rollback plan ready

### Medium Risk: Touch Event Conflicts
- **Mitigation:** Careful event handling, testing on multiple devices
- **Fallback:** Disable outside-tap close if conflicts

### Low Risk: Performance Impact
- **Mitigation:** Performance monitoring, efficient state management
- **Fallback:** Optimize or disable problematic features
