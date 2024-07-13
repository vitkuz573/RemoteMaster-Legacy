// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Drawing;
using Moq;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Services;
using RemoteMaster.Shared.Dtos;

namespace RemoteMaster.Host.Windows.Tests;

public class InputServiceTests
{
    private readonly Mock<IDesktopService> _mockDesktopService;
    private readonly Mock<IScreenCapturerService> _mockScreenCapturerService;
    private readonly InputService _inputService;

    public InputServiceTests()
    {
        _mockDesktopService = new Mock<IDesktopService>();
        _mockScreenCapturerService = new Mock<IScreenCapturerService>();
        _inputService = new InputService(_mockDesktopService.Object);
    }

    [Fact]
    public void Start_ShouldStartWorkerThread()
    {
        // Act
        _inputService.Start();

        // Assert
        Assert.NotNull(_inputService);
        _inputService.Stop();
    }

    [Fact]
    public void Stop_ShouldDisposeResources()
    {
        // Act
        _inputService.Start();
        _inputService.Stop();

        // Ensure the service is disposed
        Assert.Throws<ObjectDisposedException>(() => _inputService.HandleKeyboardInput(new KeyboardInputDto { Code = "KeyA", IsPressed = true }));
    }

    [Fact]
    public void BlockUserInput_ShouldCallDesktopService_WhenInputEnabled()
    {
        // Arrange
        _inputService.InputEnabled = true;
        _inputService.Start();

        // Act
        _inputService.BlockUserInput = true;

        // Assert
        _mockDesktopService.Verify(d => d.SwitchToInputDesktop(), Times.AtLeastOnce);
        _inputService.Stop();
    }

    [Fact]
    public void HandleMouseInput_ShouldEnqueueOperation()
    {
        // Arrange
        var mouseInputDto = new MouseInputDto
        {
            Position = new PointF(0.5f, 0.5f),
            IsPressed = true,
            Button = 0
        };

        _mockScreenCapturerService.Setup(s => s.CurrentScreenBounds).Returns(new Rectangle(0, 0, 1920, 1080));
        _mockScreenCapturerService.Setup(s => s.VirtualScreenBounds).Returns(new Rectangle(0, 0, 1920, 1080));

        // Act
        _inputService.InputEnabled = true;
        _inputService.Start();
        _inputService.HandleMouseInput(mouseInputDto, _mockScreenCapturerService.Object);

        // Assert
        _mockDesktopService.Verify(d => d.SwitchToInputDesktop(), Times.AtLeastOnce);
        _inputService.Stop();
    }

    [Fact]
    public void HandleKeyboardInput_ShouldEnqueueOperation()
    {
        // Arrange
        var keyboardInputDto = new KeyboardInputDto { Code = "KeyA", IsPressed = true };

        // Act
        _inputService.InputEnabled = true;
        _inputService.Start();
        _inputService.HandleKeyboardInput(keyboardInputDto);

        // Assert
        _mockDesktopService.Verify(d => d.SwitchToInputDesktop(), Times.AtLeastOnce);
        _inputService.Stop();
    }
}