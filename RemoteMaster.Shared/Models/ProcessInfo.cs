// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Shared.Models;

public class ProcessInfo(int id, string name)
{
    public int Id { get; } = id;

    public string Name { get; } = name;

    public long MemoryUsage { get; init; }

    public byte[]? Icon { get; init; }
}
