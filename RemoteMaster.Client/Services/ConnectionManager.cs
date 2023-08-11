// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR.Client;
using RemoteMaster.Client.Abstractions;

namespace RemoteMaster.Client.Services;

public class ConnectionManager
{
    private readonly IHubConnectionFactory _hubConnectionFactory;

    public ConnectionManager(IHubConnectionFactory hubConnectionFactory)
    {
        _hubConnectionFactory = hubConnectionFactory;
    }

    public HubConnection CreateAgentConnection(string host)
    {
        return _hubConnectionFactory.Create(host, 3564, "hubs/main");
    }

    public HubConnection CreateServerConnection(string host)
    {
        return _hubConnectionFactory.Create(host, 5076, "hubs/control", withMessagePack: true);
    }
}
