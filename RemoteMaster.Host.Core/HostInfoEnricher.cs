// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Shared.Abstractions;
using Serilog.Core;
using Serilog.Events;

namespace RemoteMaster.Host.Core;

public class HostInfoEnricher(IHostInformationService hostInformationService) : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        ArgumentNullException.ThrowIfNull(propertyFactory);

        var hostInfo = hostInformationService.GetHostInformation();

        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("HostIpAddress", hostInfo.IpAddress));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("HostName", hostInfo.Name));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("MacAddress", hostInfo.MacAddress));
    }
}
