// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Drawing;
using Moq;
using RemoteMaster.Host.Windows.Abstractions;

namespace RemoteMaster.Host.Windows.Tests;

public class ScreenCapturerServiceTests : IDisposable
{
    private readonly Mock<IDesktopService> _mockDesktopService;
    private readonly TestScreenCapturerService _screenCapturerService;

    public ScreenCapturerServiceTests()
    {
        _mockDesktopService = new Mock<IDesktopService>();
        _screenCapturerService = new TestScreenCapturerService(_mockDesktopService.Object);
    }

    [Fact]
    public void GetDisplays_ShouldReturnDisplays()
    {
        var displays = _screenCapturerService.GetDisplays();

        Assert.NotNull(displays);
        var display = displays.First();
        Assert.Equal("Test Display", display.Name);
        Assert.True(display.IsPrimary);
        Assert.Equal(new Size(1920, 1080), display.Resolution);
    }

    [Fact]
    public void SetSelectedScreen_ShouldSetSelectedScreen()
    {
        _screenCapturerService.SetSelectedScreen("New Display");

        Assert.Equal("New Display", _screenCapturerService.SelectedScreen);
    }

    [Fact]
    public void GetNextFrame_ShouldReturnFrame()
    {
        _mockDesktopService.Setup(ds => ds.SwitchToInputDesktop()).Returns(true);

        var frame = _screenCapturerService.GetNextFrame();

        Assert.NotNull(frame);
    }

    [Fact]
    public void GetThumbnail_ShouldReturnThumbnail()
    {
        _mockDesktopService.Setup(ds => ds.SwitchToInputDesktop()).Returns(true);

        var thumbnail = _screenCapturerService.GetThumbnail(100, 100);

        Assert.NotNull(thumbnail);
    }

    public void Dispose()
    {
        _screenCapturerService?.Dispose();
    }
}