# Tasks: UI-Hänger beim Hinzufügen von Devices auf den Bauplan

## Ziel
Systematische Analyse und Behebung des Problems, dass die UI beim Hinzufügen eines Geräts auf den Floorplan hängen bleibt.

---


## 1. Analyse & Debugging

- [ ] **T001: XAML-Bindings und Ressourcen prüfen**
  - Akzeptanz: Alle Bindings und Ressourcen (PlacedDeviceControl, MainPage, Floorplan) sind korrekt und verursachen keine Endlosschleifen oder Deadlocks.
- [ ] **T002: Event-Chain nachvollziehen**
  - Akzeptanz: Der Ablauf Hinzufügen → CollectionChanged → UI-Update ist lückenlos dokumentiert.
- [ ] **T003: Debug-Ausgaben an kritischen Stellen einfügen**
  - Akzeptanz: Alle Add-, CollectionChanged-, Render- und EventHandler-Punkte geben Debug-Logs aus.
- [ ] **T004: Deadlocks, Endlosschleifen, Binding-Fehler, Ressourcenprobleme prüfen**
  - Akzeptanz: Keine Endlosschleifen, Deadlocks oder Binding-Fehler mehr nachweisbar.
- [ ] **T005: Minimal-Template testen**
  - Akzeptanz: Mit reduziertem UI-Template tritt das Problem nicht mehr auf oder kann gezielt isoliert werden.


## 2. Testen & Validieren

- [ ] **T006: Unit-Tests für ViewModel-Logik (Device hinzufügen, CollectionChanged)**
  - Akzeptanz: Alle relevanten Unit-Tests laufen grün und decken die Problemstellen ab.
- [ ] **T007: UI-Tests für Responsiveness beim Hinzufügen**
  - Akzeptanz: UI bleibt nach Device-Hinzufügen bedienbar, keine Hänger.
- [ ] **T008: Debug-Logging prüfen**
  - Akzeptanz: Alle kritischen Events werden geloggt und sind nachvollziehbar.


## 3. Abschluss & Dokumentation

- [ ] **T009: Ergebnisse dokumentieren**
  - Akzeptanz: Analyse, Ursache und Fix sind in der Doku festgehalten.
- [ ] **T010: Rollback-Strategie beschreiben**
  - Akzeptanz: Es existiert ein klarer Plan zur Rücknahme der Änderungen bei Problemen.

---

## Definition of Done (DoD)
- Alle Tasks sind abgeschlossen und getestet
- UI bleibt nach Device-Hinzufügen stabil
- Ursache und Fix sind dokumentiert
- Rollback-Plan vorhanden

## Testfälle
- Gerät aus Dropdown hinzufügen → UI bleibt responsiv
- Mehrere Geräte nacheinander hinzufügen → keine Hänger
- Fehlerhafte Bindings/Events werden im Log erkannt
