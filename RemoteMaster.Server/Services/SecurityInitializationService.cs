// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Abstractions;

namespace RemoteMaster.Server.Services;

public class SecurityInitializationService(ICaCertificateService caCertificateService, IJwtSecurityService jwtSecurityService) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        caCertificateService.EnsureCaCertificateExists();
        await jwtSecurityService.EnsureKeysExistAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
