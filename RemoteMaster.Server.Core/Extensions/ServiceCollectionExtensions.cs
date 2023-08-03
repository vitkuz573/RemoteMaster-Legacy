// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RemoteMaster.Server.Core.Abstractions;
using RemoteMaster.Server.Core.Services;
using RemoteMaster.Shared;

namespace RemoteMaster.Server.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.AddConsole().AddDebug();
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddProvider(new FileLoggerProvider("RemoteMaster_Server"));
        });

        services.AddSignalR().AddMessagePackProtocol();

        services.AddSingleton<IAppState, AppState>();
        services.AddSingleton<IShutdownService, ShutdownService>();
        services.AddSingleton<IIdleTimer, IdleTimer>();
        services.AddTransient<IViewerFactory, ViewerFactory>();

        return services;
    }
}
