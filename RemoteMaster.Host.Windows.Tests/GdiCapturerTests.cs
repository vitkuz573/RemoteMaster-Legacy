// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Drawing;
using Moq;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Helpers.ScreenHelper;
using RemoteMaster.Host.Windows.Services;

namespace RemoteMaster.Host.Windows.Tests;

public class GdiCapturerTests : IDisposable
{
    private readonly Mock<ICursorRenderService> _mockCursorRenderService;
    private readonly Mock<IDesktopService> _mockDesktopService;
    private readonly GdiCapturer _gdiCapturer;

    public GdiCapturerTests()
    {
        _mockCursorRenderService = new Mock<ICursorRenderService>();
        _mockDesktopService = new Mock<IDesktopService>();

        _mockCursorRenderService.Setup(crs => crs.DrawCursor(It.IsAny<Graphics>(), It.IsAny<Rectangle>()));

        _gdiCapturer = new GdiCapturer(_mockCursorRenderService.Object, _mockDesktopService.Object);
    }

    [Fact]
    public void GetDisplays_ShouldReturnDisplays()
    {
        var displays = _gdiCapturer.GetDisplays();

        Assert.NotNull(displays);
        var displayList = displays.ToList();
        Assert.True(displayList.Count > 0);

        var primaryDisplay = displayList.First(d => d.IsPrimary);
        Assert.Equal(Screen.PrimaryScreen.DeviceName, primaryDisplay.Name);
        Assert.Equal(Screen.PrimaryScreen.Bounds.Size, primaryDisplay.Resolution);
    }

    [Fact]
    public void SetSelectedScreen_ShouldSetSelectedScreen()
    {
        var displays = _gdiCapturer.GetDisplays().ToList();
        var newDisplay = displays.FirstOrDefault(d => !d.IsPrimary)?.Name ?? displays.First().Name;

        _gdiCapturer.SetSelectedScreen(newDisplay);

        Assert.Equal(newDisplay, _gdiCapturer.SelectedScreen);
    }

    [Fact]
    public void GetNextFrame_ShouldReturnFrame()
    {
        _mockDesktopService.Setup(ds => ds.SwitchToInputDesktop()).Returns(true);

        var frame = _gdiCapturer.GetNextFrame();

        Assert.NotNull(frame);
    }

    [Fact]
    public void GetThumbnail_ShouldReturnThumbnail()
    {
        _mockDesktopService.Setup(ds => ds.SwitchToInputDesktop()).Returns(true);

        var thumbnail = _gdiCapturer.GetThumbnail(100, 100);

        Assert.NotNull(thumbnail);
    }

    public void Dispose()
    {
        _gdiCapturer?.Dispose();
    }
}