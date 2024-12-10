// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Components.Pages;

public partial class Home
{
    private RenderFragment RenderTabs() => builder =>
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
                    IsDisabled = () => NoHostsSelected() || NoHostsAvailable(),
                    Class = "mr-2"
                },
                new ActionDefinition
                {
                    Label = "Wake Up",
                    OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await WakeUp()),
                    IsVisible = () => UserHasClaim("Power", "WakeUp"),
                    IsDisabled = () => NoHostsSelected() || NoHostsAvailable(),
                    Class = "mr-2"
                },
                new ActionDefinition
                {
                    Label = "Connect",
                    OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await Connect()),
                    IsVisible = () => UserHasAnyClaim("Connect"),
                    IsDisabled = () => NoHostsSelected() || NoHostsAvailable(),
                    Class = "mr-2"
                },
                new ActionDefinition
                {
                    Label = "Lock",
                    OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await Lock()),
                    IsVisible = () => UserHasClaim("Security", "LockWorkStation"),
                    IsDisabled = () => NoHostsSelected() || NoHostsAvailable(),
                    Class = "mr-2"
                },
                new ActionDefinition
                {
                    Label = "Open Shell",
                    OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await OpenShell()),
                    IsVisible = () => UserHasClaim("Execution", "OpenShell"),
                    IsDisabled = NoHostsSelected,
                    Class = "mr-2"
                },
                new ActionDefinition
                {
                    Label = "Execute Script",
                    OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await ExecuteScript()),
                    IsVisible = () => UserHasClaim("Execution", "Scripts"),
                    IsDisabled = () => NoHostsSelected() || NoHostsAvailable(),
                    Class = "mr-2"
                },
                new ActionDefinition
                {
                    Label = "Logon",
                    OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await LogonHosts()),
                    IsVisible = () => true,
                    IsDisabled = () => NoHostsSelected() || !AnySelectedHostsAvailableOrUnavailable(),
                    Class = "mr-2"
                },
                new ActionDefinition
                {
                    Label = "Logoff",
                    OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await LogoffHosts()),
                    IsVisible = () => true,
                    IsDisabled = () => NoHostsSelected() || !AnySelectedHostsAvailableOrUnavailable(),
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
                    IsDisabled = () => NoHostsSelected() || NoHostsAvailable(),
                    Class = "mr-2"
                },
                new ActionDefinition
                {
                    Label = "Set Monitor State",
                    OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await SetMonitorState()),
                    IsVisible = () => UserHasClaim("Hardware", "SetMonitorState"),
                    IsDisabled = () => NoHostsSelected() || NoHostsAvailable(),
                    Class = "mr-2"
                },
                new ActionDefinition
                {
                    Label = "PSExec Rules",
                    OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await ManagePsExecRules()),
                    IsVisible = () => UserHasClaim("Execution", "Scripts"),
                    IsDisabled = () => NoHostsSelected() || NoHostsAvailable(),
                    Class = "mr-2"
                },
                new ActionDefinition
                {
                    Label = "Screen Recorder",
                    OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await ScreenRecorder()),
                    IsVisible = () => UserHasAnyClaim("ScreenRecording"),
                    IsDisabled = () => NoHostsSelected() || NoHostsAvailable(),
                    Class = "mr-2"
                },
                new ActionDefinition
                {
                    Label = "Domain Membership",
                    OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await DomainMembership()),
                    IsVisible = () => UserHasAnyClaim("DomainManagement"),
                    IsDisabled = () => NoHostsSelected() || NoHostsAvailable(),
                    Class = "mr-2"
                },
                new ActionDefinition
                {
                    Label = "Update",
                    OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await Update()),
                    IsVisible = () => UserHasClaim("UpdaterManagement", "Start"),
                    IsDisabled = () => NoHostsSelected() || NoHostsAvailable(),
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
                    IsDisabled = () => NoHostsSelected() || NoHostsAvailable(),
                    Class = "mr-2"
                },
                new ActionDefinition
                {
                    Label = "Task Manager",
                    OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await OpenTaskManager()),
                    IsVisible = () => UserHasAnyClaim("TaskManagement"),
                    IsDisabled = () => NoHostsSelected() || NoHostsAvailable(),
                    Class = "mr-2"
                },
                new ActionDefinition
                {
                    Label = "Device Manager",
                    OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await OpenDeviceManager()),
                    IsVisible = () => UserHasAnyClaim("DeviceManagement"),
                    IsDisabled = () => NoHostsSelected() || NoHostsAvailable(),
                    Class = "mr-2"
                },
                new ActionDefinition
                {
                    Label = "File Manager",
                    OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await OpenFileManager()),
                    IsVisible = () => UserHasAnyClaim("FileManagement"),
                    IsDisabled = () => NoHostsSelected() || NoHostsAvailable(),
                    Class = "mr-2"
                },
                new ActionDefinition
                {
                    Label = "Upload File",
                    OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await FileUpload()),
                    IsVisible = () => UserHasClaim("FileManagement", "Upload"),
                    IsDisabled = () => NoHostsSelected() || NoHostsAvailable(),
                    Class = "mr-2"
                },
                new ActionDefinition
                {
                    Label = "Registry Editor",
                    OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await OpenRegistryEditor()),
                    IsVisible = () => UserHasAnyClaim("RegistryManagement"),
                    IsDisabled = () => NoHostsSelected() || NoHostsAvailable(),
                    Class = "mr-2"
                },
                new ActionDefinition
                {
                    Label = "Message Box",
                    OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await MessageBox()),
                    IsVisible = () => UserHasClaim("Execution", "Scripts"),
                    IsDisabled = () => NoHostsSelected() || NoHostsAvailable(),
                    Class = "mr-2"
                },
                new ActionDefinition
                {
                    Label = "Send Message",
                    OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await SendMessage()),
                    IsVisible = () => UserHasAnyClaim("ChatManagement"),
                    IsDisabled = () => NoHostsSelected() || NoHostsAvailable(),
                    Class = "mr-2"
                },
                new ActionDefinition
                {
                    Label = "Chat",
                    OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await OpenChat()),
                    IsVisible = () => UserHasAnyClaim("ChatManagement"),
                    IsDisabled = () => NoHostsSelected() || NoHostsAvailable(),
                    Class = "mr-2"
                },
                new ActionDefinition
                {
                    Label = "Logs Viewer",
                    OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await OpenLogsManager()),
                    IsVisible = () => UserHasAnyClaim("LogManagement"),
                    IsDisabled = () => NoHostsSelected() || NoHostsAvailable(),
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
                    IsDisabled = NoHostsSelected,
                    Class = "mr-2"
                },
                new ActionDefinition
                {
                    Label = "Remove",
                    OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await OpenHostRemoveDialog()),
                    IsVisible = () => UserHasClaim("HostManagement", "Remove"),
                    IsDisabled = NoHostsSelected,
                    Class = "mr-2"
                },
                new ActionDefinition
                {
                    Label = "Renew Certificate",
                    OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await RenewCertificate()),
                    IsVisible = () => UserHasClaim("CertificateManagement", "Renew"),
                    IsDisabled = () => NoHostsSelected() || NoHostsAvailable(),
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
                    IsDisabled = NoHostsSelected,
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

        bool NoHostsAvailable() => !AllSelectedHostsAvailable();

        bool NoHostsSelected() => _selectedHosts.Count == 0;

        bool AnySelectedHostsAvailableOrUnavailable() => AnyHostsSelected() && _selectedHosts.Any(c => _availableHosts.ContainsKey(c.IpAddress) || _unavailableHosts.ContainsKey(c.IpAddress));

        bool AllSelectedHostsAvailable() => AnyHostsSelected() && _selectedHosts.All(c => _availableHosts.ContainsKey(c.IpAddress));

        bool AnyHostsSelected() => _selectedHosts.Count > 0;
    }
}
