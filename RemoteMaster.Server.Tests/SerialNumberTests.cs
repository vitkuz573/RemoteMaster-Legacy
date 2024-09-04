// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Aggregates.CrlAggregate.ValueObjects;

namespace RemoteMaster.Server.Tests;

public class SerialNumberTests
{
    [Fact]
    public void GenerateSerialNumber_ReturnsUniqueSerialNumbers()
    {
        // Act
        var serialNumber1 = SerialNumber.Generate();
        var serialNumber2 = SerialNumber.Generate();

        // Assert
        Assert.NotNull(serialNumber1);
        Assert.NotNull(serialNumber2);
        Assert.NotEqual(serialNumber1.Value, serialNumber2.Value);
    }

    [Fact]
    public void GenerateSerialNumber_HasExpectedLength()
    {
        // Act
        var serialNumber = SerialNumber.Generate();

        // Assert
        Assert.NotNull(serialNumber);
        Assert.Equal(64, serialNumber.Value.Length);
    }

    [Fact]
    public void GenerateSerialNumber_IsRandom()
    {
        var serialNumbers = new HashSet<string>();

        for (var i = 0; i < 1000; i++)
        {
            var serialNumber = SerialNumber.Generate();
            Assert.NotNull(serialNumber);

            Assert.False(serialNumbers.Contains(serialNumber.Value), $"Duplicate serial number found at iteration {i}");
            serialNumbers.Add(serialNumber.Value);
        }
    }
}
