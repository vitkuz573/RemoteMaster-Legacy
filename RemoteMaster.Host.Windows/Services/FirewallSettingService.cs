// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using NetFwTypeLib;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Windows.Services;

public class FirewallSettingService : IFirewallSettingService
{
    public void AddRule(string name, string applicationPath)
    {
        var netFwPolicy2Type = Type.GetTypeFromProgID("HNetCfg.FwPolicy2", false);
        var fwPolicy2 = (INetFwPolicy2)Activator.CreateInstance(netFwPolicy2Type);

        var existingRule = fwPolicy2.Rules.Cast<INetFwRule>().FirstOrDefault(rule => rule.Name == name);

        if (existingRule != null)
        {
            if (existingRule.Action == NET_FW_ACTION_.NET_FW_ACTION_ALLOW)
            {
                return;
            }

            existingRule.Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
            existingRule.Enabled = true;
            existingRule.ApplicationName = applicationPath;

            return;
        }

        var newRule = (INetFwRule)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule", false));
        newRule.Name = name;
        newRule.Description = $"Allow {name}";
        newRule.ApplicationName = applicationPath;
        newRule.Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
        newRule.Enabled = true;
        newRule.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN;
        newRule.Protocol = (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_ANY;
        newRule.InterfaceTypes = "All";

        fwPolicy2.Rules.Add(newRule);
    }
}