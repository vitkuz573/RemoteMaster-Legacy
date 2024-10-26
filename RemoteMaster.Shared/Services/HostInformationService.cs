// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.DTOs;

namespace RemoteMaster.Shared.Services;

public class HostInformationService : IHostInformationService
{
    public HostDto GetHostInformation()
    {
        var hostName = Dns.GetHostName();
        var preferredInterface = GetPreferredNetworkInterface();
        var ipv4Address = GetIPv4Address(preferredInterface);
        var macAddress = GetMacAddress(preferredInterface);

        return new HostDto(hostName, ipv4Address, macAddress);
    }

    private static NetworkInterface GetPreferredNetworkInterface()
    {
        var interfaces = NetworkInterface.GetAllNetworkInterfaces()
            .Where(nic => nic.OperationalStatus == OperationalStatus.Up)
            .Where(nic => IsSupportedNetworkInterfaceType(nic.NetworkInterfaceType))
            .Where(nic => !IsVpnAdapter(nic) || IsTrustedVpnAdapter(nic))
            .Where(nic => !IsVirtualAdapter(nic))
            .Where(nic => nic.GetIPProperties().UnicastAddresses.Any(ua => ua.Address.AddressFamily == AddressFamily.InterNetwork && !ua.Address.ToString().StartsWith("169.254")))
            .Select(nic => new
            {
                Interface = nic,
                Priority = GetInterfacePriority(nic.NetworkInterfaceType)
            }).MinBy(nic => nic.Priority)?.Interface;

        return interfaces ?? throw new InvalidOperationException("Active network interface not found. Network might be disabled or not properly configured");
    }

    private static bool IsSupportedNetworkInterfaceType(NetworkInterfaceType type)
    {
        return type == NetworkInterfaceType.Wireless80211 ||
               type == NetworkInterfaceType.Tunnel ||
               type == NetworkInterfaceType.Ethernet ||
               type == NetworkInterfaceType.FastEthernetT ||
               type == NetworkInterfaceType.FastEthernetFx ||
               type == NetworkInterfaceType.GigabitEthernet ||
               type == NetworkInterfaceType.Ethernet3Megabit;
    }

    private static int GetInterfacePriority(NetworkInterfaceType type)
    {
        return type switch
        {
            NetworkInterfaceType.Ethernet or NetworkInterfaceType.FastEthernetT or NetworkInterfaceType.FastEthernetFx or NetworkInterfaceType.GigabitEthernet => 1,
            NetworkInterfaceType.Wireless80211 => 2,
            NetworkInterfaceType.Tunnel => 3,
            _ => 4
        };
    }

    private static bool IsVpnAdapter(NetworkInterface nic)
    {
        var descriptionLower = nic.Description.ToLower();

        return descriptionLower.Contains("vpn") || descriptionLower.Contains("tun");
    }

    private static bool IsTrustedVpnAdapter(NetworkInterface nic)
    {
        var descriptionLower = nic.Description.ToLower();

        return descriptionLower.Contains("trustedvpn");
    }

    private static bool IsVirtualAdapter(NetworkInterface nic)
    {
        var descriptionLower = nic.Description.ToLower();
        var macAddress = nic.GetPhysicalAddress().ToString();

        return string.IsNullOrEmpty(macAddress) ||
               descriptionLower.Contains("tap") ||
               descriptionLower.Contains("virtual") ||
               descriptionLower.Contains("vmware") ||
               descriptionLower.Contains("wi-fi direct") ||
               descriptionLower.Contains("hyper-v") ||
               descriptionLower.Contains("pseudo");
    }

    private static IPAddress GetIPv4Address(NetworkInterface networkInterface)
    {
        var ipv4Address = networkInterface.GetIPProperties().UnicastAddresses
            .FirstOrDefault(ua => ua.Address.AddressFamily == AddressFamily.InterNetwork);

        return ipv4Address == null
            ? throw new InvalidOperationException("Valid IPv4 address not found for the selected interface.")
            : ipv4Address.Address;
    }

    private static PhysicalAddress GetMacAddress(NetworkInterface networkInterface)
    {
        return networkInterface.GetPhysicalAddress();
    }
}
