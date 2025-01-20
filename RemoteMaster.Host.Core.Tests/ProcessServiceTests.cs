// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using Moq;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Services;

namespace RemoteMaster.Host.Core.Tests;

public class ProcessServiceTests
{
    private readonly Mock<IProcess> _mockProcessWrapper;
    private readonly Mock<IProcessWrapperFactory> _mockProcessWrapperFactory;
    private readonly Mock<ICommandLineProvider> _mockCommandLineProvider;
    private readonly ProcessService _processService;

    public ProcessServiceTests()
    {
        _mockProcessWrapper = new Mock<IProcess>();
        _mockProcessWrapperFactory = new Mock<IProcessWrapperFactory>();
        _mockCommandLineProvider = new Mock<ICommandLineProvider>();

        _processService = new ProcessService(_mockProcessWrapperFactory.Object, _mockCommandLineProvider.Object);
    }

    [Fact]
    public void Start_ValidStartInfo_ReturnsProcessWrapper()
    {
        // Arrange
        var startInfo = new ProcessStartInfo
        {
            FileName = "notepad.exe"
        };

        _mockProcessWrapperFactory.Setup(f => f.Create()).Returns(_mockProcessWrapper.Object);

        // Act
        var processWrapper = _processService.Start(startInfo);

        // Assert
        Assert.NotNull(processWrapper);
        Assert.IsAssignableFrom<IProcess>(processWrapper);
        _mockProcessWrapperFactory.Verify(f => f.Create(), Times.Once);
    }

    [Fact]
    public void WaitForExit_ValidProcess_WaitsForExit()
    {
        // Arrange
        _mockProcessWrapper.Setup(p => p.WaitForExit(It.IsAny<uint>()));

        // Act
        _processService.WaitForExit(_mockProcessWrapper.Object);

        // Assert
        _mockProcessWrapper.Verify(p => p.WaitForExit(It.IsAny<uint>()), Times.Once);
    }

    [Fact]
    public async Task ReadStandardOutput_ValidProcess_ReturnsOutput()
    {
        // Arrange
        const string expectedOutput = "Test output";
        var mockStreamReader = new Mock<StreamReader>(new MemoryStream());
        mockStreamReader.Setup(s => s.ReadToEndAsync()).ReturnsAsync(expectedOutput);

        _mockProcessWrapper.Setup(p => p.StandardOutput).Returns(mockStreamReader.Object);

        // Act
        var output = await _processService.ReadStandardOutputAsync(_mockProcessWrapper.Object);

        // Assert
        Assert.Equal(expectedOutput, output);
    }
}
