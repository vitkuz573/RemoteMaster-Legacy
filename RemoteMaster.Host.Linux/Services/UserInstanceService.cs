// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Linux.Services;

public class UserInstanceService : IUserInstanceService
{
    public bool IsRunning { get; }

    public void Start() => throw new NotImplementedException();

    public void Stop() => throw new NotImplementedException();

    public void Restart() => throw new NotImplementedException();
}
