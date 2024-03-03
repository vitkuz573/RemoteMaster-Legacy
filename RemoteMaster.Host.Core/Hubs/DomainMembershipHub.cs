// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Core.Hubs;

[Authorize]
public class DomainMembershipHub(IDomainService domainService) : Hub<IDomainMembershipClient>
{
    public void SendJoinToDomain(DomainJoinRequest domainJoinRequest)
    {
        domainService.JoinToDomain(domainJoinRequest);
    }

    public void SendUnjoinFromDomain(DomainUnjoinRequest domainUnjoinRequest)
    {
        domainService.UnjoinFromDomain(domainUnjoinRequest);
    }
}
