// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Models;
using Serilog;
using Serilog.Events;

namespace RemoteMaster.Host.Core.Extensions;

public static class WebApplicationBuilderExtensions
{
    public static void ConfigureCoreUrls(this WebApplicationBuilder builder, LaunchModeBase launchModeInstance)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var serviceProvider = builder.Services.BuildServiceProvider();
        var certLoaderService = serviceProvider.GetRequiredService<ICertificateLoaderService>();

        builder.WebHost.ConfigureKestrel(options =>
        {
            switch (launchModeInstance)
            {
                case UserMode:
                    options.ListenAnyIP(5001, listenOptions =>
                    {
                        listenOptions.UseHttps(adapterOptions =>
                        {
                            adapterOptions.ServerCertificateSelector = (_, _) => certLoaderService.GetCurrentCertificate();
                        });
                    });
                    break;
                case UpdaterMode:
                    options.ListenAnyIP(6001, listenOptions =>
                    {
                        listenOptions.UseHttps(adapterOptions =>
                        {
                            adapterOptions.ServerCertificateSelector = (_, _) => certLoaderService.GetCurrentCertificate();
                        });
                    });
                    break;
                case ChatMode:
                    options.ListenAnyIP(7001, listenOptions =>
                    {
                        listenOptions.UseHttps(adapterOptions =>
                        {
                            adapterOptions.ServerCertificateSelector = (_, _) => certLoaderService.GetCurrentCertificate();
                        });
                    });
                    break;
                case ServiceMode:
                    options.ListenLocalhost(35456);
                    break;
            }
        });
    }

    public static async Task ConfigureSerilog(this WebApplicationBuilder builder, LaunchModeBase launchModeInstance)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var serviceProvider = builder.Services.BuildServiceProvider();

        var programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var fileLog = Path.Combine(programDataPath, "RemoteMaster", "Host", "RemoteMaster_Host-.log");
        var errorLog = Path.Combine(programDataPath, "RemoteMaster", "Host", "RemoteMaster_Host_Error-.log");

        string server;

        if (launchModeInstance is InstallMode)
        {
            server = launchModeInstance.GetParameterValue("server") ?? throw new InvalidOperationException("Server is required.");
        }
        else
        {
            var hostConfigurationService = serviceProvider.GetRequiredService<IHostConfigurationService>();
            var hostConfiguration = await hostConfigurationService.LoadConfigurationAsync();
            server = hostConfiguration.Server;

            if (string.IsNullOrEmpty(server))
            {
                throw new InvalidOperationException("Server address must be provided in host configuration.");
            }
        }

        builder.Host.UseSerilog((_, configuration) =>
        {
            configuration.Enrich.With(serviceProvider.GetRequiredService<HostInfoEnricher>());

#if DEBUG
        configuration.MinimumLevel.Debug();
#else
            configuration.MinimumLevel.Information();

            configuration.MinimumLevel.Override("Microsoft", LogEventLevel.Warning);
            configuration.MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning);
            configuration.MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning);
            configuration.MinimumLevel.Override("Microsoft.AspNetCore.SignalR", LogEventLevel.Warning);
            configuration.MinimumLevel.Override("Microsoft.AspNetCore.Http.Connections", LogEventLevel.Warning);
#endif

            configuration.WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");

            configuration.WriteTo.Seq($"http://{server}:5341");

            configuration.WriteTo.File(fileLog, rollingInterval: RollingInterval.Day,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");

            configuration.WriteTo.File(errorLog, restrictedToMinimumLevel: LogEventLevel.Error,
                rollingInterval: RollingInterval.Day,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");

            configuration.Filter.ByExcluding(logEvent => logEvent.MessageTemplate.Text.Contains("Successfully switched to input desktop"));
        });
    }
}
