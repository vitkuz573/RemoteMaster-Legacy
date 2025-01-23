// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Runtime.InteropServices;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Enums;
using Windows.Win32.Foundation;
using Windows.Win32.NetworkManagement.WindowsFirewall;
using Windows.Win32.System.Com;

namespace RemoteMaster.Host.Windows.Services;

public class FirewallService : IFirewallService
{
    public unsafe void AddRule(string name, NET_FW_ACTION action, NET_FW_IP_PROTOCOL protocol, NET_FW_PROFILE_TYPE2 profiles, NET_FW_RULE_DIRECTION direction, InterfaceType interfaceTypes, string? description = null, string? applicationPath = null, string? localAddress = null, string? localPort = null, string? remoteAddress = null, string? remotePort = null, string? service = null, bool edgeTraversal = false, string? icmpTypesAndCodes = null, object? interfaces = null, string? grouping = null)
    {
        var fwPolicy2 = CreateInstance<INetFwPolicy2.Interface>("E2B3C97F-6AE1-41AC-817A-F6F92166D7DD") ?? throw new InvalidOperationException("Failed to get firewall policy.");

        var existingRule = GetRule(name);

        if (existingRule != null && RulePropertiesChanged(existingRule, action, protocol, profiles, direction, applicationPath, localAddress, localPort, remoteAddress, remotePort, service, description, interfaceTypes, edgeTraversal, icmpTypesAndCodes, interfaces, grouping))
        {
            fwPolicy2.Rules->Remove((BSTR)Marshal.StringToBSTR(name));
            existingRule = null;
        }

        if (existingRule != null)
        {
            return;
        }

        var newRuleObj = CreateInstance<INetFwRule.Interface>("2C5BC43E-3369-4C33-AB0C-BE9469677AF4") ?? throw new InvalidOperationException("Failed to create new firewall rule.");

        newRuleObj.Name = (BSTR)Marshal.StringToBSTR(name);
        newRuleObj.Action = action;
        newRuleObj.Enabled = action == NET_FW_ACTION.NET_FW_ACTION_ALLOW;
        newRuleObj.Direction = direction;
        newRuleObj.Protocol = (int)protocol;
        newRuleObj.InterfaceTypes = (BSTR)Marshal.StringToBSTR(interfaceTypes.ToString());

        if (description != null)
        {
            newRuleObj.Description = (BSTR)Marshal.StringToBSTR(description);
        }

        if (applicationPath != null)
        {
            newRuleObj.ApplicationName = (BSTR)Marshal.StringToBSTR(applicationPath);
        }

        if (icmpTypesAndCodes != null)
        {
            newRuleObj.IcmpTypesAndCodes = (BSTR)Marshal.StringToBSTR(icmpTypesAndCodes);
        }

        // if (interfaces != null)
        // {
        //     newRule.Interfaces = new VARIANT(interfaces);
        // }

        if (grouping != null)
        {
            newRuleObj.Grouping = (BSTR)Marshal.StringToBSTR(grouping);
        }

        if (localAddress != null)
        {
            newRuleObj.LocalAddresses = (BSTR)Marshal.StringToBSTR(localAddress);
        }

        if (localPort != null)
        {
            newRuleObj.LocalPorts = (BSTR)Marshal.StringToBSTR(localPort);
        }

        if (remoteAddress != null)
        {
            newRuleObj.RemoteAddresses = (BSTR)Marshal.StringToBSTR(remoteAddress);
        }

        if (remotePort != null)
        {
            newRuleObj.RemotePorts = (BSTR)Marshal.StringToBSTR(remotePort);
        }

        if (service != null)
        {
            newRuleObj.ServiceName = (BSTR)Marshal.StringToBSTR(service);
        }

        newRuleObj.Profiles = (int)profiles;
        newRuleObj.EdgeTraversal = edgeTraversal;

        var unkPtr = Marshal.GetIUnknownForObject(newRuleObj);

        try
        {
            var iunk = (IUnknown*)unkPtr;
            iunk->QueryInterface(out INetFwRule* newRulePtr).ThrowOnFailure();

            fwPolicy2.Rules->Add(newRulePtr);
        }
        finally
        {
            Marshal.Release(unkPtr);
        }
    }

