using Microsoft.AspNetCore.SignalR.Client;
using RemoteMaster.Client.Abstractions;

namespace RemoteMaster.Client.Services;

public class ConnectionManager
{
    private readonly IHubConnectionFactory _hubConnectionFactory;
    private HubConnection _agentConnection;

    public bool IsServerTampered { get; private set; }

    public string StatusMessage { get; private set; }

    public ConnectionManager(IHubConnectionFactory hubConnectionFactory)
    {
        _hubConnectionFactory = hubConnectionFactory;
    }

    public HubConnection CreateAgentConnection(string host)
    {
        _agentConnection = _hubConnectionFactory.Create(host, 3564, "hubs/main");
        RegisterAgentHandlers();

        return _agentConnection;
    }

    public HubConnection CreateServerConnection(string host)
    {
        return _hubConnectionFactory.Create(host, 5076, "hubs/control", withMessagePack: true);
    }

    private void RegisterAgentHandlers()
    {
        _agentConnection.On<string>("ServerTampered", HandleServerTampered);
    }

    private void HandleServerTampered(string message)
    {
        StatusMessage = message;
        IsServerTampered = true;
    }
}
