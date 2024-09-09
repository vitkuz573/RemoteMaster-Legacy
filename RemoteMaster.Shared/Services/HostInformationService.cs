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
    public ComputerDto GetHostInformation()
    {
        var hostName = Dns.GetHostName();
        var preferredInterface = GetPreferredNetworkInterface();
        var ipv4Address = GetIPv4Address(preferredInterface);
        var macAddress = GetMacAddress(preferredInterface);

        return new ComputerDto(hostName, ipv4Address, macAddress);
    }

    private static NetworkInterface GetPreferredNetworkInterface()
    {
        var interfaces = NetworkInterface.GetAllNetworkInterfaces()
            .Where(nic => nic.OperationalStatus == OperationalStatus.Up)
            .Where(nic => nic.NetworkInterfaceType is NetworkInterfaceType.Wireless80211 or NetworkInterfaceType.Ethernet)
            .Where(nic => !IsVpnAdapter(nic))
            .Where(nic => nic.GetIPProperties().UnicastAddresses.Any(ua => ua.Address.AddressFamily == AddressFamily.InterNetwork && !ua.Address.ToString().StartsWith("169.254")))
            .Select(nic => new
            {
                Interface = nic,
                Priority = nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet ? 1 : 2
            }).MinBy(nic => nic.Priority)?.Interface;

        return interfaces ?? throw new InvalidOperationException("Active network interface not found. Network might be disabled or not properly configured");
    }

    private static bool IsVpnAdapter(NetworkInterface nic)
    {
        var descriptionLower = nic.Description.ToLower();

        return descriptionLower.Contains("vpn") ||
               descriptionLower.Contains("virtual") ||
               descriptionLower.Contains("pseudo") ||
               descriptionLower.Contains("tap-windows") ||
               descriptionLower.Contains("tap");
    }

    private static string GetIPv4Address(NetworkInterface networkInterface)
    {
        var ipv4Address = networkInterface.GetIPProperties().UnicastAddresses
            .FirstOrDefault(ua => ua.Address.AddressFamily == AddressFamily.InterNetwork);

        return ipv4Address == null
            ? throw new InvalidOperationException("Valid IPv4 address not found for the selected interface.")
            : ipv4Address.Address.ToString();
    }

    private static string GetMacAddress(NetworkInterface networkInterface)
    {
        return networkInterface.GetPhysicalAddress().ToString();
    }
}
