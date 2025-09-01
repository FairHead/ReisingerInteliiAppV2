using ReisingerIntelliApp_V4.Models;
using System.Collections.ObjectModel;

namespace ReisingerIntelliApp_V4.Services;

/// <summary>
/// Service for managing device pins on floor plans, including positioning and persistence
/// </summary>
public class FloorPlanService
{
    private readonly IBuildingStorageService _buildingStorage;

    public FloorPlanService(IBuildingStorageService buildingStorage)
    {
        _buildingStorage = buildingStorage;
    }

    /// <summary>
    /// Adds a device pin to the specified floor at the given coordinates
    /// </summary>
    public async Task<PlacedDeviceModel> AddDevicePinAsync(string buildingName, string floorName, DeviceModel savedDevice, double x, double y)
    {
        var buildings = await _buildingStorage.LoadAsync();
        var building = buildings.FirstOrDefault(b => b.BuildingName.Equals(buildingName, StringComparison.OrdinalIgnoreCase));
        if (building == null) throw new InvalidOperationException($"Building '{buildingName}' not found");

        var floor = building.Floors.FirstOrDefault(f => f.FloorName.Equals(floorName, StringComparison.OrdinalIgnoreCase));
        if (floor == null) throw new InvalidOperationException($"Floor '{floorName}' not found");

        var placedDevice = PlacedDeviceModel.FromSavedDevice(savedDevice, x, y);
        floor.PlacedDevices.Add(placedDevice);

        await _buildingStorage.SaveAsync(buildings);
        return placedDevice;
    }

    /// <summary>
    /// Adds a local device pin to the specified floor at the given coordinates
    /// </summary>
    public async Task<PlacedDeviceModel> AddLocalDevicePinAsync(string buildingName, string floorName, LocalNetworkDeviceModel localDevice, double x, double y)
    {
        var buildings = await _buildingStorage.LoadAsync();
        var building = buildings.FirstOrDefault(b => b.BuildingName.Equals(buildingName, StringComparison.OrdinalIgnoreCase));
        if (building == null) throw new InvalidOperationException($"Building '{buildingName}' not found");

        var floor = building.Floors.FirstOrDefault(f => f.FloorName.Equals(floorName, StringComparison.OrdinalIgnoreCase));
        if (floor == null) throw new InvalidOperationException($"Floor '{floorName}' not found");

        var placedDevice = PlacedDeviceModel.FromLocalDevice(localDevice, x, y);
        floor.PlacedDevices.Add(placedDevice);

        await _buildingStorage.SaveAsync(buildings);
        return placedDevice;
    }

    /// <summary>
    /// Updates the position of a device pin
    /// </summary>
    public async Task UpdatePinPositionAsync(string buildingName, string floorName, string deviceId, double x, double y)
    {
        var buildings = await _buildingStorage.LoadAsync();
        var building = buildings.FirstOrDefault(b => b.BuildingName.Equals(buildingName, StringComparison.OrdinalIgnoreCase));
        if (building == null) return;

        var floor = building.Floors.FirstOrDefault(f => f.FloorName.Equals(floorName, StringComparison.OrdinalIgnoreCase));
        if (floor == null) return;

        var placedDevice = floor.PlacedDevices.FirstOrDefault(pd => pd.DeviceId == deviceId);
        if (placedDevice != null)
        {
            placedDevice.X = x;
            placedDevice.Y = y;
            await _buildingStorage.SaveAsync(buildings);
        }
    }

    /// <summary>
    /// Updates the size of a device pin
    /// </summary>
    public async Task UpdatePinSizeAsync(string buildingName, string floorName, string deviceId, double size)
    {
        var buildings = await _buildingStorage.LoadAsync();
        var building = buildings.FirstOrDefault(b => b.BuildingName.Equals(buildingName, StringComparison.OrdinalIgnoreCase));
        if (building == null) return;

        var floor = building.Floors.FirstOrDefault(f => f.FloorName.Equals(floorName, StringComparison.OrdinalIgnoreCase));
        if (floor == null) return;

        var placedDevice = floor.PlacedDevices.FirstOrDefault(pd => pd.DeviceId == deviceId);
        if (placedDevice != null)
        {
            placedDevice.Size = Math.Max(16, Math.Min(64, size)); // Clamp between 16 and 64 pixels
            await _buildingStorage.SaveAsync(buildings);
        }
    }

