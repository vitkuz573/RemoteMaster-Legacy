// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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

        builder.WebHost.ConfigureKestrel(options =>
        {
            using (var store = new X509Store(StoreName.My, StoreLocation.LocalMachine))
            {
                store.Open(OpenFlags.ReadOnly);

                var certificates = store.Certificates.Find(X509FindType.FindBySubjectName, Dns.GetHostName(), false);

                X509Certificate2? certificate = null;

                foreach (var cert in certificates)
                {
                    if (cert.HasPrivateKey)
                    {
                        certificate = cert;
                        break;
                    }
                }

                if (certificate != null)
                {
                    if (launchModeInstance is UserMode)
                    {
                        options.ListenAnyIP(5001, listenOptions =>
                        {
                            listenOptions.UseHttps(certificate);
                        });
                    }
                    else if (launchModeInstance is UpdaterMode)
                    {
                        options.ListenAnyIP(6001, listenOptions =>
                        {
                            listenOptions.UseHttps(certificate);
                        });
                    }
                }
            }

            if (launchModeInstance is UserMode)
            {
                options.ListenAnyIP(5000);
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
            configuration.WriteTo.Seq("http://172.20.20.33:5341");
            configuration.WriteTo.File(fileLog, rollingInterval: RollingInterval.Day);
            configuration.Filter.ByExcluding(logEvent => logEvent.MessageTemplate.Text.Contains("Received hub invocation"));
            configuration.Filter.ByExcluding(logEvent => logEvent.MessageTemplate.Text.Contains("Successfully switched to input desktop"));
        });
    }
}