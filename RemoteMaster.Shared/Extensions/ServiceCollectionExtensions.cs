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
    public static void AddSharedServices(this IServiceCollection services)
    {
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
        });

        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton<IHostInformationService, HostInformationService>();
        services.AddSingleton<ISubjectService, SubjectService>();
        services.AddSingleton<ICertificateStoreService, CertificateStoreService>();
    }
}
