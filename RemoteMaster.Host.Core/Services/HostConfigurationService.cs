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
        catch (JsonException)
        {
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

    public async Task SaveConfigurationAsync(HostConfiguration config)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        var json = SerializeToJson(config);
        await WriteFileAsync(ConfigurationFileName, json);
    }

    public async Task SaveConfigurationAsync(HostConfiguration config, string filePath)
    {
        ArgumentNullException.ThrowIfNull(config);

        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or whitespace.", nameof(filePath));
        }

        var json = SerializeToJson(config);
        await WriteFileAsync(filePath, json);
    }

    private static string SerializeToJson(HostConfiguration config)
    {
        return JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
    }

    private static async Task WriteFileAsync(string fileName, string json)
    {
        await File.WriteAllTextAsync(fileName, json);
    }
}
