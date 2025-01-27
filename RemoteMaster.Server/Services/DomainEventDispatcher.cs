// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using System.Reflection;
using RemoteMaster.Server.Abstractions;

namespace RemoteMaster.Server.Services;

public sealed class DomainEventDispatcher(IServiceProvider serviceProvider, ILogger<DomainEventDispatcher> logger) : IDomainEventDispatcher
{
    private static readonly ConcurrentDictionary<Type, Type> HandlerTypeCache = new();
    private static readonly ConcurrentDictionary<Type, HandlerInvoker> HandlerInvokers = new();

    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvents);

        foreach (var domainEvent in domainEvents)
        {
            ct.ThrowIfCancellationRequested();

            await DispatchSingleEventAsync(domainEvent, ct);
        }
    }

    private async Task DispatchSingleEventAsync(IDomainEvent domainEvent, CancellationToken ct)
    {
        var eventType = domainEvent.GetType();
        
        logger.LogInformation("Dispatching event: {EventType}", eventType.Name);

        var handlerType = HandlerTypeCache.GetOrAdd(eventType, t => typeof(IDomainEventHandler<>).MakeGenericType(t));

        var handlers = serviceProvider.GetServices(handlerType).ToList();

        if (handlers.Count == 0)
        {
            logger.LogWarning("No handlers registered for {EventType}", eventType.Name);
            
            return;
        }

        logger.LogDebug("Found {Count} handlers for {EventType}", handlers.Count, eventType.Name);

        foreach (var handler in handlers)
        {
            await GetInvoker(eventType).InvokeAsync(handler, domainEvent, logger, ct);
        }
    }

    private static HandlerInvoker GetInvoker(Type eventType)
    {
        return HandlerInvokers.GetOrAdd(eventType, _ =>
        {
            var method = typeof(DomainEventDispatcher)
                .GetMethod(nameof(CreateInvoker), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(eventType);

            return (HandlerInvoker)method.Invoke(null, null)!;
        });
    }

    private static HandlerInvoker CreateInvoker<TEvent>() where TEvent : IDomainEvent => new HandlerInvoker<TEvent>();

    private abstract class HandlerInvoker
    {
        public abstract Task InvokeAsync(object handler, IDomainEvent domainEvent, ILogger logger, CancellationToken ct);
    }

    private sealed class HandlerInvoker<TEvent> : HandlerInvoker where TEvent : IDomainEvent
    {
        public async override Task InvokeAsync(object handler, IDomainEvent domainEvent, ILogger logger, CancellationToken ct)
        {
            var typedHandler = (IDomainEventHandler<TEvent>)handler;
            var typedEvent = (TEvent)domainEvent;
            var handlerType = handler.GetType().Name;

            try
            {
                logger.LogDebug("Handling {EventType} with {HandlerType}", typeof(TEvent).Name, handlerType);
                
                await typedHandler.HandleAsync(typedEvent, ct);
                
                logger.LogInformation("Successfully handled {EventType} with {HandlerType}", typeof(TEvent).Name, handlerType);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error handling {EventType} with {HandlerType}", typeof(TEvent).Name, handlerType);
            }
        }
    }
}
