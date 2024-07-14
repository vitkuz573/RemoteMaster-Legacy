// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Models;
using RemoteMaster.Host.Core.Services;
using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.Extensions;
using RemoteMaster.Shared.Models;
using RemoteMaster.Shared.Services;

namespace RemoteMaster.Host.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddCoreServices(this IServiceCollection services, LaunchModeBase launchModeInstance)
    {
        if (launchModeInstance is not UpdaterMode)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(Environment.ProcessPath)!)
                .AddJsonFile($"{Assembly.GetEntryAssembly().GetName().Name}.json");

            var configuration = builder.Build();

            services.Configure<SubjectOptions>(configuration.GetSection("subject"));
        }

        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
        });

        services.AddSharedServices();
        services.AddSingleton<IHostInformationMonitorService, HostInformationMonitorService>();
        services.AddSingleton<IFileManagerService, FileManagerService>();
        services.AddSingleton<ICertificateRequestService, CertificateRequestService>();
        services.AddSingleton<IHostLifecycleService, HostLifecycleService>();
        services.AddSingleton<IHostConfigurationService, HostConfigurationService>();
        services.AddSingleton<IAppState, AppState>();
        services.AddSingleton<IShutdownService, ShutdownService>();
        services.AddSingleton<IScreenRecorderService, ScreenRecorderService>();
        services.AddTransient<IViewerFactory, ViewerFactory>();
        services.AddSingleton<ICertificateLoaderService, CertificateLoaderService>();
        services.AddSingleton<ICertificateStoreService, CertificateStoreService>();
        services.AddSingleton<IScreenCastingService, ScreenCastingService>();
        services.AddSingleton<IApiService, ApiService>();

        services.AddHttpClient();

        services.AddSignalR().AddMessagePackProtocol();
    }
}
