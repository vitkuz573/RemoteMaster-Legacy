// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Data;
using RemoteMaster.Host.Core.Models;
using RemoteMaster.Host.Core.Services;
using RemoteMaster.Shared.Extensions;

namespace RemoteMaster.Host.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddCoreServices(this IServiceCollection services, LaunchModeBase launchMode)
    {
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
        });

        services.AddSharedServices();
        services.AddTransient<HostInfoEnricher>();
        services.AddTransient<CustomHttpClientHandler>();
        services.AddSingleton<IHostInformationUpdaterService, HostInformationUpdaterService>();
        services.AddSingleton<IFileManagerService, FileManagerService>();
        services.AddSingleton<ICertificateRequestService, CertificateRequestService>();
        services.AddSingleton<IHostLifecycleService, HostLifecycleService>();
        services.AddSingleton<IHostConfigurationService, HostConfigurationService>();
        services.AddSingleton<IAppState, AppState>();
        services.AddSingleton<IShutdownService, ShutdownService>();
        services.AddSingleton<IScreenRecorderService, ScreenRecorderService>();
        services.AddTransient<IViewerFactory, ViewerFactory>();
        services.AddSingleton<ICertificateLoaderService, CertificateLoaderService>();
        services.AddSingleton<IScreenCastingService, ScreenCastingService>();
        services.AddSingleton<IApiService, ApiService>();

        services.AddHttpClient<ApiService>().AddHttpMessageHandler<CustomHttpClientHandler>();

        services.AddSignalR().AddMessagePackProtocol();

        if (launchMode is UpdaterMode)
        {
            return;
        }

        services.AddDbContext<HostDbContext>();

        services.AddHostedService<MigrationService>();
    }
}
