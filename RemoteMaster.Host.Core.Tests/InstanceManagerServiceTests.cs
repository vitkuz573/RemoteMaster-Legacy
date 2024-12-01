// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
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
    private readonly Mock<IFileService> _fileServiceMock;
    private readonly Mock<LaunchModeBase> _launchModeMock;
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

        _fileServiceMock = new Mock<IFileService>();

        _launchModeMock = new Mock<LaunchModeBase>();
        _launchModeMock.Setup(lm => lm.Name).Returns("testmode");

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
            _fileServiceMock.Object,
            _logger);
    }

    #region Validation Tests

    [Fact]
    public void StartNewInstance_ShouldThrowArgumentNullException_WhenStartInfoIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _instanceManagerService.StartNewInstance("destinationPath", _launchModeMock.Object, null!));
    }

    #endregion

    #region File Management Tests

    [Fact]
    public void StartNewInstance_ShouldCreateDestinationDirectoryAndCopyFile_WhenDestinationPathIsProvided()
    {
        const string destinationPath = @"C:\destinationPath\executable.exe";
        var startInfo = new ProcessStartInfo();
        var executablePath = Environment.ProcessPath!;

        _fileServiceMock.Setup(f => f.CopyFile(executablePath, destinationPath, true))
            .Verifiable();

        _processWrapperMock.Setup(p => p.Start(It.IsAny<ProcessStartInfo>())).Verifiable();
        _processWrapperMock.SetupGet(p => p.Id).Returns(1234);

        var processId = _instanceManagerService.StartNewInstance(destinationPath, _launchModeMock.Object, startInfo);

        _fileServiceMock.Verify(f => f.CopyFile(executablePath, destinationPath, true), Times.Once);
        _processWrapperMock.Verify(p => p.Start(It.IsAny<ProcessStartInfo>()), Times.Once);
        Assert.Equal(1234, processId);
    }

    [Fact]
    public void StartNewInstance_ShouldThrowIOException_WhenCopyFileFails()
    {
        const string destinationPath = @"C:\destinationPath\executable.exe";
        var startInfo = new ProcessStartInfo();
        var executablePath = Environment.ProcessPath!;

        _fileServiceMock.Setup(f => f.CopyFile(executablePath, destinationPath, true))
            .Throws<IOException>();

        Assert.Throws<IOException>(() => _instanceManagerService.StartNewInstance(destinationPath, _launchModeMock.Object, startInfo));
        _fileServiceMock.Verify(f => f.CopyFile(executablePath, destinationPath, true), Times.Once);
    }

    [Fact]
    public void StartNewInstance_ShouldUseDefaultExecutablePath_WhenDestinationPathIsNull()
    {
        var startInfo = new ProcessStartInfo();
        var executablePath = Environment.ProcessPath!;

        _processWrapperMock.Setup(p => p.Start(It.Is<ProcessStartInfo>(info => info.FileName == executablePath))).Verifiable();
        _processWrapperMock.SetupGet(p => p.Id).Returns(1234);

        var processId = _instanceManagerService.StartNewInstance(null, _launchModeMock.Object, startInfo);

        _processWrapperMock.Verify(p => p.Start(It.Is<ProcessStartInfo>(info => info.FileName == executablePath)), Times.Once);
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

        var processId = _instanceManagerService.StartNewInstance(null, _launchModeMock.Object, startInfo, options);

        Assert.Equal(1234, processId);
        _nativeProcessMock.Verify(p => p.Start(startInfo), Times.Once);
    }

    [Fact]
    public void StartNewInstance_ShouldUseProcessWrapper_WhenOptionsAreNotProvided()
    {
        var startInfo = new ProcessStartInfo("executable.exe");

        _processWrapperMock.Setup(p => p.Start(startInfo)).Verifiable();
        _processWrapperMock.Setup(p => p.Id).Returns(1234);

        var processId = _instanceManagerService.StartNewInstance(null, _launchModeMock.Object, startInfo);

        Assert.Equal(1234, processId);
        _processWrapperMock.Verify(p => p.Start(startInfo), Times.Once);
    }

    #endregion

    #region Logging Tests

    [Fact]
    public void StartNewInstance_ShouldLogAndRethrowIOException_WhenIOExceptionOccurs()
    {
        const string destinationPath = @"C:\destinationPath\executable.exe";
        var startInfo = new ProcessStartInfo();
        var executablePath = Environment.ProcessPath!;

        _fileServiceMock.Setup(f => f.CopyFile(executablePath, destinationPath, true))
            .Throws<IOException>();

        using (TestCorrelator.CreateContext())
        {
            Assert.Throws<IOException>(() => _instanceManagerService.StartNewInstance(destinationPath, _launchModeMock.Object, startInfo));

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
            Assert.Throws<Exception>(() => _instanceManagerService.StartNewInstance(null, _launchModeMock.Object, startInfo, options));

            var logEvents = TestCorrelator.GetLogEventsFromCurrentContext().ToList();
            Assert.Contains(logEvents, e => e.MessageTemplate.Text.Contains("Error starting a new instance"));
        }
    }

    #endregion
}
