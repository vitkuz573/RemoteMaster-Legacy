// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Agent.Abstractions;

public interface IServiceManager
{
    bool IsServiceInstalled();

    void InstallService(string executablePath);

    void StartService();

    void StopService();

    void UninstallService();
}
