// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text.Json;
using System.Text.Json.Serialization;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Services;

public class ConfiguratorService : IConfiguratorService
{
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<ConfiguratorService> _logger;

    public ConfiguratorService(ILogger<ConfiguratorService> logger)
    {
        _logger = logger;

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task GenerateConfigFileAsync(string path, ConfigurationModel config)
    {
        try
        {
            var json = JsonSerializer.Serialize(config, _jsonOptions);
            await File.WriteAllTextAsync(path, json);
            _logger.LogInformation($"Successfully generated config file at {path}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while generating config file.");
            throw;
        }
    }
}
