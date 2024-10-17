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
    private readonly Mock<ILogger<DomainEventDispatcher>> _loggerMock;
    private readonly DomainEventDispatcher _dispatcher;

    public DomainEventDispatcherTests()
    {
        _serviceProviderMock = new Mock<IServiceProvider>();
        _loggerMock = new Mock<ILogger<DomainEventDispatcher>>();
        _dispatcher = new DomainEventDispatcher(_serviceProviderMock.Object, _loggerMock.Object);
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
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No handler found for event")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
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
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Handler is null for event")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
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
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully handled event")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_Should_Log_Error_If_Handle_Throws_Exception()
    {
        // Arrange
        var domainEvent = new Mock<IDomainEvent>();
        var domainEvents = new List<IDomainEvent> { domainEvent.Object };

        var handlerMock = new Mock<IDomainEventHandler<IDomainEvent>>();
        handlerMock.Setup(h => h.Handle(domainEvent.Object)).ThrowsAsync(new Exception("Test exception"));

        _serviceProviderMock
            .Setup(sp => sp.GetService(It.Is<Type>(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))))
            .Returns(new List<object> { handlerMock.Object });

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _dispatcher.DispatchAsync(domainEvents));

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error occurred while handling event")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_Should_Log_Warning_If_Handle_Method_Not_Found()
    {
        // Arrange
        var domainEvent = new Mock<IDomainEvent>();
        var domainEvents = new List<IDomainEvent> { domainEvent.Object };

        var handlerMock = new Mock<object>();

        _serviceProviderMock
            .Setup(sp => sp.GetService(It.Is<Type>(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))))
            .Returns(new List<object> { handlerMock.Object });

        // Act
        await _dispatcher.DispatchAsync(domainEvents);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No 'Handle' method found")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
}
