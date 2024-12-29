// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions;
using Microsoft.Extensions.Hosting;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Enums;
using Windows.Win32.NetworkManagement.WindowsFirewall;

namespace RemoteMaster.Host.Windows.Services;

public class FirewallInitializationService(IFileSystem fileSystem, IFirewallService firewallService, IApplicationPathProvider applicationPathProvider) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var currentExecutableName = fileSystem.Path.GetFileName(Environment.ProcessPath!);

        firewallService.AddRule("Remote Master Host", NET_FW_ACTION.NET_FW_ACTION_ALLOW, NET_FW_IP_PROTOCOL.NET_FW_IP_PROTOCOL_ANY, NET_FW_PROFILE_TYPE2.NET_FW_PROFILE2_ALL, NET_FW_RULE_DIRECTION.NET_FW_RULE_DIR_IN, InterfaceType.All, "Allow all traffic for RemoteMaster Host", fileSystem.Path.Combine(applicationPathProvider.RootDirectory, currentExecutableName));
        firewallService.AddRule("Remote Master Host Updater", NET_FW_ACTION.NET_FW_ACTION_ALLOW, NET_FW_IP_PROTOCOL.NET_FW_IP_PROTOCOL_ANY, NET_FW_PROFILE_TYPE2.NET_FW_PROFILE2_ALL, NET_FW_RULE_DIRECTION.NET_FW_RULE_DIR_IN, InterfaceType.All, "Allow all traffic for RemoteMaster Host Updater", fileSystem.Path.Combine(applicationPathProvider.UpdaterDirectory, currentExecutableName));

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
