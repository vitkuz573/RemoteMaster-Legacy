// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Shared.Models;

public class FileSystemItem
{
    public string Name { get; set; }

    public FileSystemItemType Type { get; set; }

    public long Size { get; set; }

    public enum FileSystemItemType
    {
        File,
        Directory
    }
}
