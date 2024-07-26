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
            }
        });
    }

    public static void ConfigureSerilog(this WebApplicationBuilder builder, string server)
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

            configuration.MinimumLevel.Override("Microsoft", LogEventLevel.Warning);
            configuration.MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning);
            configuration.MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning);
#endif
            configuration.WriteTo.Console();
            configuration.WriteTo.Seq($"http://{server}:5341");
            configuration.WriteTo.File(fileLog, rollingInterval: RollingInterval.Day);
            configuration.Filter.ByExcluding(logEvent => logEvent.MessageTemplate.Text.Contains("Received hub invocation"));
            configuration.Filter.ByExcluding(logEvent => logEvent.MessageTemplate.Text.Contains("Successfully switched to input desktop"));
        });
    }
}