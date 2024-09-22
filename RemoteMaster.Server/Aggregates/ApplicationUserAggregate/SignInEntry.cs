// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;

namespace RemoteMaster.Server.Aggregates.ApplicationUserAggregate;

public class SignInEntry
{
    private SignInEntry() { }

    internal SignInEntry(string userId, bool isSuccessful, IPAddress ipAddress)
    {
        UserId = userId ?? throw new ArgumentNullException(nameof(userId));
        SignInTime = DateTime.UtcNow;
        IsSuccessful = isSuccessful;
        IpAddress = ipAddress ?? throw new ArgumentNullException(nameof(ipAddress));
    }

    public int Id { get; private set; }

    public string UserId { get; private set; }

    public DateTime SignInTime { get; private set; }

    public bool IsSuccessful { get; private set; }

    public IPAddress IpAddress { get; private set; }

    public ApplicationUser User { get; private set; }
}
