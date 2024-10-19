// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Win32;
using RemoteMaster.Host.Windows.Abstractions;

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
        using var key = OpenSubKey(hive, keyPath, true);
        key?.SetValue(valueName, value, valueKind);
    }

    public object GetValue(RegistryHive hive, string keyPath, string valueName, object defaultValue)
    {
        using var key = OpenSubKey(hive, keyPath, false);

        return key?.GetValue(valueName, defaultValue) ?? defaultValue;
    }
}
