// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Win32;

namespace RemoteMaster.Host.Windows.Abstractions;

public interface IRegistryKey : IDisposable
{
    string Name { get; }

    string[] GetSubKeyNames();

    string[] GetValueNames();

    RegistryValueKind GetValueKind(string? name);

    object? GetValue(string name, object? defaultValue);

    void SetValue(string name, object value, RegistryValueKind valueKind);
}