using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace RemoteMaster.Client;

public interface IHubConnectionFactory
{
    HubConnection Create(string host, int port, string path, bool skipNegotiation, HttpTransportType transports, bool withMessagePack);
}

