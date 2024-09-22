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
        var serialNumber1 = SerialNumber.Generate();
        var serialNumber2 = SerialNumber.Generate();

        Assert.NotNull(serialNumber1);
        Assert.NotNull(serialNumber2);
        Assert.NotEqual(serialNumber1.Value, serialNumber2.Value);
    }

    [Fact]
    public void GenerateSerialNumber_HasExpectedLength()
    {
        var serialNumber = SerialNumber.Generate();

        Assert.NotNull(serialNumber);
        Assert.Equal(40, serialNumber.Value.Length);
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

    [Fact]
    public void FromExistingValue_ThrowsArgumentException_WhenValueIsNullOrWhiteSpace()
    {
        Assert.Throws<ArgumentException>(() => SerialNumber.FromExistingValue(null!));
        Assert.Throws<ArgumentException>(() => SerialNumber.FromExistingValue(""));
        Assert.Throws<ArgumentException>(() => SerialNumber.FromExistingValue("   "));
    }

    [Fact]
    public void FromExistingValue_ReturnsSerialNumber_WithExpectedValue()
    {
        var expectedValue = "A12345";
        var serialNumber = SerialNumber.FromExistingValue(expectedValue);

        Assert.NotNull(serialNumber);
        Assert.Equal(expectedValue, serialNumber.Value);
    }

    [Fact]
    public void ToByteArray_ReturnsCorrectByteArray()
    {
        var serialNumberString = "A1B2C3";
        var serialNumber = SerialNumber.FromExistingValue(serialNumberString);

        var byteArray = serialNumber.ToByteArray();

        Assert.NotNull(byteArray);
        Assert.Equal(3, byteArray.Length); // A1B2C3 → [A1, B2, C3]
    }

    [Fact]
    public void SerialNumber_Equals_ReturnsTrueForEqualValues()
    {
        var serialNumber1 = SerialNumber.FromExistingValue("A1B2C3");
        var serialNumber2 = SerialNumber.FromExistingValue("A1B2C3");

        Assert.True(serialNumber1.Equals(serialNumber2));
        Assert.Equal(serialNumber1.GetHashCode(), serialNumber2.GetHashCode());
    }

    [Fact]
    public void SerialNumber_Equals_ReturnsFalseForDifferentValues()
    {
        var serialNumber1 = SerialNumber.FromExistingValue("A1B2C3");
        var serialNumber2 = SerialNumber.FromExistingValue("D4E5F6");

        Assert.False(serialNumber1.Equals(serialNumber2));
    }

    [Fact]
    public void GenerateSerialNumber_PerformanceTest()
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        for (var i = 0; i < 10000; i++)
        {
            SerialNumber.Generate();
        }

        stopwatch.Stop();
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, "Generation took too long.");
    }

    [Fact]
    public void GenerateSerialNumber_IsThreadSafe()
    {
        var serialNumbers = new ConcurrentBag<string>();

        Parallel.For(0, 10000, i =>
        {
            var serialNumber = SerialNumber.Generate();
            serialNumbers.Add(serialNumber.Value);
        });

        Assert.Equal(10000, serialNumbers.Distinct().Count());
    }

    [Fact]
    public void FromExistingValue_ThrowsArgumentException_WhenValueIsTooShortOrTooLong()
    {
        var tooShortValue = "A";
        var tooLongValue = new string('A', 1000);

        Assert.Throws<ArgumentException>(() => SerialNumber.FromExistingValue(tooShortValue));
        Assert.Throws<ArgumentException>(() => SerialNumber.FromExistingValue(tooLongValue));
    }

    [Fact]
    public void SerialNumber_Equals_ReturnsFalseWhenComparingWithNull()
    {
        var serialNumber = SerialNumber.FromExistingValue("A1B2C3");

        Assert.False(serialNumber.Equals(null));
    }

    [Fact]
    public void FromExistingValue_HandlesSpecialCharactersCorrectly()
    {
        var specialCharValue = "A1B2C3-!@#";
        var ex = Assert.Throws<ArgumentException>(() => SerialNumber.FromExistingValue(specialCharValue));
        Assert.Equal("Serial number must contain only hexadecimal characters.", ex.Message);
    }
}
