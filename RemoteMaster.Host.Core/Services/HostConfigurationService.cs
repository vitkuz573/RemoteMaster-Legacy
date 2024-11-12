// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions;
using System.Text.Json;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.JsonContexts;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Core.Services;

public class HostConfigurationService(IFileSystem fileSystem) : IHostConfigurationService
{
    private readonly string _configurationFileName = $"{AppDomain.CurrentDomain.FriendlyName}.json";

    public async Task<HostConfiguration> LoadConfigurationAsync()
    {
        var configFilePath = fileSystem.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RemoteMaster", "Host", _configurationFileName);

        if (!fileSystem.File.Exists(configFilePath))
        {
            throw new InvalidDataException($"Configuration file '{configFilePath}' does not exist.");
        }

        var hostConfigurationJson = await fileSystem.File.ReadAllTextAsync(configFilePath);

        var hostConfiguration = JsonSerializer.Deserialize(hostConfigurationJson, HostJsonSerializerContext.Default.HostConfiguration);

        return hostConfiguration ?? throw new InvalidDataException($"Invalid configuration in file '{configFilePath}'.");
    }

    public async Task SaveConfigurationAsync(HostConfiguration hostConfiguration)
    {
        ArgumentNullException.ThrowIfNull(hostConfiguration);

        var hostConfigurationJson = JsonSerializer.Serialize(hostConfiguration, HostJsonSerializerContext.Default.HostConfiguration);

        var configFilePath = fileSystem.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RemoteMaster", "Host", _configurationFileName);

        await fileSystem.File.WriteAllTextAsync(configFilePath, hostConfigurationJson);
    }
}
