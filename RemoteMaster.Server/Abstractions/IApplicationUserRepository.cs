// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using RemoteMaster.Server.Aggregates.ApplicationUserAggregate;

namespace RemoteMaster.Server.Abstractions;

public interface IApplicationUserRepository : IRepository<ApplicationUser, string>
{
    Task AddSignInEntryAsync(string userId, bool isSuccessful, IPAddress ipAddress);

    Task<IEnumerable<SignInEntry>> GetAllSignInEntriesAsync();

    Task ClearSignInEntriesAsync();
}
