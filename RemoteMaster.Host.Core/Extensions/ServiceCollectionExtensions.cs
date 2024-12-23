// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.AuthorizationHandlers;
using RemoteMaster.Host.Core.HttpClientHandlers;
using RemoteMaster.Host.Core.LogEnrichers;
using RemoteMaster.Host.Core.OptionsConfigurations;
using RemoteMaster.Host.Core.Services;
using RemoteMaster.Shared.Extensions;
using TimeProvider = RemoteMaster.Host.Core.Services.TimeProvider;

namespace RemoteMaster.Host.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddCoreServices(this IServiceCollection services, string commandName)
    {
        services.AddSharedServices();

        services.AddTransient<HostInfoEnricher>();
        services.AddTransient<CustomHttpClientHandler>();
        services.AddTransient<IViewerFactory, ViewerFactory>();
        services.AddTransient<IServiceFactory, ServiceFactory>();
        services.AddTransient<ITcpClientFactory, TcpClientFactory>();
        services.AddTransient<IProcessWrapperFactory, ProcessWrapperFactory>();
        services.AddSingleton<IAuthorizationHandler, LocalhostOrAuthenticatedHandler>();
        services.AddSingleton<IHostInstaller, HostInstaller>();
        services.AddSingleton<IHostUninstaller, HostUninstaller>();
        services.AddSingleton<IHostUpdater, HostUpdater>();
        services.AddSingleton<IRsaKeyProvider, RsaKeyProvider>();
        services.AddSingleton<IHostInformationUpdaterService, HostInformationUpdaterService>();
        services.AddSingleton<IFileManagerService, FileManagerService>();
        services.AddSingleton<ICertificateRequestService, CertificateRequestService>();
        services.AddSingleton<ICertificateService, CertificateService>();
        services.AddSingleton<IHostLifecycleService, HostLifecycleService>();
        services.AddSingleton<IAppState, AppState>();
        services.AddSingleton<IShutdownService, ShutdownService>();
        services.AddSingleton<IScreenRecorderService, ScreenRecorderService>();
        services.AddSingleton<IScreenCastingService, ScreenCastingService>();
        services.AddSingleton<IApiService, ApiService>();
        services.AddSingleton<IFileService, FileService>();
        services.AddSingleton<ISyncIndicatorService, SyncIndicatorService>();
        services.AddSingleton<IProcessService, ProcessService>();
        services.AddSingleton<IInstanceManagerService, InstanceManagerService>();
        services.AddSingleton<ISessionChangeEventService, SessionChangeEventService>();
        services.AddSingleton<IUpdaterInstanceService, UpdaterInstanceService>();
        services.AddSingleton<IChatInstanceService, ChatInstanceService>();
        services.AddSingleton<IOverlayManagerService, OverlayManagerService>();
        services.AddSingleton<IServerAvailabilityService, ServerAvailabilityService>();
        services.AddSingleton<ITimeProvider, TimeProvider>();
        services.AddSingleton<IHostConfigurationService, HostConfigurationService>();
        services.AddSingleton<ICertificateLoaderService, CertificateLoaderService>();
        services.AddSingleton<ITaskManagerService, TaskManagerService>();
        services.AddSingleton<IAudioStreamingService, AudioStreamingService>();
        services.AddSingleton<IApplicationVersionProvider, ApplicationVersionProvider>();
        services.AddSingleton<IApplicationPathProvider, ApplicationPathProvider>();
        services.AddSingleton<IHostUpdaterNotifier, HostUpdaterNotifier>();
        services.AddSingleton<IRecoveryService, RecoveryService>();
        services.AddSingleton<IChecksumValidator, ChecksumValidator>();

        services.AddHttpClient<ApiService>().AddHttpMessageHandler<CustomHttpClientHandler>();

        services.AddSingleton<IConfigureOptions<KestrelServerOptions>>(provider => new KestrelConfiguration(commandName, provider.GetRequiredService<ICertificateLoaderService>()));

        if (commandName != "install")
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer();

            services.AddSingleton<IConfigureOptions<JwtBearerOptions>, ConfigureJwtBearerOptions>();
        }

        services.AddSignalR().AddMessagePackProtocol(options => options.Configure());

        switch (commandName)
        {
            case "user":
                services.AddHostedService<InputBackgroundService>();
                services.AddHostedService<TrayIconHostedService>();
                break;
            case "service":
                services.AddHostedService<CertificateManagementService>();
                services.AddHostedService<HostProcessMonitorService>();
                services.AddHostedService<HostRegistrationMonitorService>();
                break;
        }
    }
}
