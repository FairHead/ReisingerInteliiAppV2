# Device Pin Placement Feature

This document describes the device pin placement feature that allows users to place saved/local devices as interactive pins on floor plans.

## Overview

The feature allows users to:
- Select devices from WiFi or Local device dropdowns
- Place them as visual pins on floor plans by tapping the "+" button
- Drag pins to reposition them
- Resize pins to different sizes
- Access device controls (door open/close)
- Navigate to device settings
- Delete pins from the floor plan

## Components

### 1. PlacedDeviceModel (Models/Floor.cs)
Enhanced model that stores:
- Device identification (ID, name, type)
- Position as relative coordinates (0-1 range for zoom independence)
- Scale factor for pin size
- Device credentials for API calls
- Placement timestamp

### 2. DevicePin Control (Controls/DevicePin.xaml)
Interactive pin widget that provides:
- Visual representation of the device
- Drag and drop repositioning
- Tap to show action menu with:
  - Door control button (green)
  - Settings button (gray) 
  - Resize button (yellow)
  - Delete button (red)
- Scale support for different pin sizes
- Device-specific icons (WiFi vs Local)

### 3. DevicePinOverlay Control (Controls/DevicePinOverlay.xaml)
Container that manages all pins:
- Displays all placed devices from the floor's PlacedDevices collection
- Handles placement mode for adding new devices
- Forwards events from individual pins to the main page
- Manages coordinate conversion between relative and absolute positions

### 4. MainPage Integration
Extended to support:
- DevicePinOverlay embedded in floor plan display
- Event handlers for all pin interactions
- "+" button functionality for device placement
- Proper layering with zoom/pan container

### 5. MainPageViewModel Extensions
Added device pin management:
- Device placement workflow
- Door control via IntellidriveApiService
- Pin position persistence
- Placement mode state management

## Usage Workflow

### Placing a Device
1. Select the "WifiDev" or "LocalDev" tab
2. Choose a device from the dropdown (device will be highlighted)
3. Ensure a building and floor with a floor plan is selected
4. Tap the "+" button in the footer
5. Tap anywhere on the floor plan to place the device
6. The device appears as a pin with the device icon

### Interacting with Pins
1. **Reposition**: Long press and drag the pin to a new location
2. **Actions**: Tap the pin to show action buttons:
   - **Door Control**: Opens/closes the device door
   - **Settings**: Opens device settings page (placeholder)
   - **Resize**: Choose from Small/Normal/Large/Extra Large
   - **Delete**: Removes the pin from the floor plan

### Coordinate System
- Pins use relative coordinates (0.0 to 1.0) for zoom independence
- X: 0.0 = left edge, 1.0 = right edge
- Y: 0.0 = top edge, 1.0 = bottom edge
- Positions are preserved when zooming/panning the floor plan
- Coordinates are persisted and restored on app restart

## Technical Details

### Event Flow
1. User taps "+" → `MainPageViewModel.OnCenterButtonTapped()`
2. Checks for selected device and floor plan
3. Sets placement mode → `DevicePinOverlay.IsPlacementMode = true`
4. User taps floor plan → `DevicePinOverlay.OnOverlayTapped()`
5. Creates PlacedDeviceModel and adds to collection
6. Pin automatically appears and is persisted

### Persistence
- Pin data is stored in the Floor's PlacedDevices collection
- Automatically saved when pins are added, moved, or deleted
- Uses the existing BuildingStorageService for persistence
- Survives app restarts and building/floor switches

### API Integration
- Door control uses existing IntellidriveApiService methods
- Device credentials stored in PlacedDeviceModel for API calls
- Connectivity checking before attempting door operations
- Error handling with user feedback

## Files Modified/Added

### New Files
- `Controls/DevicePin.xaml` - Pin widget XAML
- `Controls/DevicePin.xaml.cs` - Pin widget code-behind
- `Controls/DevicePinOverlay.xaml` - Overlay container XAML
- `Controls/DevicePinOverlay.xaml.cs` - Overlay container code-behind
- `Tests/PlacedDeviceModelTests.cs` - Unit tests

### Modified Files
- `Models/Floor.cs` - Enhanced PlacedDeviceModel
- `Views/MainPage.xaml` - Added DevicePinOverlay to floor plan
- `Views/MainPage.xaml.cs` - Added pin event handlers
- `ViewModels/MainPageViewModel.cs` - Added device placement logic
- `ReisingerIntelliApp_V4.csproj` - Updated to .NET 8 targets

## Future Enhancements

1. **Proper Device Credentials Storage**: Store full device credentials instead of defaults
2. **Settings Navigation**: Complete implementation of DeviceSettingsTabbedPage navigation
3. **Pin Appearance Customization**: Different pin styles for different device types
4. **Multiple Pin Selection**: Select and move multiple pins at once
5. **Pin Grouping**: Group related devices together
6. **Snap to Grid**: Optional grid snapping for precise placement
7. **Pin Labels**: Optional text labels on or near pins
8. **Layer Management**: Different layers for different device types

## Testing

Run the unit tests to verify the core models:
```bash
dotnet test Tests/PlacedDeviceModelTests.cs
```

The tests verify:
- PlacedDeviceModel default values and property storage
- Floor's PlacedDevices collection functionality
- DeviceType enum values
- Coordinate range validation (0-1)
- Device credential storage