// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Shared.DTOs;

namespace RemoteMaster.Host.Core.Abstractions;

public interface IHostLifecycleService
{
    Task<bool> RegisterAsync(bool force);

    Task<bool> UnregisterAsync();

    Task<bool> UpdateHostInformationAsync();

    Task<bool> IsHostRegisteredAsync();

    Task<AddressDto> GetOrganizationAddressAsync(string organization);
}
