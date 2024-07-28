// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Entities;

public class OrganizationalUnit : INode
{
    public Guid Id { get; set; }

    public string Name { get; set; }

    public Guid? ParentId { get; set; }

    public INode? Parent { get; set; }

    public Guid OrganizationId { get; set; }

    public Organization Organization { get; set; }

    public ICollection<OrganizationalUnit> Children { get; } = [];

    public ICollection<Computer> Computers { get; } = [];

    public ICollection<ApplicationUser> AccessibleUsers { get; } = [];
}
