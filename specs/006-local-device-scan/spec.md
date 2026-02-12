# Local Device Scan Optimization

## Summary
Optimize the local device scan for speed, stability, and smooth UI updates. The scan must issue HTTP requests to `/intellidrive/version` for each IP in the range, show which IP is being scanned, and keep the progress bar responsive across platforms.

## User Stories
- As a user, I want the scan to finish quickly while still detecting devices reliably.
- As a user, I want to see which IP is currently being scanned and a smooth progress bar.
- As a user, I want the scan to work the same on Android, iOS, Windows, and macOS.

## Acceptance Criteria
- Scan fires HTTP requests for each IP to `/intellidrive/version`.
- Device is detected only when the response contains `DEVICE_SERIALNO`.
- Per-request timeout is enforced via linked cancellation tokens (no long hangs).
- UI shows current IP and progress without stuttering.
- Solution uses `IHttpClientFactory` and is cross-platform compliant.

## Out of Scope
- WiFi scan behavior changes.
- Changes to API endpoints or authentication.
