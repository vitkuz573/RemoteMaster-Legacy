// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.Services;

namespace RemoteMaster.Shared.Extensions;

public static class ServiceCollectionExtensions
{
    private static void AddCommonSharedServices(this IServiceCollection services)
    {
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
        });

        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton<IHostInformationService, HostInformationService>();
    }

    public static void AddMinimalSharedServices(this IServiceCollection services)
    {
        services.AddCommonSharedServices();
    }

    public static void AddSharedServices(this IServiceCollection services)
    {
        services.AddCommonSharedServices();

        services.AddSingleton<ISubjectService, SubjectService>();
        services.AddSingleton<ICertificateStoreService, CertificateStoreService>();
    }
}
