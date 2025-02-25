// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.DTOs;

namespace RemoteMaster.Host.Core.Hubs;

public class DomainMembershipHub(IDomainService domainService) : Hub<IDomainMembershipClient>
{
    [Authorize(Policy = "JoinDomainPolicy")]
    [HubMethodName("SendJoinToDomain")]
    public async Task SendJoinToDomainAsync(DomainJoinRequest domainJoinRequest)
    {
        await domainService.JoinToDomainAsync(domainJoinRequest);
    }

    [Authorize(Policy = "UnjoinDomainPolicy")]
    [HubMethodName("SendUnjoinFromDomain")]
    public async Task SendUnjoinFromDomainAsync(DomainUnjoinRequest domainUnjoinRequest)
    {
        await domainService.UnjoinFromDomainAsync(domainUnjoinRequest);
    }
}
