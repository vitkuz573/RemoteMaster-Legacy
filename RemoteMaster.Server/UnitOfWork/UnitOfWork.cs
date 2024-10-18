// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RemoteMaster.Server.Abstractions;

namespace RemoteMaster.Server.UnitOfWork;

public class UnitOfWork<TContext>(TContext context, IDomainEventDispatcher domainEventDispatcher, ILogger<UnitOfWork<TContext>> logger) : IUnitOfWork where TContext : DbContext
{
    private IDbContextTransaction? _transaction;
    private bool _disposed;

    public bool IsInTransaction => _transaction != null;

    public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(UnitOfWork<TContext>));
        }

        var entries = context.ChangeTracker.Entries();

        foreach (var entry in entries)
        {
            logger.LogInformation("Entity {EntityType} state: {State}", entry.Entity.GetType().Name, entry.State);
        }

        logger.LogInformation("Committing changes...");

        try
        {
            var result = await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Changes committed successfully. {ChangesCount} entities affected.", result);

            var domainEvents = GetDomainEvents().ToList();

            foreach (var domainEvent in domainEvents)
            {
                logger.LogInformation("Domain event {DomainEventType} occurred at {OccurredOn}", domainEvent.GetType().Name, domainEvent.OccurredOn);
            }

            await domainEventDispatcher.DispatchAsync(domainEvents);

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError("Error committing changes: {Message}", ex.Message);
            throw;
        }
    }

    private IEnumerable<IDomainEvent> GetDomainEvents()
    {
        var domainEntities = context.ChangeTracker
            .Entries<IAggregateRoot>()
            .Where(x => x.Entity.DomainEvents.Any())
            .Select(x => x.Entity)
            .ToList();

        var domainEvents = domainEntities
            .SelectMany(x => x.DomainEvents)
            .ToList();

        foreach (var entity in domainEntities)
        {
            entity.ClearDomainEvents();
        }

        return domainEvents;
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (!_disposed)
        {
            if (_transaction != null)
            {
                throw new InvalidOperationException("A transaction is already in progress.");
            }

            logger.LogInformation("Beginning transaction...");

            try
            {
                _transaction = await context.Database.BeginTransactionAsync(cancellationToken);
                logger.LogInformation("Transaction started with ID: {TransactionId}", _transaction.TransactionId);
            }
            catch (Exception ex)
            {
                logger.LogError("Error beginning transaction: {Message}", ex.Message);
                throw;
            }
        }
        else
        {
            throw new ObjectDisposedException(nameof(UnitOfWork<TContext>));
        }
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No transaction in progress to commit.");
        }

        if (!_disposed)
        {
            logger.LogInformation("Committing transaction with ID: {TransactionId}...", _transaction.TransactionId);

            try
            {
                await _transaction.CommitAsync(cancellationToken);
                logger.LogInformation("Transaction {TransactionId} committed successfully.", _transaction.TransactionId);
            }
            catch (Exception ex)
            {
                logger.LogError("Error committing transaction {TransactionId}: {Message}", _transaction.TransactionId, ex.Message);
                await RollbackTransactionAsync(cancellationToken);
                throw;
            }
            finally
            {
                await DisposeTransactionAsync();
            }
        }
        else
        {
            throw new ObjectDisposedException(nameof(UnitOfWork<TContext>));
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No transaction in progress to rollback.");
        }

        if (!_disposed)
        {
            logger.LogWarning("Rolling back transaction with ID: {TransactionId}...", _transaction.TransactionId);

            try
            {
                await _transaction.RollbackAsync(cancellationToken);
                logger.LogWarning("Transaction {TransactionId} rolled back successfully.", _transaction.TransactionId);
            }
            catch (Exception ex)
            {
                logger.LogError("Error rolling back transaction {TransactionId}: {Message}", _transaction.TransactionId, ex.Message);
                throw;
            }
            finally
            {
                await DisposeTransactionAsync();
            }
        }
        else
        {
            throw new ObjectDisposedException(nameof(UnitOfWork<TContext>));
        }
    }

    private async Task DisposeTransactionAsync()
    {
        if (_transaction != null)
        {
            logger.LogInformation("Disposing transaction with ID: {TransactionId}...", _transaction.TransactionId);
            await _transaction.DisposeAsync();
            _transaction = null;
            logger.LogInformation("Transaction disposed.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }

    protected async virtual ValueTask DisposeAsyncCore()
    {
        if (!_disposed)
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
            }

            await context.DisposeAsync();
            _disposed = true;

            logger.LogInformation("Context and transaction disposed asynchronously.");
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _transaction?.Dispose();
            context.Dispose();

            logger.LogInformation("Context and transaction disposed.");
        }

        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
