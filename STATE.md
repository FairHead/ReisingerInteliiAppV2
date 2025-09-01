# STATE.md – Aktueller Stand

## Implementiert
- WifiScanPage (Scan, Connect, Save)
- LocalDevicesScanPage (Start/End‑IP Scan, Liste + Persistenz)
- MainPage (Dropdowns: Saved/Local Devices)
- StructureEditorPage (Gebäude/Stockwerk/Pläne)
- PDF‑Integration (Viewer / PNG‑Konvertierung)
- Floor‑Plan‑Pins (Positionieren, Verschieben, Speichern)
- DeviceSettingsTabbedPage (Time/Speed/IO/Protocol/DoorFunction) – dynamisch, stabiler Tab‑Lifecycle
- IntellidriveApiService: `/intellidrive/parameters/set` (Parameter‑JSON)

## In Arbeit
- Konsolidierte Parameter‑Aggregation über alle Settings‑Tabs (robuste Validierung & Fehlerrückmeldung)

## Offen / Nächste Schritte
- MAUI UI‑Tests (Smoke)
- Persistenz migrieren auf SQLite (optional)
- Geräte‑Discovery verbessern (mDNS/UDP)
- Telemetrie/Diagnostics
