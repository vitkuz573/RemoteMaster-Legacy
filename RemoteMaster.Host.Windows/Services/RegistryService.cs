// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Win32;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Models;

namespace RemoteMaster.Host.Windows.Services;

public class RegistryService : IRegistryService
{
    public IRegistryKey? OpenSubKey(RegistryHive hive, string keyPath, bool writable)
    {
        using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
        var key = baseKey.OpenSubKey(keyPath, writable);

        return key == null ? null : new RegistryKeyWrapper(key);
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