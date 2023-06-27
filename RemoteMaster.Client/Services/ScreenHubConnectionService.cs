using Microsoft.AspNetCore.SignalR.Client;

public class ScreenHubConnectionService
{
    private HubConnection connection;

    public async Task<HubConnection> GetConnectionAsync(string url)
    {
        if (connection == null)
        {
            connection = new HubConnectionBuilder()
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
                await connection.StartAsync();
                Console.WriteLine("Connection started successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting connection: {ex.Message}");
            }
        }
        else if (connection.State == HubConnectionState.Disconnected)
        {
            try
            {
                await connection.StartAsync();
                Console.WriteLine("Connection restarted successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error restarting connection: {ex.Message}");
            }
        }

        return connection;
    }
}
