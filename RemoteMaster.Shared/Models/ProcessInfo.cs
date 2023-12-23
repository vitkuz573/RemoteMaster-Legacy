// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Shared.Models;

public class ProcessInfo
{
    public int Id { get; set; }

    public string Name { get; set; }

    public long MemoryUsage { get; set; }

    public double CpuUsage { get; set; }

    public string ProcessPath { get; set; }
}
