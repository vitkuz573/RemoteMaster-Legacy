// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.Logging;
using RemoteMaster.Shared.Abstractions;
using Serilog.Core;
using Serilog.Events;

namespace RemoteMaster.Host.Core.LogEnrichers;

public class HostInfoEnricher : ILogEventEnricher
{
    private readonly Lazy<LogEventProperty?> _hostIpAddress;
    private readonly Lazy<LogEventProperty?> _hostName;
    private readonly Lazy<LogEventProperty?> _hostMacAddress;

    public HostInfoEnricher(IHostInformationService hostInformationService, ILogger<HostInfoEnricher> logger)
    {
        ArgumentNullException.ThrowIfNull(hostInformationService);
        ArgumentNullException.ThrowIfNull(logger);

        try
        {
            var hostInfo = hostInformationService.GetHostInformation();

            _hostIpAddress = new Lazy<LogEventProperty?>(() =>
                new LogEventProperty("HostIpAddress", new ScalarValue(hostInfo.IpAddress)),
                LazyThreadSafetyMode.ExecutionAndPublication);

            _hostName = new Lazy<LogEventProperty?>(() =>
                new LogEventProperty("HostName", new ScalarValue(hostInfo.Name)),
                LazyThreadSafetyMode.ExecutionAndPublication);

            _hostMacAddress = new Lazy<LogEventProperty?>(() =>
                new LogEventProperty("HostMacAddress", new ScalarValue(hostInfo.MacAddress)),
                LazyThreadSafetyMode.ExecutionAndPublication);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not retrieve host information.");

            _hostIpAddress = new Lazy<LogEventProperty?>(() => null);
            _hostName = new Lazy<LogEventProperty?>(() => null);
            _hostMacAddress = new Lazy<LogEventProperty?>(() => null);
        }
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        ArgumentNullException.ThrowIfNull(propertyFactory);

        if (_hostIpAddress.Value != null)
        {
            logEvent.AddPropertyIfAbsent(_hostIpAddress.Value);
        }

        if (_hostName.Value != null)
        {
            logEvent.AddPropertyIfAbsent(_hostName.Value);
        }

        if (_hostMacAddress.Value != null)
        {
            logEvent.AddPropertyIfAbsent(_hostMacAddress.Value);
        }
    }
}
