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

        var hostConfigurationJson = await File.ReadAllTextAsync(configFilePath);
        var hostConfiguration = JsonSerializer.Deserialize<HostConfiguration>(hostConfigurationJson);

        if (hostConfiguration is { Server: not null } && hostConfiguration.Server != "" && hostConfiguration.Group != "")
        {
            return hostConfiguration;
        }

        throw new InvalidDataException($"Error reading, parsing, or validating the configuration file '{configFilePath}'.");
    }

    public async Task SaveConfigurationAsync(HostConfiguration hostConfiguration)
    {
        ArgumentNullException.ThrowIfNull(hostConfiguration);

        var jsonSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var hostConfigurationJson = JsonSerializer.Serialize(hostConfiguration, jsonSerializerOptions);
        var configFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RemoteMaster", "Host", _configurationFileName);

        await File.WriteAllTextAsync(configFilePath, hostConfigurationJson);
    }
}

