// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using MudBlazor;
using Polly;
using Polly.Retry;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Components;
using RemoteMaster.Server.Models;
using RemoteMaster.Server.Services;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Pages;

public partial class Index
{
    private bool _isMenuOpen = false;

    private void ToggleDrawer()
    {
        _isMenuOpen = !_isMenuOpen;
    }

    private HashSet<Node> Nodes { get; set; } = new();

    private readonly Dictionary<Computer, string> _scriptResults = new();
    private Node _selectedNode;

    private bool _anyComputerSelected = false;
    private readonly List<Computer> _selectedComputers = new();

    [Inject]
    private IDialogService DialogService { get; set; }

    [Inject]
    private DatabaseService DatabaseService { get; set; }

    [Inject]
    private IWakeOnLanService WakeOnLanService { get; set; }

    [Inject]
    private IJSRuntime JSRuntime { get; set; }

    [Inject]
    private IHttpContextAccessor HttpContextAccessor { get; set; }

    [Inject]
    private ILogger<Index> Logger { get; set; }

    private readonly AsyncRetryPolicy _retryPolicy = Policy
        .Handle<Exception>()
        .WaitAndRetryAsync(new[]
        {
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(7),
            TimeSpan.FromSeconds(10),
        });

    protected async override Task OnInitializedAsync()
    {
        var rootFolders = (await DatabaseService.GetNodesAsync(f => f.Parent == null && f is Folder)).Cast<Folder>().ToHashSet();

        foreach (var folder in rootFolders)
        {
            Nodes.Add(folder);
        }
    }

    private void HandleComputerSelection(Computer computer, bool isSelected)
    {
        if (isSelected)
        {
            _selectedComputers.Add(computer);
        }
        else
        {
            _selectedComputers.Remove(computer);
        }

        _anyComputerSelected = _selectedComputers.Any();
        StateHasChanged();
    }

    private async Task OnNodeSelected(Node node)
    {
        _selectedComputers.Clear();
        _anyComputerSelected = false;
    
        if (node is Folder)
        {
            _selectedNode = node;
            await UpdateComputersThumbnailsAsync(node.Nodes.OfType<Computer>());
        }
    
        StateHasChanged();
    }

    private async Task UpdateComputersThumbnailsAsync(IEnumerable<Computer> computers)
    {
        var tasks = computers.Select(UpdateComputerThumbnailAsync).ToArray();
        await Task.WhenAll(tasks);
    }

    private async Task UpdateComputerThumbnailAsync(Computer computer)
    {
        Logger.LogInformation("UpdateComputerThumbnailAsync Called for {IPAddress}", computer.IPAddress);

        var httpContext = HttpContextAccessor.HttpContext;
        var accessToken = httpContext.Request.Cookies["accessToken"];

        var connection = new HubConnectionBuilder()
            .WithUrl($"https://{computer.IPAddress}:5076/hubs/control", options =>
            {
                options.Headers.Add("Authorization", $"Bearer {accessToken}");
            })
            .AddMessagePackProtocol()
            .Build();

        try
        {
            connection.On<byte[]>("ReceiveThumbnail", async (thumbnailBytes) =>
            {
                if (thumbnailBytes?.Length > 0)
                {
                    computer.Thumbnail = thumbnailBytes;
                    await InvokeAsync(StateHasChanged);
                }
            });

            await connection.StartAsync();

            Logger.LogInformation("Calling ConnectAs with Intention.GetThumbnail for {IPAddress}", computer.IPAddress);
            await connection.InvokeAsync("ConnectAs", Intention.GetThumbnail);
        }
        catch (Exception ex)
        {
            Logger.LogError("Exception in UpdateComputerThumbnailAsync for {IPAddress}: {Message}", computer.IPAddress, ex.Message);
        }
    }

    private async Task HandleRefreshClick()
    {
        if (_selectedNode is Folder selectedFolder)
        {
            await UpdateComputersThumbnailsAsync(selectedFolder.Nodes.OfType<Computer>());
        }
    }

    private async Task OpenWindow(string url)
    {
        await JSRuntime.InvokeVoidAsync("openNewWindow", url);
    }

    private async Task ExecuteOnAvailableComputers(Func<Computer, HubConnection, Task> actionOnComputer)
    {
        var tasks = _selectedComputers.Select(IsComputerAvailable).ToArray();
        var results = await Task.WhenAll(tasks);

        var availableComputers = results.Where(r => r.isAvailable);

        foreach (var (computer, isAvailable) in availableComputers)
        {
            var accessToken = HttpContextAccessor.HttpContext?.Request.Cookies["accessToken"];

            var connection = new HubConnectionBuilder()
                .WithUrl($"https://{computer.IPAddress}:5076/hubs/control", options =>
                {
                    options.Headers.Add("Authorization", $"Bearer {accessToken}");
                })
                .AddMessagePackProtocol()
                .Build();

            connection.On<string>("ReceiveScriptResult", async (result) =>
            {
                _scriptResults[computer] = result;

                await InvokeAsync(StateHasChanged);
            });

            await connection.StartAsync();

            await _retryPolicy.ExecuteAsync(async () =>
            {
                if (connection.State == HubConnectionState.Connected)
                {
                    try
                    {
                        await actionOnComputer(computer, connection);
                    }
                    catch (HubException ex) when (ex.Message.Contains("Method does not exist"))
                    {
                        await JSRuntime.InvokeVoidAsync("showAlert", $"Host: {computer.Name}.\nThis function is not available in the current host version. Please update your host.");
                    }
                }
            });
        }

        StateHasChanged();
    }

