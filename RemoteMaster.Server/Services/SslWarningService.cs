// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using System.Net;
using RemoteMaster.Server.Abstractions;
using Serilog;

namespace RemoteMaster.Server.Services;

public class SslWarningService : ISslWarningService
{
    private readonly ConcurrentDictionary<IPAddress, bool> _sslAllowances = new();

    public bool IsSslAllowed(IPAddress ipAddress)
    {
        var isAllowed = _sslAllowances.TryGetValue(ipAddress, out var result);
        Log.Information("Checked SSL allowance for IP {IPAddress}. Allowed: {IsAllowed}", ipAddress, isAllowed && result);

        return isAllowed && result;
    }

    public void SetSslAllowance(IPAddress ipAddress, bool isAllowed)
    {
        _sslAllowances[ipAddress] = isAllowed;

        if (isAllowed)
        {
            Log.Information("SSL connection allowed for IP {IPAddress}.", ipAddress);
        }
        else
        {
            Log.Warning("SSL connection denied for IP {IPAddress}.", ipAddress);
        }
    }
}
