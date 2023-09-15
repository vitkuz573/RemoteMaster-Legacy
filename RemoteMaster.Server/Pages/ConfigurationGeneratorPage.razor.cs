// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using RemoteMaster.Server.Models;
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
    private IJSRuntime JSRuntime { get; set; }

    [Inject]
    public IHttpClientFactory HttpClientFactory { get; set; }

    private HttpClient HttpClient => HttpClientFactory.CreateClient("DefaultClient");

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
            Group = _group,
        };

        var response = await HttpClient.PostAsJsonAsync("api/config/generate", config);

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<ConfigResponse>();
            _configFileBytes = Encoding.UTF8.GetBytes(result.FileContent);
            _configFileName = result.FileName;

            _isConfigGenerated = true;
        }
        else
        {
            Logger.LogError("Failed to generate configuration file.");
        }
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
}
