using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
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
    private IJSRuntime JSRuntime { get; set; }

    [Inject]
    private ILogger<ConfigurationGeneratorPage> Logger { get; set; }

    private async Task GenerateConfig()
    {
        if (string.IsNullOrEmpty(_group))
        {
            Logger.LogWarning("Computer group is not selected.");

            return;
        }

        var serverIpAddress = GetLocalIPAddress();
        var config = new ConfigurationModel
        {
            ServerIp = serverIpAddress,
            Group = _group
        };

        byte[] bytes;

        using (var memoryStream = await ConfiguratorService.GenerateConfigFileAsync(config))
        {
            bytes = memoryStream.ToArray();
        }

        var fileName = "config.json";
        await JSRuntime.InvokeVoidAsync("downloadFile", fileName, bytes);
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
