// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Shared.DTOs;

namespace RemoteMaster.Host.Windows.Abstractions;

public interface IRegistryClient
{
    Task ReceiveRootKeys(IEnumerable<string> rootKeys);
    
    Task ReceiveRegistryValue(object? value);
    
    Task ReceiveSubKeyNames(IEnumerable<string> subKeyNames);
    
    Task ReceiveOperationResult(string message);

    Task ReceiveAllRegistryValues(IEnumerable<RegistryValueDto> values);
}
