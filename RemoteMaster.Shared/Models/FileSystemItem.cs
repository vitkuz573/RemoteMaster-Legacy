// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using static RemoteMaster.Shared.Models.FileSystemItem;

namespace RemoteMaster.Shared.Models;

public class FileSystemItem(string name, FileSystemItemType type, long size)
{
    public string Name { get; } = name;

    public FileSystemItemType Type { get; } = type;

    public long Size { get; } = size;

    public enum FileSystemItemType
    {
        File,
        Directory
    }
}
