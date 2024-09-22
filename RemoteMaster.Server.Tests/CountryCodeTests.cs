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

    [Fact]
    public void CountryCode_ThrowsArgumentException_WithInvalidCode()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new CountryCode("INVALID"));
        Assert.Throws<ArgumentException>(() => new CountryCode(""));
        Assert.Throws<ArgumentException>(() => new CountryCode(null!));
    }

    [Fact]
    public void CountryCode_Equals_ReturnsTrueForEqualCountryCodes()
    {
        // Arrange
        var countryCode1 = new CountryCode("US");
        var countryCode2 = new CountryCode("US");

        // Act & Assert
        Assert.True(countryCode1.Equals(countryCode2));
    }

    [Fact]
    public void CountryCode_Equals_ReturnsFalseForDifferentCountryCodes()
    {
        // Arrange
        var countryCode1 = new CountryCode("US");
        var countryCode2 = new CountryCode("CA");

        // Act & Assert
        Assert.False(countryCode1.Equals(countryCode2));
    }

    [Fact]
    public void CountryCode_GetHashCode_ReturnsSameHashCodeForEqualCountryCodes()
    {
        // Arrange
        var countryCode1 = new CountryCode("US");
        var countryCode2 = new CountryCode("US");

        // Act & Assert
        Assert.Equal(countryCode1.GetHashCode(), countryCode2.GetHashCode());
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
