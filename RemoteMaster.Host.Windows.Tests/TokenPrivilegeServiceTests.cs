// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Principal;
using RemoteMaster.Host.Windows.Services;

namespace RemoteMaster.Host.Windows.Tests;

public class TokenPrivilegeServiceTests
{
    private readonly TokenPrivilegeService _tokenPrivilegeService;

    public TokenPrivilegeServiceTests()
    {
        _tokenPrivilegeService = new();
    }

    [Fact]
    public void AdjustPrivilege_WithValidPrivilege_ShouldReturnTrue_IfRunAsAdmin()
    {
        // Arrange
        var privilegeName = "SeShutdownPrivilege";

        // Only run this test if the user has administrative privileges
        if (!IsUserAnAdmin())
        {
            Console.WriteLine("Test skipped because it requires administrative privileges.");
            return;
        }

        // Act
        var result = _tokenPrivilegeService.AdjustPrivilege(privilegeName);

        // Assert
        Assert.True(result, $"AdjustPrivilege should return true when attempting to adjust '{privilegeName}'.");
    }

    [Fact]
    public void AdjustPrivilege_WithInvalidPrivilege_ShouldReturnFalse()
    {
        // Arrange
        var privilegeName = "InvalidPrivilege";

        // Act
        var result = _tokenPrivilegeService.AdjustPrivilege(privilegeName);

        // Assert
        Assert.False(result, "AdjustPrivilege should return false when an invalid privilege name is provided.");
    }

    [Fact]
    public void AdjustPrivilege_ShouldThrowArgumentException_IfPrivilegeNameIsEmpty()
    {
        // Arrange
        var privilegeName = "";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _tokenPrivilegeService.AdjustPrivilege(privilegeName));
        Assert.Equal("privilegeName", exception.ParamName);
    }

    private static bool IsUserAnAdmin()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);

        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
}
