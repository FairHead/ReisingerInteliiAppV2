using System;
using System.Collections.ObjectModel;
using Xunit;
using ReisingerIntelliApp_V4.Models;

namespace ReisingerIntelliApp_V4.Tests
{
    public class PlacedDeviceModelTests
    {
        [Fact]
        public void PlacedDeviceModel_ShouldHaveCorrectDefaultValues()
        {
            // Arrange & Act
            var device = new PlacedDeviceModel();

            // Assert
            Assert.Equal(string.Empty, device.DeviceId);
            Assert.Equal(string.Empty, device.DeviceName);
            Assert.Equal(0.0, device.X);
            Assert.Equal(0.0, device.Y);
            Assert.Equal(1.0, device.Scale);
            Assert.Equal(DeviceType.WifiDevice, device.DeviceType);
            Assert.True(device.PlacedAt <= DateTime.Now);
            Assert.True(device.PlacedAt > DateTime.Now.AddSeconds(-1));
        }

        [Fact]
        public void PlacedDeviceModel_ShouldStoreRelativeCoordinates()
        {
            // Arrange
            var device = new PlacedDeviceModel
            {
                X = 0.5, // 50% from left
                Y = 0.3, // 30% from top
                Scale = 1.2
            };

            // Assert
            Assert.Equal(0.5, device.X);
            Assert.Equal(0.3, device.Y);
            Assert.Equal(1.2, device.Scale);
        }

        [Fact]
        public void PlacedDeviceModel_ShouldStoreDeviceCredentials()
        {
            // Arrange & Act
            var device = new PlacedDeviceModel
            {
                DeviceId = "test-device-123",
                DeviceName = "Test Device",
                DeviceIp = "192.168.1.100",
                Username = "admin",
                Password = "secret123",
                DeviceType = DeviceType.LocalDevice
            };

            // Assert
            Assert.Equal("test-device-123", device.DeviceId);
            Assert.Equal("Test Device", device.DeviceName);
            Assert.Equal("192.168.1.100", device.DeviceIp);
            Assert.Equal("admin", device.Username);
            Assert.Equal("secret123", device.Password);
            Assert.Equal(DeviceType.LocalDevice, device.DeviceType);
        }
    }

    public class FloorModelTests
    {
        [Fact]
        public void Floor_ShouldHavePlacedDevicesCollection()
        {
            // Arrange & Act
            var floor = new Floor();

            // Assert
            Assert.NotNull(floor.PlacedDevices);
            Assert.IsType<ObservableCollection<PlacedDeviceModel>>(floor.PlacedDevices);
            Assert.Empty(floor.PlacedDevices);
        }

        [Fact]
        public void Floor_ShouldAllowAddingPlacedDevices()
        {
            // Arrange
            var floor = new Floor();
            var device = new PlacedDeviceModel
            {
                DeviceId = "device-1",
                DeviceName = "Door Control 1",
                X = 0.2,
                Y = 0.8
            };

            // Act
            floor.PlacedDevices.Add(device);

            // Assert
            Assert.Single(floor.PlacedDevices);
            Assert.Contains(device, floor.PlacedDevices);
        }
    }

    public class DeviceTypeTests
    {
        [Fact]
        public void DeviceType_ShouldHaveCorrectEnumValues()
        {
            // Act & Assert
            Assert.Equal(0, (int)DeviceType.WifiDevice);
            Assert.Equal(1, (int)DeviceType.LocalDevice);
        }
    }
}