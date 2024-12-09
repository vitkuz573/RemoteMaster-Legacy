// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Windows.Abstractions;

namespace RemoteMaster.Host.Windows.Hubs;

public class ServiceHub(IPsExecService psExecService) : Hub<IServiceClient>
{
    [Authorize(Policy = "SetPsExecRulesPolicy")]
    public async Task SetPsExecRules(bool enable)
    {
        psExecService.Disable();

        if (enable)
        {
            await psExecService.EnableAsync();
        }
    }
}
