// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.Options;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Enums;
using RemoteMaster.Server.Options;

namespace RemoteMaster.Server.Services;

public class CertificateAuthorityFactory(IServiceProvider serviceProvider, IOptions<CertificateAuthorityOptions> options) : ICertificateAuthorityFactory
{
    private readonly CertificateAuthorityOptions _options = options.Value;

    public ICertificateAuthorityService Create()
    {
        return _options.Type switch
        {
            CaType.Internal => serviceProvider.GetRequiredService<InternalCertificateAuthorityService>(),
            CaType.ActiveDirectory => serviceProvider.GetRequiredService<ActiveDirectoryCertificateAuthorityService>(),
            _ => throw new NotSupportedException($"Unsupported CA type: {_options.Type}")
        };
    }
}
