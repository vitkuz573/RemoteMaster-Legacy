// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using Microsoft.Win32;
using Moq;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Services;

namespace RemoteMaster.Host.Windows.Tests;

public class WoLConfiguratorServiceTests
{
    [Fact]
    public void DisableFastStartup_SetsRegistryValue()
    {
        // Arrange
        var mockRegistryService = new Mock<IRegistryService>();
        var mockProcessService = new Mock<IProcessService>();
        var service = new WoLConfiguratorService(mockRegistryService.Object, mockProcessService.Object);

        // Act
        service.DisableFastStartup();

        // Assert
        mockRegistryService.Verify(r => r.SetValue(@"SYSTEM\CurrentControlSet\Control\Session Manager\Power", "HiberbootEnabled", 0, RegistryValueKind.DWord), Times.Once);
    }

    [Fact]
    public void DisablePnPEnergySaving_SetsRegistryValuesForAllAdapters()
    {
        // Arrange
        var mockRegistryService = new Mock<IRegistryService>();
        var mockProcessService = new Mock<IProcessService>();
        var mockKey = new Mock<IRegistryKey>();

        mockRegistryService.Setup(r => r.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4D36E972-E325-11CE-BFC1-08002BE10318}", true)).Returns(mockKey.Object);
        mockKey.Setup(k => k.GetSubKeyNames()).Returns(["0001", "0002"]);

        var service = new WoLConfiguratorService(mockRegistryService.Object, mockProcessService.Object);

        // Act
        service.DisablePnPEnergySaving();

        // Assert
        mockRegistryService.Verify(r => r.SetValue(@"SYSTEM\CurrentControlSet\Control\Class\{4D36E972-E325-11CE-BFC1-08002BE10318}\0001", "PnPCapabilities", 0, RegistryValueKind.DWord), Times.Once);
        mockRegistryService.Verify(r => r.SetValue(@"SYSTEM\CurrentControlSet\Control\Class\{4D36E972-E325-11CE-BFC1-08002BE10318}\0002", "PnPCapabilities", 0, RegistryValueKind.DWord), Times.Once);
    }

    [Fact]
    public void EnableWakeOnLanForAllAdapters_EnablesWakeOnLanForAllDevices()
    {
        // Arrange
        var mockRegistryService = new Mock<IRegistryService>();
        var mockProcessService = new Mock<IProcessService>();
        var mockProcess = new Mock<Process>();

        mockProcessService.Setup(p => p.Start(It.Is<ProcessStartInfo>(info => info.FileName == "powercfg.exe" && info.Arguments == "/devicequery wake_programmable"))).Returns(mockProcess.Object);
        mockProcessService.Setup(p => p.ReadStandardOutput(mockProcess.Object)).Returns("Device1\r\nDevice2");

        var service = new WoLConfiguratorService(mockRegistryService.Object, mockProcessService.Object);

        // Act
        service.EnableWakeOnLanForAllAdapters();

        // Assert
        mockProcessService.Verify(p => p.Start(It.Is<ProcessStartInfo>(info => info.FileName == "powercfg.exe" && info.Arguments == "/deviceenablewake \"Device1\"")), Times.Once);
        mockProcessService.Verify(p => p.Start(It.Is<ProcessStartInfo>(info => info.FileName == "powercfg.exe" && info.Arguments == "/deviceenablewake \"Device2\"")), Times.Once);
    }
}
