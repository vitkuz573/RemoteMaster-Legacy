// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Services;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Path.GetDirectoryName(Environment.ProcessPath)!)
            .AddJsonFile("RemoteMaster.Host.json");

        var configuration = builder.Build();

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
        });

        services.AddSingleton<IFileManagerService, FileManagerService>();
        services.AddSingleton<ISubjectService, SubjectService>();
        services.AddSingleton<ICertificateRequestService, CertificateRequestService>();
        services.AddSingleton<IHostLifecycleService, HostLifecycleService>();
        services.AddSingleton<IHostConfigurationService, HostConfigurationService>();
        services.AddSingleton<IHostInfoService, HostInfoService>();
        services.AddSingleton<IAppState, AppState>();
        services.AddSingleton<IShutdownService, ShutdownService>();
        services.AddSingleton<IServerHubService, ServerHubService>();
        services.AddTransient<IViewerFactory, ViewerFactory>();
        services.Configure<SubjectOptions>(configuration.GetSection("subject"));

        services.AddSignalR().AddMessagePackProtocol();

        return services;
    }
}
