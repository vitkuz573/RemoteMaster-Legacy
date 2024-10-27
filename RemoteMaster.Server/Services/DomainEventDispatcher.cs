// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Abstractions;

namespace RemoteMaster.Server.Services;

public partial class DomainEventDispatcher(IServiceProvider serviceProvider, ILogger<DomainEventDispatcher> logger) : IDomainEventDispatcher
{
    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "Dispatching event of type: {DomainEventType}")]
    static partial void LogDispatchingEvent(ILogger logger, string domainEventType);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Warning, Message = "No handler found for event: {DomainEventType}")]
    static partial void LogNoHandlerFound(ILogger logger, string domainEventType);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Information, Message = "Successfully handled event: {DomainEventType} by handler: {HandlerType}")]
    static partial void LogSuccessfullyHandled(ILogger logger, string domainEventType, string handlerType);

    [LoggerMessage(EventId = 1004, Level = LogLevel.Error, Message = "Error occurred while handling event: {DomainEventType} by handler: {HandlerType}")]
    static partial void LogErrorOccurred(ILogger logger, Exception exception, string domainEventType, string handlerType);

    [LoggerMessage(EventId = 1005, Level = LogLevel.Warning, Message = "Handler is null for event: {DomainEventType}")]
    static partial void LogHandlerIsNull(ILogger logger, string domainEventType);

    [LoggerMessage(EventId = 1006, Level = LogLevel.Information, Message = "Found handler of type: {HandlerType} for event: {DomainEventType}")]
    static partial void LogFoundHandler(ILogger logger, string handlerType, string domainEventType);

    [LoggerMessage(EventId = 1007, Level = LogLevel.Warning, Message = "Handle method returned null or non-task result for event: {DomainEventType} by handler: {HandlerType}")]
    static partial void LogNonTaskResult(ILogger logger, string domainEventType, string handlerType);

    [LoggerMessage(EventId = 1008, Level = LogLevel.Warning, Message = "No 'Handle' method found on handler: {HandlerType} for event: {DomainEventType}")]
    static partial void LogNoHandleMethod(ILogger logger, string handlerType, string domainEventType);

    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents)
    {
        ArgumentNullException.ThrowIfNull(domainEvents);

        foreach (var domainEvent in domainEvents)
        {
            var eventType = domainEvent.GetType();
            LogDispatchingEvent(logger, eventType.Name);

            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
            var handlers = serviceProvider.GetServices(handlerType).ToList();

            if (handlers.Count == 0)
            {
                LogNoHandlerFound(logger, eventType.Name);
                continue;
            }

            foreach (var handler in handlers)
            {
                if (handler == null)
                {
                    LogHandlerIsNull(logger, eventType.Name);
                    continue;
                }

                var handlerName = handler.GetType().Name;
                LogFoundHandler(logger, handlerName, eventType.Name);

                try
                {
                    var handleMethod = handler.GetType().GetMethod("Handle");

                    if (handleMethod != null)
                    {
                        var result = handleMethod.Invoke(handler, [domainEvent]);

                        if (result is Task task)
                        {
                            await task;
                            LogSuccessfullyHandled(logger, eventType.Name, handlerName);
                        }
                        else
                        {
                            LogNonTaskResult(logger, eventType.Name, handlerName);
                        }
                    }
                    else
                    {
                        LogNoHandleMethod(logger, handlerName, eventType.Name);
                    }
                }
                catch (Exception ex)
                {
                    LogErrorOccurred(logger, ex, eventType.Name, handlerName);
                    throw;
                }
            }
        }
    }
}
