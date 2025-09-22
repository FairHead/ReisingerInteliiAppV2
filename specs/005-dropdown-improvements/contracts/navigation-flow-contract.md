# Navigation Flow Contract

## INavigationFlowManager Interface

**Purpose**: Define auto-navigation behavior and level access control  
**Implementer**: MainPageViewModel

```csharp
public interface INavigationFlowManager
{
    // Access Control
    bool CanAccessLevelTab { get; }
    bool CanNavigateToBauplan { get; }
    
    // Auto-Navigation
    Task HandleStructureSelectedAsync(string buildingName);
    Task HandleLevelSelectedAsync(string floorId);
    Task NavigateToFirstAvailableLevelAsync();
    Task NavigateToBauplanAsync();
    
    // Events
    event EventHandler<NavigationEventArgs> NavigationRequested;
    event EventHandler<AccessControlEventArgs> AccessControlChanged;
}

public class NavigationEventArgs : EventArgs
{
    public string TargetTab { get; set; }
    public string TargetItemId { get; set; }
    public NavigationType Type { get; set; }
}

public class AccessControlEventArgs : EventArgs
{
    public string TabType { get; set; }
    public bool IsEnabled { get; set; }
    public string Reason { get; set; }
}

public enum NavigationType
{
    Auto,
    Manual,
    Forced
}
```

## Contract Requirements

### Access Control Rules
- **Level Tab Access**: Requires valid structure selection (SelectedBuildingName not null/empty)
- **Bauplan Access**: Requires both structure and level selection
- **Validation Timing**: Checked on every selection change and tab activation

### Auto-Navigation Flow
1. **Structure Selection** → Auto-switch to Levels tab → Load available levels
2. **Auto-Level Selection** → Select first level if only one available → Navigate to Bauplan
3. **Manual Override** → User can break auto-flow by manual selection

### Performance Requirements
- **Navigation Delay**: <200ms between selection and tab switch
- **Level Loading**: <300ms to display available levels after structure selection
- **Bauplan Navigation**: <500ms from level selection to plan display

## Implementation Contract

### Required State Validation
- **Before Navigation**: Validate target exists and is accessible
- **During Navigation**: Maintain consistent selection state
- **After Navigation**: Verify final state matches expected outcome

### Error Handling
- **Missing Data**: Show error message, prevent navigation
- **Invalid Selection**: Reset to last valid state
- **Navigation Failure**: Log error, maintain current view

### Event Contract
- **Event Order**: AccessControlChanged → NavigationRequested → State Updates
- **Event Data**: Complete context for UI updates and logging
- **Event Timing**: Synchronous firing, async handling

## Testing Contract

### Unit Test Requirements
- Mock structure/level data for various scenarios
- Verify access control logic with empty/populated data
- Test auto-navigation flow completeness

### Integration Test Scenarios
- Complete navigation flow: Empty → Structure → Level → Bauplan
- Access control enforcement with disabled states
- Error recovery from failed navigation attempts

### Performance Test Metrics
- Navigation timing within specified limits
- Memory usage during navigation cycles
- Event handling performance under load
