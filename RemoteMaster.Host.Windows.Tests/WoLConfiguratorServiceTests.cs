// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Moq;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Services;

namespace RemoteMaster.Host.Windows.Tests;

public class WoLConfiguratorServiceTests
{
    private readonly Mock<IRegistryService> _mockRegistryService;
    private readonly Mock<IProcessService> _mockProcessService;
    private readonly WoLConfiguratorService _service;

    public WoLConfiguratorServiceTests()
    {
        _mockRegistryService = new Mock<IRegistryService>();
        _mockProcessService = new Mock<IProcessService>();
        Mock<ILogger<WoLConfiguratorService>> mockLogger = new();
        _service = new WoLConfiguratorService(_mockRegistryService.Object, _mockProcessService.Object, mockLogger.Object);
    }

    [Fact]
    public void DisableFastStartup_SetsRegistryValue()
    {
        // Act
        _service.DisableFastStartup();

        // Assert
        _mockRegistryService.Verify(r => r.SetValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Power", "HiberbootEnabled", 0, RegistryValueKind.DWord), Times.Once);
    }

    [Fact]
    public void DisablePnPEnergySaving_SetsRegistryValuesForAllAdapters()
    {
        // Arrange
        var mockKey = new Mock<IRegistryKey>();
        _mockRegistryService.Setup(r => r.OpenSubKey(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Class\{4D36E972-E325-11CE-BFC1-08002BE10318}", true)).Returns(mockKey.Object);
        mockKey.Setup(k => k.GetSubKeyNames()).Returns(["0001", "0002"]);

        // Act
        _service.DisablePnPEnergySaving();

        // Assert
        _mockRegistryService.Verify(r => r.SetValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Class\{4D36E972-E325-11CE-BFC1-08002BE10318}\0001", "PnPCapabilities", 0, RegistryValueKind.DWord), Times.Once);
        _mockRegistryService.Verify(r => r.SetValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Class\{4D36E972-E325-11CE-BFC1-08002BE10318}\0002", "PnPCapabilities", 0, RegistryValueKind.DWord), Times.Once);
    }

    [Fact]
    public async Task EnableWakeOnLanForAllAdapters_EnablesWakeOnLanForAllDevices()
    {
        // Arrange
        var mockProcessWrapper = new Mock<IProcessWrapper>();
        _mockProcessService.Setup(p => p.Start(It.Is<ProcessStartInfo>(info => info.FileName == "powercfg.exe" && info.Arguments == "/devicequery wake_programmable"))).Returns(mockProcessWrapper.Object);
        _mockProcessService.Setup(p => p.ReadStandardOutputAsync(mockProcessWrapper.Object)).ReturnsAsync("Device1\r\nDevice2");

        // Act
        await _service.EnableWakeOnLanForAllAdaptersAsync();

        // Assert
        _mockProcessService.Verify(p => p.Start(It.Is<ProcessStartInfo>(info => info.FileName == "powercfg.exe" && info.Arguments == "/deviceenablewake \"Device1\"")), Times.Once);
        _mockProcessService.Verify(p => p.Start(It.Is<ProcessStartInfo>(info => info.FileName == "powercfg.exe" && info.Arguments == "/deviceenablewake \"Device2\"")), Times.Once);
    }
}
