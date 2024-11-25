// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Moq;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Services;

namespace RemoteMaster.Host.Core.Tests;

public class ServiceFactoryTests
{
    private static Mock<IService> CreateMockService(string name)
    {
        var mockService = new Mock<IService>();
        mockService.Setup(s => s.Name).Returns(name);

        return mockService;
    }

    [Fact]
    public void LoadAllServices_ShouldLoadAllServices()
    {
        // Arrange
        var mockService = CreateMockService("MockService");
        var services = new List<IService> { mockService.Object };
        var factory = new ServiceFactory(services);

        // Act
        var service = factory.GetService("MockService");

        // Assert
        Assert.NotNull(service);
        Assert.Equal("MockService", service.Name);
    }

    [Fact]
    public void GetService_ValidServiceName_ShouldReturnService()
    {
        // Arrange
        var mockService = CreateMockService("MockService");
        var services = new List<IService> { mockService.Object };
        var factory = new ServiceFactory(services);

        // Act
        var service = factory.GetService("MockService");

        // Assert
        Assert.NotNull(service);
        Assert.Equal("MockService", service.Name);
    }

    [Fact]
    public void GetService_InvalidServiceName_ShouldThrowException()
    {
        // Arrange
        var mockService = CreateMockService("MockService");
        var services = new List<IService> { mockService.Object };
        var factory = new ServiceFactory(services);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => factory.GetService("InvalidService"));
        Assert.Equal("Service for 'InvalidService' is not defined.", exception.Message);
    }
}
