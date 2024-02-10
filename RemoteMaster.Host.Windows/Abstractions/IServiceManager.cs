// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Host.Windows.Abstractions;

public interface IServiceManager
{
    bool IsInstalled(string serviceName);

    void Create(IServiceConfiguration serviceConfig);

    void Start(string serviceName);

    void Stop(string serviceName);

    void Delete(string serviceName);
}
