// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Runtime.InteropServices;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Enums;
using Windows.Win32.Foundation;
using Windows.Win32.NetworkManagement.WindowsFirewall;

namespace RemoteMaster.Host.Windows.Services;

public class FirewallService : IFirewallService
{
    public void AddRule(string name, NET_FW_ACTION action, NET_FW_IP_PROTOCOL protocol, NET_FW_PROFILE_TYPE2 profiles, NET_FW_RULE_DIRECTION direction, InterfaceType interfaceTypes, string? description = null, string? applicationPath = null, string? localAddress = null, string? localPort = null, string? remoteAddress = null, string? remotePort = null, string? service = null, bool edgeTraversal = false)
    {
        var fwPolicy2 = CreateInstance<INetFwPolicy2>("E2B3C97F-6AE1-41AC-817A-F6F92166D7DD") ?? throw new InvalidOperationException("Failed to get firewall policy.");
        var existingRule = GetRule(fwPolicy2, name);

        var bstrName = (BSTR)Marshal.StringToBSTR(name);
        var bstrDescription = description != null ? (BSTR)Marshal.StringToBSTR(description) : default;
        var bstrAppPath = applicationPath != null ? (BSTR)Marshal.StringToBSTR(applicationPath) : default;
        var bstrLocalAddress = localAddress != null && !string.IsNullOrEmpty(localAddress) ? (BSTR)Marshal.StringToBSTR(localAddress) : default;
        var bstrLocalPort = localPort != null && !string.IsNullOrEmpty(localPort) ? (BSTR)Marshal.StringToBSTR(localPort) : default;
        var bstrRemoteAddress = remoteAddress != null && !string.IsNullOrEmpty(remoteAddress) ? (BSTR)Marshal.StringToBSTR(remoteAddress) : default;
        var bstrRemotePort = remotePort != null && !string.IsNullOrEmpty(remotePort) ? (BSTR)Marshal.StringToBSTR(remotePort) : default;
        var bstrService = service != null ? (BSTR)Marshal.StringToBSTR(service) : default;
        var bstrInterfaceTypes = (BSTR)Marshal.StringToBSTR(interfaceTypes.ToString());

        try
        {
            if (existingRule != null && RulePropertiesChanged(existingRule, action, protocol, profiles, direction, bstrAppPath, bstrLocalAddress, bstrLocalPort, bstrRemoteAddress, bstrRemotePort, bstrService, bstrDescription, bstrInterfaceTypes, edgeTraversal))
            {
                fwPolicy2.Rules.Remove(bstrName);
                existingRule = null;
            }

            if (existingRule != null)
            {
                return;
            }

            var newRule = CreateInstance<INetFwRule>("2C5BC43E-3369-4C33-AB0C-BE9469677AF4") ?? throw new InvalidOperationException("Failed to create new firewall rule.");

            newRule.Name = bstrName;

            if (description != null)
            {
                newRule.Description = bstrDescription;
            }

            if (applicationPath != null)
            {
                newRule.ApplicationName = bstrAppPath;
            }

            newRule.Action = action;
            newRule.Enabled = action == NET_FW_ACTION.NET_FW_ACTION_ALLOW;
            newRule.Direction = direction;
            newRule.Protocol = (int)protocol;
            newRule.InterfaceTypes = bstrInterfaceTypes;

            unsafe
            {
                if (bstrLocalAddress != default && (nint)bstrLocalAddress.Value != nint.Zero && !string.IsNullOrEmpty(Marshal.PtrToStringBSTR((nint)bstrLocalAddress.Value)))
                {
                    newRule.LocalAddresses = bstrLocalAddress;
                }

                if (bstrLocalPort != default && (nint)bstrLocalPort.Value != nint.Zero && !string.IsNullOrEmpty(Marshal.PtrToStringBSTR((nint)bstrLocalPort.Value)))
                {
                    newRule.LocalPorts = bstrLocalPort;
                }

                if (bstrRemoteAddress != default && (nint)bstrRemoteAddress.Value != nint.Zero && !string.IsNullOrEmpty(Marshal.PtrToStringBSTR((nint)bstrRemoteAddress.Value)))
                {
                    newRule.RemoteAddresses = bstrRemoteAddress;
                }

                if (bstrRemotePort != default && (nint)bstrRemotePort.Value != nint.Zero && !string.IsNullOrEmpty(Marshal.PtrToStringBSTR((nint)bstrRemotePort.Value)))
                {
                    newRule.RemotePorts = bstrRemotePort;
                }
            }

            if (service != null)
            {
                newRule.ServiceName = bstrService;
            }

            newRule.Profiles = (int)profiles;
            newRule.EdgeTraversal = edgeTraversal;

            fwPolicy2.Rules.Add(newRule);
        }
        finally
        {
            Marshal.FreeBSTR(bstrName);

            if (bstrDescription != default)
            {
                Marshal.FreeBSTR(bstrDescription);
            }

            if (bstrAppPath != default)
            {
                Marshal.FreeBSTR(bstrAppPath);
            }

            if (bstrLocalAddress != default)
            {
                Marshal.FreeBSTR(bstrLocalAddress);
            }

            if (bstrLocalPort != default)
            {
                Marshal.FreeBSTR(bstrLocalPort);
            }

            if (bstrRemoteAddress != default)
            {
                Marshal.FreeBSTR(bstrRemoteAddress);
            }

            if (bstrRemotePort != default)
            {
                Marshal.FreeBSTR(bstrRemotePort);
            }

            if (bstrService != default)
            {
                Marshal.FreeBSTR(bstrService);
            }

            Marshal.FreeBSTR(bstrInterfaceTypes);
        }
    }

