// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.ApplicationUserAggregate;

namespace RemoteMaster.Server.Services;

public class ApplicationUserService(IApplicationUserRepository applicationUserRepository, IHttpContextAccessor httpContextAccessor) : IApplicationUserService
{
    public async Task AddSignInEntry(ApplicationUser user, bool isSuccessful)
    {
        ArgumentNullException.ThrowIfNull(user);

        var httpContext = httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext is not available.");
        var ipAddress = httpContext.Connection.RemoteIpAddress ?? IPAddress.None;

        await applicationUserRepository.AddSignInEntryAsync(user.Id, isSuccessful, ipAddress);
        await applicationUserRepository.SaveChangesAsync();
    }
}
