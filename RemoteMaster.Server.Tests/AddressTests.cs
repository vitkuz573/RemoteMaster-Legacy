﻿// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Aggregates.OrganizationAggregate.ValueObjects;

namespace RemoteMaster.Server.Tests;

public class AddressTests
{
    [Fact]
    public void Address_CreatesCorrectly_WithValidArguments()
    {
        // Arrange
        const string locality = "New York";
        const string state = "NY";
        var country = new CountryCode("US");

        // Act
        var address = new Address(locality, state, country);

        // Assert
        Assert.Equal(locality, address.Locality);
        Assert.Equal(state, address.State);
        Assert.Equal(country, address.Country);
    }

    [Theory]
    [InlineData(null, "NY", "US")]
    [InlineData("New York", null, "US")]
    [InlineData("New York", "NY", null)]
    public void Address_ThrowsArgumentNullException_WhenAnyArgumentIsNull(string? locality, string? state, string? countryCode)
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Address(locality!, state!, new CountryCode(countryCode!)));
    }

    [Theory]
    [InlineData("New York", "NY", "US", "New York", "NY", "US", true)]
    [InlineData("New York", "NY", "US", "Los Angeles", "CA", "US", false)]
    public void Address_Equals_ReturnsExpectedResult(string locality1, string state1, string countryCode1, string locality2, string state2, string countryCode2, bool expected)
    {
        // Arrange
        var address1 = new Address(locality1, state1, new CountryCode(countryCode1));
        var address2 = new Address(locality2, state2, new CountryCode(countryCode2));

        // Act
        var result = address1.Equals(address2);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("New York", "NY", "US", "New York", "NY", "US", true)]
    [InlineData("New York", "NY", "US", "Los Angeles", "CA", "US", false)]
    public void Address_GetHashCode_ReturnsExpectedResult(string locality1, string state1, string countryCode1, string locality2, string state2, string countryCode2, bool expected)
    {
        // Arrange
        var address1 = new Address(locality1, state1, new CountryCode(countryCode1));
        var address2 = new Address(locality2, state2, new CountryCode(countryCode2));

        // Act
        var hashCodesEqual = address1.GetHashCode() == address2.GetHashCode();

        // Assert
        Assert.Equal(expected, hashCodesEqual);
    }

    [Theory]
    [InlineData(nameof(Address.Locality), "Los Angeles")]
    [InlineData(nameof(Address.State), "CA")]
    [InlineData(nameof(Address.Country), "CA")]
    public void Address_IsImmutable(string propertyName, object newValue)
    {
        // Arrange
        var address = new Address("New York", "NY", new CountryCode("US"));

        // Act
        var updatedAddress = propertyName switch
        {
            nameof(Address.Locality) => address with { Locality = (string)newValue },
            nameof(Address.State) => address with { State = (string)newValue },
            nameof(Address.Country) => address with { Country = new CountryCode((string)newValue) },
            _ => throw new ArgumentException("Invalid property name", nameof(propertyName)),
        };

        // Assert
        Assert.NotEqual(address, updatedAddress);

        switch (propertyName)
        {
            case nameof(Address.Locality):
                Assert.Equal(newValue, updatedAddress.Locality);
                Assert.NotEqual(newValue, address.Locality);
                break;
            case nameof(Address.State):
                Assert.Equal(newValue, updatedAddress.State);
                Assert.NotEqual(newValue, address.State);
                break;
            case nameof(Address.Country):
            {
                var expectedCountry = new CountryCode((string)newValue);
                Assert.Equal(expectedCountry, updatedAddress.Country);
                Assert.NotEqual(expectedCountry, address.Country);
                break;
            }
        }
    }
}
