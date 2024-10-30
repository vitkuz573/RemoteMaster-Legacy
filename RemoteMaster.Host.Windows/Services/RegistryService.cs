// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Win32;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Shared.DTOs;

namespace RemoteMaster.Host.Windows.Services;

public class RegistryService(IRegistryKeyFactory registryKeyFactory) : IRegistryService
{
    public IEnumerable<IRegistryKey> GetRootKeys()
    {
        var keys = new List<IRegistryKey?>()
        {
            registryKeyFactory.Create(RegistryHive.LocalMachine, string.Empty, false),
            registryKeyFactory.Create(RegistryHive.CurrentUser, string.Empty, false),
            registryKeyFactory.Create(RegistryHive.ClassesRoot, string.Empty, false),
            registryKeyFactory.Create(RegistryHive.Users, string.Empty, false),
            registryKeyFactory.Create(RegistryHive.CurrentConfig, string.Empty, false),
            registryKeyFactory.Create(RegistryHive.PerformanceData, string.Empty, false)
        };

        return keys.Where(key => key != null).Cast<IRegistryKey>();
    }

    public IRegistryKey? OpenSubKey(RegistryHive hive, string keyPath, bool writable)
    {
        return registryKeyFactory.Create(hive, keyPath, writable);
    }

    public void SetValue(RegistryHive hive, string keyPath, string valueName, object value, RegistryValueKind valueKind)
    {
        using var key = OpenSubKey(hive, keyPath, true) ?? throw new InvalidOperationException($"Cannot open key at path '{keyPath}' for writing.");
        key.SetValue(valueName, value, valueKind);
    }

    public object GetValue(RegistryHive hive, string keyPath, string valueName, object defaultValue)
    {
        using var key = OpenSubKey(hive, keyPath, false);

        return key?.GetValue(valueName, defaultValue) ?? defaultValue;
    }

    public IEnumerable<RegistryValueDto> GetAllValues(RegistryHive hive, string keyPath)
    {
        using var key = OpenSubKey(hive, keyPath, false);

        if (key == null)
        {
            return [];
        }

        var valueNames = key.GetValueNames();

        return (from valueName in valueNames let value = key.GetValue(valueName, null) let valueType = key.GetValueKind(valueName) select new RegistryValueDto { Name = valueName, Value = value, ValueType = valueType }).ToList();
    }
}
