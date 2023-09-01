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
    private string _selectedFilePath = string.Empty;

    [Inject]
    private IConfiguratorService ConfiguratorService { get; set; }

    [Inject]
    private ILogger<ConfigurationGeneratorPage> Logger { get; set; }

    private async Task GenerateConfig()
    {
        if (string.IsNullOrEmpty(_selectedFilePath))
        {
            Logger.LogWarning("File path is not selected.");

            return;
        }

        var serverIpAddress = GetLocalIPAddress();
        var config = new ConfigurationModel
        {
            ServerUrl = serverIpAddress,
            ClientId = Guid.NewGuid().ToString(),
            Group = _group
        };

        await ConfiguratorService.GenerateConfigFileAsync(_selectedFilePath, config);
        _isConfigGenerated = true;
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
