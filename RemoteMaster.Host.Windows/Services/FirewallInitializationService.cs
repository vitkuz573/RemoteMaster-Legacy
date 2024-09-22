// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.Hosting;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Enums;
using Windows.Win32.NetworkManagement.WindowsFirewall;

namespace RemoteMaster.Host.Windows.Services;

public class FirewallInitializationService(IFirewallService firewallService) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var hostRootPath = Path.Combine(programFilesPath, "RemoteMaster", "Host");
        var hostApplicationPath = Path.Combine(hostRootPath, "RemoteMaster.Host.exe");
        var hostUpdaterApplicationPath = Path.Combine(hostRootPath, "Updater", "RemoteMaster.Host.exe");

        firewallService.AddRule("Remote Master Host", NET_FW_ACTION.NET_FW_ACTION_ALLOW, NET_FW_IP_PROTOCOL.NET_FW_IP_PROTOCOL_ANY, NET_FW_PROFILE_TYPE2.NET_FW_PROFILE2_ALL, NET_FW_RULE_DIRECTION.NET_FW_RULE_DIR_IN, InterfaceType.All, "Allow all traffic for RemoteMaster Host", hostApplicationPath);
        firewallService.AddRule("Remote Master Host Updater", NET_FW_ACTION.NET_FW_ACTION_ALLOW, NET_FW_IP_PROTOCOL.NET_FW_IP_PROTOCOL_ANY, NET_FW_PROFILE_TYPE2.NET_FW_PROFILE2_ALL, NET_FW_RULE_DIRECTION.NET_FW_RULE_DIR_IN, InterfaceType.All, "Allow all traffic for RemoteMaster Host Updater", hostUpdaterApplicationPath);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
