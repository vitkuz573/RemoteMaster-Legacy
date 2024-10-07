// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using System.Diagnostics;
using RemoteMaster.Server.Aggregates.CrlAggregate.ValueObjects;

namespace RemoteMaster.Server.Tests;

public class SerialNumberTests
{
    [Fact]
    public void GenerateSerialNumber_ReturnsUniqueSerialNumbers()
    {
        // Arrange & Act
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
        // Arrange & Act
        var serialNumber = SerialNumber.Generate();

        // Assert
        Assert.NotNull(serialNumber);
        Assert.Equal(40, serialNumber.Value.Length);
    }

    [Fact]
    public void GenerateSerialNumber_IsRandom()
    {
        // Arrange
        var serialNumbers = new HashSet<string>();

        // Act & Assert
        for (var i = 0; i < 1000; i++)
        {
            var serialNumber = SerialNumber.Generate();
            Assert.NotNull(serialNumber);
            Assert.False(serialNumbers.Contains(serialNumber.Value), $"Duplicate serial number found at iteration {i}");
            serialNumbers.Add(serialNumber.Value);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FromExistingValue_ThrowsArgumentException_WhenValueIsNullOrWhiteSpace(string? value)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => SerialNumber.FromExistingValue(value!));
    }

    [Fact]
    public void FromExistingValue_ReturnsSerialNumber_WithExpectedValue()
    {
        // Arrange
        const string expectedValue = "A12345";

        // Act
        var serialNumber = SerialNumber.FromExistingValue(expectedValue);

        // Assert
        Assert.NotNull(serialNumber);
        Assert.Equal(expectedValue, serialNumber.Value);
    }

    [Fact]
    public void ToByteArray_ReturnsCorrectByteArray()
    {
        // Arrange
        const string serialNumberString = "A1B2C3";
        var serialNumber = SerialNumber.FromExistingValue(serialNumberString);

        // Act
        var byteArray = serialNumber.ToByteArray();

        // Assert
        Assert.NotNull(byteArray);
        Assert.Equal(3, byteArray.Length); // A1B2C3 → [A1, B2, C3]
    }

    [Theory]
    [InlineData("A1B2C3", "A1B2C3", true)]
    [InlineData("A1B2C3", "D4E5F6", false)]
    public void SerialNumber_Equals_ReturnsExpectedResult(string value1, string value2, bool expected)
    {
        // Arrange
        var serialNumber1 = SerialNumber.FromExistingValue(value1);
        var serialNumber2 = SerialNumber.FromExistingValue(value2);

        // Act
        var result = serialNumber1.Equals(serialNumber2);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GenerateSerialNumber_PerformanceTest()
    {
        // Arrange
        var stopwatch = new Stopwatch();

        // Act
        stopwatch.Start();

        for (var i = 0; i < 10000; i++)
        {
            SerialNumber.Generate();
        }

        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, "Generation took too long.");
    }

    [Fact]
    public void GenerateSerialNumber_IsThreadSafe()
    {
        // Arrange
        var serialNumbers = new ConcurrentBag<string>();

        // Act
        Parallel.For(0, 10000, _ =>
        {
            var serialNumber = SerialNumber.Generate();
            serialNumbers.Add(serialNumber.Value);
        });

        // Assert
        Assert.Equal(10000, serialNumbers.Distinct().Count());
    }

    [Theory]
    [InlineData("A")] // Too short
    [InlineData(null)] // Null value
    [InlineData("")]   // Empty value
    [InlineData("   ")] // Whitespaces
    public void FromExistingValue_ThrowsArgumentException_WhenValueIsTooShortOrInvalid(string? value)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => SerialNumber.FromExistingValue(value!));
    }

    [Fact]
    public void FromExistingValue_ThrowsArgumentException_WhenValueIsTooLong()
    {
        // Arrange
        var tooLongValue = new string('A', 1000);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => SerialNumber.FromExistingValue(tooLongValue));
    }

    [Fact]
    public void SerialNumber_Equals_ReturnsFalseWhenComparingWithNull()
    {
        // Arrange
        var serialNumber = SerialNumber.FromExistingValue("A1B2C3");

        // Act & Assert
        Assert.False(serialNumber.Equals(null));
    }

    [Fact]
    public void FromExistingValue_HandlesSpecialCharactersCorrectly()
    {
        // Arrange
        const string specialCharValue = "A1B2C3-!@#";

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => SerialNumber.FromExistingValue(specialCharValue));
        Assert.Equal("Serial number must contain only hexadecimal characters.", ex.Message);
    }

    [Fact]
    public void SerialNumber_IsImmutable()
    {
        // Arrange
        var serialNumber = SerialNumber.FromExistingValue("A1B2C3");
        var originalByteArray = serialNumber.ToByteArray();
        var modifiedByteArray = (byte[])originalByteArray.Clone();

        // Act
        modifiedByteArray[0] = 0xFF;
        var byteArrayAfterChange = serialNumber.ToByteArray();

        // Assert
        Assert.Equal("A1B2C3", serialNumber.Value);
        Assert.Equal(originalByteArray, byteArrayAfterChange);
    }
}
