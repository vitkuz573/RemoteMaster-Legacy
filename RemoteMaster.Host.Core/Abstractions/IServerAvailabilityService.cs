// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Host.Core.Abstractions;

public interface IServerAvailabilityService
{
    Task<bool> IsServerAvailableAsync(string server, int maxAttempts, int initialRetryDelay, int maxRetryDelay, CancellationToken cancellationToken = default);
}