    private static bool RulePropertiesChanged(INetFwRule existingRule, NET_FW_ACTION action, NET_FW_IP_PROTOCOL protocol, NET_FW_PROFILE_TYPE2 profiles, NET_FW_RULE_DIRECTION direction, BSTR? bstrAppPath, BSTR? bstrLocalAddress, BSTR? bstrLocalPort, BSTR? bstrRemoteAddress, BSTR? bstrRemotePort, BSTR? bstrService, BSTR? bstrDescription, BSTR bstrInterfaceTypes, bool edgeTraversal)
    {
        if (existingRule.Action != action || existingRule.Protocol != (int)protocol || existingRule.Profiles != (int)profiles || existingRule.Direction != direction || existingRule.InterfaceTypes != bstrInterfaceTypes || existingRule.EdgeTraversal != edgeTraversal)
        {
            return true;
        }

        if (bstrAppPath.HasValue && existingRule.ApplicationName != bstrAppPath.Value)
        {
            return true;
        }

        if (bstrLocalAddress.HasValue && existingRule.LocalAddresses != bstrLocalAddress.Value)
        {
            return true;
        }

        if (bstrLocalPort.HasValue && existingRule.LocalPorts != bstrLocalPort.Value)
        {
            return true;
        }

        if (bstrRemoteAddress.HasValue && existingRule.RemoteAddresses != bstrRemoteAddress.Value)
        {
            return true;
        }

        if (bstrRemotePort.HasValue && existingRule.RemotePorts != bstrRemotePort.Value)
        {
            return true;
        }

        if (bstrService.HasValue && existingRule.ServiceName != bstrService.Value)
        {
            return true;
        }

        return bstrDescription.HasValue && existingRule.Description != bstrDescription.Value;
    }

    public void RemoveRule(string name)
    {
        var fwPolicy2 = CreateInstance<INetFwPolicy2>("E2B3C97F-6AE1-41AC-817A-F6F92166D7DD") ?? throw new InvalidOperationException("Failed to get firewall policy.");

        var bstrName = (BSTR)Marshal.StringToBSTR(name);

        try
        {
            var rule = GetRule(fwPolicy2, name);

            if (rule != null)
            {
                fwPolicy2.Rules.Remove(bstrName);
            }
        }
        finally
        {
            Marshal.FreeBSTR(bstrName);
        }
    }

    public void EnableRuleGroup(string groupName)
    {
        var fwPolicy2 = CreateInstance<INetFwPolicy2>("E2B3C97F-6AE1-41AC-817A-F6F92166D7DD") ?? throw new InvalidOperationException("Failed to get firewall policy.");
        var bstrGroupName = (BSTR)Marshal.StringToBSTR(groupName);

        try
        {
            var profileTypesBitmask = fwPolicy2.CurrentProfileTypes;
            fwPolicy2.EnableRuleGroup(profileTypesBitmask, bstrGroupName, true);
        }
        finally
        {
            Marshal.FreeBSTR(bstrGroupName);
        }
    }

    public void DisableRuleGroup(string groupName)
    {
        var fwPolicy2 = CreateInstance<INetFwPolicy2>("E2B3C97F-6AE1-41AC-817A-F6F92166D7DD") ?? throw new InvalidOperationException("Failed to get firewall policy.");
        var bstrGroupName = (BSTR)Marshal.StringToBSTR(groupName);

        try
        {
            var profileTypesBitmask = fwPolicy2.CurrentProfileTypes;
            fwPolicy2.EnableRuleGroup(profileTypesBitmask, bstrGroupName, false);
        }
        finally
        {
            Marshal.FreeBSTR(bstrGroupName);
        }
    }

    public bool RuleExists(string name)
    {
        var fwPolicy2 = CreateInstance<INetFwPolicy2>("E2B3C97F-6AE1-41AC-817A-F6F92166D7DD") ?? throw new InvalidOperationException("Failed to get firewall policy.");
        var bstrName = (BSTR)Marshal.StringToBSTR(name);

        try
        {
            return GetRule(fwPolicy2, name) != null;
        }
        finally
        {
            Marshal.FreeBSTR(bstrName);
        }
    }

    private static INetFwRule? GetRule(INetFwPolicy2 fwPolicy2, string name)
    {
        var bstrName = Marshal.StringToBSTR(name);

        try
        {
            return fwPolicy2.Rules.Item((BSTR)bstrName);
        }
        catch (FileNotFoundException ex) when (ex.HResult == unchecked((int)0x80070002))
        {
            return null;
        }
        finally
        {
            Marshal.FreeBSTR(bstrName);
        }
    }

    private static T? CreateInstance<T>(string clsid) where T : class
    {
        try
        {
            var type = Type.GetTypeFromCLSID(new Guid(clsid));

            return type == null
                ? throw new InvalidOperationException($"The type for CLSID {clsid} could not be found.")
                : (T?)Activator.CreateInstance(type);
        }
        catch (COMException ex) when (ex.ErrorCode == unchecked((int)0x80040154))
        {
            throw new InvalidOperationException($"{typeof(T).Name} COM object is not registered. Please ensure that all necessary components are installed and registered correctly.", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"An unexpected error occurred while trying to create the {typeof(T).Name} instance.", ex);
        }
    }
}