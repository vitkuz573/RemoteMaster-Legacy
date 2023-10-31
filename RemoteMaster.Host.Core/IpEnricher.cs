// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using System.Net.Sockets;
using Serilog.Core;
using Serilog.Events;

namespace RemoteMaster.Host.Core;

public class IpEnricher : ILogEventEnricher
{
    private LogEventProperty _cachedProperty;

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (logEvent == null)
        {
            throw new ArgumentNullException(nameof(logEvent));
        }

        if (propertyFactory == null)
        {
            throw new ArgumentNullException(nameof(propertyFactory));
        }

        _cachedProperty ??= CreateProperty(propertyFactory);
        logEvent.AddPropertyIfAbsent(_cachedProperty);
    }

    private static LogEventProperty CreateProperty(ILogEventPropertyFactory propertyFactory)
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        var ipAddress = host.AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)?.ToString();
        
        return propertyFactory.CreateProperty("HostIpAddress", ipAddress);
    }
}
