// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR;
using Moq;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Hubs;
using RemoteMaster.Host.Core.Services;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Core.Tests;

public class AppStateTests : IDisposable
{
    private readonly Mock<IControlClient> _controlClientMock;
    private readonly AppState _appState;
    private readonly Mock<IViewer> _viewerMock;
    private bool _viewerAddedEventTriggered;
    private bool _viewerRemovedEventTriggered;

    public AppStateTests()
    {
        Mock<IHubContext<ControlHub, IControlClient>> hubContextMock = new();
        _controlClientMock = new Mock<IControlClient>();
        var clientsMock = new Mock<IHubClients<IControlClient>>();
        clientsMock.Setup(clients => clients.All).Returns(_controlClientMock.Object);

        hubContextMock.Setup(hub => hub.Clients).Returns(clientsMock.Object);
        _appState = new AppState(hubContextMock.Object);

        Mock<HubCallerContext> contextMock = new();

        _viewerMock = new Mock<IViewer>();
        _viewerMock.Setup(v => v.ConnectionId).Returns("testConnectionId");
        _viewerMock.Setup(v => v.Context).Returns(contextMock.Object);

        _appState.ViewerAdded += (_, _) => _viewerAddedEventTriggered = true;
        _appState.ViewerRemoved += (_, _) => _viewerRemovedEventTriggered = true;
    }

    [Fact]
    public void TryAddViewer_ValidViewer_AddsViewer()
    {
        // Act
        var result = _appState.TryAddViewer(_viewerMock.Object);

        // Assert
        Assert.True(result);
        Assert.True(_viewerAddedEventTriggered);
        Assert.Contains(_viewerMock.Object, _appState.Viewers.Values);
    }

    [Fact]
    public void TryAddViewer_NullViewer_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _appState.TryAddViewer(null!));
    }

    [Fact]
    public void TryGetViewer_ExistingViewer_ReturnsTrue()
    {
        // Arrange
        _appState.TryAddViewer(_viewerMock.Object);

        // Act
        var result = _appState.TryGetViewer("testConnectionId", out var viewer);

        // Assert
        Assert.True(result);
        Assert.Equal(_viewerMock.Object, viewer);
    }

    [Fact]
    public void TryGetViewer_NonExistingViewer_ReturnsFalse()
    {
        // Act
        var result = _appState.TryGetViewer("nonExistingConnectionId", out var viewer);

        // Assert
        Assert.False(result);
        Assert.Null(viewer);
    }

    [Fact]
    public void TryRemoveViewer_ExistingViewer_RemovesViewer()
    {
        // Arrange
        _appState.TryAddViewer(_viewerMock.Object);

        // Act
        var result = _appState.TryRemoveViewer("testConnectionId");

        // Assert
        Assert.True(result);
        Assert.True(_viewerRemovedEventTriggered);
        Assert.DoesNotContain(_viewerMock.Object, _appState.Viewers.Values);
    }

    [Fact]
    public void TryRemoveViewer_NonExistingViewer_ReturnsFalse()
    {
        // Act
        var result = _appState.TryRemoveViewer("nonExistingConnectionId");

        // Assert
        Assert.False(result);
        Assert.False(_viewerRemovedEventTriggered);
    }

    [Fact]
    public void GetAllViewers_ReturnsAllViewers()
    {
        // Arrange
        _appState.TryAddViewer(_viewerMock.Object);

        // Act
        var viewers = _appState.GetAllViewers();

        // Assert
        Assert.Single(viewers);
        Assert.Contains(_viewerMock.Object, viewers);
    }

    [Fact]
    public void NotifyViewersChanged_IsCalledOnAddAndRemoveViewer()
    {
        // Arrange
        _appState.TryAddViewer(_viewerMock.Object);

        // Act
        _appState.TryRemoveViewer(_viewerMock.Object.ConnectionId);

        // Assert
        _controlClientMock.Verify(client => client.ReceiveAllViewers(It.IsAny<List<ViewerDto>>()), Times.Exactly(2));
    }

    [Fact]
    public void NotifyViewersChanged_CorrectlyFormsViewerList()
    {
        // Arrange
        _viewerMock.Setup(v => v.Group).Returns("testGroup");
        _viewerMock.Setup(v => v.UserName).Returns("testUser");
        _viewerMock.Setup(v => v.Role).Returns("testRole");
        _viewerMock.Setup(v => v.ConnectedTime).Returns(DateTime.UtcNow);
        _viewerMock.Setup(v => v.IpAddress).Returns("127.0.0.1");
        _viewerMock.Setup(v => v.AuthenticationType).Returns("testAuth");

        _appState.TryAddViewer(_viewerMock.Object);

        // Act
        _appState.TryRemoveViewer(_viewerMock.Object.ConnectionId);

        // Assert
        _controlClientMock.Verify(client => client.ReceiveAllViewers(It.Is<List<ViewerDto>>(v => v.Count == 1 && v[0].UserName == "testUser")), Times.Once);
        _controlClientMock.Verify(client => client.ReceiveAllViewers(It.Is<List<ViewerDto>>(v => v.Count == 0)), Times.Once);
    }

    [Fact]
    public void ViewerEvents_MultipleSubscribers_AllReceiveNotifications()
    {
        // Arrange
        var secondViewerAddedEventTriggered = false;
        var secondViewerRemovedEventTriggered = false;

        _appState.ViewerAdded += (_, _) => secondViewerAddedEventTriggered = true;
        _appState.ViewerRemoved += (_, _) => secondViewerRemovedEventTriggered = true;

        // Act
        _appState.TryAddViewer(_viewerMock.Object);
        _appState.TryRemoveViewer(_viewerMock.Object.ConnectionId);

        // Assert
        Assert.True(_viewerAddedEventTriggered);
        Assert.True(secondViewerAddedEventTriggered);
        Assert.True(_viewerRemovedEventTriggered);
        Assert.True(secondViewerRemovedEventTriggered);
    }

    [Fact]
    public void TryRemoveViewer_NonExistingViewer_DoesNotThrow()
    {
        // Act & Assert
        var exception = Record.Exception(() => _appState.TryRemoveViewer("nonExistingConnectionId"));
        Assert.Null(exception);
    }

    public void Dispose()
    {
        _viewerMock.Object.Dispose();
    }
}
