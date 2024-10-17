// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Win32;

namespace RemoteMaster.Host.Windows.Abstractions;

public interface IRegistryService
{
    IRegistryKey? OpenSubKey(RegistryHive hive, string keyPath, bool writable);

    void SetValue(RegistryHive hive, string keyPath, string valueName, object value, RegistryValueKind valueKind);

    object GetValue(RegistryHive hive, string keyPath, string valueName, object defaultValue);
}
