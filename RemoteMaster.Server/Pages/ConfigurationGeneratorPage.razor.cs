// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Pages;

public partial class ConfigurationGeneratorPage
{
    private bool isConfigGenerated = false;

    private readonly IConfiguratorService _configuratorService;
    private readonly ILogger<ConfigurationGeneratorPage> _logger;

    public ConfigurationGeneratorPage(IConfiguratorService configuratorPage, ILogger<ConfigurationGeneratorPage> logger)
    {
        _configuratorService = configuratorPage;
        _logger = logger;
    }

    private async Task GenerateConfig()
    {
        try
        {
            var config = new ConfigurationModel
            {
                ServerUrl = "http://example.com",
                ClientId = Guid.NewGuid().ToString()
            };

            await _configuratorService.GenerateConfigFileAsync("C:/users/vitaly/Desktop/host.json", config);
            isConfigGenerated = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while generating the config.");
            isConfigGenerated = false;
        }
    }
}
