// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;

namespace RemoteMaster.Shared.Dtos;

public class UpdateRequest(string folderPath)
{
    public string FolderPath { get; } = folderPath;

    public NetworkCredential? UserCredentials { get; init; }

    public bool ForceUpdate { get; init; }

    public bool AllowDowngrade { get; init; }
}
