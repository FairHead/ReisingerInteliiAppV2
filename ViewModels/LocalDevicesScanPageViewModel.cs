using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReisingerIntelliApp_V4.Helpers;
using ReisingerIntelliApp_V4.Models;
using ReisingerIntelliApp_V4.Services;

namespace ReisingerIntelliApp_V4.ViewModels;

public partial class LocalDevicesScanPageViewModel : ObservableObject
{
    private readonly IDeviceService _deviceService;
    private readonly IntellidriveApiService _apiService;
    private readonly IHttpClientFactory _httpClientFactory;
    private CancellationTokenSource? _cancellationTokenSource;

    private const int PerRequestTimeoutMs = 150;
    private const int UiFlushIntervalMs = 100;

    public LocalDevicesScanPageViewModel(
        IDeviceService deviceService,
        IntellidriveApiService apiService,
        IHttpClientFactory httpClientFactory)
    {
        ArgumentNullException.ThrowIfNull(deviceService);
        ArgumentNullException.ThrowIfNull(apiService);
        ArgumentNullException.ThrowIfNull(httpClientFactory);

        _deviceService = deviceService;
        _apiService = apiService;
        _httpClientFactory = httpClientFactory;

        LocalDevices = new ObservableCollection<LocalNetworkDeviceModel>();

        StartIp = "192.168.0.1";
        EndIp = "192.168.0.254";

        #pragma warning disable CS0618
        MessagingCenter.Subscribe<SaveLocalDevicePageViewModel, string>(this, "LocalDeviceAdded", (sender, savedDeviceId) =>
        {
            var match = LocalDevices.FirstOrDefault(d => d.DeviceId == savedDeviceId);
            if (match != null) match.IsAlreadySaved = true;
        });
        #pragma warning restore CS0618
    }

    [ObservableProperty]
    private ObservableCollection<LocalNetworkDeviceModel> localDevices;

    [ObservableProperty]
    private string startIp = "192.168.1.1";

    [ObservableProperty]
    private string endIp = "192.168.1.254";

    [ObservableProperty]
    private bool isScanning;

    [ObservableProperty]
    private string scanStatusMessage = "Bereit zum Scannen";

    [ObservableProperty]
    private int scannedCount;

    [ObservableProperty]
    private int totalCount;

    [ObservableProperty]
    private double progressPercentage;

    [ObservableProperty]
    private int foundDevicesCount;

    [ObservableProperty]
    private LocalNetworkDeviceModel? selectedDevice;

    [ObservableProperty]
    private string validationMessage = string.Empty;

    [ObservableProperty]
    private bool hasValidationError;

