// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RemoteMaster.Client.Core.Abstractions;
using RemoteMaster.Client.Core.Services;
using Serilog;

namespace RemoteMaster.Client.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        var serilogLogger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File(@"C:\ProgramData\RemoteMaster\Client\RemoteMaster_Client.log", rollingInterval: RollingInterval.Day)
            .Filter.ByExcluding(logEvent => logEvent.MessageTemplate.Text.Contains("Received hub invocation"))
            .CreateLogger();

        services.AddLogging(builder =>
        {
            builder.AddConsole().AddDebug();
            builder.AddSerilog(serilogLogger);
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        services.AddSignalR().AddMessagePackProtocol();

        services.AddSingleton<IAppState, AppState>();
        services.AddSingleton<IShutdownService, ShutdownService>();
        services.AddTransient<IViewerFactory, ViewerFactory>();

        return services;
    }
}
