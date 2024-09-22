// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Core.Services;

public class HostConfigurationService : IHostConfigurationService
{
    private readonly string _configurationFileName = $"{AppDomain.CurrentDomain.FriendlyName}.json";

    public async Task<HostConfiguration> LoadConfigurationAsync()
    {
        var configFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RemoteMaster", "Host", _configurationFileName);

        if (!File.Exists(configFilePath))
        {
            throw new InvalidDataException($"Configuration file '{configFilePath}' does not exist.");
        }

        var hostConfigurationJson = await File.ReadAllTextAsync(configFilePath);
        var hostConfiguration = JsonSerializer.Deserialize<HostConfiguration>(hostConfigurationJson);

        ValidateConfiguration(hostConfiguration);

        return hostConfiguration ?? throw new InvalidDataException($"Invalid configuration in file '{configFilePath}'.");
    }

    private static void ValidateConfiguration(HostConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration.Server))
        {
            throw new ValidationException("Server must not be empty.");
        }

        if (configuration.Subject == null || string.IsNullOrWhiteSpace(configuration.Subject.Organization) || configuration.Subject.OrganizationalUnit == null || configuration.Subject.OrganizationalUnit.Length == 0 || configuration.Subject.OrganizationalUnit.Any(string.IsNullOrWhiteSpace))
        {
            throw new ValidationException("Subject options must include a valid organization and organizational unit.");
        }

        if (configuration.Host == null || string.IsNullOrWhiteSpace(configuration.Host.Name) || configuration.Host.IpAddress == null || configuration.Host.MacAddress == null || configuration.Host.MacAddress.GetAddressBytes().Length == 0)
        {
            throw new ValidationException("Host must have a valid Name, IP and MAC address.");
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
