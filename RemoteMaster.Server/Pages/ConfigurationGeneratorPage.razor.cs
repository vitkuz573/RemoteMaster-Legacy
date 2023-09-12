// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Pages;

public partial class ConfigurationGeneratorPage
{
    private bool _isConfigGenerated = false;
    private string _group;

    private byte[] _configFileBytes;
    private string _configFileName = "RemoteMaster.Agent.json";

    private bool _isSpoilerVisible = false;

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

        var config = new ConfigurationModel
        {
            Server = GetLocalIPAddress(),
            Group = _group
        };

        using (var memoryStream = await ConfiguratorService.GenerateConfigFileAsync(config))
        {
            _configFileBytes = memoryStream.ToArray();
        }

        _isConfigGenerated = true;
    }

    private async Task DownloadConfig()
    {
        await JSRuntime.InvokeVoidAsync("downloadFile", _configFileName, _configFileBytes);
    }

    private string GetConfigContent()
    {
        return _configFileBytes == null ? string.Empty : Encoding.UTF8.GetString(_configFileBytes);
    }

    private void ToggleSpoiler()
    {
        _isSpoilerVisible = !_isSpoilerVisible;
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
