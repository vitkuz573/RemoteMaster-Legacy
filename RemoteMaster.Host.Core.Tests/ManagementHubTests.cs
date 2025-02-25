// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net.NetworkInformation;
using Moq;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Hubs;
using RemoteMaster.Shared.DTOs;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Core.Tests;

public class ManagementHubTests
{
    private readonly Mock<IHostLifecycleService> _mockHostLifecycleService;
    private readonly Mock<IHostConfigurationService> _mockHostConfigurationService;
    private readonly Mock<ICertificateService> _mockCertificateService;
    private readonly ManagementHub _managementHub;

    public ManagementHubTests()
    {
        _mockHostLifecycleService = new Mock<IHostLifecycleService>();
        _mockHostConfigurationService = new Mock<IHostConfigurationService>();
        _mockCertificateService = new Mock<ICertificateService>();

        _managementHub = new ManagementHub(_mockHostLifecycleService.Object, _mockHostConfigurationService.Object, _mockCertificateService.Object);
    }

    [Fact]
    public async Task Move_ShouldUpdateHostConfigurationAndRenewCertificate()
    {
        // Arrange
        var macAddress = PhysicalAddress.Parse("00:11:22:33:44:55");

        var hostMoveRequest = new HostMoveRequestDto(macAddress, "NewOrg", ["NewOU"]);

        var subject = new SubjectDto("OldOrg", ["OldOU"]);

        var hostConfiguration = new HostConfiguration(It.IsAny<string>(), subject, It.IsAny<HostDto>());

        var organizationAddress = new AddressDto("TestLocality", "TestState", "US");

        _mockHostConfigurationService.Setup(h => h.LoadAsync()).ReturnsAsync(hostConfiguration);
        _mockHostLifecycleService.Setup(h => h.GetOrganizationAddressAsync(It.IsAny<string>())).ReturnsAsync(organizationAddress);

        // Act
        await _managementHub.MoveHostAsync(hostMoveRequest);

        // Assert
        _mockHostConfigurationService.Verify(h => h.SaveAsync(It.Is<HostConfiguration>(hc =>
            hc.Subject.Organization == hostMoveRequest.Organization &&
            hc.Subject.OrganizationalUnit.SequenceEqual(hostMoveRequest.OrganizationalUnit)
        )), Times.Once);

        _mockCertificateService.Verify(h => h.IssueCertificateAsync(hostConfiguration, organizationAddress), Times.Once);
    }
}
