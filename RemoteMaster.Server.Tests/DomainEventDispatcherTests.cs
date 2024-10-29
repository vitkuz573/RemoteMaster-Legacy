// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.Logging;
using Moq;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Services;

namespace RemoteMaster.Server.Tests;

public class DomainEventDispatcherTests
{
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly TestLogger<DomainEventDispatcher> _testLogger;
    private readonly DomainEventDispatcher _dispatcher;

    public DomainEventDispatcherTests()
    {
        _serviceProviderMock = new Mock<IServiceProvider>();
        _testLogger = new TestLogger<DomainEventDispatcher>();
        _dispatcher = new DomainEventDispatcher(_serviceProviderMock.Object, _testLogger);
    }

    [Fact]
    public async Task DispatchAsync_Should_Throw_If_DomainEvents_Is_Null()
    {
        // Arrange
        IEnumerable<IDomainEvent>? domainEvents = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _dispatcher.DispatchAsync(domainEvents!));
    }

    [Fact]
    public async Task DispatchAsync_Should_Log_Warning_If_No_Handlers_Found()
    {
        // Arrange
        var domainEvent = new Mock<IDomainEvent>();
        var domainEvents = new List<IDomainEvent> { domainEvent.Object };

        _serviceProviderMock
            .Setup(sp => sp.GetService(It.Is<Type>(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))))
            .Returns(Array.Empty<object>());

        // Act
        await _dispatcher.DispatchAsync(domainEvents);

        // Assert
        var logEntry = _testLogger.LogEntries.FirstOrDefault(e =>
            e.LogLevel == LogLevel.Warning &&
            e.EventId.Id == 1002 &&
            e.Message.Contains("No handler found for event"));

        Assert.NotNull(logEntry);
    }

    [Fact]
    public async Task DispatchAsync_Should_Log_Warning_If_Handler_Is_Null()
    {
        // Arrange
        var domainEvent = new Mock<IDomainEvent>();
        var domainEvents = new List<IDomainEvent> { domainEvent.Object };

        _serviceProviderMock
            .Setup(sp => sp.GetService(It.Is<Type>(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))))
            .Returns(new List<object> { null! });

        // Act
        await _dispatcher.DispatchAsync(domainEvents);

        // Assert
        var logEntry = _testLogger.LogEntries.FirstOrDefault(e =>
            e.LogLevel == LogLevel.Warning &&
            e.EventId.Id == 1005 &&
            e.Message.Contains("Handler is null for event"));

        Assert.NotNull(logEntry);
    }

    [Fact]
    public async Task DispatchAsync_Should_Invoke_Handle_Method_Of_Handler()
    {
        // Arrange
        var domainEvent = new Mock<IDomainEvent>();
        var domainEvents = new List<IDomainEvent> { domainEvent.Object };

        var handlerMock = new Mock<IDomainEventHandler<IDomainEvent>>();
        handlerMock.Setup(h => h.Handle(domainEvent.Object)).Returns(Task.CompletedTask);

        _serviceProviderMock
            .Setup(sp => sp.GetService(It.Is<Type>(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))))
            .Returns(new List<object> { handlerMock.Object });

        // Act
        await _dispatcher.DispatchAsync(domainEvents);

        // Assert
        handlerMock.Verify(h => h.Handle(It.IsAny<IDomainEvent>()), Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_Should_Log_If_Handle_Succeeds()
    {
        // Arrange
        var domainEvent = new Mock<IDomainEvent>();
        var domainEvents = new List<IDomainEvent> { domainEvent.Object };

        var handlerMock = new Mock<IDomainEventHandler<IDomainEvent>>();
        handlerMock.Setup(h => h.Handle(domainEvent.Object)).Returns(Task.CompletedTask);

        _serviceProviderMock
            .Setup(sp => sp.GetService(It.Is<Type>(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))))
            .Returns(new List<object> { handlerMock.Object });

        // Act
        await _dispatcher.DispatchAsync(domainEvents);

        // Assert
        var logEntry = _testLogger.LogEntries.FirstOrDefault(e =>
            e.LogLevel == LogLevel.Information &&
            e.EventId.Id == 1003 &&
            e.Message.Contains("Successfully handled event"));

        Assert.NotNull(logEntry);
    }

    // [Fact]
    // public async Task DispatchAsync_Should_Log_Error_If_Handle_Throws_Exception()
    // {
    //     // Arrange
    //     var domainEvent = new Mock<IDomainEvent>().Object;
    //     var domainEvents = new List<IDomainEvent> { domainEvent };
    // 
    //     var exception = new Exception("Test exception");
    //     var handlerMock = new Mock<IDomainEventHandler<IDomainEvent>>();
    //     handlerMock.Setup(h => h.Handle(domainEvent)).ThrowsAsync(exception);
    // 
    //     _serviceProviderMock
    //         .Setup(sp => sp.GetService(It.Is<Type>(t =>
    //             t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))))
    //         .Returns(new List<object> { handlerMock.Object });
    // 
    //     // Act & Assert
    //     await Assert.ThrowsAsync<Exception>(() => _dispatcher.DispatchAsync(domainEvents));
    // 
    //     // Assert
    //     var logEntry = _testLogger.LogEntries.FirstOrDefault(e =>
    //         e.LogLevel == LogLevel.Error &&
    //         e.EventId.Id == 1004 &&
    //         e.Exception == exception &&
    //         e.Message.Contains("Error occurred while handling event"));
    // 
    //     Assert.NotNull(logEntry);
    // 
    //     // Access structured log properties
    //     var stateProperties = logEntry.State;
    // 
    //     var domainEventType = stateProperties.FirstOrDefault(kv => kv.Key == "DomainEventType").Value?.ToString();
    //     var handlerType = stateProperties.FirstOrDefault(kv => kv.Key == "HandlerType").Value?.ToString();
    // 
    //     Assert.Equal(domainEvent.GetType().Name, domainEventType);
    //     Assert.Equal(handlerMock.Object.GetType().Name, handlerType);
    // }
    // 
    // [Fact]
    // public async Task DispatchAsync_Should_Log_Warning_If_Handle_Method_Not_Found()
    // {
    //     // Arrange
    //     var domainEvent = new Mock<IDomainEvent>().Object;
    //     var domainEvents = new List<IDomainEvent> { domainEvent };
    // 
    //     var handlerMock = new Mock<object>();
    // 
    //     _serviceProviderMock
    //         .Setup(sp => sp.GetService(It.Is<Type>(t =>
    //             t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))))
    //         .Returns(new List<object> { handlerMock.Object });
    // 
    //     // Act
    //     await _dispatcher.DispatchAsync(domainEvents);
    // 
    //     // Assert
    //     var logEntry = _testLogger.LogEntries.FirstOrDefault(e =>
    //         e.LogLevel == LogLevel.Warning &&
    //         e.EventId.Id == 1008 &&
    //         e.Message.Contains("No 'Handle' method found"));
    // 
    //     Assert.NotNull(logEntry);
    // 
    //     // Access structured log properties
    //     var stateProperties = logEntry.State;
    // 
    //     var loggedHandlerType = stateProperties.FirstOrDefault(kv => kv.Key == "HandlerType").Value?.ToString();
    //     var loggedDomainEventType = stateProperties.FirstOrDefault(kv => kv.Key == "DomainEventType").Value?.ToString();
    // 
    //     Assert.Equal(handlerMock.Object.GetType().Name, loggedHandlerType);
    //     Assert.Equal(domainEvent.GetType().Name, loggedDomainEventType);
    // }
}
