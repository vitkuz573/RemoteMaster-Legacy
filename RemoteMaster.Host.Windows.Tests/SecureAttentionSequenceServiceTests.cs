// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Moq;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Enums;
using RemoteMaster.Host.Windows.Services;

namespace RemoteMaster.Host.Windows.Tests;

public class SecureAttentionSequenceServiceTests
{
    private readonly Mock<IRegistryService> _mockRegistryService;
    private readonly Mock<IRegistryKey> _mockRegistryKey;
    private readonly SecureAttentionSequenceService _service;

    public SecureAttentionSequenceServiceTests()
    {
        _mockRegistryService = new Mock<IRegistryService>();
        _mockRegistryKey = new Mock<IRegistryKey>();
        var _mockLogger = new Mock<ILogger<SecureAttentionSequenceService>>();
        _service = new SecureAttentionSequenceService(_mockRegistryService.Object, _mockLogger.Object);
    }

    [Fact]
    public void SasOption_Get_ReturnsCorrectValue()
    {
        // Arrange
        _mockRegistryService.Setup(r => r.OpenSubKey(RegistryHive.LocalMachine, It.IsAny<string>(), false)).Returns(_mockRegistryKey.Object);
        _mockRegistryKey.Setup(k => k.GetValue("SoftwareSASGeneration", null)).Returns((int)SoftwareSasOption.Services);

        // Act
        var result = _service.GetSasOption();

        // Assert
        Assert.Equal(SoftwareSasOption.Services, result);
    }

    [Fact]
    public void SasOption_Get_ReturnsNone_WhenValueIsNotDefined()
    {
        // Arrange
        _mockRegistryService.Setup(r => r.OpenSubKey(RegistryHive.LocalMachine, It.IsAny<string>(), false)).Returns(_mockRegistryKey.Object);
        _mockRegistryKey.Setup(k => k.GetValue("SoftwareSASGeneration", null)).Returns(999);

        // Act
        var result = _service.GetSasOption();

        // Assert
        Assert.Equal(SoftwareSasOption.None, result);
    }

    [Fact]
    public void SasOption_Set_SetsCorrectValue()
    {
        // Arrange
        _mockRegistryService.Setup(r => r.OpenSubKey(RegistryHive.LocalMachine, It.IsAny<string>(), true)).Returns(_mockRegistryKey.Object);

        // Act
        _service.SetSasOption(SoftwareSasOption.EaseOfAccessApplications);

        // Assert
        _mockRegistryKey.Verify(k => k.SetValue("SoftwareSASGeneration", (int)SoftwareSasOption.EaseOfAccessApplications, RegistryValueKind.DWord), Times.Once);
    }
}
