// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Models;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Helpers.ScreenHelper;
using RemoteMaster.Host.Windows.Services;

namespace RemoteMaster.Host.Windows.Tests;

public class GdiCapturingTests : IDisposable
{
    private readonly Mock<IAppState> _mockAppState;
    private readonly Mock<IDesktopService> _mockDesktopService;
    private readonly Mock<IOverlayManagerService> _mockOverlayManagerService;
    private readonly GdiCapturing _gdiCapturing;

    public GdiCapturingTests()
    {
        _mockAppState = new Mock<IAppState>();
        _mockDesktopService = new Mock<IDesktopService>();
        _mockOverlayManagerService = new Mock<IOverlayManagerService>();
        Mock<ILogger<ScreenCapturingService>> mockLogger = new();

        _mockAppState.Setup(a => a.Viewers).Returns(new Dictionary<string, IViewer>());

        _gdiCapturing = new GdiCapturing(_mockAppState.Object, _mockDesktopService.Object, _mockOverlayManagerService.Object, mockLogger.Object);
    }

    [Fact]
    public void GetDisplays_ShouldReturnDisplays()
    {
        // Act
        var displays = _gdiCapturing.GetDisplays();

        // Assert
        Assert.NotNull(displays);
        var displayList = displays.ToList();
        Assert.NotEmpty(displayList);

        var primaryDisplay = displayList.First(d => d.IsPrimary);
        Assert.Equal(Screen.PrimaryScreen.DeviceName, primaryDisplay.Name);
        Assert.Equal(Screen.PrimaryScreen.Bounds.Size, primaryDisplay.Resolution);
    }

    [Fact]
    public void SetSelectedScreen_ShouldSetSelectedScreen()
    {
        // Arrange
        using var viewer = new Viewer(Mock.Of<HubCallerContext>(), "Users", "Connection1", "TestUser", "TestRole", IPAddress.Loopback, "authType");

        _mockAppState.Setup(a => a.TryGetViewer("Connection1", out It.Ref<IViewer?>.IsAny))
            .Returns((string _, out IViewer? v) => { v = viewer; return true; });

        var displays = _gdiCapturing.GetDisplays().ToList();
        var newDisplay = displays.FirstOrDefault(d => !d.IsPrimary)?.Name ?? displays.First().Name;
        var selectedScreen = Mock.Of<IScreen>(s => s.DeviceName == newDisplay);

        // Act
        _gdiCapturing.SetSelectedScreen("Connection1", selectedScreen);

        // Assert
        Assert.Equal(newDisplay, viewer.CapturingContext.SelectedScreen?.DeviceName);
    }

    [Fact]
    public void GetNextFrame_ShouldReturnFrame()
    {
        // Arrange
        _mockDesktopService.Setup(ds => ds.SwitchToInputDesktop()).Returns(true);

        var primaryScreen = Screen.PrimaryScreen;
        var selectedScreenMock = new Mock<IScreen>();
        selectedScreenMock.Setup(s => s.DeviceName).Returns(primaryScreen.DeviceName);
        selectedScreenMock.Setup(s => s.Bounds).Returns(primaryScreen.Bounds);

        using var viewer = new Viewer(Mock.Of<HubCallerContext>(), "Users", "Connection1", "TestUser", "TestRole", IPAddress.Loopback, "authType");
        viewer.CapturingContext.SelectedScreen = selectedScreenMock.Object;

        _mockAppState.Setup(a => a.TryGetViewer("Connection1", out It.Ref<IViewer?>.IsAny))
            .Returns((string _, out IViewer? v) => { v = viewer; return true; });

        // Act
        var frame = _gdiCapturing.GetNextFrame("Connection1");

        // Assert
        Assert.NotNull(frame);
    }

    [Fact]
    public void GetThumbnail_ShouldReturnThumbnail()
    {
        // Arrange
        _mockDesktopService.Setup(ds => ds.SwitchToInputDesktop()).Returns(true);

        using var viewer = new Viewer(Mock.Of<HubCallerContext>(), "Users", "Connection1", "TestUser", "TestRole", IPAddress.Loopback, "authType");
        viewer.CapturingContext.SelectedScreen = Mock.Of<IScreen>(s => s.DeviceName == Screen.PrimaryScreen.DeviceName);

        _mockAppState.Setup(a => a.TryGetViewer("Connection1", out It.Ref<IViewer?>.IsAny))
            .Returns((string _, out IViewer? v) => { v = viewer; return true; });

        // Act
        var thumbnail = _gdiCapturing.GetThumbnail("Connection1");

        // Assert
        Assert.NotNull(thumbnail);
    }

    public void Dispose()
    {
        _gdiCapturing.Dispose();
    }
}
