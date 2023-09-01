using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Pages;

public partial class ConfigurationGeneratorPage
{
    private bool _isConfigGenerated = false;
    private string _group;

    [Inject]
    private IConfiguratorService ConfiguratorService { get; set; }

    [Inject]
    private ILogger<ConfigurationGeneratorPage> Logger { get; set; }

    private async Task GenerateConfig()
    {
        try
        {
            var serverIpAddress = GetLocalIPAddress();
            var config = new ConfigurationModel
            {
                ServerUrl = serverIpAddress,
                ClientId = Guid.NewGuid().ToString(),
                Group = _group
            };

            await ConfiguratorService.GenerateConfigFileAsync("C:/users/vitaly/Desktop/host.json", config);
            _isConfigGenerated = true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An error occurred while generating the config.");
            _isConfigGenerated = false;
        }
    }

    private static string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());

        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }

        throw new Exception("No network adapters with an IPv4 address in the system!");
    }
}
