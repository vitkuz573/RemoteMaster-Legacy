// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Shared.Models;

public class UpdateRequest(string folderPath)
{
    public string FolderPath { get; } = folderPath;

    public Credentials? UserCredentials { get; init; }

    public bool ForceUpdate { get; init; }

    public bool AllowDowngrade { get; init; }
}
