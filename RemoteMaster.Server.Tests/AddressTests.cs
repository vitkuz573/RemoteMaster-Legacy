// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
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
        var locality = "New York";
        var state = "NY";
        var country = new CountryCode("US");

        // Act
        var address = new Address(locality, state, country);

        // Assert
        Assert.Equal(locality, address.Locality);
        Assert.Equal(state, address.State);
        Assert.Equal(country, address.Country);
    }

    [Fact]
    public void Address_ThrowsArgumentNullException_WhenLocalityIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Address(null!, "NY", new CountryCode("US")));
    }

    [Fact]
    public void Address_ThrowsArgumentNullException_WhenStateIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Address("New York", null!, new CountryCode("US")));
    }

    [Fact]
    public void Address_ThrowsArgumentNullException_WhenCountryIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Address("New York", "NY", null!));
    }

    [Fact]
    public void Address_Equals_ReturnsTrueForEqualAddresses()
    {
        // Arrange
        var address1 = new Address("New York", "NY", new CountryCode("US"));
        var address2 = new Address("New York", "NY", new CountryCode("US"));

        // Act & Assert
        Assert.True(address1.Equals(address2));
    }

    [Fact]
    public void Address_Equals_ReturnsFalseForDifferentAddresses()
    {
        // Arrange
        var address1 = new Address("New York", "NY", new CountryCode("US"));
        var address2 = new Address("Los Angeles", "CA", new CountryCode("US"));

        // Act & Assert
        Assert.False(address1.Equals(address2));
    }

    [Fact]
    public void Address_GetHashCode_ReturnsSameHashCodeForEqualAddresses()
    {
        // Arrange
        var address1 = new Address("New York", "NY", new CountryCode("US"));
        var address2 = new Address("New York", "NY", new CountryCode("US"));

        // Act & Assert
        Assert.Equal(address1.GetHashCode(), address2.GetHashCode());
    }

    [Fact]
    public void Address_IsImmutable()
    {
        // Arrange
        var address = new Address("New York", "NY", new CountryCode("US"));

        // Act & Assert
        var localityProperty = address.GetType().GetProperty(nameof(Address.Locality));
        var stateProperty = address.GetType().GetProperty(nameof(Address.State));
        var countryProperty = address.GetType().GetProperty(nameof(Address.Country));

        Assert.NotNull(localityProperty);
        Assert.NotNull(stateProperty);
        Assert.NotNull(countryProperty);

        Assert.Throws<ArgumentException>(() => localityProperty!.SetValue(address, "Los Angeles"));
        Assert.Throws<ArgumentException>(() => stateProperty!.SetValue(address, "CA"));
        Assert.Throws<ArgumentException>(() => countryProperty!.SetValue(address, new CountryCode("CA")));
    }
}
