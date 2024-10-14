// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Server.Abstractions;

public interface IUnitOfWork : IDisposable
{
    bool IsInTransaction { get; }

    Task<int> CommitAsync(CancellationToken cancellationToken = default);
    
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
