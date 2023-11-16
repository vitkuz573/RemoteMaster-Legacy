// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Serilog;

namespace RemoteMaster.Host.Core.Extensions;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder ConfigureCoreUrls(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.WebHost.ConfigureKestrel(options =>
        {
            var cert = @"C:\certificate.pfx";

            if (File.Exists(cert))
            {
                var httpsCertificate = new X509Certificate2(cert, "YourPfxPassword");

                options.ListenAnyIP(5076, listenOptions =>
                {
                    listenOptions.UseHttps(httpsCertificate);
                });
            }
            else
            {
                options.ListenAnyIP(5076);
            }
        });

        return builder;
    }

    public static void ConfigureSerilog(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Host.UseSerilog((context, configuration) =>
        {
            configuration.Enrich.With(new HostInfoEnricher());
            configuration.MinimumLevel.Debug();
            configuration.WriteTo.Console();
            configuration.WriteTo.Seq("http://172.20.20.33:5341");
            configuration.WriteTo.File(@"C:\ProgramData\RemoteMaster\Host\RemoteMaster_Host.log", rollingInterval: RollingInterval.Day);
            configuration.Filter.ByExcluding(logEvent => logEvent.MessageTemplate.Text.Contains("Received hub invocation"));
            configuration.Filter.ByExcluding(logEvent => logEvent.MessageTemplate.Text.Contains("Successfully switched to input desktop"));
        });
    }
}