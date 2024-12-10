// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Shared.DTOs;

namespace RemoteMaster.Server.Models;

public class HostCategory
{
    public string Title { get; set; }

    public int Count => Hosts.Count;

    public List<HostDto> Hosts { get; } = [];

    public Func<bool> CanSelectAll { get; set; }

    public Func<bool> CanDeselectAll { get; set; }

    public Func<Task> SelectAllAction { get; set; }

    public Func<Task> DeselectAllAction { get; set; }
}
