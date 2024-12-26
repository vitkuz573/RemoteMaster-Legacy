// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Drawing;
using System.Net;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Models;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Services;

namespace RemoteMaster.Host.Windows.Tests;

public class GdiCapturingTests
{
    private readonly Mock<IAppState> _mockAppState;
    private readonly Mock<IDesktopService> _mockDesktopService;
    private readonly Mock<IOverlayManagerService> _mockOverlayManagerService;
    private readonly Mock<IScreenProvider> _mockScreenProvider;
    private readonly GdiCapturing _gdiCapturing;

    public GdiCapturingTests()
    {
        _mockAppState = new Mock<IAppState>();
        _mockDesktopService = new Mock<IDesktopService>();
        _mockOverlayManagerService = new Mock<IOverlayManagerService>();
        _mockScreenProvider = new Mock<IScreenProvider>();
        Mock<ILogger<ScreenCapturingService>> mockLogger = new();

        _mockAppState.Setup(a => a.GetAllViewers()).Returns([]);

        _gdiCapturing = new GdiCapturing(_mockAppState.Object, _mockDesktopService.Object, _mockOverlayManagerService.Object, _mockScreenProvider.Object, mockLogger.Object);
    }

    [Fact]
    public void GetDisplays_ShouldReturnDisplays()
    {
        // Arrange
        var mockPrimaryScreen = new Mock<IScreen>();
        mockPrimaryScreen.Setup(s => s.Primary).Returns(true);
        mockPrimaryScreen.Setup(s => s.DeviceName).Returns("PrimaryScreen");
        mockPrimaryScreen.Setup(s => s.Bounds).Returns(new Rectangle(0, 0, 1920, 1080));

        var mockSecondaryScreen = new Mock<IScreen>();
        mockSecondaryScreen.Setup(s => s.Primary).Returns(false);
        mockSecondaryScreen.Setup(s => s.DeviceName).Returns("SecondaryScreen");
        mockSecondaryScreen.Setup(s => s.Bounds).Returns(new Rectangle(1920, 0, 1280, 720));

        var mockVirtualScreen = new Mock<IScreen>();
        mockVirtualScreen.Setup(s => s.DeviceName).Returns("VirtualScreen");
        mockVirtualScreen.Setup(s => s.Bounds).Returns(new Rectangle(0, 0, 3200, 1080));

        var mockScreens = new List<IScreen>
        {
            mockPrimaryScreen.Object,
            mockSecondaryScreen.Object
        };

        _mockScreenProvider.Setup(sp => sp.GetAllScreens()).Returns(mockScreens);
        _mockScreenProvider.Setup(sp => sp.GetVirtualScreen()).Returns(mockVirtualScreen.Object);

        // Act
        var displays = _gdiCapturing.GetDisplays();

        // Assert
        Assert.NotNull(displays);
        var displayList = displays.ToList();
        Assert.NotEmpty(displayList);
        Assert.Equal(3, displayList.Count);

        var primaryDisplay = displayList.First(d => d.IsPrimary);
        Assert.Equal("PrimaryScreen", primaryDisplay.Name);
        Assert.Equal(new Size(1920, 1080), primaryDisplay.Resolution);
    }

    [Fact]
    public void SetSelectedScreen_ShouldSetSelectedScreen()
    {
        // Arrange
        var mockPrimaryScreen = new Mock<IScreen>();
        mockPrimaryScreen.Setup(s => s.Primary).Returns(true);
        mockPrimaryScreen.Setup(s => s.DeviceName).Returns("PrimaryScreen");
        mockPrimaryScreen.Setup(s => s.Bounds).Returns(new Rectangle(0, 0, 1920, 1080));

        var mockSecondaryScreen = new Mock<IScreen>();
        mockSecondaryScreen.Setup(s => s.Primary).Returns(false);
        mockSecondaryScreen.Setup(s => s.DeviceName).Returns("SecondaryScreen");
        mockSecondaryScreen.Setup(s => s.Bounds).Returns(new Rectangle(1920, 0, 1280, 720));

        var mockVirtualScreen = new Mock<IScreen>();
        mockVirtualScreen.Setup(s => s.DeviceName).Returns("VirtualScreen");
        mockVirtualScreen.Setup(s => s.Bounds).Returns(new Rectangle(0, 0, 3200, 1080));

        var mockScreens = new List<IScreen>
        {
            mockPrimaryScreen.Object,
            mockSecondaryScreen.Object
        };

        _mockScreenProvider.Setup(sp => sp.GetAllScreens()).Returns(mockScreens);
        _mockScreenProvider.Setup(sp => sp.GetVirtualScreen()).Returns(mockVirtualScreen.Object);

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
}
