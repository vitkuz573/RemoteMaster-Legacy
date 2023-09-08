// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using System.Net.Sockets;
using RemoteMaster.Agent.Core.Abstractions;

namespace RemoteMaster.Agent.Core.Services;

public class HostInfoProvider : IHostInfoProvider
{
    public string GetHostName()
    {
        return Dns.GetHostName();
    }

    public string GetIPv4Address()
    {
        var hostName = GetHostName();
        var allAddresses = Dns.GetHostAddresses(hostName);

        return Array.Find(allAddresses, a => a.AddressFamily == AddressFamily.InterNetwork)?.ToString() ?? "Not found";
    }
}
