# Button Command Standard - MVVM Pattern

## Overview
All buttons in the ReisingerIntelliApp V4 project follow a **consistent Command binding pattern** to maintain strict MVVM architecture.

## ? Standard Pattern

### 1. **Command Definition in ViewModel**
```csharp
using CommunityToolkit.Mvvm.Input;

public partial class MyViewModel : ObservableObject
{
    // Synchronous command
    [RelayCommand]
    private void DoAction()
    {
        // Action logic here
    }
    
    // Async command
    [RelayCommand]
    private async Task DoAsyncAction()
    {
        // Async action logic here
    }
    
    // Command with parameter
    [RelayCommand]
    private void DoActionWithParameter(string parameter)
    {
        // Action logic with parameter
    }
}
```

### 2. **XAML Binding**
```xml
<!-- Simple button -->
<Button Text="Click Me" Command="{Binding DoActionCommand}" />

<!-- Button with parameter -->
<Button Text="Delete" 
        Command="{Binding DoActionWithParameterCommand}" 
        CommandParameter="ItemId" />

<!-- ImageButton -->
<ImageButton Source="icon.svg" 
             Command="{Binding DoActionCommand}" />
```

### 3. **Event to Command Conversion (Pressed/Released)**
For events that don't have Command support (Pressed, Released), use `EventToCommandBehavior`:

```xml
xmlns:toolkit="http://schemas.microsoft.com/dotnet/2022/maui/toolkit"

<ImageButton Source="icon.svg" Command="{Binding MainCommand}">
    <ImageButton.Behaviors>
        <toolkit:EventToCommandBehavior 
            EventName="Pressed" 
            Command="{Binding PressedCommand}" />
        <toolkit:EventToCommandBehavior 
            EventName="Released" 
            Command="{Binding ReleasedCommand}" />
    </ImageButton.Behaviors>
</ImageButton>
```

## ?? Anti-Patterns (Don't Use)

### ? Clicked Event Handlers
```xml
<!-- DON'T DO THIS -->
<Button Text="Click Me" Clicked="OnButtonClicked" />
```

```csharp
// DON'T DO THIS in code-behind
private void OnButtonClicked(object sender, EventArgs e)
{
    // Business logic should NOT be in code-behind
}
```

### ? Mixed Command Types
```csharp
// DON'T DO THIS - Inconsistent command types
public ICommand SomeCommand { get; }
public RelayCommand OtherCommand { get; }
public IAsyncRelayCommand AsyncCommand { get; }

// DO THIS - Use IRelayCommand and IAsyncRelayCommand consistently
public IRelayCommand SomeCommand { get; }
public IRelayCommand OtherCommand { get; }
public IAsyncRelayCommand AsyncCommand { get; }
```

## ?? Command Naming Convention

| Action Type | Command Name Example |
|------------|---------------------|
| Navigation | `NavigateToPageCommand` |
| Data Action | `SaveDataCommand`, `DeleteItemCommand` |
| UI Action | `ToggleModeCommand`, `ShowDialogCommand` |
| Movement | `MoveUpCommand`, `MoveDownCommand` |
| Scaling | `ScalePlusCommand`, `ScaleMinusCommand` |

**Always suffix with `Command`!**

## ?? Implementation Examples

### PlacedDeviceControl (Refactored)
? **Before**: Mixed Clicked events and Commands
```xml
<ImageButton Clicked="OnMoveUpClicked" />
<ImageButton Clicked="OnDeleteClicked" />
<Button Clicked="OnExecuteSelectedModeClicked" />
```

? **After**: All Commands
```xml
<ImageButton Command="{Binding MoveUpCommand}" />
<ImageButton Command="{Binding DeleteDeviceCommand}" />
<Button Command="{Binding ExecuteSelectedModeCommand}" />
```

### GradientWifiCardComponent
? **Already Correct**: Uses internal relay commands
```xml
<TapGestureRecognizer Command="{Binding InternalAddToFloorPlanCommand}" />
<TapGestureRecognizer Command="{Binding InternalSettingsCommand}" />
<TapGestureRecognizer Command="{Binding InternalDeleteCommand}" />
```

### AppFooter
? **Updated**: Command properties with event fallback
```csharp
public ICommand? LeftSectionCommand { get; set; }
public ICommand? CenterButtonCommand { get; set; }
public ICommand? RightSectionCommand { get; set; }
```

## ??? Architecture Benefits

1. **Testability**: Commands can be tested independently without UI
2. **Separation of Concerns**: Business logic stays in ViewModels
3. **Consistency**: Same pattern across entire codebase
4. **Maintainability**: Easy to understand and modify
5. **MVVM Compliance**: Follows industry best practices

## ?? References

- [CommunityToolkit.Mvvm Documentation](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
- [MAUI Command Interface](https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/data-binding/commanding)
- [EventToCommandBehavior](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/maui/behaviors/event-to-command-behavior)

## ? Checklist for New Buttons

- [ ] Command defined in ViewModel with `[RelayCommand]` or `[AsyncRelayCommand]`
- [ ] Command name follows `{Action}Command` convention
- [ ] XAML uses `Command="{Binding ...}"` binding
- [ ] No `Clicked` event handlers in code-behind
- [ ] Parameters passed via `CommandParameter` if needed
- [ ] Async operations use `IAsyncRelayCommand`
- [ ] Events (Pressed/Released) use `EventToCommandBehavior`

---

**Last Updated**: 2025-01-11  
**Maintainer**: Development Team
