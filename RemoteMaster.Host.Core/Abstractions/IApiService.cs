// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Core.Abstractions;

public interface IApiService
{
    Task<ApiResponse<bool>?> RegisterHostAsync();

    Task<ApiResponse<bool>?> UnregisterHostAsync();

    Task<ApiResponse<bool>?> UpdateHostInformationAsync();

    Task<ApiResponse<bool>?> IsHostRegisteredAsync();

    Task<ApiResponse<byte[]>?> GetJwtPublicKeyAsync();

    Task<ApiResponse<byte[]>?> GetCaCertificateAsync();

    Task<ApiResponse<byte[]>?> IssueCertificateAsync(byte[] csrBytes);
}
