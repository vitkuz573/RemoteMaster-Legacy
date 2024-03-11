// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.Models;
using Windows.Win32.NetworkManagement.NetManagement;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host.Windows.Services;

/// <summary>
/// Provides services for managing domain membership of the machine.
/// </summary>
public class DomainService : IDomainService
{
    /// <summary>
    /// Joins the machine to a domain.
    /// </summary>
    /// <param name="domainJoinRequest">The request to join a domain, containing the domain name and user credentials.</param>
    public void JoinToDomain(DomainJoinRequest domainJoinRequest)
    {
        ArgumentNullException.ThrowIfNull(domainJoinRequest);

        var result = NetJoinDomain(null, domainJoinRequest.Domain, null, domainJoinRequest.UserCredentials.Username, domainJoinRequest.UserCredentials.Password, NET_JOIN_DOMAIN_JOIN_OPTIONS.NETSETUP_JOIN_DOMAIN | NET_JOIN_DOMAIN_JOIN_OPTIONS.NETSETUP_ACCT_CREATE);

        if (result != 0)
        {
            throw new Win32Exception((int)result, "Failed to join the domain.");
        }
    }

    /// <summary>
    /// Unjoins the machine from a domain.
    /// </summary>
    /// <param name="domainUnjoinRequest">The request to unjoin a domain, containing the user credentials.</param>
    public void UnjoinFromDomain(DomainUnjoinRequest domainUnjoinRequest)
    {
        ArgumentNullException.ThrowIfNull(domainUnjoinRequest);

        var result = NetUnjoinDomain(null, domainUnjoinRequest.UserCredentials.Username, domainUnjoinRequest.UserCredentials.Password, NETSETUP_ACCT_DELETE);

        if (result != 0)
        {
            throw new Win32Exception((int)result, "Failed to unjoin from the domain.");
        }
    }
}
