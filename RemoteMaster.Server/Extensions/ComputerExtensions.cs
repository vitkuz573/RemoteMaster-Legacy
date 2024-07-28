// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Shared.Dtos;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Extensions;

public static class ComputerExtensions
{
    public static ComputerDto ToDto(this Computer computer)
    {
        ArgumentNullException.ThrowIfNull(computer);

        return new ComputerDto
        {
            Id = computer.Id,
            Name = computer.Name,
            IpAddress = computer.IpAddress,
            MacAddress = computer.MacAddress,
            Thumbnail = computer.Thumbnail,
            ParentId = computer.ParentId,
            Parent = computer.Parent is Computer parentComputer ? parentComputer.ToDto() : null
        };
    }

    public static Computer ToEntity(this ComputerDto computerDto)
    {
        ArgumentNullException.ThrowIfNull(computerDto);

        return new Computer(computerDto.Name, computerDto.IpAddress, computerDto.MacAddress)
        {
            Id = computerDto.Id,
            Thumbnail = computerDto.Thumbnail,
            ParentId = computerDto.ParentId,
            Parent = computerDto.Parent?.ToEntity()
        };
    }
}
