// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using RemoteMaster.Host.Windows.Abstractions;

namespace RemoteMaster.Host.Windows.Hubs;

public class RegistryHub(IRegistryService registryService, ILogger<RegistryHub> logger) : Hub<IRegistryClient>
{
    [Authorize(Policy = "GetRootKeysPolicy")]
    public async Task GetRootKeys()
    {
        var rootKeys = registryService.GetRootKeys().Select(key => key.Name).ToList();

        await Clients.Caller.ReceiveRootKeys(rootKeys);
    }

    [Authorize(Policy = "GetRegistryValuePolicy")]
    public async Task GetRegistryValue(RegistryHive hive, string keyPath, string valueName, object defaultValue)
    {
        var value = registryService.GetValue(hive, keyPath, valueName, defaultValue);

        await Clients.Caller.ReceiveRegistryValue(value);
    }

    [Authorize(Policy = "SetRegistryValuePolicy")]
    public async Task SetRegistryValue(RegistryHive hive, string keyPath, string valueName, object value, RegistryValueKind valueKind)
    {
        try
        {
            registryService.SetValue(hive, keyPath, valueName, value, valueKind);

            await Clients.Caller.ReceiveOperationResult("Value set successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError("Error setting registry value: {Message}", ex.Message);

            await Clients.Caller.ReceiveOperationResult($"Error setting value: {ex.Message}");
        }
    }

    [Authorize(Policy = "GetSubKeyNamesPolicy")]
    public async Task GetSubKeyNames(RegistryHive hive, string? keyPath, string parentKey)
    {
        var subKeyNames = await registryService.GetSubKeyNamesAsync(hive, keyPath);

        await Clients.Caller.ReceiveSubKeyNames(subKeyNames.ToArray(), parentKey);
    }

    [Authorize(Policy = "GetAllRegistryValuesPolicy")]
    public async Task GetAllRegistryValues(RegistryHive hive, string keyPath)
    {
        var values = registryService.GetAllValues(hive, keyPath);

        if (values.Any())
        {
            logger.LogInformation("Fetched {ValuesCount} registry values for keyPath: {KeyPath}", values.Count(), keyPath);
        }
        else
        {
            logger.LogWarning("No registry values found for keyPath: {KeyPath}", keyPath);
        }

        await Clients.Caller.ReceiveAllRegistryValues(values.ToList());
    }

    [Authorize(Policy = "ExportRegistryBranchPolicy")]
    public async Task ExportRegistryBranch(RegistryHive hive, string? keyPath)
    {
        try
        {
            var exportedData = await registryService.ExportRegistryBranchAsync(hive, keyPath);

            await Clients.Caller.ReceiveExportedRegistryBranch(exportedData);
        }
        catch (Exception ex)
        {
            logger.LogError("Error exporting registry branch: {Message}", ex.Message);

            await Clients.Caller.ReceiveOperationResult($"Error exporting registry branch: {ex.Message}");
        }
    }
}
