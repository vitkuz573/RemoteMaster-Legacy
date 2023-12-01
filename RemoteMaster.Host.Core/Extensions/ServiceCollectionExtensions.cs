// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Services;

namespace RemoteMaster.Host.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
        });

        services.AddSingleton<ISubjectNameService, SubjectNameService>();
        services.AddSingleton<ICertificateRequestService, CertificateRequestService>();
        services.AddSingleton<IHostLifecycleService, HostLifecycleService>();
        services.AddSingleton<IHostConfigurationService, HostConfigurationService>();
        services.AddSingleton<IHostInfoService, HostInfoService>();
        services.AddSingleton<IAppState, AppState>();
        services.AddSingleton<IShutdownService, ShutdownService>();
        services.AddTransient<IViewerFactory, ViewerFactory>();

        services.AddSignalR().AddMessagePackProtocol();

        return services;
    }
}
