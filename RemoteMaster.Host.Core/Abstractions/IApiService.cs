// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net.NetworkInformation;
using RemoteMaster.Shared.DTOs;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Core.Abstractions;

public interface IApiService
{
    Task<bool> RegisterHostAsync(bool force);

    Task<bool> UnregisterHostAsync();

    Task<bool> UpdateHostInformationAsync();

    Task<bool> IsHostRegisteredAsync();

    Task<byte[]?> GetJwtPublicKeyAsync();

    Task<byte[]?> GetCaCertificateAsync();

    Task<byte[]?> IssueCertificateAsync(byte[] csrBytes);

    Task<HostMoveRequest?> GetHostMoveRequestAsync(PhysicalAddress macAddress);

    Task<bool> AcknowledgeMoveRequestAsync(PhysicalAddress macAddress);

    Task<OrganizationDto?> GetOrganizationAsync(string name);
}
