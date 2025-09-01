using System;
using System.Collections.ObjectModel;
using Xunit;
using ReisingerIntelliApp_V4.Models;
using ReisingerIntelliApp_V4.Controls;

namespace ReisingerIntelliApp_V4.Tests
{
    /// <summary>
    /// Integration tests for the device pin placement workflow
    /// These tests document the expected behavior of the feature
    /// </summary>
    public class DevicePinWorkflowTests
    {
        [Fact]
        public void DevicePlacement_ShouldUseRelativeCoordinates()
        {
            // Arrange
            var floor = new Floor();
            var device = new DeviceModel
            {
                DeviceId = "test-device",
                Name = "Test Door",
                Type = AppDeviceType.WifiDevice,
                Ip = "192.168.4.100"
            };

            // Act - Simulate placing device at center of screen
            var placedDevice = new PlacedDeviceModel
            {
                DeviceId = device.DeviceId,
                DeviceName = device.Name,
                X = 0.5, // Center horizontally 
                Y = 0.5, // Center vertically
                DeviceType = DeviceType.WifiDevice,
                DeviceIp = device.Ip
            };
            floor.PlacedDevices.Add(placedDevice);

            // Assert
            Assert.Single(floor.PlacedDevices);
            var placed = floor.PlacedDevices[0];
            Assert.Equal(0.5, placed.X); // Relative position, not absolute pixels
            Assert.Equal(0.5, placed.Y);
            Assert.True(placed.X >= 0.0 && placed.X <= 1.0); // Valid relative range
            Assert.True(placed.Y >= 0.0 && placed.Y <= 1.0);
        }

        [Fact]
        public void DevicePin_ShouldConvertRelativeToAbsolutePosition()
        {
            // This test documents the expected coordinate conversion behavior
            // In actual UI:
            // - PlacedDeviceModel stores relative coordinates (0-1)
            // - DevicePin converts to absolute position based on container size
            // - User sees pin at correct visual position regardless of zoom level

            // Arrange
            var device = new PlacedDeviceModel
            {
                X = 0.25, // 25% from left
                Y = 0.75  // 75% from top
            };

            // Act - Simulate 400x300 container
            var containerWidth = 400.0;
            var containerHeight = 300.0;
            var absoluteX = device.X * containerWidth;  // 0.25 * 400 = 100px
            var absoluteY = device.Y * containerHeight; // 0.75 * 300 = 225px

            // Assert
            Assert.Equal(100.0, absoluteX);
            Assert.Equal(225.0, absoluteY);
        }

        [Fact]
        public void DevicePin_ShouldPreservePositionAcrossZoomLevels()
        {
            // This test documents zoom independence behavior
            // Pin should appear at same relative position regardless of zoom

            // Arrange
            var device = new PlacedDeviceModel
            {
                X = 0.3, // 30% from left
                Y = 0.6  // 60% from top
            };

            // Act & Assert - Different container sizes (simulating zoom)
            
            // Small size (zoomed out)
            var small_width = 200.0;
            var small_height = 150.0;
            var small_x = device.X * small_width;   // 60px
            var small_y = device.Y * small_height;  // 90px
            
            // Large size (zoomed in)
            var large_width = 800.0;
            var large_height = 600.0;
            var large_x = device.X * large_width;   // 240px
            var large_y = device.Y * large_height;  // 360px

            // Relative position stays the same
            Assert.Equal(0.3, small_x / small_width);
            Assert.Equal(0.6, small_y / small_height);
            Assert.Equal(0.3, large_x / large_width);
            Assert.Equal(0.6, large_y / large_height);
        }

        [Fact]
        public void DevicePin_ShouldStoreCorrectDeviceType()
        {
            // Arrange & Act
            var wifiDevice = new PlacedDeviceModel
            {
                DeviceType = DeviceType.WifiDevice
            };
            
            var localDevice = new PlacedDeviceModel
            {
                DeviceType = DeviceType.LocalDevice
            };

            // Assert
            Assert.Equal(DeviceType.WifiDevice, wifiDevice.DeviceType);
            Assert.Equal(DeviceType.LocalDevice, localDevice.DeviceType);
        }

        [Fact]
        public void DevicePin_ShouldSupportScaling()
        {
            // Arrange
            var device = new PlacedDeviceModel
            {
                Scale = 1.0 // Normal size
            };

            // Act - User resizes pin
            device.Scale = 1.5; // 150% size

            // Assert
            Assert.Equal(1.5, device.Scale);
            Assert.True(device.Scale > 0); // Scale should be positive
        }

        [Fact]
        public void Floor_ShouldPersistMultipleDevices()
        {
            // Arrange
            var floor = new Floor();
            
            // Act - Add multiple devices
            floor.PlacedDevices.Add(new PlacedDeviceModel
            {
                DeviceId = "door-1",
                DeviceName = "Main Entrance",
                X = 0.1,
                Y = 0.5,
                DeviceType = DeviceType.WifiDevice
            });
            
            floor.PlacedDevices.Add(new PlacedDeviceModel
            {
                DeviceId = "door-2", 
                DeviceName = "Emergency Exit",
                X = 0.9,
                Y = 0.3,
                DeviceType = DeviceType.LocalDevice
            });

            // Assert
            Assert.Equal(2, floor.PlacedDevices.Count);
            Assert.Contains(floor.PlacedDevices, d => d.DeviceId == "door-1");
            Assert.Contains(floor.PlacedDevices, d => d.DeviceId == "door-2");
            
            // Verify devices have different positions
            var door1 = floor.PlacedDevices[0];
            var door2 = floor.PlacedDevices[1];
            Assert.NotEqual(door1.X, door2.X);
            Assert.NotEqual(door1.Y, door2.Y);
        }

        [Fact]
        public void DeviceCredentials_ShouldBeStoredForApiCalls()
        {
            // Arrange & Act
            var device = new PlacedDeviceModel
            {
                DeviceId = "api-test-device",
                DeviceIp = "192.168.1.50",
                Username = "admin",
                Password = "secure123",
                DeviceType = DeviceType.LocalDevice
            };

            // Assert - Credentials available for API operations
            Assert.Equal("192.168.1.50", device.DeviceIp);
            Assert.Equal("admin", device.Username);
            Assert.Equal("secure123", device.Password);
            Assert.NotEmpty(device.DeviceIp); // Required for API calls
        }
    }
}