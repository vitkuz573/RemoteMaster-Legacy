// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.Options;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Enums;
using RemoteMaster.Server.Options;
using RemoteMaster.Server.Services;

namespace RemoteMaster.Server.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCertificateAuthorityService(this IServiceCollection services)
    {
        services.AddTransient<ICertificateAuthorityService>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<CertificateOptions>>().Value;

            return options.CaType switch
            {
                CaType.Internal => provider.GetRequiredService<InternalCertificateAuthorityService>(),
                CaType.ActiveDirectory => provider.GetRequiredService<ActiveDirectoryCertificateAuthorityService>(),
                _ => throw new NotSupportedException("Unknown CA type.")
            };
        });

        services.AddTransient<InternalCertificateAuthorityService>();
        services.AddTransient<ActiveDirectoryCertificateAuthorityService>();

        return services;
    }
}
