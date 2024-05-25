// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Server.Abstractions;

public interface IRemoteSchtasksService
{
    bool CopyAndExecuteRemoteFile(string sourceFilePath, string remoteMachineName, string destinationFolderPath, string? username = null, string? password = null, string? arguments = null);
}

