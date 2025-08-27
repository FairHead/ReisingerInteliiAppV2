using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ReisingerIntelliApp_V4.Models
{
    public partial class NetworkDataModel : ObservableObject
    {
        public string? Name { get; set; }
        public string? Ssid { get; set; }
        
        public string SsidName
        {
            get => !string.IsNullOrWhiteSpace(Ssid) ? Ssid : "Unknown"; 
            set => Ssid = value;
        }

        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? DeviceId { get; set; }
        public int IpAddress { get; set; }
        public string IpAddressString => new System.Net.IPAddress(BitConverter.GetBytes(IpAddress)).ToString();
        public string? GatewayAddress { get; set; }
        public object? Bssid { get; set; }
        public object? SignalStrength { get; set; }
        public object? SecurityType { get; set; }
        public bool IsConnected { get; set; }
        public bool IsAlreadySaved { get; set; }
        public string SerialNumber { get; set; } = string.Empty;
        public string FirmwareVersion { get; set; } = string.Empty;
        public bool LatestFirmware { get; set; }
        public string SoftwareVersion { get; set; } = string.Empty;
        public string ModuleType { get; set; } = "Default";
        public string ModuleId { get; set; } = string.Empty;
        public string? BearerToken { get; set; }

        // WiFi specific properties
        public string SignalStrengthText 
        { 
            get
            {
                if (SignalStrength is int signalLevel)
                {
                    // Convert dBm to signal quality percentage for Android
                    var quality = Math.Max(0, Math.Min(100, 2 * (signalLevel + 100)));
                    return $"{quality}% ({signalLevel} dBm)";
                }
                return SignalStrength?.ToString() ?? "Unknown";
            }
        }
        public string SecurityTypeText => SecurityType?.ToString() ?? "Open";
        public string ConnectedStatus => IsConnected ? "Connected" : "Available";
    }
}
