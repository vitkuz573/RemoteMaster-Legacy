// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Client.Core.Abstractions;

public interface IPowerService
{
    void Shutdown(string message, uint timeout = 0, bool forceAppsClosed = true);

    void Reboot(string message, uint timeout = 0, bool forceAppsClosed = true);
}
