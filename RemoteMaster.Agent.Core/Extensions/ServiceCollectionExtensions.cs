// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RemoteMaster.Agent.Core.Models;
using Serilog;

namespace RemoteMaster.Agent.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services, IConfiguration configuration)
    {
        var serilogLogger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File(@"C:\Logs\RemoteMaster_Agent.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        services.AddLogging(builder =>
        {
            builder.AddConsole().AddDebug();
            builder.AddSerilog(serilogLogger);
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        services.AddSignalR();

        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        services.Configure<ClientSettings>(configuration.GetSection("Client"));

        var clientSection = configuration.GetSection("Client");
        if (!clientSection.Exists())
        {
            serilogLogger.Warning("Client section does not exist in the configuration.");
        }
        else
        {
            serilogLogger.Information("Client section exists with Path: {Path}, CertificateThumbprint: {Thumbprint}", clientSection["Path"], clientSection["CertificateThumbprint"]);
        }

        return services;
    }
}
