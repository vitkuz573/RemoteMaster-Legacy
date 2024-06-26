// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Win32;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Services;

namespace RemoteMaster.Host.Windows.Tests;

public class RegistryKeyFactoryTests
{
    private readonly IRegistryKeyFactory _registryKeyFactory;

    public RegistryKeyFactoryTests()
    {
        _registryKeyFactory = new RegistryKeyFactory();
    }

    [Fact]
    public void OpenSubKey_ValidKey_ReturnsRegistryKeyWrapper()
    {
        // Arrange
        var hive = RegistryHive.CurrentUser;
        var keyPath = @"Software\Microsoft";
        var writable = false;

        // Act
        var result = _registryKeyFactory.OpenSubKey(hive, keyPath, writable);

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<IRegistryKey>(result);
    }

    [Fact]
    public void OpenSubKey_InvalidKey_ReturnsNull()
    {
        // Arrange
        var hive = RegistryHive.CurrentUser;
        var keyPath = @"Software\NonExistentKey";
        var writable = false;

        // Act
        var result = _registryKeyFactory.OpenSubKey(hive, keyPath, writable);

        // Assert
        Assert.Null(result);
    }
}