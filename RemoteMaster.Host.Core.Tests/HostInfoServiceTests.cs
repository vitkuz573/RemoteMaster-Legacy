// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Services;

namespace RemoteMaster.Host.Core.Tests;

public class HostInfoServiceTests
{
    private readonly HostInfoService _hostInfoService;

    public HostInfoServiceTests()
    {
        _hostInfoService = new HostInfoService();
    }

    [Fact]
    public void GetHostName_ShouldReturnNonEmptyString()
    {
        // Arrange

        // Act
        var result = _hostInfoService.GetHostName();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(result));
    }

    [Fact]
    public void GetIPv4Address_ShouldReturnValidAddress()
    {
        // Arrange

        // Act
        var result = _hostInfoService.GetIPv4Address();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(result));
    }

    [Fact]
    public void GetMacAddress_ShouldReturnValidMac()
    {
        // Arrange

        // Act
        var result = _hostInfoService.GetMacAddress();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(result));
    }
}