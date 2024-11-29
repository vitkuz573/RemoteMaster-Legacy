// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Services;

public class TimeProvider : ITimeProvider
{
    public Task Delay(int milliseconds, CancellationToken cancellationToken = default)
    {
        return Task.Delay(milliseconds, cancellationToken);
    }
}
