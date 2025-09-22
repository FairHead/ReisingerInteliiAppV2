# Dropdown State Management Contract

## IDropdownStateManager Interface

**Purpose**: Define dropdown state operations and events  
**Implementer**: MainPageViewModel

```csharp
public interface IDropdownStateManager
{
    // State Properties
    bool ShowStructuresEmptyState { get; }
    bool IsLevelDropdownEnabled { get; }
    bool IsStructuresDropdownOpen { get; }
    bool IsLevelsDropdownOpen { get; }
    bool IsDevicesDropdownOpen { get; }
    
    // Operations
    Task CloseAllDropdownsAsync();
    Task OpenDropdownAsync(string tabType);
    Task HandleOutsideTapAsync();
    
    // Events
    event EventHandler<DropdownStateChangedEventArgs> DropdownStateChanged;
}

public class DropdownStateChangedEventArgs : EventArgs
{
    public string TabType { get; set; }
    public bool IsOpen { get; set; }
    public bool HasBackgroundOverlay { get; set; }
}
```

## Contract Requirements

### State Consistency
- **Guarantee**: Only one dropdown can be open at a time
- **Validation**: All other dropdown states set to false when one opens
- **Exception**: InvalidOperationException if multiple dropdowns attempted simultaneously

### Performance Contract
- **Response Time**: State changes must complete within 100ms
- **Animation Sync**: State updates must precede XAML animations
- **Memory**: No memory leaks from event subscriptions

### Event Timing
- **Before State Change**: DropdownStateChanged fired before UI updates
- **After Animation**: State persisted after animation completion
- **Error Recovery**: State reset to consistent state on exceptions

## Implementation Contract

### Required Dependencies
- ILogger<MainPageViewModel> for error logging
- IDispatcher for UI thread marshalling
- IBuildingStorageService for data validation

### Error Handling Requirements
- **Null Reference**: Check tab type validity before state changes
- **Threading**: All state changes on UI thread only
- **Recovery**: Reset to closed state if invalid transition attempted

### Testing Requirements
- Mock IBuildingStorageService for empty state testing
- Verify event firing order and timing
- Performance tests for state transition speed
