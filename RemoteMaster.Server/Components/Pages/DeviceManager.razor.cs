// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Claims;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using Polly;
using RemoteMaster.Server.Models;
using RemoteMaster.Shared.DTOs;
using RemoteMaster.Shared.Formatters;

namespace RemoteMaster.Server.Components.Pages;

[Authorize]
public partial class DeviceManager : IAsyncDisposable
{
    [Parameter]
    public string Host { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;

    [Inject(Key = "Resilience-Pipeline")]
    public ResiliencePipeline<string> ResiliencePipeline { get; set; } = default!;

    private HubConnection? _connection;
    private ClaimsPrincipal? _user;
    private List<DeviceDto> _deviceItems = [];
    private bool _firstRenderCompleted;

    private readonly Dictionary<string, bool> _devicePanelState = [];

    private bool _isCategoryGrouping = true;

    private static readonly Dictionary<string, DeviceClassInfo> DeviceClassInfoMap = new()
    {
        { "Monitor", new DeviceClassInfo("Monitor", "Monitor", new IconInfo("monitor", "material-symbols-outlined"), "Display Devices") },
        { "System", new DeviceClassInfo("System", "System Device", new IconInfo("settings", "material-icons"), "System Devices") },
        { "Net", new DeviceClassInfo("Net", "Network Device", new IconInfo("network_check", "material-symbols-outlined"), "Network Devices") },
        { "SoftwareComponent", new DeviceClassInfo("SoftwareComponent", "Software Component", new IconInfo("widgets", "material-symbols-outlined"), "System Devices") },
        { "Display", new DeviceClassInfo("Display", "Display", new IconInfo("tv", "material-symbols-outlined"), "Display Devices") },
        { "PrintQueue", new DeviceClassInfo("PrintQueue", "Print Queue", new IconInfo("print", "material-icons"), "Display Devices") },
        { "DiskDrive", new DeviceClassInfo("DiskDrive", "Disk Drive", new IconInfo("storage", "material-symbols-outlined"), "Storage Devices") },
        { "HIDClass", new DeviceClassInfo("HIDClass", "Input Device", new IconInfo("gamepad", "material-symbols-outlined"), "Input Devices") },
        { "SoftwareDevice", new DeviceClassInfo("SoftwareDevice", "Software Device", new IconInfo("important_devices", "material-symbols-outlined"), "System Devices") },
        { "USB", new DeviceClassInfo("USB", "USB Device", new IconInfo("usb", "material-symbols-outlined"), "Network Devices") },
        { "MEDIA", new DeviceClassInfo("MEDIA", "Media Device", new IconInfo("perm_media", "material-icons"), "Audio Devices") },
        { "UCMCLIENT", new DeviceClassInfo("UCMCLIENT", "UCM Client", new IconInfo("developer_board", "material-symbols-outlined"), "Specialized Devices") },
        { "SCSIAdapter", new DeviceClassInfo("SCSIAdapter", "SCSI Adapter", new IconInfo("dns", "material-icons"), "Storage Devices") },
        { "Ports", new DeviceClassInfo("Ports", "Ports", new IconInfo("settings_input_hdmi", "material-symbols-outlined"), "Network Devices") },
        { "VolumeSnapshot", new DeviceClassInfo("VolumeSnapshot", "Volume Snapshot", new IconInfo("camera_roll", "material-symbols-outlined"), "Storage Devices") },
        { "Mouse", new DeviceClassInfo("Mouse", "Mouse", new IconInfo("mouse", "material-icons"), "Input Devices") },
        { "Keyboard", new DeviceClassInfo("Keyboard", "Keyboard", new IconInfo("keyboard", "material-icons"), "Input Devices") },
        { "Computer", new DeviceClassInfo("Computer", "Computer", new IconInfo("computer", "material-symbols-outlined"), "System Devices") },
        { "Processor", new DeviceClassInfo("Processor", "Processor", new IconInfo("memory", "material-symbols-outlined"), "System Devices") },
        { "HDC", new DeviceClassInfo("HDC", "Hard Disk Controller", new IconInfo("hard_drive", "material-symbols-outlined"), "Storage Devices") },
        { "Volume", new DeviceClassInfo("Volume", "Volume", new IconInfo("disc_full", "material-symbols-outlined"), "Storage Devices") },
        { "SecurityDevices", new DeviceClassInfo("SecurityDevices", "Security Devices", new IconInfo("security", "material-icons"), "System Devices") },
        { "Bluetooth", new DeviceClassInfo("Bluetooth", "Bluetooth Device", new IconInfo("bluetooth", "material-symbols-outlined"), "Network Devices") },
        { "Firmware", new DeviceClassInfo("Firmware", "Firmware", new IconInfo("memory", "material-symbols-outlined"), "System Devices") },
        { "AudioEndpoint", new DeviceClassInfo("AudioEndpoint", "Audio Endpoint", new IconInfo("speaker", "material-symbols-outlined"), "Audio Devices") }
    };

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            _firstRenderCompleted = true;
        }
    }

    protected async override Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateTask;
        _user = authState.User;

        if (_user?.Identity?.IsAuthenticated == true)
        {
            await InitializeHostConnectionAsync();
            await FetchDevices();
        }
    }

    private void ToggleGrouping()
    {
        _isCategoryGrouping = !_isCategoryGrouping;
    }

    private async Task FetchDevices()
    {
        await SafeInvokeAsync(() => _connection!.InvokeAsync("GetDevices"));
    }

    private async Task InitializeHostConnectionAsync()
    {
        var userId = _user?.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID is not found.");

        _connection = new HubConnectionBuilder()
            .WithUrl($"https://{Host}:5001/hubs/devicemanager", options =>
            {
                options.AccessTokenProvider = async () =>
                {
                    var accessTokenResult = await AccessTokenProvider.GetAccessTokenAsync(userId);

                    return accessTokenResult.IsSuccess ? accessTokenResult.Value : null;
                };
            })
            .AddMessagePackProtocol(options =>
            {
                var resolver = CompositeResolver.Create([new IPAddressFormatter(), new PhysicalAddressFormatter()], [ContractlessStandardResolver.Instance]);

                options.SerializerOptions = MessagePackSerializerOptions.Standard.WithResolver(resolver);
            })
            .Build();

        _connection.On<List<DeviceDto>>("ReceiveDeviceList", async deviceItems =>
        {
            _deviceItems = deviceItems;

            await InvokeAsync(StateHasChanged);
        });

        _connection.Closed += async _ =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
            await _connection.StartAsync();
        };

        await _connection.StartAsync();
    }

    private async Task SafeInvokeAsync(Func<Task> action)
    {
        var result = await ResiliencePipeline.ExecuteAsync(async _ =>
        {
            if (_connection is not { State: HubConnectionState.Connected })
            {
                throw new InvalidOperationException("Connection is not active");
            }

            await action();
            await Task.CompletedTask;

            return "Success";

        }, CancellationToken.None);

        if (result == "This function is not available in the current host version. Please update your host.")
        {
            if (_firstRenderCompleted)
            {
                await JsRuntime.InvokeVoidAsync("alert", result);
            }
        }
    }

    private void TogglePanel(string deviceClass)
    {
        if (!_devicePanelState.TryAdd(deviceClass, true))
        {
            _devicePanelState[deviceClass] = !_devicePanelState[deviceClass];
        }
    }

    private bool IsPanelOpen(string deviceClass) => _devicePanelState.ContainsKey(deviceClass) && _devicePanelState[deviceClass];

    [JSInvokable]
    public async Task OnBeforeUnload()
    {
        await DisposeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection != null)
        {
            try
            {
                await _connection.DisposeAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError("An error occurred while asynchronously disposing the connection for host {Host}: {Message}", Host, ex.Message);
            }
        }

        GC.SuppressFinalize(this);
    }

    private async Task EnableDevice(string deviceInstanceId)
    {
        await SafeInvokeAsync(() => _connection!.InvokeAsync("EnableDevice", deviceInstanceId));

        await FetchDevices();

        StateHasChanged();
    }

    private async Task DisableDevice(string deviceInstanceId)
    {
        await SafeInvokeAsync(() => _connection!.InvokeAsync("DisableDevice", deviceInstanceId));

        await FetchDevices();

        StateHasChanged();
    }
}
