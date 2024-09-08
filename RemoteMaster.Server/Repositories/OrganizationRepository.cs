// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.OrganizationAggregate;
using RemoteMaster.Server.Data;

namespace RemoteMaster.Server.Repositories;

public class OrganizationRepository(ApplicationDbContext context) : IOrganizationRepository
{
    public async Task<Organization?> GetByIdAsync(Guid id)
    {
        return await context.Organizations
            .Include(o => o.OrganizationalUnits)
            .ThenInclude(ou => ou.Children)
            .Include(o => o.OrganizationalUnits)
            .ThenInclude(ou => ou.Computers)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<IEnumerable<Organization>> GetByIdsAsync(IEnumerable<Guid> ids)
    {
        return await context.Organizations
            .Include(o => o.OrganizationalUnits)
            .Where(o => ids.Contains(o.Id))
            .ToListAsync();
    }

    public async Task<IEnumerable<Organization>> GetAllAsync()
    {
        return await context.Organizations
            .Include(o => o.OrganizationalUnits)
            .ToListAsync();
    }

    public async Task<IEnumerable<Organization>> FindAsync(Expression<Func<Organization, bool>> predicate)
    {
        return await context.Organizations
            .Where(predicate)
            .ToListAsync();
    }

    public async Task AddAsync(Organization entity)
    {
        await context.Organizations.AddAsync(entity);
    }

    public async Task UpdateAsync(Organization entity)
    {
        context.Organizations.Update(entity);
    }

    public async Task DeleteAsync(Organization entity)
    {
        context.Organizations.Remove(entity);
    }

    public async Task SaveChangesAsync()
    {
        await context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Computer>> FindComputersAsync(Expression<Func<Computer, bool>> predicate)
    {
        return await context.Organizations
            .SelectMany(o => o.OrganizationalUnits)
            .SelectMany(ou => ou.Computers)
            .Where(predicate)
            .ToListAsync();
    }

    public async Task RemoveComputerAsync(Guid organizationId, Guid unitId, Guid computerId)
    {
        var organization = await context.Organizations
            .Include(o => o.OrganizationalUnits)
            .ThenInclude(ou => ou.Computers)
            .FirstOrDefaultAsync(o => o.Id == organizationId) ?? throw new InvalidOperationException("Organization not found.");
        
        var unit = organization.OrganizationalUnits.FirstOrDefault(ou => ou.Id == unitId) ?? throw new InvalidOperationException("Organizational unit not found.");
        var computer = unit.Computers.FirstOrDefault(c => c.Id == computerId) ?? throw new InvalidOperationException("Computer not found.");
        
        unit.RemoveComputer(computer.Id);
        context.Computers.Remove(computer);
    }

    public async Task<OrganizationalUnit?> GetOrganizationalUnitByIdAsync(Guid unitId)
    {
        return await context.Organizations
            .SelectMany(o => o.OrganizationalUnits)
            .Include(ou => ou.UserOrganizationalUnits)
            .FirstOrDefaultAsync(ou => ou.Id == unitId);
    }

    public async Task<Organization?> GetOrganizationByUnitIdAsync(Guid unitId)
    {
        return await context.Organizations
            .Include(o => o.OrganizationalUnits)
            .ThenInclude(ou => ou.Children)
            .ThenInclude(c => c.Computers)
            .FirstOrDefaultAsync(o => o.OrganizationalUnits.Any(ou => ou.Id == unitId));
    }

    public async Task MoveComputerAsync(Guid sourceOrganizationId, Guid targetOrganizationId, Guid computerId, Guid sourceUnitId, Guid targetUnitId)
    {
        var sourceOrganization = await context.Organizations
            .Include(o => o.OrganizationalUnits)
            .ThenInclude(ou => ou.Computers)
            .FirstOrDefaultAsync(o => o.Id == sourceOrganizationId) ?? throw new InvalidOperationException("Source organization not found.");
        
        var targetOrganization = await context.Organizations
            .Include(o => o.OrganizationalUnits)
            .ThenInclude(ou => ou.Computers)
            .FirstOrDefaultAsync(o => o.Id == targetOrganizationId) ?? throw new InvalidOperationException("Target organization not found.");
        
        var sourceUnit = sourceOrganization.OrganizationalUnits.FirstOrDefault(u => u.Id == sourceUnitId) ?? throw new InvalidOperationException("Source unit not found.");
        var computer = sourceUnit.Computers.FirstOrDefault(c => c.Id == computerId) ?? throw new InvalidOperationException("Computer not found in the source unit.");
        var targetUnit = targetOrganization.OrganizationalUnits.FirstOrDefault(u => u.Id == targetUnitId) ?? throw new InvalidOperationException("Target unit not found.");
        
        sourceUnit.RemoveComputer(computer.Id);
        computer.SetOrganizationalUnit(targetUnit.Id);
        targetUnit.AddExistingComputer(computer);

        context.Organizations.Update(sourceOrganization);
        context.Organizations.Update(targetOrganization);
    }

    public async Task<IEnumerable<CertificateRenewalTask>> GetAllCertificateRenewalTasksAsync()
    {
        return await context.CertificateRenewalTasks
            .Include(task => task.Computer)
            .Include(task => task.Organization)
            .Include(task => task.OrganizationalUnit)
            .ToListAsync();
    }

    public async Task CreateCertificateRenewalTaskAsync(Guid organizationId, Guid computerId, DateTimeOffset plannedDate)
    {
        var organization = await GetByIdAsync(organizationId);

        var certificateRenewalTask = organization?.CreateCertificateRenewalTask(computerId, plannedDate);

        await context.CertificateRenewalTasks.AddAsync(certificateRenewalTask);
    }

    public async Task DeleteCertificateRenewalTaskAsync(Guid taskId)
    {
        var task = await context.CertificateRenewalTasks
            .FirstOrDefaultAsync(t => t.Id == taskId) ?? throw new InvalidOperationException("Task not found.");

        context.CertificateRenewalTasks.Remove(task);
    }

    public async Task MarkCertificateRenewalTaskCompleted(Guid taskId)
    {
        var task = await context.CertificateRenewalTasks
            .FirstOrDefaultAsync(t => t.Id == taskId) ?? throw new InvalidOperationException("Task not found.");

        task.MarkCompleted();
    }

    public async Task MarkCertificateRenewalTaskFailed(Guid taskId)
    {
        var task = await context.CertificateRenewalTasks
            .FirstOrDefaultAsync(t => t.Id == taskId) ?? throw new InvalidOperationException("Task not found.");

        task.MarkFailed();
    }
}
