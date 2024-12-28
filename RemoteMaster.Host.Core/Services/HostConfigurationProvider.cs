// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Core.Services;

public class HostConfigurationProvider : IHostConfigurationProvider
{
    private readonly Lock _lock = new();
    private HostConfiguration? _current;

    public HostConfiguration? Current
    {
        get
        {
            using (_lock.EnterScope())
            {
                return _current;
            }
        }
    }

    public void SetConfiguration(HostConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        using (_lock.EnterScope())
        {
            _current = configuration;
        }
    }
}
