// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR;
using Moq;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Hubs;
using RemoteMaster.Host.Core.Services;

namespace RemoteMaster.Host.Core.Tests;

public class AppStateTests : IDisposable
{
    private readonly Mock<IHubContext<ControlHub, IControlClient>> _hubContextMock;
    private readonly Mock<IControlClient> _controlClientMock;
    private readonly AppState _appState;
    private readonly Mock<IViewer> _viewerMock;
    private bool _viewerAddedEventTriggered;
    private bool _viewerRemovedEventTriggered;

    public AppStateTests()
    {
        _hubContextMock = new Mock<IHubContext<ControlHub, IControlClient>>();
        _controlClientMock = new Mock<IControlClient>();
        var clientsMock = new Mock<IHubClients<IControlClient>>();
        clientsMock.Setup(clients => clients.All).Returns(_controlClientMock.Object);

        _hubContextMock.Setup(hub => hub.Clients).Returns(clientsMock.Object);
        _appState = new AppState(_hubContextMock.Object);

        _viewerMock = new Mock<IViewer>();
        _viewerMock.Setup(v => v.ConnectionId).Returns("testConnectionId");

        _appState.ViewerAdded += (sender, viewer) => _viewerAddedEventTriggered = true;
        _appState.ViewerRemoved += (sender, viewer) => _viewerRemovedEventTriggered = true;
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
        Assert.Throws<ArgumentNullException>(() => _appState.TryAddViewer(null));
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

    public void Dispose()
    {
        _viewerMock?.Object.Dispose();
    }
}