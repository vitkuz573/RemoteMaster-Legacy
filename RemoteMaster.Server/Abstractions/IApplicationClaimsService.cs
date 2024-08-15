// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Linq.Expressions;
using FluentResults;
using RemoteMaster.Server.Entities;

namespace RemoteMaster.Server.Abstractions;

public interface IApplicationClaimsService
{
    Task<Result<IList<ApplicationClaim>>> GetClaimsAsync(Expression<Func<ApplicationClaim, bool>>? predicate = null);
    
    Task<Result<ApplicationClaim>> AddClaimAsync(ApplicationClaim claim);
    
    Task<Result> RemoveClaimAsync(Guid claimId);
    
    Task<Result<ApplicationClaim>> UpdateClaimAsync(ApplicationClaim claim);
}