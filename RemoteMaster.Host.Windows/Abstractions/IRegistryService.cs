// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Win32;
using RemoteMaster.Shared.DTOs;

namespace RemoteMaster.Host.Windows.Abstractions;

public interface IRegistryService
{
    IEnumerable<IRegistryKey> GetRootKeys();
    
    IRegistryKey? OpenSubKey(RegistryHive hive, string keyPath, bool writable);
    
    void SetValue(RegistryHive hive, string keyPath, string valueName, object value, RegistryValueKind valueKind);
    
    object GetValue(RegistryHive hive, string keyPath, string valueName, object defaultValue);
    
    IEnumerable<RegistryValueDto> GetAllValues(RegistryHive hive, string keyPath);
    
    Task<IEnumerable<string>> GetSubKeyNamesAsync(RegistryHive hive, string keyPath);
    
    Task<byte[]> ExportRegistryBranchAsync(RegistryHive hive, string? keyPath);
}
