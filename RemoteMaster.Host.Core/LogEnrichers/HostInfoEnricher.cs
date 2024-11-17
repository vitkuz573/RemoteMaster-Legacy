// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Shared.Abstractions;
using Serilog.Core;
using Serilog.Events;

namespace RemoteMaster.Host.Core.LogEnrichers;

public class HostInfoEnricher : ILogEventEnricher
{
    private readonly Lazy<LogEventProperty> _hostIpAddress;
    private readonly Lazy<LogEventProperty> _hostName;
    private readonly Lazy<LogEventProperty> _hostMacAddress;

    public HostInfoEnricher(IHostInformationService hostInformationService)
    {
        ArgumentNullException.ThrowIfNull(hostInformationService);

        var hostInfo = hostInformationService.GetHostInformation();

        _hostIpAddress = new Lazy<LogEventProperty>(() => new LogEventProperty("HostIpAddress", new ScalarValue(hostInfo.IpAddress)), LazyThreadSafetyMode.ExecutionAndPublication);
        _hostName = new Lazy<LogEventProperty>(() => new LogEventProperty("HostName", new ScalarValue(hostInfo.Name)), LazyThreadSafetyMode.ExecutionAndPublication);
        _hostMacAddress = new Lazy<LogEventProperty>(() => new LogEventProperty("HostMacAddress", new ScalarValue(hostInfo.MacAddress)), LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        ArgumentNullException.ThrowIfNull(propertyFactory);

        logEvent.AddPropertyIfAbsent(_hostIpAddress.Value);
        logEvent.AddPropertyIfAbsent(_hostName.Value);
        logEvent.AddPropertyIfAbsent(_hostMacAddress.Value);
    }
}
