using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

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