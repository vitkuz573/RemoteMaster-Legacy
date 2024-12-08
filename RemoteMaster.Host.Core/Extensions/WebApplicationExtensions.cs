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

public static class WebApplicationExtensions
{
    public static void ConfigureSerilog(this WebApplication app, string? server = null)
    {
        ArgumentNullException.ThrowIfNull(app);

        var programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var fileLog = Path.Combine(programDataPath, "RemoteMaster", "Host", "RemoteMaster_Host-.log");
        var errorLog = Path.Combine(programDataPath, "RemoteMaster", "Host", "RemoteMaster_Host_Error-.log");

        var loggerConfiguration = new LoggerConfiguration()
            .Enrich.FromLogContext();

        if (server == null)
        {
            using var scope = app.Services.CreateScope();

            var hostConfigurationService = scope.ServiceProvider.GetRequiredService<IHostConfigurationService>();
            var hostInfoEnricher = scope.ServiceProvider.GetRequiredService<HostInfoEnricher>();

            var hostConfiguration = hostConfigurationService.LoadConfigurationAsync().GetAwaiter().GetResult();
            server = hostConfiguration.Server;

            if (string.IsNullOrEmpty(server))
            {
                throw new InvalidOperationException("Server address must be provided in host configuration.");
            }

            loggerConfiguration.Enrich.With(hostInfoEnricher);
        }

#if DEBUG
        loggerConfiguration.MinimumLevel.Debug();
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

        if (!string.IsNullOrEmpty(server))
        {
            loggerConfiguration.WriteTo.Seq($"http://{server}:5341");
        }

        loggerConfiguration.WriteTo.File(fileLog, rollingInterval: RollingInterval.Day,
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");

        loggerConfiguration.WriteTo.File(errorLog, restrictedToMinimumLevel: LogEventLevel.Error,
            rollingInterval: RollingInterval.Day,
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");

        Log.Logger = loggerConfiguration.CreateLogger();

        app.Lifetime.ApplicationStopped.Register(Log.CloseAndFlush);
    }
}
