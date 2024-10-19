// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Win32;

namespace RemoteMaster.Shared.DTOs;

public class RegistryValueDto
{
    public string Name { get; set; }
    
    public object? Value { get; set; }
    
    public RegistryValueKind ValueType { get; set; }
}
