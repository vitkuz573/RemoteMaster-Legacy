// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Abstractions;

namespace RemoteMaster.Server.Services;

public class DomainEventDispatcher(IServiceProvider serviceProvider, ILogger<DomainEventDispatcher> logger) : IDomainEventDispatcher
{
    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents)
    {
        ArgumentNullException.ThrowIfNull(domainEvents);

        foreach (var domainEvent in domainEvents)
        {
            var eventType = domainEvent.GetType();
            logger.LogInformation("Dispatching event of type: {DomainEventType}", eventType.Name);

            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
            var handlers = serviceProvider.GetServices(handlerType).ToList();

            if (!handlers.Any())
            {
                logger.LogWarning("No handler found for event: {DomainEventType}", eventType.Name);
                continue;
            }

            foreach (var handler in handlers)
            {
                logger.LogInformation("Found handler of type: {HandlerType} for event: {DomainEventType}", handler.GetType().Name, eventType.Name);

                try
                {
                    var handleMethod = handlerType.GetMethod("Handle");

                    if (handleMethod != null)
                    {
                        var task = (Task)handleMethod.Invoke(handler, [domainEvent]);
                        await task;
                    }

                    logger.LogInformation("Successfully handled event: {DomainEventType} by handler: {HandlerType}", eventType.Name, handler.GetType().Name);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error occurred while handling event: {DomainEventType} by handler: {HandlerType}", eventType.Name, handler.GetType().Name);
                    throw;
                }
            }
        }
    }
}
