// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Win32;
using RemoteMaster.Host.Windows.Abstractions;

namespace RemoteMaster.Host.Windows.Models;

public class RegistryKeyWrapper(RegistryKey registryKey) : IRegistryKey
{
    public string[] GetSubKeyNames()
    {
        return registryKey.GetSubKeyNames();
    }

    public object GetValue(string name, object defaultValue)
    {
        return registryKey.GetValue(name, defaultValue);
    }

    public void SetValue(string name, object value, RegistryValueKind valueKind)
    {
        registryKey.SetValue(name, value, valueKind);
    }

    public void Dispose()
    {
        registryKey.Dispose();
    }
}