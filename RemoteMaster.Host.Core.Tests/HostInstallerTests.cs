// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions.TestingHelpers;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Moq;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Models;
using RemoteMaster.Host.Core.Services;
using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.DTOs;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Core.Tests;

public class HostInstallerTests
{
    private readonly Mock<ICertificateService> _mockCertificateService;
    private readonly Mock<IHostInformationService> _mockHostInformationService;
    private readonly Mock<IHostConfigurationService> _mockHostConfigurationService;
    private readonly Mock<IServiceFactory> _mockServiceFactory;
    private readonly Mock<IHostLifecycleService> _mockHostLifecycleService;
    private readonly MockFileSystem _mockFileSystem;
    private readonly Mock<IFileService> _mockFileService;
    private readonly Mock<IProcessService> _mockProcessService;
    private readonly Mock<IApplicationPathProvider> _mockApplicationPathProvider;
    private readonly Mock<ILogger<HostInstaller>> _mockLogger;
    private readonly HostInstaller _installer;

    public HostInstallerTests()
    {
        _mockCertificateService = new Mock<ICertificateService>();
        _mockHostInformationService = new Mock<IHostInformationService>();
        _mockHostConfigurationService = new Mock<IHostConfigurationService>();
        _mockServiceFactory = new Mock<IServiceFactory>();
        _mockHostLifecycleService = new Mock<IHostLifecycleService>();
        _mockFileSystem = new MockFileSystem();
        _mockFileService = new Mock<IFileService>();
        _mockProcessService = new Mock<IProcessService>();
        _mockApplicationPathProvider = new Mock<IApplicationPathProvider>();
        _mockLogger = new Mock<ILogger<HostInstaller>>();

        _installer = new HostInstaller(
            _mockCertificateService.Object,
            _mockHostInformationService.Object,
            _mockHostConfigurationService.Object,
            _mockServiceFactory.Object,
            _mockHostLifecycleService.Object,
            _mockFileSystem,
            _mockFileService.Object,
            _mockProcessService.Object,
            _mockApplicationPathProvider.Object,
            _mockLogger.Object);
    }

    #region InstallAsync Success Tests

    [Fact]
    public async Task InstallAsync_ShouldInstallAndStartHostServiceSuccessfully()
    {
        // Arrange
        var installRequest = new HostInstallRequest("test-server", "TestOrg", "TestOU");
        var hostInformation = new HostDto("TestHost", IPAddress.Parse("127.0.0.1"), PhysicalAddress.Parse("001122334455"));
        var organizationAddress = new AddressDto("Locality", "State", "Country");
        var serviceMock = new Mock<IService>();

        _mockHostInformationService.Setup(h => h.GetHostInformation()).Returns(hostInformation);

        _mockServiceFactory.Setup(f => f.GetService("RCHost")).Returns(serviceMock.Object);
        serviceMock.Setup(s => s.IsInstalled).Returns(false);

        _mockHostLifecycleService.Setup(l => l.GetOrganizationAddressAsync(installRequest.Organization))
            .ReturnsAsync(organizationAddress);

        // Act
        await _installer.InstallAsync(installRequest);

        // Assert
        serviceMock.Verify(s => s.Create(), Times.Once);
        serviceMock.Verify(s => s.Start(), Times.Once);

        _mockCertificateService.Verify(c => c.GetCaCertificateAsync(), Times.Once);
        _mockCertificateService.Verify(c => c.IssueCertificateAsync(It.IsAny<HostConfiguration>(), organizationAddress), Times.Once);

        _mockLogger.VerifyLog(LogLevel.Information, "Starting installation...", Times.Once());
        _mockLogger.VerifyLog(LogLevel.Information, "Server: test-server", Times.Once());
        _mockLogger.VerifyLog(LogLevel.Information, "Host Name: TestHost, IP Address: 127.0.0.1, MAC Address: 001122334455", Times.Once());
        _mockLogger.VerifyLog(LogLevel.Information, "Distinguished Name: CN=TestHost, O=TestOrg, OU=TestOU", Times.Once());
    }