    private async Task Connect(string action)
    {
        if (action == "control")
        {
            await ExecuteOnAvailableComputers(async (computer, connection) => await OpenWindow($"/{computer.IPAddress}/connect?imageQuality=25&cursorTracking=false&inputEnabled=true"));
        }
        else if (action == "view")
        {
            await ExecuteOnAvailableComputers(async (computer, connection) => await OpenWindow($"/{computer.IPAddress}/connect?imageQuality=25&cursorTracking=true&inputEnabled=false"));
        }
    }

    private async Task OpenShell(string application, bool isSystem = false)
    {    
        await ExecuteOnAvailableComputers(async (computer, connection) =>
        {
            ProcessStartInfo startInfo;
    
            if (application == "ssh")
            {
                var command = $"ssh user@{computer.IPAddress}";

                startInfo = new ProcessStartInfo()
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C {command}",
                    UseShellExecute = true,
                };
            }
            else
            {
                var sParameter = isSystem ? "-s" : "";
                var command = @$"/C psexec \\{computer.IPAddress} {sParameter} -nobanner -accepteula {application}";
                
                startInfo = new ProcessStartInfo()
                {
                    FileName = "cmd.exe",
                    Arguments = command,
                    UseShellExecute = true,
                };
            }
    
            await Task.Run(() => Process.Start(startInfo));
        });
    }

    private async Task Power(string action)
    {
        if (action == "power")
        {
            await ExecuteOnAvailableComputers(async (computer, connection) => await connection.InvokeAsync("ShutdownComputer", "", 0, true));
        }
        else if (action == "reboot")
        {
            await ExecuteOnAvailableComputers(async (computer, connection) => await connection.InvokeAsync("RebootComputer", "", 0, true));

        }
        else if (action == "wakeup")
        {
            foreach (var computer in _selectedComputers)
            {
                WakeOnLanService.WakeUp(computer.MACAddress);
            }
        }
    }

    private async Task DomainMember(bool isJoin)
    {
        var domain = "it-ktk.local";
        var username = "vitaly@it-ktk.local";
        var password = "WaLL@8V1";
    
        if (isJoin)
        {
            await ExecuteOnAvailableComputers(async (computer, connection) => await connection.InvokeAsync("SendJoinToDomain", domain, username, password));
        }
        else
        {
            await ExecuteOnAvailableComputers(async (computer, connection) => await connection.InvokeAsync("SendUnjoinFromDomain", username, password));
        }
    }

    private async Task Update()
    {
        var sharedFolder = @"\\SERVER-DC02\Win\RemoteMaster";
        var username = "support@it-ktk.local";
        var password = "bonesgamer123!!";
        
        await ExecuteOnAvailableComputers(async (computer, connection) => await connection.InvokeAsync("SendUpdateHost", sharedFolder, username, password));
    }

    private async Task ScreenRecording(bool start)
    {
        if (start)
        {
            await ExecuteOnAvailableComputers(async (computer, connection) =>
            {
                var requesterName = Environment.MachineName;
                var currentDate = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var fileName = $@"C:\{requesterName}_{computer.IPAddress}_{currentDate}.mp4";
    
                await connection.InvokeAsync("SendStartScreenRecording", fileName);
            });
        }
        else
        {
            await ExecuteOnAvailableComputers(async (computer, connection) => await connection.InvokeAsync("SendStopScreenRecording"));
        }
    }
    
    private async Task SetMonitorState(MonitorState state)
    {
        await ExecuteOnAvailableComputers(async (computer, connection) => await connection.InvokeAsync("SetMonitorState", state));
    }

    private async Task ExecuteScript()
    {
        var fileData = await JSRuntime.InvokeAsync<JsonElement>("selectFile");
    
        if (fileData.TryGetProperty("content", out var contentElement) && fileData.TryGetProperty("name", out var nameElement))
        {
            var fileContent = contentElement.GetString();
            var fileName = nameElement.GetString();
    
            var extension = Path.GetExtension(fileName);
            string shellType;
    
            switch (extension)
            {
                case ".ps1":
                    shellType = "PowerShell";
                    break;
                case ".bat":
                case ".cmd":
                    shellType = "CMD";
                    break;
                default:
                    Logger.LogError("Unknown script type.");
                    return;
            }
    
            await ExecuteOnAvailableComputers(async (computer, connection) => await connection.InvokeAsync("SendScript", fileContent, shellType));
    
            var dialogParameters = new DialogParameters
            {
                { "Results", _scriptResults }
            };

            foreach (var result in _scriptResults)
            {
                Console.WriteLine(result.Value);
            }
            
            await DialogService.ShowAsync<ScriptResults>("Script Results", dialogParameters);
        }
    }
    
    private async Task ManagePSExecRules(bool isEnabled)
    {
        await ExecuteOnAvailableComputers(async (computer, connection) => await connection.InvokeAsync("SetPSExecRules", isEnabled));
    }

    private static async Task<(Computer computer, bool isAvailable)> IsComputerAvailable(Computer computer)
    {
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(computer.IPAddress, 1000);

            return (computer, reply.Status == IPStatus.Success);
        }
        catch
        {
            return (computer, false);
        }
    }

    private async Task OpenHostConfigGenerator()
    {
        var dialogOptions = new DialogOptions
        {
            CloseOnEscapeKey = true,
        };
        
        await DialogService.ShowAsync<HostConfigurationGenerator>("Host Configuration Generator", dialogOptions);
    }
}
