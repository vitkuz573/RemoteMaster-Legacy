﻿// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Data;

namespace RemoteMaster.Server.UnitOfWork;

public class CrlUnitOfWork(CrlDbContext context, IDomainEventDispatcher domainEventDispatcher, ICrlRepository certificateRevocationLists, ILogger<UnitOfWork<CrlDbContext>> logger) : UnitOfWork<CrlDbContext>(context, domainEventDispatcher, logger), ICrlUnitOfWork
{
    public ICrlRepository CertificateRevocationLists { get; } = certificateRevocationLists;
}
