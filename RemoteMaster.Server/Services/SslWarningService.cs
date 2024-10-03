// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using System.Net;
using RemoteMaster.Server.Abstractions;

namespace RemoteMaster.Server.Services;

public class SslWarningService : ISslWarningService
{
    private readonly ConcurrentDictionary<IPAddress, bool> _sslAllowances = new();

    public bool IsSslAllowed(IPAddress ipAddress)
    {
        return _sslAllowances.TryGetValue(ipAddress, out var isAllowed) && isAllowed;
    }

    public void SetSslAllowance(IPAddress ipAddress, bool isAllowed)
    {
        _sslAllowances[ipAddress] = isAllowed;
    }
}
