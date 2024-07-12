// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Core.Abstractions;

public interface IHostLifecycleService
{
    Task RegisterAsync(HostConfiguration hostConfiguration);

    Task UnregisterAsync(HostConfiguration hostConfiguration);

    Task IssueCertificateAsync(HostConfiguration hostConfiguration);

    Task RenewCertificateAsync(HostConfiguration hostConfiguration);

    void RemoveCertificate();

    Task UpdateHostInformationAsync(HostConfiguration hostConfiguration);

    Task<bool> IsHostRegisteredAsync(HostConfiguration hostConfiguration);

    Task GetCaCertificateAsync();
}
