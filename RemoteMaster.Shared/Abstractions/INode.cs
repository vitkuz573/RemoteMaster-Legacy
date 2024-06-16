// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Shared.Models;

public interface INode
{
    Guid NodeId { get; }

    string Name { get; set; }

    Guid? ParentId { get; set; }

    INode? Parent { get; set; }
}
