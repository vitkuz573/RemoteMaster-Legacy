// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions;
using System.Text.Json;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.JsonContexts;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Core.Services;

public class HostConfigurationService(IFileSystem fileSystem, IApplicationPathProvider applicationPathProvider, IHostConfigurationProvider hostConfigurationProvider) : IHostConfigurationService
{
    private readonly string _configPath = fileSystem.Path.Combine(applicationPathProvider.DataDirectory, $"{AppDomain.CurrentDomain.FriendlyName}.json");

    public async Task<HostConfiguration> LoadAsync()
    {
        if (!fileSystem.File.Exists(_configPath))
        {
            throw new InvalidDataException($"Configuration file '{_configPath}' does not exist.");
        }

        var hostConfigurationJson = await fileSystem.File.ReadAllTextAsync(_configPath);

        var hostConfiguration = JsonSerializer.Deserialize(hostConfigurationJson, HostJsonSerializerContext.Default.HostConfiguration) ?? throw new InvalidDataException($"Invalid configuration in file '{_configPath}'.");
        
        hostConfigurationProvider.SetConfiguration(hostConfiguration);

        return hostConfiguration;
    }

    public async Task SaveAsync(HostConfiguration hostConfiguration)
    {
        ArgumentNullException.ThrowIfNull(hostConfiguration);

        var hostConfigurationJson = JsonSerializer.Serialize(hostConfiguration, HostJsonSerializerContext.Default.HostConfiguration);

        await fileSystem.File.WriteAllTextAsync(_configPath, hostConfigurationJson);

        hostConfigurationProvider.SetConfiguration(hostConfiguration);
    }
}
