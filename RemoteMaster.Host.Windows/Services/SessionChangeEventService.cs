// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Models;

namespace RemoteMaster.Host.Windows.Services;

public class SessionChangeEventService : ISessionChangeEventService
{
    public event EventHandler<SessionChangeEventArgs>? SessionChanged;

    public void OnSessionChanged(nuint reason)
    {
        var args = new SessionChangeEventArgs(reason);

        SessionChanged?.Invoke(this, args);
    }
}
