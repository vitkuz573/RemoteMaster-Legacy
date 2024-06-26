// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Win32;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Models;

namespace RemoteMaster.Host.Windows.Services;

public class RegistryKeyFactory : IRegistryKeyFactory
{
    public IRegistryKey? OpenSubKey(RegistryHive hive, string keyPath, bool writable)
    {
        using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
        var key = baseKey.OpenSubKey(keyPath, writable);

        return key == null ? null : new RegistryKeyWrapper(key);
    }
}