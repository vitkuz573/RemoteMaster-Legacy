// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Core.Services;

public class HostInformationService : IHostInformationService
{
    public Computer GetHostInformation()
    {
        var hostName = Dns.GetHostName();
        var ipv4Address = GetIPv4Address(hostName);
        var macAddress = GetMacAddress(ipv4Address);

        return new Computer
        {
            Name = hostName,
            IpAddress = ipv4Address,
            MacAddress = macAddress
        };
    }

    private static string GetIPv4Address(string hostName)
    {
        var ipv4Address = Dns.GetHostAddresses(hostName).FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);

        return ipv4Address?.ToString() ?? throw new InvalidOperationException("IPv4 address not found");
    }

    private static string GetMacAddress(string ipv4Address)
    {
        var targetInterface = NetworkInterface.GetAllNetworkInterfaces()
                                  .FirstOrDefault(nic => nic.GetIPProperties()
                                      .UnicastAddresses
                                      .Any(address => address.Address.ToString() == ipv4Address)) 
                              ?? throw new InvalidOperationException("MAC address not found");

        return targetInterface.GetPhysicalAddress().ToString();
    }
}
