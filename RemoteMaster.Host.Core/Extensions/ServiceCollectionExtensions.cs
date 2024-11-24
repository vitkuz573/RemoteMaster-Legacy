// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.AuthorizationHandlers;
using RemoteMaster.Host.Core.HttpClientHandlers;
using RemoteMaster.Host.Core.LaunchModes;
using RemoteMaster.Host.Core.LogEnrichers;
using RemoteMaster.Host.Core.NamedOptionsConfigurations;
using RemoteMaster.Host.Core.ParameterHandlers;
using RemoteMaster.Host.Core.Services;
using RemoteMaster.Shared.Extensions;

namespace RemoteMaster.Host.Core.Extensions;

public static class ServiceCollectionExtensions
{
    private static void AddCoreParameterHandlers(this IServiceCollection services)
    {
        services.AddTransient<IParameterHandler, StringParameterHandler>();
        services.AddTransient<IParameterHandler, BooleanParameterHandler>();
    }

    private static void AddCommonCoreServices(this IServiceCollection services)
    {
        services.AddTransient<HostInfoEnricher>();
        services.AddSingleton<IHelpService, HelpService>();
        services.AddSingleton<IHostConfigurationService, HostConfigurationService>();
        services.AddSingleton<ICertificateLoaderService, CertificateLoaderService>();
    }

    public static void AddMinimalCoreServices(this IServiceCollection services)
    {
        services.AddCommonCoreServices();
        services.AddCoreParameterHandlers();
        services.AddMinimalSharedServices();

        services.AddSingleton<IArgumentParser, ArgumentParser>();
        services.AddSingleton<ILaunchModeProvider, LaunchModeProvider>();
    }

    public static async Task AddCoreServices(this IServiceCollection services, LaunchModeBase launchModeInstance)
    {
        services.AddCommonCoreServices();
        services.AddSharedServices();

        services.AddTransient<CustomHttpClientHandler>();
        services.AddSingleton<IAuthorizationHandler, LocalhostOrAuthenticatedHandler>();
        services.AddSingleton<IRsaKeyProvider, RsaKeyProvider>();
        services.AddSingleton<IHostInformationUpdaterService, HostInformationUpdaterService>();
        services.AddSingleton<IFileManagerService, FileManagerService>();
        services.AddSingleton<ICertificateRequestService, CertificateRequestService>();
        services.AddSingleton<IHostLifecycleService, HostLifecycleService>();
        services.AddSingleton<IAppState, AppState>();
        services.AddSingleton<IShutdownService, ShutdownService>();
        services.AddSingleton<IScreenRecorderService, ScreenRecorderService>();
        services.AddTransient<IViewerFactory, ViewerFactory>();
        services.AddSingleton<IScreenCastingService, ScreenCastingService>();
        services.AddSingleton<IApiService, ApiService>();
        services.AddSingleton<IFileService, FileService>();
        services.AddSingleton<IScreenRecorderService, ScreenRecorderService>();

        services.AddHttpClient<ApiService>().AddHttpMessageHandler<CustomHttpClientHandler>();

        if (launchModeInstance is not InstallMode)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer();

            services.AddSingleton<IConfigureOptions<JwtBearerOptions>, ConfigureJwtBearerOptions>();

            services.AddAuthorizationBuilder().AddCustomPolicies();
        }

        services.AddSignalR().AddMessagePackProtocol(options => options.Configure());

        switch (launchModeInstance)
        {
            case UserMode:
                services.AddHostedService<InputBackgroundService>();
                break;
            case ServiceMode:
                services.AddHostedService<CertificateManagementService>();
                services.AddHostedService<HostProcessMonitorService>();
                services.AddHostedService<HostRegistrationMonitorService>();
                break;
        }
    }
}
