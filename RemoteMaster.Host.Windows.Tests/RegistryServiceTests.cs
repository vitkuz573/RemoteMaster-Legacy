// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Moq;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Services;

namespace RemoteMaster.Host.Windows.Tests;

public class RegistryServiceTests
{
    private readonly Mock<IRegistryKeyFactory> _mockRegistryKeyFactory;
    private readonly Mock<ILogger<RegistryService>> _mockLogger;
    private readonly Mock<IRegistryKey> _mockRegistryKey;
    private readonly RegistryService _registryService;

    public RegistryServiceTests()
    {
        _mockRegistryKeyFactory = new Mock<IRegistryKeyFactory>();
        _mockLogger = new Mock<ILogger<RegistryService>>();
        _mockRegistryKey = new Mock<IRegistryKey>();
        _registryService = new RegistryService(_mockRegistryKeyFactory.Object, _mockLogger.Object);
    }

    [Fact]
    public void OpenSubKey_ValidKey_ReturnsRegistryKeyWrapper()
    {
        // Arrange
        const RegistryHive hive = RegistryHive.CurrentUser;
        const string keyPath = @"Software\MyApp";
        _mockRegistryKeyFactory.Setup(f => f.Create(hive, keyPath, false)).Returns(_mockRegistryKey.Object);

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
        const RegistryHive hive = RegistryHive.CurrentUser;
        const string keyPath = @"Software\MyApp";
        const string valueName = "TestValue";
        const string value = "Test";
        const RegistryValueKind valueKind = RegistryValueKind.String;

        _mockRegistryKeyFactory.Setup(f => f.Create(hive, keyPath, true)).Returns(_mockRegistryKey.Object);

        // Act
        _registryService.SetValue(hive, keyPath, valueName, value, valueKind);

        // Assert
        _mockRegistryKey.Verify(k => k.SetValue(valueName, value, valueKind), Times.Once);
    }

    [Fact]
    public void GetValue_KeyExists_ReturnsValue()
    {
        // Arrange
        const RegistryHive hive = RegistryHive.CurrentUser;
        const string keyPath = @"Software\MyApp";
        const string valueName = "TestValue";
        const string expectedValue = "Test";

        _mockRegistryKey.Setup(k => k.GetValue(valueName, It.IsAny<object>())).Returns(expectedValue);
        _mockRegistryKeyFactory.Setup(f => f.Create(hive, keyPath, false)).Returns(_mockRegistryKey.Object);

        // Act
        var result = _registryService.GetValue(hive, keyPath, valueName, null!);

        // Assert
        Assert.Equal(expectedValue, result);
    }

    [Fact]
    public void GetValue_KeyDoesNotExist_ReturnsDefaultValue()
    {
        // Arrange
        const RegistryHive hive = RegistryHive.CurrentUser;
        const string keyPath = @"Software\NonExistent";
        const string valueName = "TestValue";
        const string defaultValue = "Default";

        _mockRegistryKey.Setup(k => k.GetValue(valueName, It.IsAny<object>())).Returns(defaultValue);
        _mockRegistryKeyFactory.Setup(f => f.Create(hive, keyPath, false)).Returns(_mockRegistryKey.Object);

        // Act
        var result = _registryService.GetValue(hive, keyPath, valueName, defaultValue);

        // Assert
        Assert.Equal(defaultValue, result);
    }

    [Fact]
    public void GetAllValues_ValidKey_ReturnsAllValues()
    {
        // Arrange
        const RegistryHive hive = RegistryHive.CurrentUser;
        const string keyPath = @"Software\MyApp";
        var valueNames = new[] { "TestValue1", "TestValue2" };
        var values = new object[] { "Test1", 2 };
        var valueKinds = new[] { RegistryValueKind.String, RegistryValueKind.DWord };

        _mockRegistryKey.Setup(k => k.GetValueNames()).Returns(valueNames);
        _mockRegistryKey.Setup(k => k.GetValue("TestValue1", null)).Returns(values[0]);
        _mockRegistryKey.Setup(k => k.GetValue("TestValue2", null)).Returns(values[1]);
        _mockRegistryKey.Setup(k => k.GetValueKind("TestValue1")).Returns(valueKinds[0]);
        _mockRegistryKey.Setup(k => k.GetValueKind("TestValue2")).Returns(valueKinds[1]);
        _mockRegistryKeyFactory.Setup(f => f.Create(hive, keyPath, false)).Returns(_mockRegistryKey.Object);

        // Act
        var result = _registryService.GetAllValues(hive, keyPath).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.Name == "TestValue1" && (string)r.Value! == "Test1" && r.ValueType == RegistryValueKind.String);
        Assert.Contains(result, r => r.Name == "TestValue2" && (int)r.Value! == 2 && r.ValueType == RegistryValueKind.DWord);
    }

    [Fact]
    public void GetAllValues_KeyDoesNotExist_ReturnsEmpty()
    {
        // Arrange
        const RegistryHive hive = RegistryHive.CurrentUser;
        const string keyPath = @"Software\NonExistent";

        _mockRegistryKeyFactory.Setup(f => f.Create(hive, keyPath, false)).Returns((IRegistryKey)null!);

        // Act
        var result = _registryService.GetAllValues(hive, keyPath);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void SetValue_KeyDoesNotExist_ThrowsException()
    {
        // Arrange
        const RegistryHive hive = RegistryHive.CurrentUser;
        const string keyPath = @"Software\NonExistent";
        const string valueName = "TestValue";
        const string value = "Test";
        const RegistryValueKind valueKind = RegistryValueKind.String;

        _mockRegistryKeyFactory.Setup(f => f.Create(hive, keyPath, true)).Returns((IRegistryKey)null!);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _registryService.SetValue(hive, keyPath, valueName, value, valueKind));
    }

    [Fact]
    public void OpenSubKey_KeyCannotBeOpened_ReturnsNull()
    {
        // Arrange
        const RegistryHive hive = RegistryHive.CurrentUser;
        const string keyPath = @"Software\InaccessibleKey";

        _mockRegistryKeyFactory.Setup(f => f.Create(hive, keyPath, false)).Returns((IRegistryKey)null!);

        // Act
        var result = _registryService.OpenSubKey(hive, keyPath, false);

        // Assert
        Assert.Null(result);
    }
}