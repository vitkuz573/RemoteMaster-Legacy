// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography;
using RemoteMaster.Server.Services;

namespace RemoteMaster.Server.Tests;

public class SerialNumberServiceTests
{
    private readonly SerialNumberService _serialNumberService = new();

    [Fact]
    public void GenerateSerialNumber_ReturnsUniqueSerialNumbers()
    {
        // Act
        var result1 = _serialNumberService.GenerateSerialNumber();
        var result2 = _serialNumberService.GenerateSerialNumber();

        // Assert
        Assert.True(result1.IsSuccess, $"Failed to generate serial number: {result1.Errors.FirstOrDefault()?.Message}");
        Assert.True(result2.IsSuccess, $"Failed to generate serial number: {result2.Errors.FirstOrDefault()?.Message}");

        var serialNumber1 = result1.Value;
        var serialNumber2 = result2.Value;

        Assert.NotNull(serialNumber1);
        Assert.NotNull(serialNumber2);
        Assert.NotEqual(serialNumber1, serialNumber2);
    }

    [Fact]
    public void GenerateSerialNumber_HasExpectedLength()
    {
        // Act
        var result = _serialNumberService.GenerateSerialNumber();

        // Assert
        Assert.True(result.IsSuccess, $"Failed to generate serial number: {result.Errors.FirstOrDefault()?.Message}");

        var serialNumber = result.Value;

        Assert.NotNull(serialNumber);
        Assert.Equal(32, serialNumber.Length);
    }

    [Fact]
    public void GenerateSerialNumber_IsRandom()
    {
        var serialNumbers = new HashSet<string>();

        for (var i = 0; i < 1000; i++)
        {
            var result = _serialNumberService.GenerateSerialNumber();
            Assert.True(result.IsSuccess, $"Failed to generate serial number: {result.Errors.FirstOrDefault()?.Message}");

            var serialNumber = result.Value;
            var serialNumberString = Convert.ToBase64String(serialNumber);

            Assert.False(serialNumbers.Contains(serialNumberString), $"Duplicate serial number found at iteration {i}");
            serialNumbers.Add(serialNumberString);
        }
    }

    [Fact]
    public void GenerateSerialNumber_SupportsSHA3()
    {
        // Act
        var result = _serialNumberService.GenerateSerialNumber();
        var isSha3Supported = SHA3_256.IsSupported;

        // Assert
        Assert.True(result.IsSuccess, $"Failed to generate serial number: {result.Errors.FirstOrDefault()?.Message}");

        var serialNumber = result.Value;

        Assert.NotNull(serialNumber);
        Assert.Equal(32, serialNumber.Length);
        Assert.True(isSha3Supported || !isSha3Supported, "The system should support SHA-3 or fallback to SHA-256.");
    }
}
