// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Models;

namespace RemoteMaster.Host.Abstractions;

public interface IUpdaterServiceManager
{
    event Action<string, MessageType> MessageReceived;

    void InstallOrUpdate();

    void Uninstall();
}
