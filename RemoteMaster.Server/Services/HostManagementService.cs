// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Abstractions;

namespace RemoteMaster.Server.Services;

public class HostManagementService(IComputerRepository computerRepository, IOrganizationalUnitRepository organizationalUnitRepository)
{
    public async Task<bool> MoveComputerToUnitAsync(Guid computerId, Guid newUnitId)
    {
        var computer = await computerRepository.GetByIdAsync(computerId) ?? throw new InvalidOperationException("Computer not found.");

        var parent = await organizationalUnitRepository.GetByIdAsync(newUnitId);

        computer.ChangeParent(parent);
        
        await computerRepository.UpdateAsync(computer);
        await computerRepository.SaveChangesAsync();

        return true;
    }
}
