// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Services;

namespace RemoteMaster.Server.Tests;

public class QueryParameterServiceTests
{
    private readonly FakeNavigationManager _navigationManager;
    private readonly IQueryParameterService _queryParameterService;

    public QueryParameterServiceTests()
    {
        _navigationManager = new FakeNavigationManager("http://example.com/", "http://example.com/");
        _queryParameterService = new QueryParameterService(_navigationManager);
    }

    [Fact]
    public void GetParameter_ExistingKey_ReturnsCorrectValue()
    {
        // Arrange
        _navigationManager.NavigateTo("http://example.com/?param1=value1");

        // Act
        var result = _queryParameterService.GetParameter("param1", "defaultValue");

        // Assert
        Assert.Equal("value1", result);
    }

    [Fact]
    public void GetParameter_NonExistingKey_ReturnsDefaultValue()
    {
        // Arrange
        _navigationManager.NavigateTo("http://example.com/");

        // Act
        var result = _queryParameterService.GetParameter("param1", "defaultValue");

        // Assert
        Assert.Equal("defaultValue", result);
    }

    [Fact]
    public void UpdateParameter_ValidKeyAndValue_NavigatesToUpdatedUri()
    {
        // Arrange
        _navigationManager.NavigateTo("http://example.com/?param1=value1");

        // Act
        _queryParameterService.UpdateParameter("param2", "value2");

        // Assert
        Assert.Equal("http://example.com/?param1=value1&param2=value2", _navigationManager.Uri);
    }
}
