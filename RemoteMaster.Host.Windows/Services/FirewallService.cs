// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Runtime.InteropServices;
using RemoteMaster.Host.Core.Abstractions;
using Windows.Win32.Foundation;
using Windows.Win32.NetworkManagement.WindowsFirewall;

namespace RemoteMaster.Host.Windows.Services;

public class FirewallService : IFirewallService
{
    public void AddRule(string name, string applicationPath)
    {
        var fwPolicy2 = GetFirewallPolicy();
        if (fwPolicy2 == null)
        {
            throw new InvalidOperationException("Failed to get firewall policy.");
        }

        var existingRule = GetRule(fwPolicy2, name);

        var bstrName = (BSTR)Marshal.StringToBSTR(name);
        var bstrAppPath = (BSTR)Marshal.StringToBSTR(applicationPath);
        var bstrDescription = (BSTR)Marshal.StringToBSTR($"Allow {name}");
        var bstrAll = (BSTR)Marshal.StringToBSTR("All");

        try
        {
            if (existingRule != null)
            {
                if (existingRule.Action == NET_FW_ACTION.NET_FW_ACTION_ALLOW)
                {
                    return;
                }

                existingRule.Action = NET_FW_ACTION.NET_FW_ACTION_ALLOW;
                existingRule.Enabled = true;
                existingRule.ApplicationName = bstrAppPath;

                return;
            }

            var newRule = CreateFwRuleInstance() ?? throw new InvalidOperationException("Failed to create new firewall rule.");

            newRule.Name = bstrName;
            newRule.Description = bstrDescription;
            newRule.ApplicationName = bstrAppPath;
            newRule.Action = NET_FW_ACTION.NET_FW_ACTION_ALLOW;
            newRule.Enabled = true;
            newRule.Direction = NET_FW_RULE_DIRECTION.NET_FW_RULE_DIR_IN;
            newRule.Protocol = (int)NET_FW_IP_PROTOCOL.NET_FW_IP_PROTOCOL_ANY;
            newRule.InterfaceTypes = bstrAll;

            fwPolicy2.Rules.Add(newRule);
        }
        finally
        {
            Marshal.FreeBSTR((IntPtr)bstrName);
            Marshal.FreeBSTR((IntPtr)bstrAppPath);
            Marshal.FreeBSTR((IntPtr)bstrDescription);
            Marshal.FreeBSTR((IntPtr)bstrAll);
        }
    }

    private static INetFwPolicy2 GetFirewallPolicy()
    {
        try
        {
            var clsid = new Guid("E2B3C97F-6AE1-41AC-817A-F6F92166D7DD");
            var type = Type.GetTypeFromCLSID(clsid);

            return (INetFwPolicy2)Activator.CreateInstance(type);
        }
        catch (COMException ex) when (ex.ErrorCode == unchecked((int)0x80040154))
        {
            throw new InvalidOperationException("INetFwPolicy2 COM object is not registered. Please ensure that all necessary components are installed and registered correctly.", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("An unexpected error occurred while trying to get the firewall policy.", ex);
        }
    }

    private static INetFwRule CreateFwRuleInstance()
    {
        try
        {
            var clsid = new Guid("2C5BC43E-3369-4C33-AB0C-BE9469677AF4");
            var type = Type.GetTypeFromCLSID(clsid);

            return (INetFwRule)Activator.CreateInstance(type);
        }
        catch (COMException ex) when (ex.ErrorCode == unchecked((int)0x80040154))
        {
            throw new InvalidOperationException("INetFwRule COM object is not registered. Please ensure that all necessary components are installed and registered correctly.", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("An unexpected error occurred while trying to create the firewall rule instance.", ex);
        }
    }

    private static INetFwRule? GetRule(INetFwPolicy2 fwPolicy2, string name)
    {
        var bstrName = Marshal.StringToBSTR(name);

        try
        {
            return fwPolicy2.Rules.Item((BSTR)bstrName);
        }
        catch (Exception ex)
        {
            return null;
        }
        finally
        {
            Marshal.FreeBSTR(bstrName);
        }
    }
}
