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

        _instanceManagerService = new InstanceManagerService(
            _nativeProcessFactoryMock.Object,
            _processWrapperFactoryMock.Object,
            _mockFileSystem,
            _logger);
    }

    #region Validation Tests

    [Fact]
    public void StartNewInstance_ShouldThrowArgumentNullException_WhenStartInfoIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _instanceManagerService.StartNewInstance("destinationPath", null!));
    }

    #endregion

    #region File Management Tests

    [Fact]
    public void PrepareExecutable_ShouldCreateDestinationDirectory_WhenItDoesNotExist()
    {
        const string destinationPath = @"C:\new\destinationPath\executable.exe";
        var destinationDirectory = _mockFileSystem.Path.GetDirectoryName(destinationPath);
        var sourcePath = Environment.ProcessPath!;

        _mockFileSystem.AddFile(sourcePath, new MockFileData("test content"));

        _instanceManagerService.StartNewInstance(destinationPath, new ProcessStartInfo());

        Assert.True(_mockFileSystem.Directory.Exists(destinationDirectory));
    }

    [Fact]
    public void PrepareExecutable_ShouldThrowIOException_WhenCopyFails()
    {
        const string destinationPath = @"C:\destination\executable.exe";
        var fileMock = new Mock<IFile>();
        fileMock.Setup(f => f.Copy(It.IsAny<string>(), It.IsAny<string>(), true))
            .Throws<IOException>();

        var mockFileSystem = new Mock<IFileSystem>();
        mockFileSystem.SetupGet(fs => fs.File).Returns(fileMock.Object);
        mockFileSystem.SetupGet(fs => fs.Directory).Returns(_mockFileSystem.Directory);
        mockFileSystem.SetupGet(fs => fs.Path).Returns(_mockFileSystem.Path);

        var instanceManager = new InstanceManagerService(
            _nativeProcessFactoryMock.Object,
            _processWrapperFactoryMock.Object,
            mockFileSystem.Object,
            _logger);

        Assert.Throws<IOException>(() =>
            instanceManager.StartNewInstance(destinationPath, new ProcessStartInfo()));
    }

    [Fact]
    public void PrepareExecutable_ShouldCopyExecutable_WhenDestinationPathIsProvided()
    {
        var executablePath = Environment.ProcessPath!;
        const string destinationPath = @"C:\destinationPath\executable.exe";
        var destinationDirectory = _mockFileSystem.Path.GetDirectoryName(destinationPath);
        var startInfo = new ProcessStartInfo(executablePath);

        _mockFileSystem.AddFile(executablePath, new MockFileData("test content"));
        _processWrapperMock.Setup(p => p.Start(startInfo)).Verifiable();
        _processWrapperMock.SetupGet(p => p.Id).Returns(1234);

        var processId = _instanceManagerService.StartNewInstance(destinationPath, startInfo);

        Assert.True(_mockFileSystem.Directory.Exists(destinationDirectory));
        Assert.True(_mockFileSystem.File.Exists(destinationPath));
        Assert.Equal("test content", _mockFileSystem.File.ReadAllText(destinationPath));

        _processWrapperMock.Verify(p => p.Start(startInfo), Times.Once);
        Assert.Equal(1234, processId);
    }

    #endregion

    #region Process Creation Tests

    [Fact]
    public void StartNewInstance_ShouldUseNativeProcess_WhenOptionsAreProvided()
    {
        var startInfo = new ProcessStartInfo("executable.exe");
        var options = new Mock<INativeProcessOptions>().Object;

        _nativeProcessMock.Setup(p => p.Start(startInfo)).Verifiable();
        _nativeProcessMock.Setup(p => p.Id).Returns(1234);

        var processId = _instanceManagerService.StartNewInstance(null, startInfo, options);

        Assert.Equal(1234, processId);
        _nativeProcessMock.Verify(p => p.Start(startInfo), Times.Once);
    }

    [Fact]
    public void StartNewInstance_ShouldUseProcessWrapper_WhenOptionsAreNotProvided()
    {
        var startInfo = new ProcessStartInfo("executable.exe");

        _processWrapperMock.Setup(p => p.Start(startInfo)).Verifiable();
        _processWrapperMock.Setup(p => p.Id).Returns(1234);

        var processId = _instanceManagerService.StartNewInstance(null, startInfo);

        Assert.Equal(1234, processId);
        _processWrapperMock.Verify(p => p.Start(startInfo), Times.Once);
    }

    #endregion

    #region Logging Tests

    [Fact]
    public void StartNewInstance_ShouldLogAndRethrowIOException_WhenIOExceptionOccurs()
    {
        const string sourcePath = @"C:\sourcePath\executable.exe";
        const string destinationPath = @"C:\destinationPath\executable.exe";
        var startInfo = new ProcessStartInfo(sourcePath);

        var fileMock = new Mock<IFile>();
        fileMock.Setup(f => f.Copy(It.IsAny<string>(), It.IsAny<string>(), true))
            .Throws<IOException>();

        var mockFileSystem = new Mock<IFileSystem>();
        mockFileSystem.SetupGet(fs => fs.File).Returns(fileMock.Object);
        mockFileSystem.SetupGet(fs => fs.Directory).Returns(_mockFileSystem.Directory);
        mockFileSystem.SetupGet(fs => fs.Path).Returns(_mockFileSystem.Path);

        var instanceManager = new InstanceManagerService(
            _nativeProcessFactoryMock.Object,
            _processWrapperFactoryMock.Object,
            mockFileSystem.Object,
            _logger);

        using (TestCorrelator.CreateContext())
        {
            Assert.Throws<IOException>(() =>
                instanceManager.StartNewInstance(destinationPath, startInfo));

            var logEvents = TestCorrelator.GetLogEventsFromCurrentContext().ToList();
            Assert.Contains(logEvents, e => e.MessageTemplate.Text.Contains("IO error occurred"));
        }
    }

    [Fact]
    public void StartNewInstance_ShouldLogAndRethrowException_WhenProcessStartFails()
    {
        var startInfo = new ProcessStartInfo("executable.exe");
        var options = new Mock<INativeProcessOptions>().Object;

        _nativeProcessMock.Setup(p => p.Start(startInfo))
            .Throws(new Exception("Process start failed"));

        using (TestCorrelator.CreateContext())
        {
            Assert.Throws<Exception>(() =>
                _instanceManagerService.StartNewInstance(null, startInfo, options));

            var logEvents = TestCorrelator.GetLogEventsFromCurrentContext().ToList();
            Assert.Contains(logEvents, e => e.MessageTemplate.Text.Contains("Error starting new instance"));
        }
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void StartNewInstance_ShouldHandleEmptyDestinationPath()
    {
        // Arrange
        var startInfo = new ProcessStartInfo("executable.exe");
        _processWrapperMock.Setup(p => p.Start(startInfo)).Verifiable();
        _processWrapperMock.Setup(p => p.Id).Returns(1234);

        // Act
        var processId = _instanceManagerService.StartNewInstance(null, startInfo);

        // Assert
        Assert.Equal(1234, processId);
        _processWrapperMock.Verify(p => p.Start(startInfo), Times.Once);
    }

    #endregion
}
