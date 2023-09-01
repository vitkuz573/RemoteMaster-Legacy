// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Pages;

public partial class ConfigurationGeneratorPage
{
    private bool isConfigGenerated = false;

    [Inject]
    private IConfiguratorService ConfiguratorService { get; set; }

    [Inject]
    private ILogger<ConfigurationGeneratorPage> Logger { get; set; }

    private async Task GenerateConfig()
    {
        try
        {
            var config = new ConfigurationModel
            {
                ServerUrl = "http://example.com",
                ClientId = Guid.NewGuid().ToString()
            };

            await ConfiguratorService.GenerateConfigFileAsync("C:/users/vitaly/Desktop/host.json", config);
            isConfigGenerated = true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An error occurred while generating the config.");
            isConfigGenerated = false;
        }
    }
}
