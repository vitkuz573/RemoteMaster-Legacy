// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Moq;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Services;

namespace RemoteMaster.Host.Windows.Tests;

public class HostUpdaterTests
{
    private readonly Mock<INetworkDriveService> _networkDriveServiceMock;
    private readonly Mock<IUserInstanceService> _userInstanceServiceMock;
    private readonly Mock<IServiceFactory> _serviceFactoryMock;
    private readonly HostUpdater _hostUpdater;

    public HostUpdaterTests()
    {
        _networkDriveServiceMock = new Mock<INetworkDriveService>();
        _userInstanceServiceMock = new Mock<IUserInstanceService>();
        _serviceFactoryMock = new Mock<IServiceFactory>();
        _hostUpdater = new HostUpdater(_networkDriveServiceMock.Object, _userInstanceServiceMock.Object, _serviceFactoryMock.Object);
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowArgumentNullException_WhenFolderPathIsNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _hostUpdater.UpdateAsync(null, "username", "password"));
    }

    [Fact]
    public async Task UpdateAsync_ShouldMapNetworkDrive_WhenFolderPathIsNetworkPath()
    {
        var folderPath = @"\\network\path";
        await _hostUpdater.UpdateAsync(folderPath, "username", "password");

        _networkDriveServiceMock.Verify(s => s.MapNetworkDrive(folderPath, "username", "password"), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ShouldNotMapNetworkDrive_WhenFolderPathIsNotNetworkPath()
    {
        var folderPath = @"C:\local\path";
        await _hostUpdater.UpdateAsync(folderPath, null, null);

        _networkDriveServiceMock.Verify(s => s.MapNetworkDrive(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}