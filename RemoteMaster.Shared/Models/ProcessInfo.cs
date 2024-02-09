// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Shared.Models;

public class ProcessInfo
{
    public int Id { get; init; }

    public string Name { get; init; }

    public long MemoryUsage { get; init; }

    public string ProcessPath { get; set; }

    public byte[]? Icon { get; init; }
}
