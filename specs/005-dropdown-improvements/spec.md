# Spec: Dropdown Logic Improvements & UI Enhancements

## Overview
Verbesserung der Dropdown-Listen Logik auf der MainPage für bessere UX bei Structures/Levels Navigation mit automatischer Selektion, Empty States und native Dropdown-Verhalten.

## User Stories

### US1: Empty State für Structures Dropdown
**Als** User **möchte ich** eine hilfreiche Anzeige sehen wenn keine Structures vorhanden sind  
**damit ich** weiß wie ich ein neues Structure anlegen kann.

**Acceptance Criteria:**
- Wenn keine Structures gespeichert → Info Card zeigt "Nothing to display yet"
- Card enthält "Add Structure" Label + Button
- Button führt zur gleichen Seite wie der Plus-Button unten (StructureEditor)
- Card Design konsistent mit bestehenden Info-Cards

### US2: Level Dropdown Access Control  
**Als** User **soll ich** nur auf Level Dropdown zugreifen können wenn ein Structure ausgewählt ist  
**damit die** Navigation logisch und konsistent bleibt.

**Acceptance Criteria:**
- Level Dropdown deaktiviert solange kein Structure ausgewählt
- Visual Feedback (grayed out/disabled state)
- Keine Funktionalität wenn disabled

### US3: Automatische Level Navigation
**Als** User **möchte ich** automatisch zur Level Dropdown wechseln wenn ich ein Structure auswähle  
**damit ich** schnell zu meinem gewünschten Level navigieren kann.

**Acceptance Criteria:**
- Structure Auswahl → automatischer Wechsel zu Level Dropdown
- Erstes Level wird standardmäßig ausgewählt (falls vorhanden)
- Bauplan wird automatisch angezeigt für erstes Level
- User kann manuell anderes Level wählen → dieses bleibt ausgewählt

### US4: Synchronized Level Selection
**Als** User **möchte ich** dass die aktuelle Level-Auswahl mit dem angezeigten Bauplan synchron ist  
**damit ich** immer weiß zu welchem Level der aktuelle Bauplan gehört.

**Acceptance Criteria:**
- Aktuell angezeigter Bauplan → entsprechender Level-Eintrag ist markiert
- Level Dropdown öffnen → korrekter Eintrag ist visuell hervorgehoben
- Bauplan wechseln → Level Selection wird automatisch aktualisiert

### US5: Dropdown Background Overlay
**Als** User **möchte ich** ein visuelles Feedback wenn Dropdowns geöffnet sind  
**damit ich** klar erkenne welcher Bereich aktiv ist.

**Acceptance Criteria:**
- Offene Dropdown → leicht dunkelgrauer, fast transparenter Hintergrund
- Hintergrund-Größe: volle Breite, Höhe = Anzahl Dropdown-Einträge
- Nur für geöffnete Dropdowns sichtbar
- Hintergrund verschwindet beim Schließen

### US6: Native Dropdown Close Behavior
**Als** User **erwarte ich** natives Dropdown-Verhalten für alle Listen  
**damit die** Bedienung intuitiv und konsistent ist.

**Acceptance Criteria:**
- Klick außerhalb Dropdown → automatisches Schließen
- Erneuter Klick auf aktive Tab → Dropdown schließen (bestehend beibehalten)
- Touch/Tap außerhalb Background → Dropdown schließen
- Konsistent für alle Dropdown-Listen (Structures, Levels, Devices)

## Technical Requirements

### Data Flow
1. **Empty State Check:** `StructuresDropdownItems.Count == 0` → Empty State Card
2. **Level Access:** `SelectedBuildingName != null` → Level Dropdown enabled
3. **Auto Navigation:** Structure Select → `CurrentActiveTab = "Levels"` + Select first level
4. **Sync Selection:** `StructuresVM.SelectedLevel` ↔ Level Dropdown Selection
5. **Background Overlay:** Dropdown State → Conditional Background Visibility

### UI Components Affected
- **MainPage.xaml:** Dropdown containers, Background overlays
- **MainPageViewModel.cs:** Navigation logic, Selection synchronization
- **Existing Cards:** KEINE ÄNDERUNGEN an bestehenden Styles/Borders

### Performance Considerations
- Dropdown State Management ohne UI-Thread Blocking
- Background Overlay nur bei Bedarf rendern
- Efficient Selection Synchronization

## Implementation Notes

### Phase 1: Empty States & Access Control
- Empty State Card für Structures
- Level Dropdown disable/enable logic

### Phase 2: Navigation & Synchronization  
- Auto-navigation nach Structure Auswahl
- Level Selection Sync mit angezeigtem Bauplan

### Phase 3: Native Dropdown Behavior
- Background Overlay Implementation
- Outside-click Close Logic
- Touch Event Handling

## Constraints
- **KEINE Änderungen** an bestehenden Card Styles/Borders
- **KEINE Änderungen** an befüllten Dropdown-Einträgen Design
- **Bestehende UI** Formatierung muss unverändert bleiben
- **Backwards Compatibility** mit allen bestehenden Funktionen

## Success Criteria
- Empty State wird korrekt angezeigt
- Level Dropdown nur zugänglich wenn Structure gewählt
- Automatische Navigation funktioniert flüssig
- Level Selection immer synchron mit Bauplan
- Background Overlay funktioniert für alle Dropdowns
- Native Close-Verhalten implementiert
- Keine visuellen Regressionen in bestehenden Components
