# Reisinger IntelliApp V4

A .NET MAUI mobile application based on Figma designs with a dynamic tab-based navigation system.

## Features Implemented

### UI Structure
- **Header**: Dark theme with "Reisinger App" title
- **Tab Bar**: Four navigational tabs - "Structures", "Levels", "Wifi Dev", "Local Dev"
- **Main Content**: Dynamic dropdown area with content grids
- **Footer**: Bottom navigation with "My Place", central "+" button, and "Preferences"

### Interactive Functionality

#### Tab System
- **Active Tab Indication**: Selected tab shows blue text color and blue underline
- **Tab Switching**: Clicking a tab opens its corresponding dropdown
- **Auto-Close**: Clicking the same tab or tapping outside closes the dropdown
- **Exclusive Selection**: Opening one dropdown automatically closes others

#### Dropdown Menus

**Structures Tab**:
- Grid layout with 6 structure cards
- Each card shows house icon and location names (Main HQ, Storage Facility, etc.)

**Levels Tab**:
- Grid layout with 6 level cards  
- Each card shows floor icon and floor descriptions (First Floor Right Section, etc.)

**Wifi Dev Tab**:
- Grid layout with 4 device cards
- Each card shows WiFi icon, device name, and MAC address
- Action buttons (+ and ⚙) for each device
- Blue "Scan for Devices in WiFi-Ap Mode" button

**Local Dev Tab**:
- Grid layout with 4 device cards
- Each card shows signal icon, device name, and IP address
- Action buttons (+ and ⚙) for each device  
- Blue "Scan Local Network for Devices" button

### Technical Implementation

#### Architecture
- **MAUI Framework**: Cross-platform mobile application
- **MVVM Pattern**: Clean separation of UI and logic
- **Dynamic UI**: Runtime generation of dropdown content
- **Responsive Design**: Adaptive grid layouts

#### Key Components
- Dynamic grid with auto-sizing rows
- Tap gesture handling for navigation
- Custom card components with icons and text
- Border styling with rounded corners
- Color theming matching Figma design

#### Platforms Supported
- Windows (WinUI)
- Android
- iOS
- macOS (Catalyst)

## How to Run

1. Ensure .NET 9 MAUI workload is installed
2. Open the solution in Visual Studio or VS Code
3. Build the project: `dotnet build`
4. Run on desired platform: `dotnet run --framework net9.0-windows10.0.19041.0`

## Design Compliance

The implementation closely follows the provided Figma designs:
- Exact color scheme (dark theme with blue accents)
- Matching typography and spacing
- Consistent card layouts and grid structures
- Proper tab interaction behaviors
- Faithful reproduction of all UI elements

## Future Enhancements

- Implement actual device scanning functionality
- Add data persistence for device lists
- Connect to real backend services
- Add loading states and error handling
- Implement device configuration screens
