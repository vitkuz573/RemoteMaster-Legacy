// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RemoteMaster.Server.Abstractions;

namespace RemoteMaster.Server.UnitOfWork;

public class UnitOfWork<TContext>(TContext context, ILogger<UnitOfWork<TContext>> logger) : IUnitOfWork where TContext : DbContext
{
    private IDbContextTransaction? _transaction;
    private bool _disposed;

    public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Committing changes...");

            return await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError("Error committing changes: {Message}", ex.Message);
            throw;
        }
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Beginning transaction...");

            _transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError("Error beginning transaction: {Message}", ex.Message);
            throw;
        }
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_transaction != null)
            {
                logger.LogInformation("Committing transaction...");

                await _transaction.CommitAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogError("Error committing transaction: {Message}", ex.Message);
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_transaction != null)
            {
                logger.LogWarning("Rolling back transaction...");

                await _transaction.RollbackAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogError("Error rolling back transaction: {Message}", ex.Message);
            throw;
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _transaction?.Dispose();
                context.Dispose();

                logger.LogInformation("Context and transaction disposed.");
            }

            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
