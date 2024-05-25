// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Management.Automation;

namespace RemoteMaster.Server.Abstractions;

public interface IHostService
{
    void DeployAndExecuteHost(string localFilePath, string remoteFilePath, string host, PSCredential credential, string launchMode);
}
