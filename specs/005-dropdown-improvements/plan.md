# Plan: Dropdown Logic Improvements Implementation

## Architecture Impact

### Component Changes
- **MainPage.xaml:** Background overlays, Empty state cards, Dropdown containers
- **MainPageViewModel.cs:** Navigation logic, Selection sync, State management
- **Existing Styles:** KEINE Änderungen (kritisch für UI-Konsistenz)

### Data Flow Extensions
```
Structure Selection → Auto Level Navigation → Bauplan Display
     ↓                       ↓                      ↓
Empty State Check     First Level Select    Selection Sync
```

### UI State Management
- Dropdown Open/Close States
- Background Overlay Visibility
- Level Access Control (enabled/disabled)
- Selection Synchronization

## Technical Implementation

### 1. Empty State Infrastructure
**File:** `MainPageViewModel.cs`
- Property: `ShowStructuresEmptyState` (bool)
- Logic: `StructuresDropdownItems.Count == 0 → true`
- Command: `NavigateToStructureEditorCommand` (reuse existing)

**File:** `MainPage.xaml` 
- Conditional Card in Structures area
- Button binds to existing navigation command
- Consistent styling with Info cards

### 2. Level Access Control
**File:** `MainPageViewModel.cs`
- Property: `IsLevelDropdownEnabled` (bool)
- Logic: `SelectedBuildingName != null → enabled`
- Binding to Level dropdown container

### 3. Auto Navigation Logic
**File:** `MainPageViewModel.cs`
- Enhance: `OnStructureSelected()` method
- Add: Auto-switch to "Levels" tab
- Add: Select first available level
- Trigger: Bauplan update for selected level

### 4. Selection Synchronization
**File:** `MainPageViewModel.cs`
- Method: `SynchronizeLevelSelection()`
- Watch: `StructuresVM.SelectedLevel` changes
- Update: Level dropdown visual selection
- Bidirectional sync

### 5. Background Overlay System
**File:** `MainPage.xaml`
- Add: Overlay containers for each dropdown area
- Binding: Dropdown open states
- Styling: Semi-transparent gray, dynamic height

**File:** `MainPageViewModel.cs`
- Properties: `IsStructuresDropdownOpen`, `IsLevelsDropdownOpen`, etc.
- Logic: Toggle states on dropdown open/close

### 6. Native Close Behavior
**File:** `MainPage.xaml.cs` (Code-behind)
- Event: Background tap detection
- Method: `CloseAllDropdowns()`
- Integration: Existing tab click logic

## DI & Services Integration

### No New Services Required
- Reuse existing: `NavigationService`, `IBuildingStorageService`
- Existing Commands: Plus button navigation logic

### ViewModel Properties Extension
```csharp
// New Properties in MainPageViewModel
public bool ShowStructuresEmptyState { get; set; }
public bool IsLevelDropdownEnabled { get; set; }
public bool IsStructuresDropdownOpen { get; set; }
public bool IsLevelsDropdownOpen { get; set; }
public bool IsDevicesDropdownOpen { get; set; }
```

## UI Implementation Details

### Empty State Card Template
```xml
<ContentView IsVisible="{Binding ShowStructuresEmptyState}">
    <Border> <!-- Reuse existing card styling -->
        <StackLayout>
            <Label Text="Nothing to display yet" />
            <Label Text="Add Structure" />
            <Button Command="{Binding NavigateToStructureEditorCommand}" />
        </StackLayout>
    </Border>
</ContentView>
```

### Background Overlay Template  
```xml
<BoxView IsVisible="{Binding IsStructuresDropdownOpen}"
         BackgroundColor="#40000000"
         HeightRequest="{Binding StructuresDropdownHeight}" />
```

### Level Dropdown Access Control
```xml
<ContentView IsEnabled="{Binding IsLevelDropdownEnabled}"
             Opacity="{Binding IsLevelDropdownEnabled, Converter={StaticResource BoolToOpacityConverter}}">
    <!-- Existing Level Dropdown Content -->
</ContentView>
```

## Event Flow & State Management

### Structure Selection Flow
1. User selects Structure → `OnStructureSelected(structure)`
2. Set `SelectedBuildingName = structure.Name`
3. Enable Level dropdown: `IsLevelDropdownEnabled = true`
4. Auto-navigate: `CurrentActiveTab = "Levels"`
5. Load levels: `LoadLevelsAsync()`
6. Select first level: `SelectedLevelName = firstLevel.Name`
7. Update bauplan: `StructuresVM.SelectedLevel = firstLevel`

### Dropdown Close Flow
1. User taps outside → `OnBackgroundTapped()`
2. Check dropdown states → Close if open
3. Update state properties → `IsXxxDropdownOpen = false`
4. Background overlays hide automatically via binding

### Selection Sync Flow
1. Bauplan changes → `StructuresVM.SelectedLevel` updates
2. Watch property change → `SynchronizeLevelSelection()`
3. Update `SelectedLevelName` → Level dropdown selection updates
4. Maintain bidirectional consistency

## Testing Strategy

### Unit Tests
- Empty state logic: No structures → ShowEmptyState = true
- Access control: No building → Level disabled
- Navigation: Structure select → Auto level navigation
- Sync: Bauplan change → Level selection update

### Integration Tests  
- Full navigation flow: Empty → Structure → Level → Bauplan
- Close behavior: Outside tap → All dropdowns close
- State persistence: Navigation → States maintained

### UI Tests
- Visual: Background overlays appear/disappear correctly
- Touch: Outside tap closes dropdowns
- Styling: No regression in existing cards

## Rollback Plan

### Safe Implementation
- Feature flags for each enhancement
- Preserve all existing functionality
- No breaking changes to current UI

### Rollback Triggers
- Visual regression in existing cards
- Performance degradation
- Touch event conflicts
- Navigation flow breaks

### Rollback Process
1. Disable new features via flags
2. Restore original navigation logic
3. Remove new UI components
4. Validate existing functionality

## Performance Considerations

### Efficient State Management
- Minimal property change notifications
- Debounced dropdown state updates
- Lazy loading of empty state content

### Memory Management  
- Weak event subscriptions
- Proper disposal of new event handlers
- No memory leaks in background overlays

### UI Responsiveness
- Fast dropdown open/close animations
- Smooth auto-navigation transitions
- No blocking operations in selection sync

## Dependencies & Prerequisites

### Existing Code Reuse
- Plus button navigation command
- Current dropdown styling system
- Existing card templates
- Current property change infrastructure

### New Dependencies
- None (use existing MAUI/MVVM patterns)

### Prerequisites
- Current MainPageViewModel structure intact
- Existing dropdown containers functional  
- Card styling system available for reuse
