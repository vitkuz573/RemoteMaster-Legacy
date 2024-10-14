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

    public bool IsInTransaction => _transaction != null;

    public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Committing changes...");

            var result = await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Changes committed successfully.");

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError("Error committing changes: {Message}", ex.Message);
            throw;
        }
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            throw new InvalidOperationException("A transaction is already in progress.");
        }

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
        if (_transaction == null)
        {
            throw new InvalidOperationException("No transaction in progress to commit.");
        }

        try
        {
            logger.LogInformation("Committing transaction...");

            await _transaction.CommitAsync(cancellationToken);

            logger.LogInformation("Transaction committed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError("Error committing transaction: {Message}", ex.Message);
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No transaction in progress to rollback.");
        }

        try
        {
            logger.LogWarning("Rolling back transaction...");

            await _transaction.RollbackAsync(cancellationToken);

            logger.LogWarning("Transaction rolled back successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError("Error rolling back transaction: {Message}", ex.Message);
            throw;
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    private async Task DisposeTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.DisposeAsync();
            _transaction = null;

            logger.LogInformation("Transaction disposed.");
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
