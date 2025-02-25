// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Hubs;
using RemoteMaster.Host.Core.Services;
using RemoteMaster.Shared.DTOs;

namespace RemoteMaster.Host.Core.Tests;

public class AppStateTests
{
    private readonly Mock<IHubContext<ControlHub, IControlClient>> _hubContextMock;
    private readonly Mock<IControlClient> _controlClientMock;
    private readonly Mock<ILogger<AppState>> _loggerMock;
    private readonly AppState _appState;

    public AppStateTests()
    {
        _hubContextMock = new Mock<IHubContext<ControlHub, IControlClient>>();
        _controlClientMock = new Mock<IControlClient>();
        _loggerMock = new Mock<ILogger<AppState>>();

        // Setup Clients.All to return the mock IControlClient
        var clientsMock = new Mock<IHubClients<IControlClient>>();
        clientsMock.Setup(clients => clients.All).Returns(_controlClientMock.Object);
        _hubContextMock.Setup(hc => hc.Clients).Returns(clientsMock.Object);

        _appState = new AppState(_hubContextMock.Object, _loggerMock.Object);
    }

    #region TryGetViewer Tests

    [Fact]
    public async Task TryGetViewer_ViewerExists_ReturnsTrueAndViewer()
    {
        // Arrange
        var viewerMock = new Mock<IViewer>();
        const string connectionId = "conn1";
        viewerMock.Setup(v => v.ConnectionId).Returns(connectionId);

        await _appState.TryAddViewerAsync(viewerMock.Object);

        // Act
        var result = _appState.TryGetViewer(connectionId, out var retrievedViewer);

        // Assert
        Assert.True(result);
        Assert.Equal(viewerMock.Object, retrievedViewer);
    }

    [Fact]
    public void TryGetViewer_ViewerDoesNotExist_ReturnsFalseAndNull()
    {
        // Arrange
        const string connectionId = "nonexistent";

        // Act
        var result = _appState.TryGetViewer(connectionId, out var retrievedViewer);

        // Assert
        Assert.False(result);
        Assert.Null(retrievedViewer);
    }

    #endregion

    #region TryAddViewer Tests

