// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text.Json;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.Models;
using System.ComponentModel.DataAnnotations;

namespace RemoteMaster.Host.Core.Services;

public class HostConfigurationService : IHostConfigurationService
{
    private readonly string _configurationFileName = $"{AppDomain.CurrentDomain.FriendlyName}.json";

    public async Task<HostConfiguration> LoadConfigurationAsync(bool isInternal = true)
    {
        var configFilePath = isInternal ? _configurationFileName : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RemoteMaster", "Host", _configurationFileName);

        if (!File.Exists(configFilePath))
        {
            throw new InvalidDataException($"Configuration file '{configFilePath}' does not exist.");
        }

        var hostConfigurationJson = await File.ReadAllTextAsync(configFilePath);
        var hostConfiguration = JsonSerializer.Deserialize<HostConfiguration>(hostConfigurationJson);

        ValidateConfiguration(hostConfiguration, isInternal);

        return hostConfiguration ?? throw new InvalidDataException($"Invalid configuration in file '{configFilePath}'.");
    }

    private static void ValidateConfiguration(HostConfiguration? config, bool isInternal)
    {
        if (config == null)
        {
            throw new InvalidDataException("Configuration is null.");
        }

        if (string.IsNullOrWhiteSpace(config.Server))
        {
            throw new ValidationException("Server IP must not be empty.");
        }

        if (config.Subject == null || string.IsNullOrWhiteSpace(config.Subject.Organization) || config.Subject.OrganizationalUnit == null || config.Subject.OrganizationalUnit.Length == 0 || config.Subject.OrganizationalUnit.Any(string.IsNullOrWhiteSpace) || string.IsNullOrWhiteSpace(config.Subject.Locality) || string.IsNullOrWhiteSpace(config.Subject.State) || string.IsNullOrWhiteSpace(config.Subject.Country))
        {
            throw new ValidationException("Subject options must be fully specified.");
        }

        switch (isInternal)
        {
            case true when config.Host != null:
                throw new ValidationException("For internal configurations, Host must be null.");
            case false when (config.Host == null || string.IsNullOrWhiteSpace(config.Host.IpAddress) || string.IsNullOrWhiteSpace(config.Host.MacAddress)):
                throw new ValidationException("For external configurations, Host must have a valid IP and MAC address.");
        }
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
