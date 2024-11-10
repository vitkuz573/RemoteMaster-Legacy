// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using MessagePack;
using MessagePack.Resolvers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.ParameterHandlers;
using RemoteMaster.Host.Core.Services;
using RemoteMaster.Shared.Extensions;
using RemoteMaster.Shared.Formatters;

namespace RemoteMaster.Host.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddCoreParameterHandlers(this IServiceCollection services)
    {
        services.AddTransient<IParameterHandler, StringParameterHandler>();
        services.AddTransient<IParameterHandler, BooleanParameterHandler>();
    }

    public static void AddMinimalCoreServices(this IServiceCollection services)
    {
        services.AddCoreParameterHandlers();

        services.AddSingleton<IHelpService, HelpService>();
        services.AddSingleton<IArgumentParser, ArgumentParser>();
        services.AddSingleton<ILaunchModeProvider, LaunchModeProvider>();
    }

    public static void AddCoreServices(this IServiceCollection services)
    {
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
        });

        services.AddSharedServices();

        services.AddTransient<HostInfoEnricher>();
        services.AddTransient<CustomHttpClientHandler>();
        services.AddSingleton<IHelpService, HelpService>();
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

        services.AddSignalR().AddMessagePackProtocol(options =>
        {
            var resolver = CompositeResolver.Create([new IPAddressFormatter(), new PhysicalAddressFormatter()], [ContractlessStandardResolver.Instance]);

            options.SerializerOptions = MessagePackSerializerOptions.Standard.WithResolver(resolver);
        });
    }
}
