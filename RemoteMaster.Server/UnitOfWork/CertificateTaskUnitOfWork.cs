// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Data;

namespace RemoteMaster.Server.UnitOfWork;

public class CertificateTaskUnitOfWork(CertificateTaskDbContext context, IDomainEventDispatcher domainEventDispatcher, ICertificateRenewalTaskRepository certificateRenewalTasks, ILogger<UnitOfWork<CertificateTaskDbContext>> logger) : UnitOfWork<CertificateTaskDbContext>(context, domainEventDispatcher, logger), ICertificateTaskUnitOfWork
{
    public ICertificateRenewalTaskRepository CertificateRenewalTasks { get; } = certificateRenewalTasks;
}
