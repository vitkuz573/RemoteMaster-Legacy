// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.

using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace RemoteMaster.Client.Abstractions;

public interface IHubConnectionFactory
{
    HubConnection Create(string host, int port, string path, bool skipNegotiation = true, HttpTransportType transports = HttpTransportType.WebSockets, bool withMessagePack = false);
}

