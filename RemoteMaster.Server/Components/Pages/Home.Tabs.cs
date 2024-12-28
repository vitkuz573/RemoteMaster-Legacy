// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using MudBlazor;
using RemoteMaster.Server.Components.Dialogs;
using RemoteMaster.Server.Enums;
using RemoteMaster.Server.Models;
using RemoteMaster.Shared.DTOs;
using RemoteMaster.Shared.Extensions;

namespace RemoteMaster.Server.Components.Pages;

public partial class Home
{
    private RenderFragment RenderTabs()
    {
        return builder =>
        {
            var seq = 0;

            foreach (var tab in GetTabs().Where(tab => tab.Actions.Any(a => a.IsVisible())))
            {
                builder.OpenComponent<MudTabPanel>(seq++);
                builder.AddAttribute(seq++, "Text", tab.Title);
                builder.AddAttribute(seq++, "Icon", tab.Icon);
                builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(tabBuilder =>
                {
                    var innerSeq = 0;

                    foreach (var action in tab.Actions.Where(a => a.IsVisible()))
                    {
                        tabBuilder.OpenComponent<MudButton>(innerSeq++);
                        tabBuilder.AddAttribute(innerSeq++, "Color", Color.Primary);
                        tabBuilder.AddAttribute(innerSeq++, "Variant", Variant.Filled);
                        tabBuilder.AddAttribute(innerSeq++, "OnClick", action.OnClick);
                        tabBuilder.AddAttribute(innerSeq++, "Disabled", action.IsDisabled());
                        tabBuilder.AddAttribute(innerSeq++, "Class", action.Class);
                        tabBuilder.AddAttribute(innerSeq++, "ChildContent", (RenderFragment)(cb => cb.AddContent(0, action.Label)));
                        tabBuilder.CloseComponent();
                    }
                }));

                builder.CloseComponent();
            }
        };
    }

    private List<TabDefinition> GetTabs()
    {
        var tabs = new List<TabDefinition>();

        // Main Tab
        var mainTab = new TabDefinition("Main", Icons.Material.Filled.Api)
        {
            Actions =
            {
                new ActionDefinition
                {
                    Label = "Power",
                    OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await Power()),
                    IsVisible = () => UserHasClaim("Power", "Reboot") || UserHasClaim("Power", "Shutdown"),
                    IsDisabled = () => _selectedHosts.Count == 0 || !_selectedHosts.All(hd => _availableHosts.ContainsKey(hd.IpAddress)),
                    Class = "mr-2"
                },
                new ActionDefinition
                {
                    Label = "Wake Up",
                    OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await WakeUp()),
                    IsVisible = () => UserHasClaim("Power", "WakeUp"),
                    IsDisabled = () => _selectedHosts.Count == 0 || _selectedHosts.All(hd => _availableHosts.ContainsKey(hd.IpAddress)),
                    Class = "mr-2"
                },
                new ActionDefinition
                {
                    Label = "Connect",
                    OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await Connect()),
                    IsVisible = () => UserHasAnyClaim("Connect"),
                    IsDisabled = () => _selectedHosts.Count == 0 || !_selectedHosts.All(hd => _availableHosts.ContainsKey(hd.IpAddress)),
                    Class = "mr-2"
                },
                new ActionDefinition
                {
                    Label = "Lock",
                    OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await Lock()),
                    IsVisible = () => UserHasClaim("Security", "LockWorkStation"),
                    IsDisabled = () => _selectedHosts.Count == 0 || !_selectedHosts.All(hd => _availableHosts.ContainsKey(hd.IpAddress)),
                    Class = "mr-2"
                },
                new ActionDefinition
                {
                    Label = "Open Shell",
                    OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await OpenShell()),
                    IsVisible = () => UserHasClaim("Execution", "OpenShell"),
                    IsDisabled = () => _selectedHosts.Count == 0,
                    Class = "mr-2"
                },
                new ActionDefinition
                {
                    Label = "Execute Script",
                    OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await ExecuteScript()),
                    IsVisible = () => UserHasClaim("Execution", "Scripts"),
                    IsDisabled = () => _selectedHosts.Count == 0 || !_selectedHosts.All(hd => _availableHosts.ContainsKey(hd.IpAddress)),
                    Class = "mr-2"
                },
                new ActionDefinition
                {
                    Label = "Logon",
                    OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await LogonHosts()),
                    IsVisible = () => true,
                    IsDisabled = () => _selectedHosts.Count == 0 || _selectedHosts.Any(hd => _availableHosts.ContainsKey(hd.IpAddress) || _unavailableHosts.ContainsKey(hd.IpAddress)),
                    Class = "mr-2"
                },
                new ActionDefinition
                {
                    Label = "Logoff",
                    OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await LogoffHosts()),
                    IsVisible = () => true,
                    IsDisabled = () => _selectedHosts.Count == 0 || !_selectedHosts.Any(hd => _availableHosts.ContainsKey(hd.IpAddress) || _unavailableHosts.ContainsKey(hd.IpAddress)),
                    Class = "mr-2"
                },
                new ActionDefinition
                {
                    Label = "Refresh",
                    OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await Refresh()),
                    IsVisible = () => true,
                    IsDisabled = () => false,
                    Class = "ml-auto"
                }
            }
        };

        // Service Tab
        var serviceTab = new TabDefinition("Service", Icons.Material.Filled.Key)
        {
            Actions =
            {
                new ActionDefinition
                {
                    Label = "App Launcher",
                    OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await AppLauncher()),
                    IsVisible = () => UserHasClaim("Execution", "Scripts"),
                    IsDisabled = () => _selectedHosts.Count == 0 || !_selectedHosts.All(hd => _availableHosts.ContainsKey(hd.IpAddress)),
                    Class = "mr-2"
                },
                new ActionDefinition
                {
                    Label = "Set Monitor State",
                    OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await SetMonitorState()),
                    IsVisible = () => UserHasClaim("Hardware", "SetMonitorState"),
                    IsDisabled = () => _selectedHosts.Count == 0 || !_selectedHosts.All(hd => _availableHosts.ContainsKey(hd.IpAddress)),
                    Class = "mr-2"
                },
                new ActionDefinition
                {
                    Label = "PSExec Rules",
                    OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await ManagePsExecRules()),
                    IsVisible = () => UserHasClaim("Execution", "Scripts"),
                    IsDisabled = () => _selectedHosts.Count == 0 || !_selectedHosts.All(hd => _availableHosts.ContainsKey(hd.IpAddress)),
                    Class = "mr-2"
                },
                new ActionDefinition
                {
                    Label = "Screen Recorder",
                    OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await ScreenRecorder()),
                    IsVisible = () => UserHasAnyClaim("ScreenRecording"),
                    IsDisabled = () => _selectedHosts.Count == 0 || !_selectedHosts.All(hd => _availableHosts.ContainsKey(hd.IpAddress)),
                    Class = "mr-2"
                },
                new ActionDefinition
                {
                    Label = "Domain Membership",
                    OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await DomainMembership()),
                    IsVisible = () => UserHasAnyClaim("DomainManagement"),
                    IsDisabled = () => _selectedHosts.Count == 0 || !_selectedHosts.All(hd => _availableHosts.ContainsKey(hd.IpAddress)),
                    Class = "mr-2"
                },
                new ActionDefinition
                {
                    Label = "Update",
                    OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await Update()),
                    IsVisible = () => UserHasClaim("UpdaterManagement", "Start"),
                    IsDisabled = () => _selectedHosts.Count == 0 || !_selectedHosts.All(hd => _availableHosts.ContainsKey(hd.IpAddress)),
                    Class = "mr-2"
                }
            }
        };

        // Tools Tab
        var toolsTab = new TabDefinition("Tools", Icons.Material.Filled.MiscellaneousServices)
        {
            Actions =
            {
                new ActionDefinition
                {
                    Label = "WIM Boot",
                    OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await WimBoot()),
                    IsVisible = () => UserHasClaim("Execution", "Scripts"),
                    IsDisabled = () => _selectedHosts.Count == 0 || !_selectedHosts.All(hd => _availableHosts.ContainsKey(hd.IpAddress)),
                    Class = "mr-2"
                },
                new ActionDefinition
                {
                    Label = "Task Manager",
                    OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await OpenTaskManager()),
                    IsVisible = () => UserHasAnyClaim("TaskManagement"),
                    IsDisabled = () => _selectedHosts.Count == 0 || !_selectedHosts.All(hd => _availableHosts.ContainsKey(hd.IpAddress)),
                    Class = "mr-2"
                },
                new ActionDefinition
                {
                    Label = "Device Manager",
                    OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await OpenDeviceManager()),
                    IsVisible = () => UserHasAnyClaim("DeviceManagement"),
                    IsDisabled = () => _selectedHosts.Count == 0 || !_selectedHosts.All(hd => _availableHosts.ContainsKey(hd.IpAddress)),
                    Class = "mr-2"
                },
                new ActionDefinition
                {
                    Label = "File Manager",
                    OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await OpenFileManager()),
                    IsVisible = () => UserHasAnyClaim("FileManagement"),
                    IsDisabled = () => _selectedHosts.Count == 0 || !_selectedHosts.All(hd => _availableHosts.ContainsKey(hd.IpAddress)),
                    Class = "mr-2"
                },
                new ActionDefinition
                {
                    Label = "Upload File",
                    OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await FileUpload()),
                    IsVisible = () => UserHasClaim("FileManagement", "Upload"),
                    IsDisabled = () => _selectedHosts.Count == 0 || !_selectedHosts.All(hd => _availableHosts.ContainsKey(hd.IpAddress)),
                    Class = "mr-2"
                },
                new ActionDefinition
                {
                    Label = "Registry Editor",
                    OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await OpenRegistryEditor()),
                    IsVisible = () => UserHasAnyClaim("RegistryManagement"),
                    IsDisabled = () => _selectedHosts.Count == 0 || !_selectedHosts.All(hd => _availableHosts.ContainsKey(hd.IpAddress)),
                    Class = "mr-2"
                },
                new ActionDefinition
                {
                    Label = "Message Box",
                    OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await MessageBox()),
                    IsVisible = () => UserHasClaim("Execution", "Scripts"),
                    IsDisabled = () => _selectedHosts.Count == 0 || !_selectedHosts.All(hd => _availableHosts.ContainsKey(hd.IpAddress)),
                    Class = "mr-2"
                },
                new ActionDefinition
                {
                    Label = "Send Message",
                    OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await SendMessage()),
                    IsVisible = () => UserHasAnyClaim("ChatManagement"),
                    IsDisabled = () => _selectedHosts.Count == 0 || !_selectedHosts.All(hd => _availableHosts.ContainsKey(hd.IpAddress)),
                    Class = "mr-2"
                },
                new ActionDefinition
                {
                    Label = "Chat",
                    OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await OpenChat()),
                    IsVisible = () => UserHasAnyClaim("ChatManagement"),
                    IsDisabled = () => _selectedHosts.Count == 0 || !_selectedHosts.All(hd => _availableHosts.ContainsKey(hd.IpAddress)),
                    Class = "mr-2"
                },
                new ActionDefinition
                {
                    Label = "Logs Viewer",
                    OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await OpenLogsManager()),
                    IsVisible = () => UserHasAnyClaim("LogManagement"),
                    IsDisabled = () => _selectedHosts.Count == 0 || !_selectedHosts.All(c => _availableHosts.ContainsKey(c.IpAddress)),
                    Class = "mr-2"
                }
            }
        };

        // Management Tab
        var managementTab = new TabDefinition("Management", Icons.Material.Filled.Settings)
        {
            Actions =
            {
                new ActionDefinition
                {
                    Label = "Host Info",
                    OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await OpenHostInfo()),
                    IsVisible = () => UserHasClaim("HostManagement", "View"),
                    IsDisabled = () => _selectedHosts.Count != 1,
                    Class = "mr-2"
                },
                new ActionDefinition
                {
                    Label = "Move",
                    OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await OpenHostMoveDialog()),
                    IsVisible = () => UserHasClaim("HostManagement", "Move"),
                    IsDisabled = () => _selectedHosts.Count == 0,
                    Class = "mr-2"
                },
                new ActionDefinition
                {
                    Label = "Remove",
                    OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await OpenHostRemoveDialog()),
                    IsVisible = () => UserHasClaim("HostManagement", "Remove"),
                    IsDisabled = () => _selectedHosts.Count == 0,
                    Class = "mr-2"
                },
                new ActionDefinition
                {
                    Label = "Renew Certificate",
                    OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await RenewCertificate()),
                    IsVisible = () => UserHasClaim("CertificateManagement", "Renew"),
                    IsDisabled = () => _selectedHosts.Count == 0 || !_selectedHosts.All(hd => _availableHosts.ContainsKey(hd.IpAddress)),
                    Class = "mr-2"
                }
            }
        };

        // Extra Tab
        var extraTab = new TabDefinition("Extra", Icons.Material.Filled.Settings)
        {
            Actions =
            {
                new ActionDefinition
                {
                    Label = "Remote Executor",
                    OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await RemoteExecutor()),
                    IsVisible = () => UserHasClaim("Execution", "Scripts"),
                    IsDisabled = () => _selectedHosts.Count > 0,
                    Class = "mr-2"
                }
            }
        };

        tabs.Add(mainTab);
        tabs.Add(serviceTab);
        tabs.Add(toolsTab);
        tabs.Add(managementTab);
        tabs.Add(extraTab);

        return tabs;
    }

    private async Task OpenTaskManager() => await OpenHostWindow("taskmanager");

    private async Task OpenDeviceManager() => await OpenHostWindow("devicemanager");

    private async Task OpenFileManager() => await OpenHostWindow("filemanager", 1120);

    private async Task OpenRegistryEditor() => await OpenHostWindow("registry", 1120);

    private async Task OpenLogsManager() => await OpenHostWindow("logs", 1120);

    private async Task OpenChat() => await OpenHostWindow("chat");

    private async Task OpenHostWindow(string path, uint width = 800, uint height = 800)
    {
        var module = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/windowOperations.js");
    
        foreach (var url in _selectedHosts.Select(host => $"/{host.IpAddress}/{path}"))
        {
            await module.InvokeVoidAsync("openNewWindow", url, width, height);
        }
    }

    private async Task ExecuteDialog<TDialog>(string title, DialogParameters? parameters = null, DialogOptions? options = null) where TDialog : ComponentBase
    {
        var parametersWithAdditional = parameters ?? [];
    
        if (parametersWithAdditional.TryGet<Dictionary<string, object>>(nameof(CommonDialogWrapper<TDialog>.AdditionalParameters)) == null)
        {
            parametersWithAdditional.Add(nameof(CommonDialogWrapper<TDialog>.AdditionalParameters), new Dictionary<string, object>());
        }
    
        await DialogService.ShowAsync<CommonDialogWrapper<TDialog>>(title, parametersWithAdditional, options);
    }

    private async Task ExecuteAction<TDialog>(string title, bool onlyAvailable = true, bool startConnection = true, string hubPath = "hubs/control", DialogOptions? dialogOptions = null, bool requireConnections = true, bool includeHosts = true, Dictionary<string, object>? additionalParameters = null) where TDialog : ComponentBase
    {
        var hosts = includeHosts
            ? onlyAvailable
                ? _selectedHosts.Where(hd => _availableHosts.ContainsKey(hd.IpAddress)).ToList()
                : _selectedHosts.ToList()
            : [];

        if (includeHosts && hosts.Count == 0)
        {
            return;
        }

        dialogOptions ??= new DialogOptions
        {
            MaxWidth = MaxWidth.ExtraExtraLarge,
            FullWidth = true
        };

        var dialogParameters = new DialogParameters();

        if (includeHosts)
        {
            dialogParameters.Add(nameof(CommonDialogWrapper<TDialog>.Hosts), new ConcurrentDictionary<HostDto, HubConnection?>(hosts.ToDictionary(hd => hd, _ => (HubConnection?)null)));
            dialogParameters.Add(nameof(CommonDialogWrapper<TDialog>.HubPath), hubPath);
            dialogParameters.Add(nameof(CommonDialogWrapper<TDialog>.StartConnection), startConnection);
            dialogParameters.Add(nameof(CommonDialogWrapper<TDialog>.RequireConnections), requireConnections);
        }

        if (additionalParameters != null)
        {
            dialogParameters.Add(nameof(CommonDialogWrapper<TDialog>.AdditionalParameters), additionalParameters);
        }

        await ExecuteDialog<TDialog>(title, dialogParameters, dialogOptions);
    }

    private async Task WimBoot() => await ExecuteAction<BootToWimDialog>("WIM Boot", hubPath: "hubs/control");

    private async Task OpenHostRemoveDialog()
    {
        var additionalParameters = new Dictionary<string, object>
        {
            { nameof(RemoveHostsDialog.OnHostsRemoved), EventCallback.Factory.Create<IEnumerable<HostDto>>(this, OnHostsRemoved) }
        };

        await ExecuteAction<RemoveHostsDialog>("Remove Hosts", onlyAvailable: false, startConnection: false, requireConnections: false, additionalParameters: additionalParameters);

        return;

        async Task OnHostsRemoved(IEnumerable<HostDto> removedHosts)
        {
            foreach (var removedHost in removedHosts)
            {
                _selectedHosts.RemoveAll(hd => hd.Id == removedHost.Id);
                _availableHosts.TryRemove(removedHost.IpAddress, out _);
                _unavailableHosts.TryRemove(removedHost.IpAddress, out _);
                _pendingHosts.TryRemove(removedHost.IpAddress, out _);
            }

            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task OpenHostMoveDialog()
    {
        var additionalParameters = new Dictionary<string, object>
        {
            { nameof(MoveHostsDialog.OnHostsMoved), EventCallback.Factory.Create<IEnumerable<HostDto>>(this, OnHostsMoved) }
        };

        await ExecuteAction<MoveHostsDialog>("Move Hosts", onlyAvailable: false, hubPath: "hubs/management", additionalParameters: additionalParameters);

        return;

        async Task OnHostsMoved(IEnumerable<HostDto> movedHosts)
        {
            foreach (var movedHost in movedHosts)
            {
                _selectedHosts.RemoveAll(hd => hd.Id == movedHost.Id);
                _availableHosts.TryRemove(movedHost.IpAddress, out _);
                _unavailableHosts.TryRemove(movedHost.IpAddress, out _);
                _pendingHosts.TryRemove(movedHost.IpAddress, out _);
            }

            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task OpenHostInfo() => await ExecuteAction<HostDialog>("Host Info", false, false, requireConnections: false);

    private async Task RemoteExecutor() => await ExecuteAction<RemoteCommandDialog>("Remote Command", includeHosts: false);

    private async Task Power() => await ExecuteAction<PowerDialog>("Power");

    private async Task WakeUp() => await ExecuteAction<WakeUpDialog>("Wake Up", false, false, requireConnections: false);

    private async Task Connect() => await ExecuteAction<ConnectDialog>("Connect");

    private async Task Lock() => await ExecuteAction<LockWorkStationDialog>("Lock Workstation");

    private async Task OpenShell() => await ExecuteAction<OpenShellDialog>("Open Shell", false, false, requireConnections: false);

    private async Task ExecuteScript() => await ExecuteAction<ScriptExecutorDialog>("Script Executor");

    private async Task ManagePsExecRules() => await ExecuteAction<PsExecRulesDialog>("PSExec Rules", hubPath: "hubs/service");

    private async Task AppLauncher() => await ExecuteAction<AppLauncherDialog>("App Launcher");

    private async Task SetMonitorState() => await ExecuteAction<MonitorStateDialog>("Set Monitor State");

    private async Task ScreenRecorder() => await ExecuteAction<ScreenRecorderDialog>("Screen Recorder", hubPath: "hubs/screenrecorder");

    private async Task DomainMembership() => await ExecuteAction<DomainMembershipDialog>("Domain Membership", hubPath: "hubs/domainmembership");

    private async Task Update() => await ExecuteAction<UpdateDialog>("Update", hubPath: "hubs/updater");

    private async Task FileUpload() => await ExecuteAction<FileUploadDialog>("Upload File", hubPath: "hubs/filemanager");

    private async Task MessageBox() => await ExecuteAction<MessageBoxDialog>("Message Box");

    private async Task SendMessage() => await ExecuteAction<SendMessageDialog>("Send Message", hubPath: "hubs/chat");

    private async Task RenewCertificate() => await ExecuteAction<RenewCertificateDialog>("Renew Certificate", hubPath: "hubs/certificate");

    private async Task Refresh()
    {
        if (_logonCts?.Token.IsCancellationRequested ?? true)
        {
            return;
        }

        var hostsToRefresh = _availableHosts.Values.Concat(_unavailableHosts.Values).ToList();

        foreach (var hostDto in hostsToRefresh.TakeWhile(_ => !(_logonCts?.Token.IsCancellationRequested ?? true)))
        {
            await LogonHost(hostDto, _logonCts!.Token);
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task LogonHosts()
    {
        if (_logonCts?.Token.IsCancellationRequested ?? true)
        {
            return;
        }

        var logonTasks = _selectedHosts.Select(async host =>
        {
            await LogonHost(host, _logonCts!.Token);
            await InvokeAsync(StateHasChanged);
        });

        await Task.WhenAll(logonTasks);

        ResetSelections();
    }

    private async Task LogonHost(HostDto hostDto, CancellationToken cancellationToken)
    {
        try
        {
            var accessTokenResult = await AccessTokenProvider.GetAccessTokenAsync(_currentUser!.Id);
    
            _accessToken = accessTokenResult.IsSuccess ? accessTokenResult.Value : null;
    
            const string url = "hubs/control?thumbnail=true";
    
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
    
            var connection = await SetupConnection(hostDto, url, true, cancellationToken);
    
            if (cancellationToken.IsCancellationRequested || connection.State != HubConnectionState.Connected)
            {
                await SetHostState(hostDto, HostState.Unavailable, cancellationToken);

                return;
            }
    
            connection.On<byte[]>("ReceiveThumbnail", async thumbnailBytes =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    Logger.LogWarning("Thumbnail reception canceled for host {IPAddress}", hostDto.IpAddress);

                    return;
                }
    
                if (thumbnailBytes.Length > 0)
                {
                    hostDto.Thumbnail = thumbnailBytes;

                    await SetHostState(hostDto, HostState.Available, cancellationToken);
                }
                else
                {
                    await SetHostState(hostDto, HostState.Unavailable, cancellationToken);
                }
    
                await InvokeAsync(StateHasChanged);
            });
    
            connection.On("ReceiveCloseConnection", async () =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
    
                await connection.StopAsync(cancellationToken);
            });
        }
        catch (Exception ex)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                Logger.LogError("Exception in LogonHost for {IPAddress}: {Message}", hostDto.IpAddress, ex.Message);
    
                await SetHostState(hostDto, HostState.Unavailable, cancellationToken);
            }
        }
    }
    
    private async Task LogoffHosts()
    {
        if (_logonCts?.Token.IsCancellationRequested ?? true)
        {
            return;
        }

        var cancellationToken = _logonCts.Token;

        var tasks = _selectedHosts
            .Where(hd => _availableHosts.ContainsKey(hd.IpAddress) || _unavailableHosts.ContainsKey(hd.IpAddress))
            .Select(host => LogoffHost(host, cancellationToken));

        await Task.WhenAll(tasks);
    
        ResetSelections();
    }
    
    private async Task LogoffHost(HostDto hostDto, CancellationToken cancellationToken) => await SetHostState(hostDto, HostState.Pending, cancellationToken);

    private async Task SetHostState(HostDto hostDto, HostState targetState, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        if (targetState is HostState.Unavailable or HostState.Pending)
        {
            hostDto.Thumbnail = null;
        }

        _availableHosts.TryRemove(hostDto.IpAddress, out _);
        _unavailableHosts.TryRemove(hostDto.IpAddress, out _);
        _pendingHosts.TryRemove(hostDto.IpAddress, out _);

        switch (targetState)
        {
            case HostState.Available:
                _availableHosts.TryAdd(hostDto.IpAddress, hostDto);
                break;
            case HostState.Unavailable:
                _unavailableHosts.TryAdd(hostDto.IpAddress, hostDto);
                break;
            case HostState.Pending:
                _pendingHosts.TryAdd(hostDto.IpAddress, hostDto);
                break;
        }

        await InvokeAsync(StateHasChanged);
    }

    private void ResetSelections()
    {
        foreach (var host in _selectedHosts.ToList())
        {
            SelectHost(host, false);
        }
    }

    private async Task<HubConnection> SetupConnection(HostDto hostDto, string hubPath, bool startConnection, CancellationToken cancellationToken)
    {
        var connection = new HubConnectionBuilder()
            .WithUrl($"https://{hostDto.IpAddress}:5001/{hubPath}", options =>
            {
                options.HttpMessageHandlerFactory = handler =>
                {
                    if (handler is HttpClientHandler clientHandler)
                    {
                        clientHandler.ServerCertificateCustomValidationCallback = (_, cert, chain, sslPolicyErrors) =>
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                return false;
                            }

                            if (SslWarningService.IsSslAllowed(hostDto.IpAddress))
                            {
                                return true;
                            }

                            if (cert == null)
                            {
                                return false;
                            }

                            int keySize;

                            switch (cert.PublicKey.Oid.Value)
                            {
                                case "1.2.840.113549.1.1.1":
                                    {
                                        using var rsa = cert.GetRSAPublicKey();
                                        keySize = rsa?.KeySize ?? 0;
                                        break;
                                    }
                                case "1.2.840.10040.4.1":
                                    {
                                        using var dsa = cert.GetDSAPublicKey();
                                        keySize = dsa?.KeySize ?? 0;
                                        break;
                                    }
                                case "1.2.840.10045.2.1":
                                    {
                                        using var ecdsa = cert.GetECDsaPublicKey();
                                        keySize = ecdsa?.KeySize ?? 0;
                                        break;
                                    }
                                default:
                                    {
                                        keySize = 0;
                                        break;
                                    }
                            }

                            var certificateInfo = new CertificateInfo(
                                cert.Issuer,
                                cert.Subject,
                                cert.GetExpirationDateString(),
                                cert.GetEffectiveDateString(),
                                cert.SignatureAlgorithm.FriendlyName ?? "Unknown",
                                keySize.ToString(),
                                chain?.ChainElements.Select(e => e.Certificate.Subject).ToList() ?? []
                            );

                            return sslPolicyErrors == SslPolicyErrors.None || Task.Run(() => ShowSslWarningDialog(hostDto.IpAddress, sslPolicyErrors, certificateInfo), cancellationToken).Result;

                        };
                    }

                    return handler;
                };

                options.AccessTokenProvider = () => Task.FromResult(_accessToken);
            })
            .AddMessagePackProtocol(options => options.Configure())
            .Build();

        if (!startConnection)
        {
            return connection;
        }

        try
        {
            await connection.StartAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during connection start");
        }

        return connection;
    }
}
