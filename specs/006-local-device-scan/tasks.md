# Tasks

## T1 [P0][bug] Local scan hangs due to long per-request timeouts
- Update scan to enforce per-request timeout via `CancellationTokenSource.CancelAfter`.
- Ensure request timeout does not rely on platform-specific handler defaults.

## T2 [P0][bug] Progress and current IP not visible or stuttering
- Throttle UI updates while still showing current IP and progress.
- Ensure updates run on UI thread without blocking scan tasks.

## T3 [P1][refactor] Use HttpClientFactory for scan client
- Use `IHttpClientFactory` to build the scan `HttpClient`.
- Keep handler configuration cross-platform.

## Tests
- Manual: run scan on Android and confirm scan completes quickly, progress updates, and devices appear when `DEVICE_SERIALNO` is present.
