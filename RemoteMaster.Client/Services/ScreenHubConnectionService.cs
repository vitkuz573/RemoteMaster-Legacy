using Microsoft.AspNetCore.SignalR.Client;

namespace RemoteMaster.Client.Services;

public class ScreenHubConnectionService
{
    private HubConnection _connection;

    public async Task<HubConnection> GetConnectionAsync(string url)
    {
        if (_connection == null)
        {
            _connection = new HubConnectionBuilder()
                .WithUrl(url, options =>
                {
                    options.HttpMessageHandlerFactory = _ => new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                    };
                })
                .Build();

            try
            {
                await _connection.StartAsync();
                Console.WriteLine("Connection started successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting connection: {ex.Message}");
            }
        }
        else if (_connection.State == HubConnectionState.Disconnected)
        {
            try
            {
                await _connection.StartAsync();
                Console.WriteLine("Connection restarted successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error restarting connection: {ex.Message}");
            }
        }

        return _connection;
    }
}
