// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Moq;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Models;
using RemoteMaster.Host.Windows.Services;
using Serilog;
using Serilog.Sinks.TestCorrelator;

namespace RemoteMaster.Host.Windows.Tests;

public class InstanceStarterServiceTests
{
    private readonly Mock<INativeProcess> _nativeProcessMock;
    private readonly Mock<INativeProcessFactory> _nativeProcessFactoryMock;
    private readonly MockFileSystem _mockFileSystem;
    private readonly InstanceStarterService _instanceStarterService;

    public InstanceStarterServiceTests()
    {
        _nativeProcessMock = new Mock<INativeProcess>();
        _nativeProcessFactoryMock = new Mock<INativeProcessFactory>();
        _nativeProcessFactoryMock.Setup(factory => factory.Create()).Returns(_nativeProcessMock.Object);
        _mockFileSystem = new MockFileSystem();

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.TestCorrelator()
            .CreateLogger();

        _instanceStarterService = new InstanceStarterService(_nativeProcessFactoryMock.Object, _mockFileSystem);
    }

    [Fact]
    public void StartNewInstance_ShouldThrowArgumentNullException_WhenStartInfoIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _instanceStarterService.StartNewInstance("executablePath", "destinationPath", null));
    }

    [Fact]
    public void StartNewInstance_ShouldCopyExecutable_WhenDestinationPathIsProvided()
    {
        // Arrange
        var executablePath = "C:\\sourcePath\\executable.exe";
        var destinationPath = "C:\\destinationPath\\executable.exe";
        var destinationDirectory = _mockFileSystem.Path.GetDirectoryName(destinationPath);
        var startInfo = new NativeProcessStartInfo { FileName = executablePath };

        _mockFileSystem.AddFile(executablePath, new MockFileData("test content"));

        // Act
        _instanceStarterService.StartNewInstance(executablePath, destinationPath, startInfo);

        // Assert
        Assert.True(_mockFileSystem.Directory.Exists(destinationDirectory));
        Assert.True(_mockFileSystem.File.Exists(destinationPath));
    }

    [Fact]
    public void StartNewInstance_ShouldLogAndRethrowIOException_WhenIOExceptionOccurs()
    {
        // Arrange
        var executablePath = "C:\\sourcePath\\executable.exe";
        var destinationPath = "C:\\destinationPath\\executable.exe";
        var startInfo = new NativeProcessStartInfo { FileName = executablePath };

        var fileMock = new Mock<IFile>();
        fileMock.Setup(f => f.Copy(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Throws<IOException>();

        var mockFileSystem = new TestableMockFileSystem(fileMock.Object, new Dictionary<string, MockFileData>
        {
            { executablePath, new MockFileData("content") }
        });

        var instanceStarterService = new InstanceStarterService(_nativeProcessFactoryMock.Object, mockFileSystem);

        // Act & Assert
        using (TestCorrelator.CreateContext())
        {
            var ex = Assert.Throws<IOException>(() => instanceStarterService.StartNewInstance(executablePath, destinationPath, startInfo));

            var logEvents = TestCorrelator.GetLogEventsFromCurrentContext().ToList();

            Assert.Contains(logEvents, e => e.MessageTemplate.Text.Contains("IO error occurred while copying the executable"));
        }
    }

    [Fact]
    public void StartNewInstance_ShouldLogAndRethrowException_WhenExceptionOccurs()
    {
        // Arrange
        var executablePath = "C:\\sourcePath\\executable.exe";
        var destinationPath = "C:\\destinationPath\\executable.exe";
        var startInfo = new NativeProcessStartInfo { FileName = executablePath };

        var fileMock = new Mock<IFile>();
        fileMock.Setup(f => f.Copy(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Throws<Exception>();

        var mockFileSystem = new TestableMockFileSystem(fileMock.Object, new Dictionary<string, MockFileData>
        {
            { executablePath, new MockFileData("content") }
        });

        var instanceStarterService = new InstanceStarterService(_nativeProcessFactoryMock.Object, mockFileSystem);

        // Act & Assert
        using (TestCorrelator.CreateContext())
        {
            var ex = Assert.Throws<Exception>(() => instanceStarterService.StartNewInstance(executablePath, destinationPath, startInfo));

            var logEvents = TestCorrelator.GetLogEventsFromCurrentContext().ToList();

            Assert.Contains(logEvents, e => e.MessageTemplate.Text.Contains("Error starting new instance of the host"));
        }
    }

    [Fact]
    public void StartNewInstance_ShouldStartProcess_WithCorrectStartInfo()
    {
        // Arrange
        var executablePath = "C:\\sourcePath\\executable.exe";
        var startInfo = new NativeProcessStartInfo { FileName = executablePath };

        _nativeProcessMock.Setup(x => x.Start()).Verifiable();
        _nativeProcessMock.Setup(x => x.Id).Returns(1234);

        // Act
        var processId = _instanceStarterService.StartNewInstance(executablePath, null, startInfo);

        // Assert
        Assert.Equal(1234, processId);
        _nativeProcessMock.Verify(x => x.Start(), Times.Once);
    }
}