    [Fact]
    public async Task InstallAsync_ShouldInstallHostService_WhenNotInstalled()
    {
        // Arrange
        var installRequest = new HostInstallRequest("test-server", "TestOrg", "TestOU");
        var hostInformation = new HostDto("TestHost", IPAddress.Parse("127.0.0.1"), PhysicalAddress.Parse("001122334455"));
        var organizationAddress = new AddressDto("Locality", "State", "Country");
        var serviceMock = new Mock<IService>();

        _mockHostInformationService.Setup(h => h.GetHostInformation()).Returns(hostInformation);
        _mockServiceFactory.Setup(f => f.GetService("RCHost")).Returns(serviceMock.Object);
        serviceMock.Setup(s => s.IsInstalled).Returns(false);

        _mockHostLifecycleService.Setup(l => l.GetOrganizationAddressAsync(installRequest.Organization))
            .ReturnsAsync(organizationAddress);

        // Act
        await _installer.InstallAsync(installRequest);

        // Assert
        serviceMock.Verify(s => s.Create(), Times.Once);
        serviceMock.Verify(s => s.Start(), Times.Once);
        _mockCertificateService.Verify(c => c.GetCaCertificateAsync(), Times.Once);
        _mockCertificateService.Verify(c => c.IssueCertificateAsync(It.IsAny<HostConfiguration>(), organizationAddress), Times.Once);
    }

    [Fact]
    public async Task InstallAsync_ShouldUpdateHostService_WhenAlreadyInstalled()
    {
        // Arrange
        var installRequest = new HostInstallRequest("test-server", "TestOrg", "TestOU");
        var hostInformation = new HostDto("TestHost", IPAddress.Parse("127.0.0.1"), PhysicalAddress.Parse("001122334455"));
        var serviceMock = new Mock<IService>();

        _mockHostInformationService.Setup(h => h.GetHostInformation()).Returns(hostInformation);

        serviceMock.Setup(s => s.IsInstalled).Returns(true);
        _mockServiceFactory.Setup(f => f.GetService("RCHost")).Returns(serviceMock.Object);

        // Act
        await _installer.InstallAsync(installRequest);

        // Assert
        serviceMock.Verify(s => s.Stop(), Times.Once);
        serviceMock.Verify(s => s.Create(), Times.Never);
        serviceMock.Verify(s => s.Start(), Times.Once);
    }

    #endregion

    #region InstallAsync Error Tests

    [Fact]
    public async Task InstallAsync_ShouldLogError_WhenHostInformationFails()
    {
        // Arrange
        var installRequest = new HostInstallRequest("test-server", "TestOrg", "TestOU");

        _mockHostInformationService.Setup(h => h.GetHostInformation()).Throws(new InvalidOperationException("Test error"));

        // Act
        await _installer.InstallAsync(installRequest);

        // Assert
        _mockLogger.VerifyLog(LogLevel.Error, "An error occurred: Test error", Times.Once());
    }

    #endregion

    #region InstallAsync Error Handling Tests

    [Fact]
    public async Task InstallAsync_ShouldLogError_WhenExceptionOccurs()
    {
        // Arrange
        var installRequest = new HostInstallRequest("test-server", "TestOrg", "TestOU");
        _mockHostInformationService.Setup(h => h.GetHostInformation()).Throws(new InvalidOperationException("Test error"));

        // Act
        await _installer.InstallAsync(installRequest);

        // Assert
        _mockLogger.VerifyLog(LogLevel.Error, "An error occurred: Test error", Times.Once());
    }

    #endregion

    #region CopyToTargetPath Tests

    [Fact]
    public void CopyToTargetPath_ShouldCopyFileToTargetPath()
    {
        // Arrange
        var sourcePath = Environment.ProcessPath!;
        var sourceFileName = Path.GetFileName(sourcePath);
        const string targetDirectoryPath = "C:\\ProgramFiles\\RemoteMaster\\Host";
        var targetPath = Path.Combine(targetDirectoryPath, sourceFileName);

        // Act
        InvokePrivateMethod(_installer, "CopyToTargetPath", [targetDirectoryPath]);

        // Assert
        _mockFileService.Verify(fs => fs.CopyFile(sourcePath, targetPath, true), Times.Once);
    }

    [Fact]
    public void CopyToTargetPath_ShouldLogWarning_WhenCopyFails()
    {
        // Arrange
        _mockFileService.Setup(fs => fs.CopyFile(It.IsAny<string>(), It.IsAny<string>(), true))
                        .Throws(new IOException("Copy failed"));

        // Act
        InvokePrivateMethod(_installer, "CopyToTargetPath", ["C:\\ProgramFiles\\RemoteMaster\\Host"]);

        // Assert
        _mockLogger.VerifyLog(LogLevel.Warning, "Failed to copy files to", Times.Once());
    }

    #endregion

    #region Helper Methods

    private static void InvokePrivateMethod(object instance, string methodName, object[] parameters)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new MissingMethodException($"The method '{methodName}' was not found.");
        method.Invoke(instance, parameters);
    }

    #endregion
}
