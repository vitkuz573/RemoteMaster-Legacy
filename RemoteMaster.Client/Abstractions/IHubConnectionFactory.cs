// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace RemoteMaster.Client.Abstractions;

public interface IHubConnectionFactory
{
    HubConnection Create(string host, int port, string path, bool skipNegotiation = true, HttpTransportType transports = HttpTransportType.WebSockets, bool withMessagePack = false);
}

