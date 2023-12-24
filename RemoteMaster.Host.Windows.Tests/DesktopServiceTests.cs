// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Windows.Services;

namespace RemoteMaster.Host.Windows.Tests;

public class DesktopServiceTests
{
    private readonly DesktopService _desktopService;

    public DesktopServiceTests()
    {
        _desktopService = new();
    }

    [Fact]
    public void GetCurrentDesktop_ShouldReturnTrue_IfDesktopNameIsRetrieved()
    {
        // Act
        var result = _desktopService.GetCurrentDesktop(out var desktopName);

        // Assert
        Assert.True(result, "GetCurrentDesktop should return true when desktop name is successfully retrieved.");
        Assert.NotNull(desktopName);
    }

    [Fact]
    public void SwitchToInputDesktop_ShouldReturnTrue_OnSuccess()
    {
        // Act
        var result = _desktopService.SwitchToInputDesktop();

        // Assert
        Assert.True(result, "SwitchToInputDesktop should return true when switching is successful.");
    }
}
