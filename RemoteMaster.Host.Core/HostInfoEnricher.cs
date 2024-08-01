// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Shared.Abstractions;
using Serilog.Core;
using Serilog.Events;

namespace RemoteMaster.Host.Core;

public class HostInfoEnricher : ILogEventEnricher
{
    private readonly Lazy<LogEventProperty> _hostIpAddress;
    private readonly Lazy<LogEventProperty> _hostName;
    private readonly Lazy<LogEventProperty> _macAddress;

    public HostInfoEnricher(IHostInformationService hostInformationService)
    {
        ArgumentNullException.ThrowIfNull(hostInformationService);

        var hostInfo = hostInformationService.GetHostInformation();

        _hostIpAddress = new Lazy<LogEventProperty>(() => new LogEventProperty("HostIpAddress", new ScalarValue(hostInfo.IpAddress)), LazyThreadSafetyMode.ExecutionAndPublication);
        _hostName = new Lazy<LogEventProperty>(() => new LogEventProperty("HostName", new ScalarValue(hostInfo.Name)), LazyThreadSafetyMode.ExecutionAndPublication);
        _macAddress = new Lazy<LogEventProperty>(() => new LogEventProperty("MacAddress", new ScalarValue(hostInfo.MacAddress)), LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        ArgumentNullException.ThrowIfNull(propertyFactory);

        logEvent.AddPropertyIfAbsent(_hostIpAddress.Value);
        logEvent.AddPropertyIfAbsent(_hostName.Value);
        logEvent.AddPropertyIfAbsent(_macAddress.Value);
    }
}