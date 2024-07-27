// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Shared.Abstractions;

public interface INode
{
    Guid Id { get; }

    string Name { get; }

    Guid? ParentId { get; set; }

    INode? Parent { get; set; }
}
