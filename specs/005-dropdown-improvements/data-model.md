# Data Model: Dropdown Logic Improvements & UI Enhancements

## Existing Data Models (No Changes Required)

### DropdownItemModel
**Purpose**: Existing model for dropdown list items  
**Location**: `Models/DropdownItemModel.cs`  
**Fields**: Id, Icon, Text, HasActions, ShowStatus, IsSelected, IsConnected, IsPlacedOnCurrentFloor, IsActionEnabled  
**Usage**: Unchanged - existing dropdown rendering logic remains identical  
**State Transitions**: N/A - display-only model

### Building & Floor Models  
**Purpose**: Existing domain models for structures and levels  
**Location**: `Models/Building.cs`, `Models/Floor.cs`  
**Usage**: Unchanged - existing data persistence and loading logic remains identical  
**Relationships**: Existing Building → Floors hierarchy maintained

## New ViewModel State Properties

### MainPageViewModel Extensions
**Purpose**: Manage dropdown UI state and behavior  
**Location**: `ViewModels/MainPageViewModel.cs`

#### Empty State Management
```csharp
public bool ShowStructuresEmptyState { get; set; }
```
**Validation Rules**: 
- True when DropdownItems.Count == 0 AND CurrentActiveTab == "Structures"
- False otherwise
- Updates automatically when structures are loaded/deleted

#### Access Control State
```csharp
public bool IsLevelDropdownEnabled { get; set; }
```
**Validation Rules**:
- True when SelectedBuildingName is not null or empty
- False when no structure selected
- Updates automatically when SelectedBuildingName changes

#### Background Overlay State
```csharp
public bool IsStructuresDropdownOpen { get; set; }
public bool IsLevelsDropdownOpen { get; set; }  
public bool IsDevicesDropdownOpen { get; set; }
```
**Validation Rules**:
- Only one can be true at a time (mutual exclusion)
- All false when no dropdown is open
- Updates when dropdown tabs are activated/deactivated
- State Transitions: Closed → Open (on tab tap) → Closed (on outside tap or different tab)

## Data Flow Relationships

### Empty State Flow
```
Building Collection Empty → ShowStructuresEmptyState = true → Empty Card Visible
Building Added → ShowStructuresEmptyState = false → Empty Card Hidden
```

### Access Control Flow  
```
No Structure Selected → IsLevelDropdownEnabled = false → Level Tab Disabled
Structure Selected → IsLevelDropdownEnabled = true → Level Tab Enabled
```

### Navigation Flow
```
Structure Selected → Auto-switch to Levels → Load First Level → Show Bauplan
Level Selected → Update Bauplan → Sync Selection State
```

### Overlay Flow
```
Tab Activated → Set Corresponding DropdownOpen = true → Show Background Overlay
Outside Tap → All DropdownOpen = false → Hide All Overlays
```

## Validation Logic

### Empty State Validation
- **Rule**: Empty state only visible when legitimate empty condition exists
- **Check**: Verify building storage actually empty, not just loading state
- **Fallback**: Show loading indicator during async operations

### Access Control Validation  
- **Rule**: Level access requires valid structure selection
- **Check**: SelectedBuildingName exists in current building list
- **Fallback**: Clear level selection if selected building no longer exists

### State Consistency Validation
- **Rule**: UI state matches actual data state at all times
- **Check**: Selected items match displayed content
- **Fallback**: Refresh UI state if inconsistencies detected

## Error Handling Strategy

### Data Loading Errors
- **Empty State**: Show error message instead of "nothing to display"
- **Building Load Failure**: Disable dropdown, show error state
- **Level Load Failure**: Keep structure selection, show level error

### State Synchronization Errors
- **Selection Mismatch**: Reset to first available item
- **Missing Data**: Clear selection, return to safe state
- **Navigation Error**: Log error, maintain current view

## Performance Considerations

### Property Change Notifications
- **Frequency**: State changes limited to user interactions (low frequency)
- **Impact**: Single property updates, no cascade effects
- **Optimization**: SetProperty handles change detection efficiently

### Collection Operations
- **Empty State Check**: O(1) operation on Count property
- **Selection Updates**: O(n) for highlighting, minimal n (typically <20 items)
- **State Validation**: Lazy evaluation, only on property access

### Memory Usage
- **New Properties**: 5 boolean fields = ~5 bytes additional memory
- **Event Subscriptions**: Standard MVVM pattern, automatic cleanup
- **UI Element Creation**: Conditional XAML rendering, elements created only when needed

## Testing Data Requirements

### Unit Test Data
- Empty building list for empty state testing
- Single building with/without floors for access control testing  
- Multiple buildings for selection state testing

### Integration Test Scenarios
- Navigate through full structure → level → bauplan flow
- Test all dropdown combinations (4 tabs × open/closed states)
- Verify outside-tap behavior on each dropdown type

### Performance Test Metrics
- Property change notification timing (<1ms)
- Dropdown open/close animation smoothness (60fps)
- State synchronization delay (<100ms)
