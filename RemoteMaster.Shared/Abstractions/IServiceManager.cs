// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Shared.Abstractions;

public interface IServiceManager
{
    bool IsServiceInstalled(string serviceName);

    void InstallService(string serviceName, string displayName, string executablePath, string startType, IEnumerable<string> dependencies);

    void StartService(string serviceName);

    void StopService(string serviceName);

    void UninstallService(string serviceName);
}
