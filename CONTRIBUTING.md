# Contributing – ReisingerIntelliApp_V4 (.NET MAUI, MVVM)

Danke für deinen Beitrag! Diese Richtlinie stellt sicher, dass Änderungen **stabil**, **nachvollziehbar** und **CI‑grün** sind.

## 1) Branch‑Strategie
- `main`: stabile Releases
- `dev`: Integration
- `feature/<kurz>` | `fix/<kurz>` | `hotfix/<kurz>`

## 2) Issue first
- Jede Änderung startet mit einem Issue.
- PR verlinkt ein Issue: `Closes #<nr>`.

## 3) Conventional Commits
```
<type>(<scope>): <beschreibung>
types: feat | fix | refactor | docs | test | build | ci | chore | perf
scope: ui | api | viewmodel | service | navigation | pdf | floorplan
```

## 4) Code‑Guidelines
- MVVM strikt: Logik ins ViewModel/Services, Views nur UI
- DI in `MauiProgram.cs`, keine Service‑Locator‑Muster
- Naming: `*Page`, `*ViewModel`, `*Service`
- Keine Hardcodes in XAML: Styles/Resources nutzen
- Async/await, CancellationTokens, Null‑Checks

## 5) Tests
- `dotnet test` grün
- Unit‑Tests für ViewModels/Services mit Mocks

## 6) CI/Build
- Lokal: `dotnet restore && dotnet build -c Release`
- PRs ohne grünen CI‑Status werden nicht gemergt

## 7) DoD‑Checkliste (für PR)
- [ ] Issue verlinkt
- [ ] Feature vollständig (Navigation/DI/Bindings)
- [ ] Tests aktualisiert/neu
- [ ] Manuelles Smoke‑Testing (Android/iOS/Windows) dokumentiert
- [ ] Doku aktualisiert (`STATE.md`/`README.md`/`ARCHITECTURE.md`)
- [ ] Keine Secrets/Debug‑Artefakte

## 8) Referenzen
- `ARCHITECTURE.md` (Struktur/Flows)
- `.github/copilot.md` (Pipeline für Copilot)
