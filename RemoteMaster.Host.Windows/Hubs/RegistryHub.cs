// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using RemoteMaster.Host.Windows.Abstractions;

namespace RemoteMaster.Host.Windows.Hubs;

[Authorize]
public class RegistryHub(IRegistryService registryService, ILogger<RegistryHub> logger) : Hub<IRegistryClient>
{
    public async Task GetRootKeys()
    {
        var rootKeys = registryService.GetRootKeys()
                                      .Select(key => key.Name)
                                      .ToList();

        await Clients.Caller.ReceiveRootKeys(rootKeys);
    }

    public async Task GetRegistryValue(RegistryHive hive, string keyPath, string valueName, object defaultValue)
    {
        var value = registryService.GetValue(hive, keyPath, valueName, defaultValue);
        
        await Clients.Caller.ReceiveRegistryValue(value);
    }

    public async Task SetRegistryValue(RegistryHive hive, string keyPath, string valueName, object value, RegistryValueKind valueKind)
    {
        registryService.SetValue(hive, keyPath, valueName, value, valueKind);

        await Clients.Caller.ReceiveOperationResult("Value set successfully.");
    }

    public async Task GetSubKeyNames(RegistryHive hive, string? keyPath, string parentKey)
    {
        logger.LogInformation("Fetching subkeys for hive: {Hive}, keyPath: {KeyPath}", hive, keyPath ?? "<root>");

        using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
        
        if (baseKey == null)
        {
            logger.LogError("Failed to open base key: {Hive}", hive);
            await Clients.Caller.ReceiveSubKeyNames([], parentKey);

            return;
        }

        using var key = string.IsNullOrEmpty(keyPath) ? baseKey : baseKey.OpenSubKey(keyPath);
        
        if (key == null)
        {
            logger.LogError("Failed to open key: {KeyPath}", keyPath ?? "<root>");
            await Clients.Caller.ReceiveSubKeyNames([], parentKey);

            return;
        }

        var subKeyNames = key.GetSubKeyNames().ToList();

        logger.LogInformation("Fetched {SubKeyCount} subkeys for keyPath: {KeyPath}", subKeyNames.Count, keyPath ?? "<root>");

        await Clients.Caller.ReceiveSubKeyNames(subKeyNames, parentKey);
    }

    public async Task GetAllRegistryValues(RegistryHive hive, string keyPath)
    {
        logger.LogInformation("Fetching all registry values for hive: {Hive}, keyPath: {KeyPath}", hive, keyPath);

        var values = registryService.GetAllValues(hive, keyPath);

        if (values.Any())
        {
            logger.LogInformation("Fetched {ValuesCount} registry values for keyPath: {KeyPath}", values.Count(), keyPath);
        }
        else
        {
            logger.LogWarning("No registry values found for keyPath: {KeyPath}", keyPath);
        }

        await Clients.Caller.ReceiveAllRegistryValues(values);
    }
}
