// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Data;

namespace RemoteMaster.Server.UnitOfWork;

public class ApplicationUnitOfWork(ApplicationDbContext context, IApplicationClaimRepository applicationClaimRepository, IApplicationUserRepository applicationUserRepository, IOrganizationRepository organizationRepository, ILogger<UnitOfWork<ApplicationDbContext>> logger) : UnitOfWork<ApplicationDbContext>(context, logger), IApplicationUnitOfWork
{
    public IApplicationClaimRepository ApplicationClaims { get; } = applicationClaimRepository;

    public IApplicationUserRepository ApplicationUsers { get; } = applicationUserRepository;

    public IOrganizationRepository Organizations { get; } = organizationRepository;
}
