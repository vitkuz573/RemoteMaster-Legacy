// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Win32;
using Moq;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Services;

namespace RemoteMaster.Host.Windows.Tests;

public class RegistryServiceTests
{
    private readonly Mock<IRegistryKeyFactory> _mockRegistryKeyFactory;
    private readonly Mock<IRegistryKey> _mockRegistryKey;
    private readonly RegistryService _registryService;

    public RegistryServiceTests()
    {
        _mockRegistryKeyFactory = new Mock<IRegistryKeyFactory>();
        _mockRegistryKey = new Mock<IRegistryKey>();
        _registryService = new RegistryService(_mockRegistryKeyFactory.Object);
    }

    [Fact]
    public void OpenSubKey_ValidKey_ReturnsRegistryKeyWrapper()
    {
        // Arrange
        var hive = RegistryHive.CurrentUser;
        var keyPath = @"Software\MyApp";
        _mockRegistryKeyFactory.Setup(f => f.OpenSubKey(hive, keyPath, false)).Returns(_mockRegistryKey.Object);

        // Act
        var result = _registryService.OpenSubKey(hive, keyPath, false);

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<IRegistryKey>(result);
    }

    [Fact]
    public void SetValue_ValidParameters_SetsRegistryValue()
    {
        // Arrange
        var hive = RegistryHive.CurrentUser;
        var keyPath = @"Software\MyApp";
        var valueName = "TestValue";
        var value = "Test";
        var valueKind = RegistryValueKind.String;

        _mockRegistryKeyFactory.Setup(f => f.OpenSubKey(hive, keyPath, true)).Returns(_mockRegistryKey.Object);

        // Act
        _registryService.SetValue(hive, keyPath, valueName, value, valueKind);

        // Assert
        _mockRegistryKey.Verify(k => k.SetValue(valueName, value, valueKind), Times.Once);
    }

    [Fact]
    public void GetValue_KeyExists_ReturnsValue()
    {
        // Arrange
        var hive = RegistryHive.CurrentUser;
        var keyPath = @"Software\MyApp";
        var valueName = "TestValue";
        var expectedValue = "Test";

        _mockRegistryKey.Setup(k => k.GetValue(valueName, It.IsAny<object>())).Returns(expectedValue);
        _mockRegistryKeyFactory.Setup(f => f.OpenSubKey(hive, keyPath, false)).Returns(_mockRegistryKey.Object);

        // Act
        var result = _registryService.GetValue(hive, keyPath, valueName, null);

        // Assert
        Assert.Equal(expectedValue, result);
    }

    [Fact]
    public void GetValue_KeyDoesNotExist_ReturnsDefaultValue()
    {
        // Arrange
        var hive = RegistryHive.CurrentUser;
        var keyPath = @"Software\NonExistent";
        var valueName = "TestValue";
        var defaultValue = "Default";

        _mockRegistryKey.Setup(k => k.GetValue(valueName, It.IsAny<object>())).Returns(defaultValue);
        _mockRegistryKeyFactory.Setup(f => f.OpenSubKey(hive, keyPath, false)).Returns(_mockRegistryKey.Object);

        // Act
        var result = _registryService.GetValue(hive, keyPath, valueName, defaultValue);

        // Assert
        Assert.Equal(defaultValue, result);
    }
}