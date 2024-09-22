// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Aggregates.OrganizationAggregate.ValueObjects;

namespace RemoteMaster.Server.Tests;

public class CountryCodeTests
{
    [Fact]
    public void CountryCode_CreatesCorrectly_WithValidCode()
    {
        // Arrange
        var code = "US";

        // Act
        var countryCode = new CountryCode(code);

        // Assert
        Assert.Equal(code, countryCode.Code);
    }

    [Theory]
    [InlineData("INVALID")]
    [InlineData("")]
    [InlineData(null)]
    public void CountryCode_ThrowsArgumentException_WithInvalidCode(string invalidCode)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new CountryCode(invalidCode));
    }

    [Theory]
    [InlineData("US", "US", true)]
    [InlineData("US", "CA", false)]
    public void CountryCode_Equals_ReturnsExpectedResult(string code1, string code2, bool expected)
    {
        // Arrange
        var countryCode1 = new CountryCode(code1);
        var countryCode2 = new CountryCode(code2);

        // Act
        var result = countryCode1.Equals(countryCode2);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("US", "US", true)]
    [InlineData("US", "CA", false)]
    public void CountryCode_GetHashCode_ReturnsExpectedResult(string code1, string code2, bool expected)
    {
        // Arrange
        var countryCode1 = new CountryCode(code1);
        var countryCode2 = new CountryCode(code2);

        // Act
        var hashCodesEqual = countryCode1.GetHashCode() == countryCode2.GetHashCode();

        // Assert
        Assert.Equal(expected, hashCodesEqual);
    }

    [Fact]
    public void CountryCode_IsImmutable()
    {
        // Arrange
        var countryCode = new CountryCode("US");

        // Act & Assert
        var codeProperty = countryCode.GetType().GetProperty(nameof(CountryCode.Code));

        Assert.NotNull(codeProperty);
        Assert.Throws<ArgumentException>(() => codeProperty!.SetValue(countryCode, "CA"));
    }
}
