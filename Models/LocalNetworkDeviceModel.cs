using CommunityToolkit.Mvvm.ComponentModel;

namespace ReisingerIntelliApp_V4.Models
{
    public partial class LocalNetworkDeviceModel : ObservableObject
    {
        [ObservableProperty]
        private string id = string.Empty;

        [ObservableProperty]
        private string name = string.Empty;

        [ObservableProperty]
        private string ipAddress = string.Empty;

        [ObservableProperty]
        private string deviceId = string.Empty;

        [ObservableProperty]
        private string serialNumber = string.Empty;

        [ObservableProperty]
        private string firmwareVersion = string.Empty;

        [ObservableProperty]
        private string softwareVersion = string.Empty;

        [ObservableProperty]
        private string deviceType = "Intellidrive";

        [ObservableProperty]
        private bool latestFirmware;

        [ObservableProperty]
        private bool isAlreadySaved;

        public bool IsNotAlreadySaved => !IsAlreadySaved;

        [ObservableProperty]
        private string customName = string.Empty;

        [ObservableProperty]
        private DateTime discoveredAt = DateTime.Now;

        [ObservableProperty]
        private DateTime lastSeen = DateTime.Now;

        [ObservableProperty]
        private DateTime responseTime = DateTime.Now;

        [ObservableProperty]
        private bool isOnline = true;

        // Display properties for UI
        public string DisplayName => !string.IsNullOrEmpty(CustomName) ? CustomName : 
                                   !string.IsNullOrEmpty(Name) ? Name : 
                                   !string.IsNullOrEmpty(SerialNumber) ? $"Intellidrive {SerialNumber}" :
                                   $"Intellidrive {IpAddress}";
        
        public string DeviceInfo => $"{SerialNumber} • {IpAddress}";
        public string StatusText => IsOnline ? "Online" : "Offline";
        public string SavedStatusText => IsAlreadySaved ? "Gespeichert" : "Neu";
    }
}
