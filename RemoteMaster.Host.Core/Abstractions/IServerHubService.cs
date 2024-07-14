// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Core.Abstractions;

public interface IServerHubService
{
    Task ConnectAsync(string server);

    Task<HostMoveRequest?> GetHostMoveRequest(string macAddress);

    Task AcknowledgeMoveRequest(string macAddress);

    Task<bool> IssueCertificateAsync(byte[] signingRequest);

    Task<bool> RegisterHostAsync(HostConfiguration hostConfiguration);

    Task<bool> UnregisterHostAsync(HostConfiguration hostConfiguration);

    Task<bool> UpdateHostInformationAsync(HostConfiguration hostConfiguration);

    Task<bool> IsHostRegisteredAsync(HostConfiguration hostConfiguration);

    void OnReceiveCertificate(Action<byte[]> onReceiveCertificate);

    void OnReceiveHostGuid(Action<Guid> onReceiveHostGuid);

    Task<byte[]> GetPublicKeyAsync();

    Task<bool> GetCaCertificateAsync();
}
