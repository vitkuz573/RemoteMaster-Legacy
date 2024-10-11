// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Host.Core.Models;

public class ModuleInfo
{
    public string Name { get; set; } = string.Empty;

    public Version Version { get; set; } = new(1, 0);

    public DateTime? ReleaseDate { get; set; }

    public string? Description { get; set; }

    public string[] Dependencies { get; set; } = [];

    public string? Author { get; set; }

    public string EntryPoint { get; set; } = string.Empty;
}