    public unsafe void RemoveRule(string name)
    {
        var fwPolicy2 = CreateInstance<INetFwPolicy2.Interface>("E2B3C97F-6AE1-41AC-817A-F6F92166D7DD") ?? throw new InvalidOperationException("Failed to get firewall policy.");
        fwPolicy2.Rules->Remove((BSTR)Marshal.StringToBSTR(name));
    }

    public unsafe void EnableRuleGroup(string groupName)
    {
        var fwPolicy2 = CreateInstance<INetFwPolicy2.Interface>("E2B3C97F-6AE1-41AC-817A-F6F92166D7DD") ?? throw new InvalidOperationException("Failed to get firewall policy.");
        var profileTypesBitmask = fwPolicy2.CurrentProfileTypes;
        var bstrGroupName = (BSTR)Marshal.StringToBSTR(groupName);

        try
        {
            VARIANT_BOOL enabled;
            var hr = fwPolicy2.IsRuleGroupEnabled(profileTypesBitmask, bstrGroupName, &enabled);
            hr.ThrowOnFailure();

            if (!enabled)
            {
                fwPolicy2.EnableRuleGroup(profileTypesBitmask, bstrGroupName, true);
            }
        }
        finally
        {
            Marshal.FreeBSTR(bstrGroupName);
        }
    }

    public unsafe void DisableRuleGroup(string groupName)
    {
        var fwPolicy2 = CreateInstance<INetFwPolicy2.Interface>("E2B3C97F-6AE1-41AC-817A-F6F92166D7DD") ?? throw new InvalidOperationException("Failed to get firewall policy.");
        var profileTypesBitmask = fwPolicy2.CurrentProfileTypes;
        var bstrGroupName = (BSTR)Marshal.StringToBSTR(groupName);

        try
        {
            VARIANT_BOOL enabled;
            var hr = fwPolicy2.IsRuleGroupEnabled(profileTypesBitmask, bstrGroupName, &enabled);
            hr.ThrowOnFailure();

            if (enabled)
            {
                fwPolicy2.EnableRuleGroup(profileTypesBitmask, bstrGroupName, false);
            }
        }
        finally
        {
            Marshal.FreeBSTR(bstrGroupName);
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
    }

    private static unsafe bool RulePropertiesChanged(INetFwRule* existingRule, NET_FW_ACTION action, NET_FW_IP_PROTOCOL protocol, NET_FW_PROFILE_TYPE2 profiles, NET_FW_RULE_DIRECTION direction, string? appPath, string? localAddress, string? localPort, string? remoteAddress, string? remotePort, string? service, string? description, InterfaceType interfaceTypes, bool edgeTraversal, string? icmpTypesAndCodes, object? interfaces, string? grouping)
    {
        return existingRule->Action != action ||
               existingRule->Protocol != (int)protocol ||
               existingRule->Profiles != (int)profiles ||
               existingRule->Direction != direction ||
               !string.Equals(Marshal.PtrToStringBSTR(existingRule->InterfaceTypes), interfaceTypes.ToString(), StringComparison.Ordinal) ||
               existingRule->EdgeTraversal != edgeTraversal ||
               !Equals(existingRule->ApplicationName, appPath) ||
               !Equals(existingRule->LocalAddresses, localAddress) ||
               !Equals(existingRule->LocalPorts, localPort) ||
               !Equals(existingRule->RemoteAddresses, remoteAddress) ||
               !Equals(existingRule->RemotePorts, remotePort) ||
               !Equals(existingRule->ServiceName, service) ||
               !Equals(existingRule->Description, description) ||
               !Equals(existingRule->IcmpTypesAndCodes, icmpTypesAndCodes) ||
               !Equals(existingRule->Interfaces, interfaces) ||
               !Equals(existingRule->Grouping, grouping);
    }

    private static unsafe INetFwRule* GetRule(string name)
    {
        var fwPolicy2 = CreateInstance<INetFwPolicy2.Interface>("E2B3C97F-6AE1-41AC-817A-F6F92166D7DD") ?? throw new InvalidOperationException("Failed to get firewall policy.");

        try
        {
            return fwPolicy2.Rules->Item((BSTR)Marshal.StringToBSTR(name));
        }
        catch (FileNotFoundException)
        {
            return null;
        }
    }
}
