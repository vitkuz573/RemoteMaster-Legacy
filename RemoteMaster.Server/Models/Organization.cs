// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Data;
using RemoteMaster.Server.Entities;
using RemoteMaster.Shared.Abstractions;

namespace RemoteMaster.Server.Models;

public class Organization : INode
{
    public Guid Id { get; set; }
    
    public string Name { get; set; }
    
    public string Locality { get; set; }
    
    public string State { get; set; }
    
    public string Country { get; set; }

    public Guid? ParentId { get; set; }
    
    public INode? Parent { get; set; }

    public ICollection<OrganizationalUnit> OrganizationalUnits { get; } = [];

    public ICollection<ApplicationUser> AccessibleUsers { get; } = [];
}
