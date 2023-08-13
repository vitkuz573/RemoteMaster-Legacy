// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR.Client;

namespace RemoteMaster.Client.Abstractions;

public interface IRemoteConnectionManager
{
    void CreateConnectionAsync(IConnectionType connectionType, string url, bool useMessagePack = false);

    Task RemoveConnectionAsync(IConnectionType connectionType);

    HubConnection? GetConnection(IConnectionType connectionType);

    Task StartConnectionAsync(IConnectionType connectionType);

    Task StopConnectionAsync(IConnectionType connectionType);
}