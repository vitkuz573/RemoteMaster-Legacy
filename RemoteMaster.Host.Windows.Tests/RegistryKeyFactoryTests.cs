// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Win32;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Services;

namespace RemoteMaster.Host.Windows.Tests;

public class RegistryKeyFactoryTests
{
    private readonly RegistryKeyFactory _registryKeyFactory = new();

    [Fact]
    public void OpenSubKey_ValidKey_ReturnsRegistryKeyWrapper()
    {
        // Arrange
        const RegistryHive hive = RegistryHive.CurrentUser;
        const string keyPath = @"Software\Microsoft";
        const bool writable = false;

        // Act
        var result = _registryKeyFactory.Create(hive, keyPath, writable);

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<IRegistryKey>(result);
    }

    [Fact]
    public void OpenSubKey_InvalidKey_ReturnsNull()
    {
        // Arrange
        const RegistryHive hive = RegistryHive.CurrentUser;
        const string keyPath = @"Software\NonExistentKey";
        const bool writable = false;

        // Act
        var result = _registryKeyFactory.Create(hive, keyPath, writable);

        // Assert
        Assert.Null(result);
    }
}