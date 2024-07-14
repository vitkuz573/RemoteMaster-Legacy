// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Core.Abstractions;

public interface IApiService
{
    Task<ApiResponse<bool>> RegisterHostAsync(HostConfiguration hostConfiguration);

    Task<ApiResponse<bool>> UnregisterHostAsync(HostConfiguration hostConfiguration);

    Task<ApiResponse<bool>> UpdateHostInformationAsync(HostConfiguration hostConfiguration);

    Task<ApiResponse<bool>> IsHostRegisteredAsync(HostConfiguration hostConfiguration);
}
