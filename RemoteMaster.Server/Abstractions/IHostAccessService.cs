// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Claims;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Abstractions;

public interface IHostAccessService
{
    Task<AccessInitializationResult> InitializeAccessAsync(string host, ClaimsPrincipal? user);
}
