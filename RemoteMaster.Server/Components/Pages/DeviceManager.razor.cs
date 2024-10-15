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

    private static readonly Dictionary<string, string> HumanReadableDeviceClassNames = new()
    {
        { "Monitor", "Monitor" },
        { "System", "System Device" },
        { "Net", "Network Device" },
        { "SoftwareComponent", "Software Component" },
        { "Display", "Display" },
        { "PrintQueue", "Print Queue" },
        { "DiskDrive", "Disk Drive" },
        { "HIDClass", "Input Device" },
        { "SoftwareDevice", "Software Device" },
        { "USB", "USB Device" },
        { "MEDIA", "Media Device" },
        { "UCMCLIENT", "UCM Client" },
        { "SCSIAdapter", "SCSI Adapter" },
        { "Ports", "Ports" },
        { "VolumeSnapshot", "Volume Snapshot" },
        { "Mouse", "Mouse" },
        { "Keyboard", "Keyboard" },
        { "Computer", "Computer" },
        { "Processor", "Processor" },
        { "HDC", "Hard Disk Controller" },
        { "Volume", "Volume" },
        { "SecurityDevices", "Security Devices" },
        { "Bluetooth", "Bluetooth Device" },
        { "Firmware", "Firmware" },
        { "AudioEndpoint", "Audio Endpoint" }
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

    private static IconInfo GetIconForDeviceClass(string deviceClass)
    {
        return deviceClass switch
        {
            "Monitor" => new IconInfo("monitor", "material-symbols-outlined"),
            "System" => new IconInfo("settings", "material-icons"),
            "Net" => new IconInfo("network_check", "material-symbols-outlined"),
            "SoftwareComponent" => new IconInfo("widgets", "material-symbols-outlined"),
            "Display" => new IconInfo("tv", "material-symbols-outlined"),
            "PrintQueue" => new IconInfo("print", "material-icons"),
            "DiskDrive" => new IconInfo("storage", "material-symbols-outlined"),
            "HIDClass" => new IconInfo("gamepad", "material-symbols-outlined"),
            "SoftwareDevice" => new IconInfo("important_devices", "material-symbols-outlined"),
            "USB" => new IconInfo("usb", "material-symbols-outlined"),
            "MEDIA" => new IconInfo("perm_media", "material-icons"),
            "UCMCLIENT" => new IconInfo("developer_board", "material-symbols-outlined"),
            "SCSIAdapter" => new IconInfo("dns", "material-icons"),
            "Ports" => new IconInfo("settings_input_hdmi", "material-symbols-outlined"),
            "VolumeSnapshot" => new IconInfo("camera_roll", "material-symbols-outlined"),
            "Mouse" => new IconInfo("mouse", "material-icons"),
            "Keyboard" => new IconInfo("keyboard", "material-icons"),
            "Computer" => new IconInfo("computer", "material-symbols-outlined"),
            "Processor" => new IconInfo("memory", "material-symbols-outlined"),
            "HDC" => new IconInfo("hard_drive", "material-symbols-outlined"),
            "Volume" => new IconInfo("disc_full", "material-symbols-outlined"),
            "SecurityDevices" => new IconInfo("security", "material-icons"),
            "Bluetooth" => new IconInfo("bluetooth", "material-symbols-outlined"),
            "Firmware" => new IconInfo("memory", "material-symbols-outlined"),
            "AudioEndpoint" => new IconInfo("speaker", "material-symbols-outlined"),
            _ => new IconInfo("devices", "material-icons")
        };
    }
}
