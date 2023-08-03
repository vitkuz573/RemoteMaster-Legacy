// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using RemoteMaster.Client.Abstractions;

namespace RemoteMaster.Client.Services;

public class HubConnectionFactory : IHubConnectionFactory
{
    public HubConnection Create(string host, int port, string path, bool skipNegotiation = true, HttpTransportType transports = HttpTransportType.WebSockets, bool withMessagePack = false)
    {
        var hubConnectionBuilder = new HubConnectionBuilder()
            .WithUrl($"http://{host}:{port}/{path}", options =>
            {
                options.SkipNegotiation = skipNegotiation;
                options.Transports = transports;
            });

        if (withMessagePack)
        {
            hubConnectionBuilder.AddMessagePackProtocol();
        }

        return hubConnectionBuilder.Build();
    }
}