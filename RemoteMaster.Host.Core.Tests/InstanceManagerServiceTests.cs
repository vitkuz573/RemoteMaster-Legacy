// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Services;
using Serilog;
using Serilog.Sinks.TestCorrelator;

namespace RemoteMaster.Host.Core.Tests;

public class InstanceManagerServiceTests
{
    private readonly Mock<INativeProcessFactory> _nativeProcessFactoryMock;
    private readonly Mock<IProcess> _nativeProcessMock;
    private readonly Mock<IProcessWrapperFactory> _processWrapperFactoryMock;
    private readonly Mock<IProcess> _processWrapperMock;
    private readonly MockFileSystem _mockFileSystem;
    private readonly ILogger<InstanceManagerService> _logger;
    private readonly InstanceManagerService _instanceManagerService;

    public InstanceManagerServiceTests()
    {
        _nativeProcessFactoryMock = new Mock<INativeProcessFactory>();
        _nativeProcessMock = new Mock<IProcess>();

        _processWrapperFactoryMock = new Mock<IProcessWrapperFactory>();
        _processWrapperMock = new Mock<IProcess>();

        _nativeProcessFactoryMock
            .Setup(factory => factory.Create(It.IsAny<INativeProcessOptions>()))
            .Returns(_nativeProcessMock.Object);

        _processWrapperFactoryMock
            .Setup(factory => factory.Create())
            .Returns(_processWrapperMock.Object);

        _mockFileSystem = new MockFileSystem();

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.TestCorrelator()
            .CreateLogger();

        var services = new ServiceCollection()
            .AddLogging(builder => builder.AddSerilog())
            .BuildServiceProvider();

        _logger = services.GetRequiredService<ILogger<InstanceManagerService>>();

        _instanceManagerService = new InstanceManagerService(_nativeProcessFactoryMock.Object, _processWrapperFactoryMock.Object, _mockFileSystem, _logger);
    }

    [Fact]
    public void StartNewInstance_ShouldThrowArgumentNullException_WhenStartInfoIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _instanceManagerService.StartNewInstance("destinationPath", null!));
    }

    [Fact]
    public void StartNewInstance_ShouldCopyExecutable_WhenDestinationPathIsProvided()
    {
        // Arrange
        var executablePath = Environment.ProcessPath!;
        const string destinationPath = @"C:\destinationPath\executable.exe";
        var destinationDirectory = _mockFileSystem.Path.GetDirectoryName(destinationPath);
        var startInfo = new ProcessStartInfo(executablePath);

        _mockFileSystem.AddFile(executablePath, new MockFileData("test content"));

        _processWrapperMock.Setup(p => p.Start(startInfo)).Verifiable();
        _processWrapperMock.SetupGet(p => p.Id).Returns(1234);

        // Act
        var processId = _instanceManagerService.StartNewInstance(destinationPath, startInfo);

        // Assert
        Assert.True(_mockFileSystem.Directory.Exists(destinationDirectory));
        Assert.True(_mockFileSystem.File.Exists(destinationPath));
        Assert.Equal("test content", _mockFileSystem.File.ReadAllText(destinationPath));

        _processWrapperMock.Verify(p => p.Start(startInfo), Times.Once);

        Assert.Equal(1234, processId);
    }

    [Fact]
    public void StartNewInstance_ShouldLogAndRethrowIOException_WhenIOExceptionOccurs()
    {
        // Arrange
        const string executablePath = @"C:\sourcePath\executable.exe";
        const string destinationPath = @"C:\destinationPath\executable.exe";
        var startInfo = new ProcessStartInfo(executablePath);
    
        var fileMock = new Mock<IFile>();
        fileMock.Setup(f => f.Copy(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Throws<IOException>();
    
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { executablePath, new MockFileData("content") }
        });
    
        var fileSystemMock = new Mock<IFileSystem>();
        fileSystemMock.SetupGet(fs => fs.File).Returns(fileMock.Object);
        fileSystemMock.SetupGet(fs => fs.Directory).Returns(mockFileSystem.Directory);
        fileSystemMock.SetupGet(fs => fs.Path).Returns(mockFileSystem.Path);
    
        var instanceStarterService = new InstanceManagerService(_nativeProcessFactoryMock.Object, _processWrapperFactoryMock.Object, fileSystemMock.Object, _logger);
    
        // Act & Assert
        using (TestCorrelator.CreateContext())
        {
            Assert.Throws<IOException>(() => instanceStarterService.StartNewInstance(destinationPath, startInfo));
    
            var logEvents = TestCorrelator.GetLogEventsFromCurrentContext().ToList();
    
            Assert.Contains(logEvents, e => e.MessageTemplate.Text.Contains("IO error occurred while copying the executable"));
        }
    }

    [Fact]
    public void StartNewInstance_ShouldLogAndRethrowException_WhenProcessStartFails()
    {
        // Arrange
        const string executablePath = @"C:\sourcePath\executable.exe";
        var startInfo = new ProcessStartInfo(executablePath);
        var options = new Mock<INativeProcessOptions>().Object;

        _nativeProcessMock.Setup(x => x.Start(startInfo)).Throws(new Exception("Process start failed"));

        var instanceManagerService = new InstanceManagerService(_nativeProcessFactoryMock.Object, _processWrapperFactoryMock.Object, _mockFileSystem, _logger);

        // Act & Assert
        using (TestCorrelator.CreateContext())
        {
            Assert.Throws<Exception>(() => instanceManagerService.StartNewInstance(null, startInfo, options));

            var logEvents = TestCorrelator.GetLogEventsFromCurrentContext().ToList();

            Assert.Contains(logEvents, e => e.MessageTemplate.Text.Contains("Error starting new instance of the host"));
        }
    }

    [Fact]
    public void StartNewInstance_ShouldStartProcess_WithCorrectStartInfo()
    {
        // Arrange
        const string executablePath = @"C:\sourcePath\executable.exe";
        var startInfo = new ProcessStartInfo(executablePath);
        var options = new Mock<INativeProcessOptions>().Object;

        _nativeProcessMock.Setup(x => x.Start(startInfo)).Verifiable();
        _nativeProcessMock.Setup(x => x.Id).Returns(1234);

        // Act
        var processId = _instanceManagerService.StartNewInstance(null, startInfo, options);

        // Assert
        Assert.Equal(1234, processId);
        _nativeProcessMock.Verify(x => x.Start(startInfo), Times.Once);
    }
}
