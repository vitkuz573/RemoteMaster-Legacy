// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Data;
using RemoteMaster.Server.Entities;
using RemoteMaster.Server.Models;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Services;

public class ApplicationClaimsService(ApplicationDbContext applicationDbContext) : IApplicationClaimsService
{
    public async Task<Result<IList<ApplicationClaim>>> GetClaimsAsync(Expression<Func<ApplicationClaim, bool>>? predicate = null)
    {
        try
        {
            var query = applicationDbContext.ApplicationClaims.AsQueryable();

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            var claims = await query.ToListAsync();

            return Result<IList<ApplicationClaim>>.Success(claims);
        }
        catch (Exception ex)
        {
            return Result<IList<ApplicationClaim>>.Failure($"Error: Failed to retrieve ApplicationClaims.", exception: ex);
        }
    }

    public async Task<Result<ApplicationClaim>> AddClaimAsync(ApplicationClaim claim)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(claim);

            await applicationDbContext.ApplicationClaims.AddAsync(claim);
            await applicationDbContext.SaveChangesAsync();

            return Result<ApplicationClaim>.Success(claim);
        }
        catch (Exception ex)
        {
            return Result<ApplicationClaim>.Failure($"Error: Failed to add ApplicationClaim.", exception: ex);
        }
    }

    public async Task<Result> RemoveClaimAsync(Guid claimId)
    {
        try
        {
            var claim = await applicationDbContext.ApplicationClaims.FindAsync(claimId);

            if (claim == null)
            {
                return Result.Failure($"Error: ApplicationClaim with ID '{claimId}' not found.");
            }

            applicationDbContext.ApplicationClaims.Remove(claim);
            await applicationDbContext.SaveChangesAsync();

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Error: Failed to remove ApplicationClaim.", exception: ex);
        }
    }

    public async Task<Result<ApplicationClaim>> UpdateClaimAsync(ApplicationClaim claim)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(claim);

            var existingClaim = await applicationDbContext.ApplicationClaims.FindAsync(claim.Id);

            if (existingClaim == null)
            {
                return Result<ApplicationClaim>.Failure($"Error: ApplicationClaim with ID '{claim.Id}' not found.");
            }

            applicationDbContext.Entry(existingClaim).CurrentValues.SetValues(claim);
            await applicationDbContext.SaveChangesAsync();

            return Result<ApplicationClaim>.Success(claim);
        }
        catch (Exception ex)
        {
            return Result<ApplicationClaim>.Failure($"Error: Failed to update ApplicationClaim.", exception: ex);
        }
    }
}