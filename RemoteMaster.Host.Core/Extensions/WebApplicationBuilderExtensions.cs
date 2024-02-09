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
    public static void ConfigureCoreUrls(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.WebHost.ConfigureKestrel(options =>
        {
            var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var certificatePath = Path.Combine(programData, "RemoteMaster", "Security", "certificate.pfx");

            if (File.Exists(certificatePath))
            {
                var httpsCertificate = new X509Certificate2(certificatePath, "YourPfxPassword");

                options.ListenAnyIP(5001, listenOptions =>
                {
                    listenOptions.UseHttps(httpsCertificate);
                });
            }

            options.ListenAnyIP(5000);
        });
    }

    public static void ConfigureSerilog(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var fileLog = Path.Combine(programData, "RemoteMaster", "Host", "RemoteMaster_Host.log");

        builder.Host.UseSerilog((_, configuration) =>
        {
            configuration.Enrich.With(new HostInfoEnricher());
            configuration.MinimumLevel.Debug();
            configuration.WriteTo.Console();
            configuration.WriteTo.Seq("http://172.20.20.33:5341");
            configuration.WriteTo.File(fileLog, rollingInterval: RollingInterval.Day);
            configuration.Filter.ByExcluding(logEvent => logEvent.MessageTemplate.Text.Contains("Received hub invocation"));
            configuration.Filter.ByExcluding(logEvent => logEvent.MessageTemplate.Text.Contains("Successfully switched to input desktop"));
        });
    }
}