// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions;
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
    private readonly Mock<ICertificateService> _certificateServiceMock;
    private readonly Mock<IHostInformationService> _hostInformationServiceMock;
    private readonly Mock<IHostConfigurationService> _hostConfigurationServiceMock;
    private readonly Mock<IServiceFactory> _serviceFactoryMock;
    private readonly Mock<IHostLifecycleService> _hostLifecycleServiceMock;
    private readonly Mock<IFileSystem> _fileSystemMock;
    private readonly Mock<IFileService> _fileServiceMock;
    private readonly Mock<IProcessService> _processServiceMock;
    private readonly Mock<ILogger<HostInstaller>> _loggerMock;
    private readonly HostInstaller _installer;

    public HostInstallerTests()
    {
        _certificateServiceMock = new Mock<ICertificateService>();
        _hostInformationServiceMock = new Mock<IHostInformationService>();
        _hostConfigurationServiceMock = new Mock<IHostConfigurationService>();
        _serviceFactoryMock = new Mock<IServiceFactory>();
        _hostLifecycleServiceMock = new Mock<IHostLifecycleService>();
        _fileSystemMock = new Mock<IFileSystem>();
        _fileServiceMock = new Mock<IFileService>();
        _processServiceMock = new Mock<IProcessService>();
        _loggerMock = new Mock<ILogger<HostInstaller>>();

        _fileSystemMock.Setup(fs => fs.Path.Combine(It.IsAny<string>(), It.IsAny<string>()))
                       .Returns((string path1, string path2) => $"{path1}\\{path2}");

        _processServiceMock.Setup(ps => ps.GetProcessPath()).Returns("C:\\default\\test.exe");

        _installer = new HostInstaller(
            _certificateServiceMock.Object,
            _hostInformationServiceMock.Object,
            _hostConfigurationServiceMock.Object,
            _serviceFactoryMock.Object,
            _hostLifecycleServiceMock.Object,
            _fileSystemMock.Object,
            _fileServiceMock.Object,
            _processServiceMock.Object,
            _loggerMock.Object);
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

        _hostInformationServiceMock.Setup(h => h.GetHostInformation()).Returns(hostInformation);

        _serviceFactoryMock.Setup(f => f.GetService("RCHost")).Returns(serviceMock.Object);
        serviceMock.Setup(s => s.IsInstalled).Returns(false);

        _hostLifecycleServiceMock.Setup(l => l.GetOrganizationAddressAsync(installRequest.Organization))
            .ReturnsAsync(organizationAddress);

        // Act
        await _installer.InstallAsync(installRequest);

        // Assert
        serviceMock.Verify(s => s.Create(), Times.Once);
        serviceMock.Verify(s => s.Start(), Times.Once);

        _certificateServiceMock.Verify(c => c.GetCaCertificateAsync(), Times.Once);
        _certificateServiceMock.Verify(c => c.IssueCertificateAsync(It.IsAny<HostConfiguration>(), organizationAddress), Times.Once);

        _loggerMock.VerifyLog(LogLevel.Information, "Starting installation...", Times.Once());
        _loggerMock.VerifyLog(LogLevel.Information, "Server: test-server", Times.Once());
        _loggerMock.VerifyLog(LogLevel.Information, "Host Name: TestHost, IP Address: 127.0.0.1, MAC Address: 001122334455", Times.Once());
        _loggerMock.VerifyLog(LogLevel.Information, "Distinguished Name: CN=TestHost, O=TestOrg, OU=TestOU", Times.Once());
    }

    [Fact]
    public async Task InstallAsync_ShouldInstallHostService_WhenNotInstalled()
    {
        // Arrange
        var installRequest = new HostInstallRequest("test-server", "TestOrg", "TestOU");
        var hostInformation = new HostDto("TestHost", IPAddress.Parse("127.0.0.1"), PhysicalAddress.Parse("001122334455"));
        var organizationAddress = new AddressDto("Locality", "State", "Country");
        var serviceMock = new Mock<IService>();

        _hostInformationServiceMock.Setup(h => h.GetHostInformation()).Returns(hostInformation);
        _serviceFactoryMock.Setup(f => f.GetService("RCHost")).Returns(serviceMock.Object);
        serviceMock.Setup(s => s.IsInstalled).Returns(false);

        _hostLifecycleServiceMock.Setup(l => l.GetOrganizationAddressAsync(installRequest.Organization))
            .ReturnsAsync(organizationAddress);

        // Act
        await _installer.InstallAsync(installRequest);

        // Assert
        serviceMock.Verify(s => s.Create(), Times.Once);
        serviceMock.Verify(s => s.Start(), Times.Once);
        _certificateServiceMock.Verify(c => c.GetCaCertificateAsync(), Times.Once);
        _certificateServiceMock.Verify(c => c.IssueCertificateAsync(It.IsAny<HostConfiguration>(), organizationAddress), Times.Once);
    }

    [Fact]
    public async Task InstallAsync_ShouldUpdateHostService_WhenAlreadyInstalled()
    {
        // Arrange
        var installRequest = new HostInstallRequest("test-server", "TestOrg", "TestOU");
        var hostInformation = new HostDto("TestHost", IPAddress.Parse("127.0.0.1"), PhysicalAddress.Parse("001122334455"));
        var serviceMock = new Mock<IService>();

        _hostInformationServiceMock.Setup(h => h.GetHostInformation()).Returns(hostInformation);

        _processServiceMock.Setup(ps => ps.GetProcessPath()).Returns("C:\\test.exe");

        serviceMock.Setup(s => s.IsInstalled).Returns(true);
        _serviceFactoryMock.Setup(f => f.GetService("RCHost")).Returns(serviceMock.Object);

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

        _hostInformationServiceMock.Setup(h => h.GetHostInformation()).Throws(new InvalidOperationException("Test error"));

        // Act
        await _installer.InstallAsync(installRequest);

        // Assert
        _loggerMock.VerifyLog(LogLevel.Error, "An error occurred: Test error", Times.Once());
    }

    #endregion

    #region InstallAsync Error Handling Tests

    [Fact]
    public async Task InstallAsync_ShouldLogError_WhenExceptionOccurs()
    {
        // Arrange
        var installRequest = new HostInstallRequest("test-server", "TestOrg", "TestOU");
        _hostInformationServiceMock.Setup(h => h.GetHostInformation()).Throws(new InvalidOperationException("Test error"));

        // Act
        await _installer.InstallAsync(installRequest);

        // Assert
        _loggerMock.VerifyLog(LogLevel.Error, "An error occurred: Test error", Times.Once());
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

        _processServiceMock.Setup(ps => ps.GetProcessPath()).Returns(sourcePath);

        _fileSystemMock.Setup(fs => fs.Path.Combine(targetDirectoryPath, sourceFileName))
            .Returns(targetPath);

        // Act
        InvokePrivateMethod(_installer, "CopyToTargetPath", [targetDirectoryPath]);

        // Assert
        _fileServiceMock.Verify(fs => fs.CopyFile(sourcePath, targetPath, true), Times.Once);
    }

    [Fact]
    public void CopyToTargetPath_ShouldLogWarning_WhenCopyFails()
    {
        // Arrange
        _fileServiceMock.Setup(fs => fs.CopyFile(It.IsAny<string>(), It.IsAny<string>(), true))
                        .Throws(new IOException("Copy failed"));

        // Act
        InvokePrivateMethod(_installer, "CopyToTargetPath", ["C:\\ProgramFiles\\RemoteMaster\\Host"]);

        // Assert
        _loggerMock.VerifyLog(LogLevel.Warning, "Failed to copy files to", Times.Once());
    }

    [Fact]
    public void CopyToTargetPath_ShouldCopyFilesToTargetDirectory()
    {
        // Arrange
        const string targetDirectoryPath = "C:\\ProgramFiles\\RemoteMaster\\Host";
        const string sourceExecutablePath = "C:\\test.exe";
        const string targetExecutablePath = "C:\\ProgramFiles\\RemoteMaster\\Host\\test.exe";

        _processServiceMock.Setup(ps => ps.GetProcessPath()).Returns(sourceExecutablePath);

        _fileSystemMock.Setup(fs => fs.Path.Combine(targetDirectoryPath, "test.exe"))
            .Returns(targetExecutablePath);

        // Act
        InvokePrivateMethod(_installer, "CopyToTargetPath", [targetDirectoryPath]);

        // Assert
        _fileServiceMock.Verify(f => f.CopyFile(sourceExecutablePath, targetExecutablePath, true), Times.Once);
    }

    #endregion

    #region Helper Methods

    private static void InvokePrivateMethod(object instance, string methodName, object[] parameters)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        
        if (method == null)
        {
            throw new MissingMethodException($"The method '{methodName}' was not found.");
        }

        method.Invoke(instance, parameters);
    }

    #endregion
}
