// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Linq.Expressions;

namespace RemoteMaster.Server.Abstractions;

public interface IRepository<TEntity, in TId> where TEntity : class, IAggregateRoot
{
    Task<TEntity?> GetByIdAsync(TId id);

    Task<IEnumerable<TEntity>> GetByIdsAsync(IEnumerable<TId> ids);

    Task<IEnumerable<TEntity>> GetAllAsync();

    Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate);

    Task AddAsync(TEntity entity);

    void Update(TEntity entity);

    void Delete(TEntity entity);

    Task SaveChangesAsync();
}
