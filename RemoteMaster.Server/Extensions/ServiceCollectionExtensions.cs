// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Services;

namespace RemoteMaster.Server.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCertificateAuthorityService(this IServiceCollection services)
    {
        services.AddTransient<ICertificateAuthorityFactory, CertificateAuthorityFactory>();

        services.AddTransient<InternalCertificateAuthorityService>();
        services.AddTransient<ActiveDirectoryCertificateAuthorityService>();

        services.AddTransient(provider =>
        {
            var factory = provider.GetRequiredService<ICertificateAuthorityFactory>();

            return factory.Create();
        });

        return services;
    }
}
