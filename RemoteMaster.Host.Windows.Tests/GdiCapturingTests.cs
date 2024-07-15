// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Drawing;
using Moq;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Helpers.ScreenHelper;
using RemoteMaster.Host.Windows.Services;

namespace RemoteMaster.Host.Windows.Tests;

public class GdiCapturingTests : IDisposable
{
    private readonly Mock<ICursorRenderService> _mockCursorRenderService;
    private readonly Mock<IDesktopService> _mockDesktopService;
    private readonly GdiCapturing _gdiCapturing;

    public GdiCapturingTests()
    {
        _mockCursorRenderService = new Mock<ICursorRenderService>();
        _mockDesktopService = new Mock<IDesktopService>();

        _mockCursorRenderService.Setup(crs => crs.DrawCursor(It.IsAny<Graphics>(), It.IsAny<Rectangle>()));
        _mockCursorRenderService.Setup(crs => crs.ClearCache());

        _gdiCapturing = new GdiCapturing(_mockCursorRenderService.Object, _mockDesktopService.Object);
    }

    [Fact]
    public void GetDisplays_ShouldReturnDisplays()
    {
        var displays = _gdiCapturing.GetDisplays();

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
        var displays = _gdiCapturing.GetDisplays().ToList();
        var newDisplay = displays.FirstOrDefault(d => !d.IsPrimary)?.Name ?? displays.First().Name;

        _gdiCapturing.SetSelectedScreen(newDisplay);

        Assert.Equal(newDisplay, _gdiCapturing.SelectedScreen);
    }

    [Fact]
    public void GetNextFrame_ShouldReturnFrame()
    {
        _mockDesktopService.Setup(ds => ds.SwitchToInputDesktop()).Returns(true);

        var frame = _gdiCapturing.GetNextFrame();

        Assert.NotNull(frame);
    }

    [Fact]
    public void GetThumbnail_ShouldReturnThumbnail()
    {
        _mockDesktopService.Setup(ds => ds.SwitchToInputDesktop()).Returns(true);

        var thumbnail = _gdiCapturing.GetThumbnail(100, 100);

        Assert.NotNull(thumbnail);
    }

    public void Dispose()
    {
        _gdiCapturing?.Dispose();
    }
}