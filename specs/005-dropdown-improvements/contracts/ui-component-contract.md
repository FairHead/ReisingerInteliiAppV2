# UI Component Contract

## IDropdownUIComponent Interface

**Purpose**: Define UI rendering and interaction behavior for dropdown components  
**Implementers**: MainPage.xaml, DropdownEmptyState.xaml, DropdownOverlay.xaml

```csharp
public interface IDropdownUIComponent
{
    // Visual State Management
    void SetEmptyState(bool isVisible, string message);
    void SetDropdownEnabled(bool isEnabled, string reason);
    void SetBackgroundOverlay(bool isVisible, double opacity);
    
    // Interaction Handling
    Task HandleOutsideTapAsync(Point tapLocation);
    Task HandleTabTapAsync(string tabType);
    Task HandleItemSelectionAsync(string itemId);
    
    // Animation Contract
    Task AnimateDropdownOpenAsync(TimeSpan duration);
    Task AnimateDropdownCloseAsync(TimeSpan duration);
    Task AnimateOverlayAsync(bool show, TimeSpan duration);
}
```

## Visual State Contract

### Empty State Rendering
- **Display Condition**: ShowStructuresEmptyState = true AND CurrentActiveTab = "Structures"
- **Visual Elements**: Icon, message text, optional action button
- **Positioning**: Centered in dropdown area, consistent with dropdown list style
- **Animation**: Fade in/out with 200ms duration

### Disabled State Rendering  
- **Display Condition**: IsLevelDropdownEnabled = false
- **Visual Treatment**: 50% opacity, disabled color scheme, no tap response
- **Accessibility**: Announce disabled state to screen readers
- **Visual Cues**: Grayed out appearance, disabled cursor

### Background Overlay Rendering
- **Display Condition**: Any dropdown open (IsStructuresDropdownOpen OR IsLevelsDropdownOpen OR IsDevicesDropdownOpen)
- **Visual Properties**: Semi-transparent background (40% opacity), blocks underlying content interaction
- **Positioning**: Full-screen overlay behind dropdown, above main content
- **Z-Index**: Dropdown content > Overlay > Main content

## Interaction Contract

### Touch/Tap Handling
- **Dropdown Tab Tap**: Open corresponding dropdown, close others, show overlay
- **Outside Tap**: Close all dropdowns, hide overlay, maintain current selection
- **Item Selection**: Update selection state, trigger navigation if applicable
- **Hit Testing**: Accurate touch target recognition, minimum 44pt touch targets

### Gesture Recognition
- **Tap Gesture**: Primary interaction method for dropdown and item selection
- **Outside Detection**: Coordinate-based detection outside dropdown bounds
- **Accessibility**: Support for VoiceOver/TalkBack navigation
- **Platform Consistency**: Native feel on iOS/Android/Windows

## Animation Contract

### Dropdown Animations
- **Open Animation**: Slide down with easing, 250ms duration, 60fps target
- **Close Animation**: Slide up with easing, 200ms duration, 60fps target
- **Bounce Effect**: Subtle spring animation on open (iOS-style)

### Overlay Animations  
- **Show Overlay**: Fade in from 0% to 40% opacity, 150ms duration
- **Hide Overlay**: Fade out to 0% opacity, 100ms duration
- **Synchronization**: Overlay animation starts with dropdown animation

### Performance Requirements
- **Frame Rate**: Maintain 60fps during all animations
- **Memory**: No animation memory leaks or retained references
- **Interruption**: Smooth animation cancellation if new animation triggered

## Platform-Specific Contract

### iOS Implementation
- **Native Feel**: Use UIKit-style animations and feedback
- **Safe Area**: Respect safe area for dropdown positioning
- **Accessibility**: VoiceOver announcements for state changes

### Android Implementation  
- **Material Design**: Follow Material Design dropdown patterns
- **Ripple Effects**: Material ripple on tap interactions
- **Accessibility**: TalkBack support for navigation

### Windows Implementation
- **Fluent Design**: Use Fluent Design System patterns
- **Mouse/Touch**: Support both mouse and touch interactions
- **High Contrast**: Respect high contrast accessibility settings

## Testing Contract

### Visual Testing Requirements
- **Screenshot Tests**: Capture all visual states for regression testing
- **Animation Tests**: Verify smooth 60fps animations
- **Cross-Platform**: Consistent appearance across platforms

### Interaction Testing
- **Touch Accuracy**: Verify all touch targets respond correctly
- **Accessibility**: Test with screen readers and accessibility tools
- **Edge Cases**: Test rapid tapping, interrupted animations

### Performance Testing
- **Memory Usage**: Monitor during animation cycles
- **CPU Usage**: Verify acceptable CPU load during animations
- **Battery Impact**: Test animation efficiency on mobile devices
