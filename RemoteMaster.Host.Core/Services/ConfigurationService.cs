// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text.Json;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Core.Services;

public class ConfigurationService : IConfigurationService
{
    public ConfigurationModel LoadConfiguration()
    {
        var fileName = GetConfigurationFileName();

        if (!TryReadFile(fileName, out var json))
        {
            throw new FileNotFoundException("Configuration file not found.");
        }

        if (!TryDeserializeJson(json, out var config) || !IsValidConfig(config))
        {
            throw new InvalidDataException("Error parsing or validating the configuration file.");
        }

        return config;
    }

    public string GetConfigurationFileName() => $"{AppDomain.CurrentDomain.FriendlyName}.json";

    private static bool TryReadFile(string fileName, out string content)
    {
        if (File.Exists(fileName))
        {
            using var reader = new StreamReader(fileName);
            content = reader.ReadToEnd();

            return true;
        }

        content = string.Empty;

        return false;
    }

    private static bool TryDeserializeJson(string json, out ConfigurationModel? config)
    {
        try
        {
            config = JsonSerializer.Deserialize<ConfigurationModel>(json);

            return true;
        }
        catch (JsonException)
        {
            config = null;

            return false;
        }
    }

    private static bool IsValidConfig(ConfigurationModel? config)
    {
        return config != null && !string.IsNullOrWhiteSpace(config.Server) && !string.IsNullOrWhiteSpace(config.Group);
    }
}