    [RelayCommand]
    private async Task StartLocalScanAsync()
    {
        if (IsScanning)
            return;

        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();

        IsScanning = true;
        ScannedCount = 0;
        FoundDevicesCount = 0;
        ProgressPercentage = 0;
        LocalDevices.Clear();
        ScanStatusMessage = "Starte Scan...";

        if (!IsValidIpAddress(StartIp) || !IsValidIpAddress(EndIp))
        {
            ScanStatusMessage = "Ungültige IP-Adressen";
            IsScanning = false;
            return;
        }

        if (!IsValidIpRange(StartIp, EndIp))
        {
            ScanStatusMessage = "Ungültiger IP-Bereich";
            IsScanning = false;
            return;
        }

        var ipList = GenerateIpRange(StartIp, EndIp);
        TotalCount = ipList.Count;

        // Pre-load saved devices once for IsAlreadySaved lookup
        HashSet<string> savedIds;
        try
        {
            var saved = await _deviceService.GetSavedLocalDevicesAsync();
            savedIds = new HashSet<string>(saved.Select(d => d.DeviceId), StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            savedIds = [];
        }

        var sw = Stopwatch.StartNew();

        try
        {
            await Task.Run(() => RunScanAsync(ipList, savedIds, _cancellationTokenSource.Token));

            sw.Stop();
            MainThread.BeginInvokeOnMainThread(() =>
                ScanStatusMessage = LocalDevices.Count > 0
                    ? $"✅ {LocalDevices.Count} Geräte gefunden ({sw.Elapsed.TotalSeconds:F1}s)"
                    : $"Keine Geräte gefunden ({sw.Elapsed.TotalSeconds:F1}s)");
        }
        catch (OperationCanceledException)
        {
            MainThread.BeginInvokeOnMainThread(() => ScanStatusMessage = "Scan abgebrochen");
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(() => ScanStatusMessage = $"Fehler: {ex.Message}");
        }
        finally
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                IsScanning = false;
                ScannedCount = TotalCount;
                ProgressPercentage = 100;
            });
        }
    }

    /// <summary>
    /// Sequential HTTP scan – one IP at a time.
    /// SocketsHttpHandler.ConnectTimeout=80ms ensures unreachable hosts fail fast.
    /// PeriodicTimer drives UI updates every 100ms.
    /// </summary>
    private async Task RunScanAsync(List<string> ipAddresses, HashSet<string> savedIds, CancellationToken ct)
    {
        var total = ipAddresses.Count;
        var counter = new ScanCounter();
        var deviceQueue = new ConcurrentQueue<LocalNetworkDeviceModel>();

        using var flushCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var flushTask = RunUiFlushLoopAsync(deviceQueue, total, counter, flushCts.Token);

        using var client = _httpClientFactory.CreateClient("scanner");

        foreach (var ip in ipAddresses)
        {
            if (ct.IsCancellationRequested)
                break;

            var device = await IdentifyDeviceAsync(client, ip, ct).ConfigureAwait(false);

            if (device != null)
            {
                device.IsAlreadySaved = savedIds.Contains(device.DeviceId);
                deviceQueue.Enqueue(device);
            }

            Interlocked.Increment(ref counter.Scanned);
        }

        await flushCts.CancelAsync();
        try { await flushTask.ConfigureAwait(false); } catch (OperationCanceledException) { }
        FlushToUi(deviceQueue, total, Volatile.Read(ref counter.Scanned));
    }

    /// <summary>
    /// GET http://{ip}/intellidrive/version → JSON deserialisieren → Gerät wenn Content.DEVICE_SERIALNO vorhanden.
    /// </summary>
    private static async Task<LocalNetworkDeviceModel?> IdentifyDeviceAsync(
        HttpClient client, string ip, CancellationToken ct)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(PerRequestTimeoutMs);

            using var request = new HttpRequestMessage(HttpMethod.Get, $"http://{ip}/intellidrive/version");
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                return null;

            await using var stream = await response.Content.ReadAsStreamAsync(cts.Token).ConfigureAwait(false);
            var r = await JsonSerializer.DeserializeAsync(stream, AppJsonSerializerContext.Default.IntellidriveVersionResponse, cts.Token).ConfigureAwait(false);

            var serial = r?.Content?.DeviceSerialNumber;
            if (string.IsNullOrWhiteSpace(serial))
                return null;

            var deviceId = string.IsNullOrWhiteSpace(r!.DeviceId) ? serial : r.DeviceId;
            var version = !string.IsNullOrWhiteSpace(r.Message) ? r.Message
                        : !string.IsNullOrWhiteSpace(r.FirmwareVersion) ? r.FirmwareVersion
                        : "Unknown";

            return new LocalNetworkDeviceModel
            {
                Id = deviceId,
                DeviceId = deviceId,
                Name = $"Intellidrive {serial}",
                IpAddress = ip,
                LastSeen = DateTime.Now,
                IsOnline = true,
                DeviceType = "Intellidrive",
                FirmwareVersion = version,
                SerialNumber = serial,
                LatestFirmware = r.LatestFirmware,
                ResponseTime = DateTime.Now,
                DiscoveredAt = DateTime.Now
            };
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return null;
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    // ──────────────────────────────────────────────
    //  UI Flush Loop
    // ──────────────────────────────────────────────

    /// <summary>
    /// PeriodicTimer (120ms) flushes found devices and updates progress on MainThread.
    /// </summary>
    private async Task RunUiFlushLoopAsync(
        ConcurrentQueue<LocalNetworkDeviceModel> deviceQueue,
        int total,
        ScanCounter counter,
        CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(UiFlushIntervalMs));
        try
        {
            while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
            {
                FlushToUi(deviceQueue, total, Volatile.Read(ref counter.Scanned));
            }
        }
        catch (OperationCanceledException) { }
    }

    /// <summary>
    /// Drains up to 10 devices from queue and updates progress on MainThread.
    /// Only dispatches when something actually changed to reduce UI thread pressure.
    /// </summary>
    private void FlushToUi(ConcurrentQueue<LocalNetworkDeviceModel> queue, int total, int scanned)
    {
        var batch = new List<LocalNetworkDeviceModel>(10);
        for (var i = 0; i < 10 && queue.TryDequeue(out var device); i++)
            batch.Add(device);

        // Skip dispatch if nothing changed since last flush
        if (batch.Count == 0 && scanned == ScannedCount)
            return;

        var pct = total > 0 ? (double)scanned / total * 100.0 : 0;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            foreach (var d in batch)
                LocalDevices.Add(d);

            FoundDevicesCount = LocalDevices.Count;
            ScannedCount = scanned;
            ProgressPercentage = pct;

            if (batch.Count > 0)
                ScanStatusMessage = $"✅ Gefunden: {batch[^1].Name} ({batch[^1].IpAddress})";
            else
                ScanStatusMessage = $"Scanne... ({scanned}/{total})";
        });
    }

    // ──────────────────────────────────────────────
    //  Helpers
    // ──────────────────────────────────────────────

    private static List<string> GenerateIpRange(string startIp, string endIp)
    {
        var start = BitConverter.ToUInt32(IPAddress.Parse(startIp).GetAddressBytes().Reverse().ToArray(), 0);
        var end = BitConverter.ToUInt32(IPAddress.Parse(endIp).GetAddressBytes().Reverse().ToArray(), 0);

        var ips = new List<string>((int)(end - start + 1));
        for (var i = start; i <= end; i++)
        {
            var bytes = BitConverter.GetBytes(i).Reverse().ToArray();
            ips.Add(new IPAddress(bytes).ToString());
        }
        return ips;
    }

    [RelayCommand]
    private void StopScan()
    {
        _cancellationTokenSource?.Cancel();
    }

    [RelayCommand]
    private async Task AddDeviceAsync(LocalNetworkDeviceModel device)
    {
        if (device == null) return;

        var payload = new { ip = device.IpAddress, name = device.DisplayName, serial = device.SerialNumber, firmware = device.FirmwareVersion, deviceId = device.DeviceId };
        var json = JsonSerializer.Serialize(payload);
        await Shell.Current.GoToAsync($"savelocaldevice?deviceData={Uri.EscapeDataString(json)}");
    }

    [RelayCommand]
    private async Task SaveDeviceAsync(LocalNetworkDeviceModel device)
    {
        var model = new DeviceModel
        {
            DeviceId = device.DeviceId,
            Name = device.DisplayName,
            IpAddress = device.IpAddress,
            SerialNumber = device.SerialNumber,
            FirmwareVersion = device.FirmwareVersion,
            Type = AppDeviceType.LocalDevice,
            ConnectionType = ConnectionType.Local,
            LastSeen = device.LastSeen,
            IsOnline = device.IsOnline,
            Ip = device.IpAddress
        };

        await _deviceService.SaveDeviceAsync(model);
        device.IsAlreadySaved = true;
        ScanStatusMessage = $"✅ Gespeichert: {device.DisplayName}";

        #pragma warning disable CS0618
        MessagingCenter.Send(this, "LocalDeviceAdded");
        #pragma warning restore CS0618
    }

    [RelayCommand]
    private void SelectDevice(LocalNetworkDeviceModel device) => SelectedDevice = device;

    [RelayCommand]
    private void SetCommonIpRange(string range)
    {
        (StartIp, EndIp) = range.ToLower() switch
        {
            "192.168.1.x" => ("192.168.1.1", "192.168.1.254"),
            "192.168.0.x" => ("192.168.0.1", "192.168.0.254"),
            "10.0.0.x" => ("10.0.0.1", "10.0.0.254"),
            "172.16.0.x" => ("172.16.0.1", "172.16.0.254"),
            _ => (StartIp, EndIp)
        };
        ScanStatusMessage = $"{StartIp} - {EndIp}";
    }

    private static bool IsValidIpAddress(string ip) => IPAddress.TryParse(ip, out _);

    private static bool IsValidIpRange(string startIp, string endIp)
    {
        var start = BitConverter.ToUInt32(IPAddress.Parse(startIp).GetAddressBytes().Reverse().ToArray(), 0);
        var end = BitConverter.ToUInt32(IPAddress.Parse(endIp).GetAddressBytes().Reverse().ToArray(), 0);
        return start <= end;
    }

    private sealed class ScanCounter
    {
        public int Scanned;
    }
}