    /// <summary>
    /// Removes a device pin from the floor
    /// </summary>
    public async Task RemovePinAsync(string buildingName, string floorName, string deviceId)
    {
        var buildings = await _buildingStorage.LoadAsync();
        var building = buildings.FirstOrDefault(b => b.BuildingName.Equals(buildingName, StringComparison.OrdinalIgnoreCase));
        if (building == null) return;

        var floor = building.Floors.FirstOrDefault(f => f.FloorName.Equals(floorName, StringComparison.OrdinalIgnoreCase));
        if (floor == null) return;

        var placedDevice = floor.PlacedDevices.FirstOrDefault(pd => pd.DeviceId == deviceId);
        if (placedDevice != null)
        {
            floor.PlacedDevices.Remove(placedDevice);
            await _buildingStorage.SaveAsync(buildings);
        }
    }

    /// <summary>
    /// Gets all device pins for the specified floor
    /// </summary>
    public async Task<ObservableCollection<PlacedDeviceModel>> GetFloorPinsAsync(string buildingName, string floorName)
    {
        var buildings = await _buildingStorage.LoadAsync();
        var building = buildings.FirstOrDefault(b => b.BuildingName.Equals(buildingName, StringComparison.OrdinalIgnoreCase));
        if (building == null) return new ObservableCollection<PlacedDeviceModel>();

        var floor = building.Floors.FirstOrDefault(f => f.FloorName.Equals(floorName, StringComparison.OrdinalIgnoreCase));
        return floor?.PlacedDevices ?? new ObservableCollection<PlacedDeviceModel>();
    }

    /// <summary>
    /// Converts absolute screen coordinates to relative floor plan coordinates (0.0 to 1.0)
    /// This accounts for the current zoom and pan state of the floor plan viewer
    /// </summary>
    public (double relativeX, double relativeY) ScreenToFloorCoordinates(
        double screenX, double screenY, 
        double containerWidth, double containerHeight,
        double imageWidth, double imageHeight,
        double scale, double translationX, double translationY)
    {
        // Account for the current scale and translation
        var adjustedX = (screenX - translationX) / scale;
        var adjustedY = (screenY - translationY) / scale;

        // Convert to image space
        var imageX = adjustedX * (imageWidth / containerWidth);
        var imageY = adjustedY * (imageHeight / containerHeight);

        // Normalize to 0.0-1.0 range relative to the floor plan image
        var relativeX = imageX / imageWidth;
        var relativeY = imageY / imageHeight;

        // Clamp to valid range
        relativeX = Math.Max(0.0, Math.Min(1.0, relativeX));
        relativeY = Math.Max(0.0, Math.Min(1.0, relativeY));

        return (relativeX, relativeY);
    }

    /// <summary>
    /// Converts relative floor plan coordinates (0.0 to 1.0) to absolute screen coordinates
    /// This accounts for the current zoom and pan state of the floor plan viewer
    /// </summary>
    public (double screenX, double screenY) FloorToScreenCoordinates(
        double relativeX, double relativeY,
        double containerWidth, double containerHeight,
        double imageWidth, double imageHeight,
        double scale, double translationX, double translationY)
    {
        // Convert from relative coordinates to image space
        var imageX = relativeX * imageWidth;
        var imageY = relativeY * imageHeight;

        // Convert to container space
        var containerX = imageX * (containerWidth / imageWidth);
        var containerY = imageY * (containerHeight / imageHeight);

        // Apply scale and translation
        var screenX = (containerX * scale) + translationX;
        var screenY = (containerY * scale) + translationY;

        return (screenX, screenY);
    }
}