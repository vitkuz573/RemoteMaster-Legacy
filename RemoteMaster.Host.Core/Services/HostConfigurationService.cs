// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text.Json;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Core.Services;

public class HostConfigurationService : IHostConfigurationService
{
    private readonly string _configurationFileName = $"{AppDomain.CurrentDomain.FriendlyName}.json";

    public async Task<HostConfiguration> LoadConfigurationAsync(bool isInternal = true)
    {
        var configFilePath = isInternal ? _configurationFileName : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RemoteMaster", "Host", _configurationFileName);

        if (!File.Exists(configFilePath))
        {
            throw new InvalidDataException($"Error reading, parsing, or validating the configuration file '{configFilePath}'.");
        }

        var json = await File.ReadAllTextAsync(configFilePath);
        var config = JsonSerializer.Deserialize<HostConfiguration>(json);

        if (config is { Server: not null } && config.Server != "" && config.Group != "")
        {
            return config;
        }

        throw new InvalidDataException($"Error reading, parsing, or validating the configuration file '{configFilePath}'.");
    }

    public async Task SaveConfigurationAsync(HostConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var jsonSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(config, jsonSerializerOptions);
        var configFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RemoteMaster", "Host", _configurationFileName);

        await File.WriteAllTextAsync(configFilePath, json);
    }
}

