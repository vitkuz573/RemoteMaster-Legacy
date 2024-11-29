// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Moq;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Services;

namespace RemoteMaster.Host.Core.Tests;

public class OverlayManagerServiceTests
{
    private readonly Mock<IScreenOverlay> _mockOverlay1 = new();
    private readonly Mock<IScreenOverlay> _mockOverlay2 = new();
    private readonly OverlayManagerService _service;

    public OverlayManagerServiceTests()
    {
        _mockOverlay1.SetupGet(o => o.Name).Returns("Overlay1");
        _mockOverlay2.SetupGet(o => o.Name).Returns("Overlay2");

        _service = new OverlayManagerService([_mockOverlay1.Object, _mockOverlay2.Object]);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldInitializeServiceWithOverlays()
    {
        // Act
        var activeOverlays = _service.ActiveOverlays;

        // Assert
        Assert.Empty(activeOverlays);
    }

    #endregion

    #region IsOverlayActive Tests

    [Fact]
    public void IsOverlayActive_ShouldReturnFalse_WhenOverlayIsNotActive()
    {
        // Act
        var result = _service.IsOverlayActive("Overlay1");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsOverlayActive_ShouldReturnTrue_WhenOverlayIsActive()
    {
        // Arrange
        _service.ActivateOverlay("Overlay1");

        // Act
        var result = _service.IsOverlayActive("Overlay1");

        // Assert
        Assert.True(result);
    }

    #endregion

    #region ActivateOverlay Tests

    [Fact]
    public void ActivateOverlay_ShouldActivateOverlay_WhenOverlayExists()
    {
        // Act
        _service.ActivateOverlay("Overlay1");

        // Assert
        Assert.Contains(_mockOverlay1.Object, _service.ActiveOverlays);
    }

    [Fact]
    public void ActivateOverlay_ShouldNotActivateOverlay_WhenOverlayDoesNotExist()
    {
        // Act
        _service.ActivateOverlay("NonExistentOverlay");

        // Assert
        Assert.Empty(_service.ActiveOverlays);
    }

    [Fact]
    public void ActivateOverlay_ShouldNotAddDuplicateOverlay_WhenOverlayIsAlreadyActive()
    {
        // Arrange
        _service.ActivateOverlay("Overlay1");

        // Act
        _service.ActivateOverlay("Overlay1");

        // Assert
        Assert.Single(_service.ActiveOverlays);
    }

    #endregion

    #region DeactivateOverlay Tests

    [Fact]
    public void DeactivateOverlay_ShouldDeactivateOverlay_WhenOverlayIsActive()
    {
        // Arrange
        _service.ActivateOverlay("Overlay1");

        // Act
        _service.DeactivateOverlay("Overlay1");

        // Assert
        Assert.DoesNotContain(_mockOverlay1.Object, _service.ActiveOverlays);
    }

    [Fact]
    public void DeactivateOverlay_ShouldDoNothing_WhenOverlayIsNotActive()
    {
        // Act
        _service.DeactivateOverlay("Overlay1");

        // Assert
        Assert.Empty(_service.ActiveOverlays);
    }

    [Fact]
    public void DeactivateOverlay_ShouldDoNothing_WhenOverlayDoesNotExist()
    {
        // Act
        _service.DeactivateOverlay("NonExistentOverlay");

        // Assert
        Assert.Empty(_service.ActiveOverlays);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ActivateOverlay_ShouldHandleCaseInsensitiveNames()
    {
        // Act
        _service.ActivateOverlay("overlay1");

        // Assert
        Assert.Contains(_mockOverlay1.Object, _service.ActiveOverlays);
    }

    [Fact]
    public void DeactivateOverlay_ShouldHandleCaseInsensitiveNames()
    {
        // Arrange
        _service.ActivateOverlay("Overlay1");

        // Act
        _service.DeactivateOverlay("overlay1");

        // Assert
        Assert.DoesNotContain(_mockOverlay1.Object, _service.ActiveOverlays);
    }

    [Fact]
    public void ActivateOverlay_ShouldNotThrow_WhenOverlayCollectionIsEmpty()
    {
        // Arrange
        var service = new OverlayManagerService([]);

        // Act & Assert
        var exception = Record.Exception(() => service.ActivateOverlay("Overlay1"));
        Assert.Null(exception);
    }

    [Fact]
    public void DeactivateOverlay_ShouldNotThrow_WhenOverlayCollectionIsEmpty()
    {
        // Arrange
        var service = new OverlayManagerService([]);

        // Act & Assert
        var exception = Record.Exception(() => service.DeactivateOverlay("Overlay1"));
        Assert.Null(exception);
    }

    #endregion
}
