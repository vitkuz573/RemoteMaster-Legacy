// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Windows.Win32.NetworkManagement.WindowsFirewall;
using RemoteMaster.Host.Windows.Enums;

namespace RemoteMaster.Host.Windows.Abstractions;

public interface IFirewallService
{
    void AddRule(string name, NET_FW_ACTION action, NET_FW_IP_PROTOCOL protocol, NET_FW_PROFILE_TYPE2 profiles, string? description = null, string? applicationPath = null, string? localPort = null, string? remoteIp = null, string? service = null, InterfaceType interfaceTypes = InterfaceType.All);

    void RemoveRule(string name);

    void EnableRuleGroup(string groupName);

    void DisableRuleGroup(string groupName);

    bool RuleExists(string name);
}