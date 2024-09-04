// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Server.Aggregates.ApplicationUserAggregate;

public class SignInEntry
{
    private SignInEntry() { }

    public SignInEntry(string userId, bool isSuccessful, string ipAddress)
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

    public string IpAddress { get; private set; }

    public ApplicationUser User { get; private set; }
}
