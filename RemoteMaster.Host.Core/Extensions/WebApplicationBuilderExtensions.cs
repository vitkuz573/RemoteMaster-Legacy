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
            if (launchModeInstance is UserMode)
            {
                options.ListenAnyIP(5001, listenOptions =>
                {
                    listenOptions.UseHttps(adapterOptions =>
                    {
                        adapterOptions.ServerCertificateSelector = (context, name) => certLoaderService.GetCurrentCertificate();
                    });
                });
            }
            else if (launchModeInstance is UpdaterMode)
            {
                options.ListenAnyIP(6001, listenOptions =>
                {
                    listenOptions.UseHttps(adapterOptions =>
                    {
                        adapterOptions.ServerCertificateSelector = (context, name) => certLoaderService.GetCurrentCertificate();
                    });
                });
            }
        });
    }

    public static void ConfigureSerilog(this WebApplicationBuilder builder, LaunchModeBase launchModeInstance)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var fileLog = Path.Combine(programDataPath, "RemoteMaster", "Host", "RemoteMaster_Host.log");

        builder.Host.UseSerilog((_, configuration) =>
        {
            configuration.Enrich.With(new HostInfoEnricher());
#if DEBUG
            configuration.MinimumLevel.Debug();
#else
            configuration.MinimumLevel.Information();
            
            if (launchModeInstance is UpdaterMode)
            {
                configuration.MinimumLevel.Override("Microsoft", LogEventLevel.Warning);
                configuration.MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning);
            }
#endif
            configuration.WriteTo.Console();
            // configuration.WriteTo.Seq("http://192.168.0.103:5341");
            configuration.WriteTo.File(fileLog, rollingInterval: RollingInterval.Day);
            configuration.Filter.ByExcluding(logEvent => logEvent.MessageTemplate.Text.Contains("Received hub invocation"));
            configuration.Filter.ByExcluding(logEvent => logEvent.MessageTemplate.Text.Contains("Successfully switched to input desktop"));
        });
    }
}