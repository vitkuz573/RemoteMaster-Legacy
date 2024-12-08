// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.LogEnrichers;
using Serilog;
using Serilog.Events;

namespace RemoteMaster.Host.Core.Extensions;

public static class WebApplicationBuilderExtensions
{
    public static void ConfigureSerilog(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var fileLog = Path.Combine(programDataPath, "RemoteMaster", "Host", "RemoteMaster_Host-.log");
        var errorLog = Path.Combine(programDataPath, "RemoteMaster", "Host", "RemoteMaster_Host_Error-.log");

        builder.Host.UseSerilog((context, services, loggerConfiguration) =>
        {
            // var hostConfigurationService = services.GetRequiredService<IHostConfigurationService>();
            // var hostInfoEnricher = services.GetRequiredService<HostInfoEnricher>();
            // 
            // var hostConfiguration = hostConfigurationService.LoadConfigurationAsync().GetAwaiter().GetResult();
            // 
            // var server = hostConfiguration.Server;
            // 
            // if (string.IsNullOrEmpty(server))
            // {
            //     throw new InvalidOperationException("Server address must be provided in host configuration.");
            // }
            // 
            // loggerConfiguration.Enrich.With(hostInfoEnricher);

#if DEBUG
            configuration.MinimumLevel.Debug();
#else
            loggerConfiguration.MinimumLevel.Information();

            loggerConfiguration.MinimumLevel.Override("Microsoft", LogEventLevel.Warning);
            loggerConfiguration.MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning);
            loggerConfiguration.MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning);
            loggerConfiguration.MinimumLevel.Override("Microsoft.AspNetCore.SignalR", LogEventLevel.Warning);
            loggerConfiguration.MinimumLevel.Override("Microsoft.AspNetCore.Http.Connections", LogEventLevel.Warning);
#endif

            loggerConfiguration.WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");

            // loggerConfiguration.WriteTo.Seq($"http://{server}:5341");

            loggerConfiguration.WriteTo.File(fileLog, rollingInterval: RollingInterval.Day,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");

            loggerConfiguration.WriteTo.File(errorLog, restrictedToMinimumLevel: LogEventLevel.Error,
                rollingInterval: RollingInterval.Day,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");
        });
    }
}
