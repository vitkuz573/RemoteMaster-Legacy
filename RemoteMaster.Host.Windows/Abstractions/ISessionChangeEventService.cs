// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Windows.Models;

namespace RemoteMaster.Host.Windows.Abstractions;

public interface ISessionChangeEventService
{
    event EventHandler<SessionChangeEventArgs> SessionChanged;

    void OnSessionChanged(SessionChangeEventArgs e);
}

