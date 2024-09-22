// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.Hosting;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Enums;

namespace RemoteMaster.Host.Windows.Services;

public class SasInitializationService(ISecureAttentionSequenceService secureAttentionSequenceService) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (secureAttentionSequenceService.SasOption != SoftwareSasOption.ServicesAndEaseOfAccessApplications)
        {
            secureAttentionSequenceService.SasOption = SoftwareSasOption.ServicesAndEaseOfAccessApplications;
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
