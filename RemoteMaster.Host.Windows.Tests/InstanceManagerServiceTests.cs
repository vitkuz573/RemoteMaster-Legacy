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

public class InstanceManagerServiceTests
{
    private readonly Mock<INativeProcess> _nativeProcessMock;
    private readonly Mock<INativeProcessFactory> _nativeProcessFactoryMock;
    private readonly MockFileSystem _mockFileSystem;
    private readonly InstanceManagerService _instanceManagerService;

    public InstanceManagerServiceTests()
    {
        _nativeProcessMock = new Mock<INativeProcess>();
        _nativeProcessFactoryMock = new Mock<INativeProcessFactory>();
        _nativeProcessFactoryMock.Setup(factory => factory.Create()).Returns(_nativeProcessMock.Object);
        _mockFileSystem = new MockFileSystem();

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.TestCorrelator()
            .CreateLogger();

        _instanceManagerService = new InstanceManagerService(_nativeProcessFactoryMock.Object, _mockFileSystem);
    }

    [Fact]
    public void StartNewInstance_ShouldThrowArgumentNullException_WhenStartInfoIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _instanceManagerService.StartNewInstance("destinationPath", null!));
    }

    // [Fact]
    // public void StartNewInstance_ShouldCopyExecutable_WhenDestinationPathIsProvided()
    // {
    //     // Arrange
    //     const string executablePath = @"C:\sourcePath\executable.exe";
    //     const string destinationPath = @"C:\destinationPath\executable.exe";
    //     var destinationDirectory = _mockFileSystem.Path.GetDirectoryName(destinationPath);
    //     var startInfo = new NativeProcessStartInfo { FileName = executablePath };
    // 
    //     _mockFileSystem.AddFile(executablePath, new MockFileData("test content"));
    // 
    //     // Act
    //     _instanceManagerService.StartNewInstance(destinationPath, startInfo);
    // 
    //     // Assert
    //     Assert.True(_mockFileSystem.Directory.Exists(destinationDirectory));
    //     Assert.True(_mockFileSystem.File.Exists(destinationPath));
    // }

    [Fact]
    public void StartNewInstance_ShouldLogAndRethrowIOException_WhenIOExceptionOccurs()
    {
        // Arrange
        const string executablePath = @"C:\sourcePath\executable.exe";
        const string destinationPath = @"C:\destinationPath\executable.exe";
        var startInfo = new NativeProcessStartInfo { FileName = executablePath };

        var fileMock = new Mock<IFile>();
        fileMock.Setup(f => f.Copy(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Throws<IOException>();

        var mockFileSystem = new TestableMockFileSystem(fileMock.Object, new Dictionary<string, MockFileData>
        {
            { executablePath, new MockFileData("content") }
        });

        var instanceStarterService = new InstanceManagerService(_nativeProcessFactoryMock.Object, mockFileSystem);

        // Act & Assert
        using (TestCorrelator.CreateContext())
        {
            Assert.Throws<IOException>(() => instanceStarterService.StartNewInstance(destinationPath, startInfo));

            var logEvents = TestCorrelator.GetLogEventsFromCurrentContext().ToList();

            Assert.Contains(logEvents, e => e.MessageTemplate.Text.Contains("IO error occurred while copying the executable"));
        }
    }

    [Fact]
    public void StartNewInstance_ShouldLogAndRethrowException_WhenExceptionOccurs()
    {
        // Arrange
        const string executablePath = @"C:\sourcePath\executable.exe";
        const string destinationPath = @"C:\destinationPath\executable.exe";
        var startInfo = new NativeProcessStartInfo { FileName = executablePath };

        var fileMock = new Mock<IFile>();
        fileMock.Setup(f => f.Copy(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Throws<Exception>();

        var mockFileSystem = new TestableMockFileSystem(fileMock.Object, new Dictionary<string, MockFileData>
        {
            { executablePath, new MockFileData("content") }
        });

        var instanceStarterService = new InstanceManagerService(_nativeProcessFactoryMock.Object, mockFileSystem);

        // Act & Assert
        using (TestCorrelator.CreateContext())
        {
            Assert.Throws<Exception>(() => instanceStarterService.StartNewInstance(destinationPath, startInfo));

            var logEvents = TestCorrelator.GetLogEventsFromCurrentContext().ToList();

            Assert.Contains(logEvents, e => e.MessageTemplate.Text.Contains("Error starting new instance of the host"));
        }
    }

    [Fact]
    public void StartNewInstance_ShouldStartProcess_WithCorrectStartInfo()
    {
        // Arrange
        const string executablePath = @"C:\sourcePath\executable.exe";
        var startInfo = new NativeProcessStartInfo { FileName = executablePath };

        _nativeProcessMock.Setup(x => x.Start()).Verifiable();
        _nativeProcessMock.Setup(x => x.Id).Returns(1234);

        // Act
        var processId = _instanceManagerService.StartNewInstance(null, startInfo);

        // Assert
        Assert.Equal(1234, processId);
        _nativeProcessMock.Verify(x => x.Start(), Times.Once);
    }
}