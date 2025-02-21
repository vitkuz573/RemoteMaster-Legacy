// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Shared.DTOs;

public class OrganizationDto(Guid? id, string name, AddressDto address)
{
    public Guid? Id { get; } = id;

    public string Name { get; } = name;

    public AddressDto Address { get; } = address;
}
