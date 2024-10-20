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

#pragma warning disable CA2000
        using var key = string.IsNullOrEmpty(keyPath)
            ? RegistryKey.OpenBaseKey(hive, RegistryView.Default)
            : RegistryKey.OpenBaseKey(hive, RegistryView.Default)?.OpenSubKey(keyPath);
#pragma warning restore CA2000

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
        var values = registryService.GetAllValues(hive, keyPath);

        await Clients.Caller.ReceiveAllRegistryValues(values);
    }
}
