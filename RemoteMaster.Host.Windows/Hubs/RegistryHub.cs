// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Win32;
using RemoteMaster.Host.Windows.Abstractions;

namespace RemoteMaster.Host.Windows.Hubs;

[Authorize]
public class RegistryHub(IRegistryService registryService) : Hub<IRegistryClient>
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

    public async Task GetSubKeyNames(RegistryHive hive, string keyPath)
    {
        using var key = registryService.OpenSubKey(hive, keyPath, writable: false);
        var subKeyNames = key?.GetSubKeyNames() ?? Enumerable.Empty<string>();

        await Clients.Caller.ReceiveSubKeyNames(subKeyNames);
    }

    public async Task GetAllRegistryValues(RegistryHive hive, string keyPath)
    {
        var values = registryService.GetAllValues(hive, keyPath);

        await Clients.Caller.ReceiveAllRegistryValues(values);
    }
}
