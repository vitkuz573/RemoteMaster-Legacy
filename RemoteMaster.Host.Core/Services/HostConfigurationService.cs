// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text.Json;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Core.Services;

public class HostConfigurationService : IHostConfigurationService
{
    public async Task<HostConfiguration> LoadConfigurationAsync()
    {
        var config = await TryReadAndDeserializeFileAsync(ConfigurationFileName);
        
        if (config != null)
        {
            return config;
        }

        throw new InvalidDataException($"Error reading, parsing, or validating the configuration file '{ConfigurationFileName}'.");
    }

    public async Task<HostConfiguration> LoadConfigurationAsync(string filePath)
    {
        var config = await TryReadAndDeserializeFileAsync(filePath);

        if (config != null)
        {
            return config;
        }

        throw new InvalidDataException($"Error reading, parsing, or validating the configuration file '{filePath}'.");
    }

    public string ConfigurationFileName => $"{AppDomain.CurrentDomain.FriendlyName}.json";

    private static async Task<HostConfiguration?> TryReadAndDeserializeFileAsync(string fileName)
    {
        var json = await ReadFileAsync(fileName);
        
        if (json is not null && TryDeserializeJson(json, out var config) && IsValidConfig(config))
        {
            return config;
        }

        return null;
    }

    private static async Task<string?> ReadFileAsync(string fileName)
    {
        if (File.Exists(fileName))
        {
            return await File.ReadAllTextAsync(fileName);
        }

        return null;
    }

    private static bool TryDeserializeJson(string json, out HostConfiguration? config)
    {
        try
        {
            config = JsonSerializer.Deserialize<HostConfiguration>(json);
            
            return true;
        }
        catch (JsonException ex)
        {
            // You might want to log the exception here.
            config = null;

            return false;
        }
    }

    private static bool IsValidConfig(HostConfiguration? config)
    {
        return config switch
        {
            { Server: not null and not "", Group: not null and not "" } => true,
            _ => false
        };
    }
}
