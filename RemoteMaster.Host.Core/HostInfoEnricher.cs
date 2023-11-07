// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using System.Net.Sockets;
using Serilog.Core;
using Serilog.Events;

namespace RemoteMaster.Host.Core;

public class HostInfoEnricher : ILogEventEnricher
{
    private LogEventProperty? _cachedIpProperty;
    private LogEventProperty? _cachedHostProperty;

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

        _cachedIpProperty ??= CreateIpProperty(propertyFactory);
        _cachedHostProperty ??= CreateHostProperty(propertyFactory);

        logEvent.AddPropertyIfAbsent(_cachedIpProperty);
        logEvent.AddPropertyIfAbsent(_cachedHostProperty);
    }

    private static LogEventProperty CreateIpProperty(ILogEventPropertyFactory propertyFactory)
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        var ipAddress = host.AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)?.ToString();

        return propertyFactory.CreateProperty("HostIpAddress", ipAddress);
    }

    private static LogEventProperty CreateHostProperty(ILogEventPropertyFactory propertyFactory)
    {
        var hostName = Dns.GetHostName();

        return propertyFactory.CreateProperty("HostName", hostName);
    }
}
