// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel;
using RemoteMaster.Host.Windows.Abstractions;
using Windows.Win32.NetworkManagement.NetManagement;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host.Windows.Services;

public class DomainService : IDomainService
{
    public void JoinToDomain(string domain, string user, string password)
    {
        var result = NetJoinDomain(null, domain, null, user, password, NET_JOIN_DOMAIN_JOIN_OPTIONS.NETSETUP_JOIN_DOMAIN | NET_JOIN_DOMAIN_JOIN_OPTIONS.NETSETUP_ACCT_CREATE);

        if (result != 0)
        {
            throw new Win32Exception((int)result);
        }
    }

    public void UnjoinFromDomain(string user, string password)
    {
        var result = NetUnjoinDomain(null, user, password, 0x00000004); // NETSETUP_ACCT_DELETE

        if (result != 0)
        {
            throw new Win32Exception((int)result);
        }
    }
}
