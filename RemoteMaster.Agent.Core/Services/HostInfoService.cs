// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using RemoteMaster.Agent.Core.Abstractions;

namespace RemoteMaster.Agent.Core.Services;

public partial class HostInfoService : IHostInfoService
{
    public string GetHostName() => Dns.GetHostName();

    public string GetIPv4Address()
    {
        var hostName = GetHostName();
        var allAddresses = Dns.GetHostAddresses(hostName);

        return Array.Find(allAddresses, a => a.AddressFamily == AddressFamily.InterNetwork)?.ToString() ?? "Not found";
    }

    public string GetMacAddress()
    {
        var ipv4Address = GetIPv4Address();

        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            var ipProperties = nic.GetIPProperties();

            foreach (var ip in ipProperties.UnicastAddresses)
            {
                if (ip.Address.ToString() == ipv4Address)
                {
                    return MacAddressRegex().Replace(nic.GetPhysicalAddress().ToString(), "$1:");
                }
            }
        }

        return string.Empty;
    }

    [GeneratedRegex("(..)(?!$)")]
    private static partial Regex MacAddressRegex();
}