# Local Device Scan Optimization Plan

## Impacted Areas
- `ViewModels/LocalDevicesScanPageViewModel.cs`
- `Helpers/ServiceCollectionExtensions.cs`

## Approach
1. Use `IHttpClientFactory` with a named client configured for scan timeouts.
2. Run scan with parallel HTTP calls and per-request cancellation tokens for reliable timeouts.
3. Throttle UI updates to keep progress and current IP visible without UI stalls.

## Rollback
- Revert `ViewModels/LocalDevicesScanPageViewModel.cs` and `Helpers/ServiceCollectionExtensions.cs` to previous scan logic.
