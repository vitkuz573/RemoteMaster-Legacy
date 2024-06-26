// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using Moq;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Services;

namespace RemoteMaster.Host.Windows.Tests;

public class ProcessServiceTests
{
    private readonly Mock<IProcessWrapper> _mockProcessWrapper;
    private readonly Mock<IProcessWrapperFactory> _mockProcessWrapperFactory;
    private readonly ProcessService _processService;

    public ProcessServiceTests()
    {
        _mockProcessWrapper = new Mock<IProcessWrapper>();
        _mockProcessWrapperFactory = new Mock<IProcessWrapperFactory>();
        _processService = new ProcessService(_mockProcessWrapperFactory.Object);
    }

    [Fact]
    public void Start_ValidStartInfo_ReturnsProcessWrapper()
    {
        // Arrange
        var startInfo = new ProcessStartInfo
        {
            FileName = "notepad.exe"
        };
        _mockProcessWrapperFactory.Setup(f => f.Create(startInfo)).Returns(_mockProcessWrapper.Object);

        // Act
        var processWrapper = _processService.Start(startInfo);

        // Assert
        Assert.NotNull(processWrapper);
        Assert.IsAssignableFrom<IProcessWrapper>(processWrapper);
        _mockProcessWrapperFactory.Verify(f => f.Create(startInfo), Times.Once);
    }

    [Fact]
    public void WaitForExit_ValidProcess_WaitsForExit()
    {
        // Arrange
        _mockProcessWrapper.Setup(p => p.WaitForExit());

        // Act
        _processService.WaitForExit(_mockProcessWrapper.Object);

        // Assert
        _mockProcessWrapper.Verify(p => p.WaitForExit(), Times.Once);
    }

    [Fact]
    public void ReadStandardOutput_ValidProcess_ReturnsOutput()
    {
        // Arrange
        var expectedOutput = "Test output";
        _mockProcessWrapper.Setup(p => p.ReadStandardOutput()).Returns(expectedOutput);

        // Act
        var output = _processService.ReadStandardOutput(_mockProcessWrapper.Object);

        // Assert
        Assert.Equal(expectedOutput, output);
    }
}