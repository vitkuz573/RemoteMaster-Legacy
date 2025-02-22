// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Shared.DTOs;

public class OrganizationalUnitDto(Guid? id, string name, Guid organizationId, Guid? parentId)
{
    public Guid? Id { get; } = id;

    public string Name { get; } = name;

    public Guid OrganizationId { get; } = organizationId;

    public Guid? ParentId { get; } = parentId;
}
