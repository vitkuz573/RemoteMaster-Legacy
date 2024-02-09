// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Shared.Models;

public class FileSystemItem
{
    public string Name { get; init; }

    public FileSystemItemType Type { get; init; }

    public long Size { get; init; }

    public enum FileSystemItemType
    {
        File,
        Directory
    }
}
