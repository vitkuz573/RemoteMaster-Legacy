// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Windows.Enums;
using Windows.Win32.NetworkManagement.WindowsFirewall;

namespace RemoteMaster.Host.Windows.Abstractions;

public interface IFirewallService
{
    void AddRule(string name, NET_FW_ACTION action, NET_FW_IP_PROTOCOL protocol, NET_FW_PROFILE_TYPE2 profiles, NET_FW_RULE_DIRECTION direction, InterfaceType interfaceTypes, string? description = null, string? applicationPath = null, string? localAddress = null, string? localPort = null, string? remoteAddress = null, string? remotePort = null, string? service = null, bool edgeTraversal = false);

    void RemoveRule(string name);

    void EnableRuleGroup(string groupName);

    void DisableRuleGroup(string groupName);

    bool RuleExists(string name);
}