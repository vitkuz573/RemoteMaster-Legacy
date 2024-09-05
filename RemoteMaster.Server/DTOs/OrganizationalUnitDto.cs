// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Server.DTOs;

public class OrganizationalUnitDto
{
    public Guid? Id { get; set; }
    
    public string Name { get; set; }
    
    public Guid OrganizationId { get; set; }
    
    public Guid? ParentId { get; set; }
}