    [Fact]
    public async Task TryAddViewer_ValidViewer_AddsSuccessfully()
    {
        // Arrange
        var viewerMock = new Mock<IViewer>();
        const string connectionId = "conn2";
        viewerMock.Setup(v => v.ConnectionId).Returns(connectionId);

        var eventInvoked = false;
        IViewer? addedViewer = null;

        _appState.ViewerAdded += (_, viewer) =>
        {
            eventInvoked = true;
            addedViewer = viewer;
        };

        // Act
        var result = await _appState.TryAddViewerAsync(viewerMock.Object);

        // Assert
        Assert.True(result);
        Assert.True(eventInvoked);
        Assert.Equal(viewerMock.Object, addedViewer);

        // Verify that ReceiveAllViewers was called once with the correct DTO
        _controlClientMock.Verify(c => c.ReceiveAllViewers(It.Is<List<ViewerDto>>(list =>
            list.Count == 1 &&
            list[0].ConnectionId == connectionId
        )), Times.Once);

        // Verify that no error was logged
        _loggerMock.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Never);
    }

    [Fact]
    public async Task TryAddViewer_DuplicateViewer_AddsFails()
    {
        // Arrange
        var viewerMock1 = new Mock<IViewer>();
        var viewerMock2 = new Mock<IViewer>();
        const string connectionId = "conn3";
        viewerMock1.Setup(v => v.ConnectionId).Returns(connectionId);
        viewerMock2.Setup(v => v.ConnectionId).Returns(connectionId);

        var eventInvoked = false;

        _appState.ViewerAdded += (_, _) =>
        {
            eventInvoked = true;
        };

        // Act
        var firstAddResult = await _appState.TryAddViewerAsync(viewerMock1.Object);
        var secondAddResult = await _appState.TryAddViewerAsync(viewerMock2.Object);

        // Assert
        Assert.True(firstAddResult);
        Assert.False(secondAddResult);
        Assert.True(eventInvoked); // Event should only be invoked once

        // Verify that ReceiveAllViewers was called only once
        _controlClientMock.Verify(c => c.ReceiveAllViewers(It.IsAny<List<ViewerDto>>()), Times.Once);

        // Verify that an error was logged for the failed add
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Failed to add viewer with connection ID {connectionId}.")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task TryAddViewer_NullViewer_ThrowsArgumentNullException()
    {
        // Arrange
        IViewer? nullViewer = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _appState.TryAddViewerAsync(nullViewer!));

        // Verify that nothing was added or logged
        _controlClientMock.Verify(c => c.ReceiveAllViewers(It.IsAny<List<ViewerDto>>()), Times.Never);
        _loggerMock.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Never);
    }

    #endregion

    #region TryRemoveViewer Tests

    [Fact]
    public async Task TryRemoveViewer_ExistingViewer_RemovesSuccessfully()
    {
        // Arrange
        var viewerMock = new Mock<IViewer>();
        const string connectionId = "conn4";
        viewerMock.Setup(v => v.ConnectionId).Returns(connectionId);
        viewerMock.Setup(v => v.Context).Returns(new Mock<HubCallerContext>().Object);
        viewerMock.Setup(v => v.Dispose());

        await _appState.TryAddViewerAsync(viewerMock.Object);

        var eventInvoked = false;
        IViewer? removedViewer = null;

        _appState.ViewerRemoved += (_, viewer) =>
        {
            eventInvoked = true;
            removedViewer = viewer;
        };

        // Act
        var result = await _appState.TryRemoveViewerAsync(connectionId);

        // Assert
        Assert.True(result);
        Assert.True(eventInvoked);
        Assert.Equal(viewerMock.Object, removedViewer);

        // Verify that Abort and Dispose were called
        viewerMock.Verify(v => v.Context.Abort(), Times.Once);
        viewerMock.Verify(v => v.Dispose(), Times.Once);

        // Verify that ReceiveAllViewers was called twice (once for add, once for remove)
        _controlClientMock.Verify(c => c.ReceiveAllViewers(It.IsAny<List<ViewerDto>>()), Times.Exactly(2));

        // Verify that an information log was written
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Viewer with connection ID {connectionId} removed successfully.")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task TryRemoveViewer_NonExistingViewer_FailsToRemove()
    {
        // Arrange
        const string connectionId = "nonexistent";

        // Act
        var result = await _appState.TryRemoveViewerAsync(connectionId);

        // Assert
        Assert.False(result);

        // Verify that an error was logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Failed to remove viewer with connection ID {connectionId}.")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);

        // Verify that ReceiveAllViewers was never called
        _controlClientMock.Verify(c => c.ReceiveAllViewers(It.IsAny<List<ViewerDto>>()), Times.Never);
    }

    #endregion

    #region GetAllViewers Tests

    [Fact]
    public void GetAllViewers_NoViewers_ReturnsEmptyList()
    {
        // Act
        var viewers = _appState.GetAllViewers();

        // Assert
        Assert.Empty(viewers);
    }

    [Fact]
    public async Task GetAllViewers_WithViewers_ReturnsCorrectList()
    {
        // Arrange
        var viewerMock1 = new Mock<IViewer>();
        var viewerMock2 = new Mock<IViewer>();
        const string connectionId1 = "conn5";
        const string connectionId2 = "conn6";
        viewerMock1.Setup(v => v.ConnectionId).Returns(connectionId1);
        viewerMock2.Setup(v => v.ConnectionId).Returns(connectionId2);

        await _appState.TryAddViewerAsync(viewerMock1.Object);
        await _appState.TryAddViewerAsync(viewerMock2.Object);

        // Act
        var viewers = _appState.GetAllViewers();

        // Assert
        Assert.Equal(2, viewers.Count);
        Assert.Contains(viewers, v => v == viewerMock1.Object);
        Assert.Contains(viewers, v => v == viewerMock2.Object);
    }

    #endregion

    #region Events Tests

    [Fact]
    public async Task ViewerAdded_EventIsTriggeredWhenViewerIsAdded()
    {
        // Arrange
        var viewerMock = new Mock<IViewer>();
        const string connectionId = "conn7";
        viewerMock.Setup(v => v.ConnectionId).Returns(connectionId);

        var eventInvoked = false;
        IViewer? addedViewer = null;

        _appState.ViewerAdded += (_, viewer) =>
        {
            eventInvoked = true;
            addedViewer = viewer;
        };

        // Act
        var result = await _appState.TryAddViewerAsync(viewerMock.Object);

        // Assert
        Assert.True(result);
        Assert.True(eventInvoked);
        Assert.Equal(viewerMock.Object, addedViewer);
    }

    [Fact]
    public async Task ViewerRemoved_EventIsTriggeredWhenViewerIsRemoved()
    {
        // Arrange
        var viewerMock = new Mock<IViewer>();
        const string connectionId = "conn8";
        viewerMock.Setup(v => v.ConnectionId).Returns(connectionId);
        viewerMock.Setup(v => v.Context).Returns(new Mock<HubCallerContext>().Object);
        viewerMock.Setup(v => v.Dispose());

        var eventInvoked = false;
        IViewer? removedViewer = null;

        _appState.ViewerRemoved += (_, viewer) =>
        {
            eventInvoked = true;
            removedViewer = viewer;
        };

        await _appState.TryAddViewerAsync(viewerMock.Object);

        // Act
        var result = await _appState.TryRemoveViewerAsync(connectionId);

        // Assert
        Assert.True(result);
        Assert.True(eventInvoked);
        Assert.Equal(viewerMock.Object, removedViewer);
    }

    #endregion

    #region NotifyViewersChanged Tests

    // These tests should not attempt to call NotifyViewersChanged directly since it's private.
    // Instead, they verify that adding/removing viewers triggers the appropriate SignalR calls.

    [Fact]
    public async Task AddingViewer_NotifiesClients()
    {
        // Arrange
        var viewerMock = new Mock<IViewer>();
        const string connectionId = "conn13";
        viewerMock.Setup(v => v.ConnectionId).Returns(connectionId);
        viewerMock.Setup(v => v.Group).Returns("Group1");
        viewerMock.Setup(v => v.UserName).Returns("User1");
        viewerMock.Setup(v => v.Role).Returns("Role1");
        viewerMock.Setup(v => v.ConnectedTime).Returns(DateTime.UtcNow);
        viewerMock.Setup(v => v.IpAddress).Returns(IPAddress.Parse("127.0.0.1"));
        viewerMock.Setup(v => v.AuthenticationType).Returns("Type1");

        // Act
        var result = await _appState.TryAddViewerAsync(viewerMock.Object);

        // Assert
        Assert.True(result);

        _controlClientMock.Verify(c => c.ReceiveAllViewers(It.Is<List<ViewerDto>>(list =>
            list.Count == 1 &&
            list[0].ConnectionId == connectionId &&
            list[0].Group == "Group1" &&
            list[0].UserName == "User1" &&
            list[0].Role == "Role1" &&
            list[0].IpAddress.ToString() == "127.0.0.1" &&
            list[0].AuthenticationType == "Type1"
        )), Times.Once);
    }

    [Fact]
    public async Task RemovingViewer_NotifiesClients()
    {
        // Arrange
        var viewerMock = new Mock<IViewer>();
        const string connectionId = "conn14";
        viewerMock.Setup(v => v.ConnectionId).Returns(connectionId);
        viewerMock.Setup(v => v.Group).Returns("Group2");
        viewerMock.Setup(v => v.UserName).Returns("User2");
        viewerMock.Setup(v => v.Role).Returns("Role2");
        viewerMock.Setup(v => v.ConnectedTime).Returns(DateTime.UtcNow.AddMinutes(-5));
        viewerMock.Setup(v => v.IpAddress).Returns(IPAddress.Parse("192.168.1.1"));
        viewerMock.Setup(v => v.AuthenticationType).Returns("Type2");
        viewerMock.Setup(v => v.Context).Returns(new Mock<HubCallerContext>().Object);
        viewerMock.Setup(v => v.Dispose());

        await _appState.TryAddViewerAsync(viewerMock.Object);

        // Act
        var result = await _appState.TryRemoveViewerAsync(connectionId);

        // Assert
        Assert.True(result);

        _controlClientMock.Verify(c => c.ReceiveAllViewers(It.Is<List<ViewerDto>>(list =>
            list.Count == 0
        )), Times.Once);
    }

    #endregion

    #region Logging Tests

    [Fact]
    public async Task TryAddViewer_FailedToAdd_LogsError()
    {
        // Arrange
        var viewerMock1 = new Mock<IViewer>();
        var viewerMock2 = new Mock<IViewer>();
        const string connectionId = "conn15";
        viewerMock1.Setup(v => v.ConnectionId).Returns(connectionId);
        viewerMock2.Setup(v => v.ConnectionId).Returns(connectionId);

        await _appState.TryAddViewerAsync(viewerMock1.Object); // First add succeeds

        // Act
        var secondAddResult = await _appState.TryAddViewerAsync(viewerMock2.Object); // Should fail

        // Assert
        Assert.False(secondAddResult);

        // Verify that an error was logged for the failed add
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Failed to add viewer with connection ID {connectionId}.")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task TryRemoveViewer_SuccessfulRemoval_LogsInformation()
    {
        // Arrange
        var viewerMock = new Mock<IViewer>();
        const string connectionId = "conn16";
        viewerMock.Setup(v => v.ConnectionId).Returns(connectionId);
        viewerMock.Setup(v => v.Context).Returns(new Mock<HubCallerContext>().Object);
        viewerMock.Setup(v => v.Dispose());

        await _appState.TryAddViewerAsync(viewerMock.Object);

        // Act
        var result = await _appState.TryRemoveViewerAsync(connectionId);

        // Assert
        Assert.True(result);

        // Verify that an information log was written
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Viewer with connection ID {connectionId} removed successfully.")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task TryRemoveViewer_FailedRemoval_LogsError()
    {
        // Arrange
        const string connectionId = "nonexistent3";

        // Act
        var result = await _appState.TryRemoveViewerAsync(connectionId);

        // Assert
        Assert.False(result);

        // Verify that an error was logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Failed to remove viewer with connection ID {connectionId}.")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }

    #endregion
}
