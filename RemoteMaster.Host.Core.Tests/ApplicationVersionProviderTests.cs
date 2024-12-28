// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Reflection;
using Moq;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Services;

namespace RemoteMaster.Host.Core.Tests;

public class ApplicationVersionProviderTests
{
    private readonly Mock<IAssemblyProvider> _assemblyProviderMock;
    private readonly Mock<IFileVersionInfoProvider> _fileVersionInfoProviderMock;
    private readonly Mock<IAssemblyAttributeProvider> _assemblyAttributeProviderMock;
    private readonly ApplicationVersionProvider _versionProvider;

    public ApplicationVersionProviderTests()
    {
        _assemblyProviderMock = new Mock<IAssemblyProvider>();
        _fileVersionInfoProviderMock = new Mock<IFileVersionInfoProvider>();
        _assemblyAttributeProviderMock = new Mock<IAssemblyAttributeProvider>();
        _versionProvider = new ApplicationVersionProvider(
            _assemblyProviderMock.Object,
            _fileVersionInfoProviderMock.Object,
            _assemblyAttributeProviderMock.Object);
    }

    #region GetVersionFromAssembly Tests

    [Fact]
    public void GetVersionFromAssembly_NoAssemblyName_ReturnsEntryAssemblyVersion()
    {
        // Arrange
        var mockAssembly = new Mock<Assembly>();
        const string assemblyVersion = "1.2.3.4";
        var mockAttribute = new AssemblyFileVersionAttribute(assemblyVersion);

        _assemblyAttributeProviderMock.Setup(ap => ap.GetCustomAttribute<AssemblyFileVersionAttribute>(mockAssembly.Object))
                                     .Returns(mockAttribute);

        _assemblyProviderMock.Setup(ap => ap.GetEntryAssembly())
                             .Returns(mockAssembly.Object);

        var expectedVersion = new Version(1, 2, 3, 4);

        // Act
        var actualVersion = _versionProvider.GetVersionFromAssembly();

        // Assert
        Assert.Equal(expectedVersion, actualVersion);
        _assemblyProviderMock.Verify(ap => ap.GetEntryAssembly(), Times.Once);
        _assemblyAttributeProviderMock.Verify(ap => ap.GetCustomAttribute<AssemblyFileVersionAttribute>(mockAssembly.Object), Times.Once);
    }

    [Fact]
    public void GetVersionFromAssembly_NoAssemblyName_EntryAssemblyIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        _assemblyProviderMock.Setup(ap => ap.GetEntryAssembly())
                             .Returns((Assembly?)null);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => _versionProvider.GetVersionFromAssembly());
        Assert.Equal("Failed to retrieve the entry assembly.", exception.Message);
        _assemblyProviderMock.Verify(ap => ap.GetEntryAssembly(), Times.Once);
    }

    [Theory]
    [InlineData("System.Runtime", "2.1.0.0")]
    [InlineData("mscorlib", "4.0.0.0")]
    public void GetVersionFromAssembly_WithValidAssemblyName_ReturnsCorrectVersion(string assemblyName, string fileVersion)
    {
        // Arrange
        var mockAssembly = new Mock<Assembly>();
        var mockAttribute = new AssemblyFileVersionAttribute(fileVersion);

        _assemblyAttributeProviderMock.Setup(ap => ap.GetCustomAttribute<AssemblyFileVersionAttribute>(mockAssembly.Object))
                                     .Returns(mockAttribute);

        _assemblyProviderMock.Setup(ap => ap.GetAssemblyByName(assemblyName))
                             .Returns(mockAssembly.Object);

        var expectedVersion = Version.Parse(fileVersion);

        // Act
        var actualVersion = _versionProvider.GetVersionFromAssembly(assemblyName);

        // Assert
        Assert.Equal(expectedVersion, actualVersion);
        _assemblyProviderMock.Verify(ap => ap.GetAssemblyByName(assemblyName), Times.Once);
        _assemblyAttributeProviderMock.Verify(ap => ap.GetCustomAttribute<AssemblyFileVersionAttribute>(mockAssembly.Object), Times.Once);
    }

    [Fact]
    public void GetVersionFromAssembly_WithInvalidAssemblyName_ThrowsArgumentException()
    {
        // Arrange
        const string invalidAssemblyName = "NonExistentAssembly";

        _assemblyProviderMock.Setup(ap => ap.GetAssemblyByName(invalidAssemblyName))
                             .Returns((Assembly?)null);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _versionProvider.GetVersionFromAssembly(invalidAssemblyName));
        Assert.Contains("not found", exception.Message);
        Assert.Equal("assemblyName", exception.ParamName);
        _assemblyProviderMock.Verify(ap => ap.GetAssemblyByName(invalidAssemblyName), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GetVersionFromAssembly_WithEmptyOrWhitespaceAssemblyName_ReturnsEntryAssemblyVersion(string assemblyName)
    {
        // Arrange
        var mockAssembly = new Mock<Assembly>();
        const string assemblyVersion = "5.6.7.8";
        var mockAttribute = new AssemblyFileVersionAttribute(assemblyVersion);

        _assemblyAttributeProviderMock.Setup(ap => ap.GetCustomAttribute<AssemblyFileVersionAttribute>(mockAssembly.Object))
                                     .Returns(mockAttribute);

        _assemblyProviderMock.Setup(ap => ap.GetEntryAssembly())
                              .Returns(mockAssembly.Object);

        var expectedVersion = new Version(5, 6, 7, 8);

        // Act
        var actualVersion = _versionProvider.GetVersionFromAssembly(assemblyName);

        // Assert
        Assert.Equal(expectedVersion, actualVersion);
        _assemblyProviderMock.Verify(ap => ap.GetEntryAssembly(), Times.Once);
        _assemblyAttributeProviderMock.Verify(ap => ap.GetCustomAttribute<AssemblyFileVersionAttribute>(mockAssembly.Object), Times.Once);
    }

    [Fact]
    public void GetVersionFromAssembly_WithAssemblyName_CaseInsensitive()
    {
        // Arrange
        const string originalAssemblyName = "TestAssembly";
        var upperCaseAssemblyName = originalAssemblyName.ToUpperInvariant();
        var mockAssembly = new Mock<Assembly>();
        const string assemblyVersion = "9.8.7.6";
        var mockAttribute = new AssemblyFileVersionAttribute(assemblyVersion);

        _assemblyAttributeProviderMock.Setup(ap => ap.GetCustomAttribute<AssemblyFileVersionAttribute>(mockAssembly.Object))
                                     .Returns(mockAttribute);

        _assemblyProviderMock.Setup(ap => ap.GetAssemblyByName(upperCaseAssemblyName))
                              .Returns(mockAssembly.Object);

        var expectedVersion = new Version(9, 8, 7, 6);

        // Act
        var actualVersion = _versionProvider.GetVersionFromAssembly(upperCaseAssemblyName);

        // Assert
        Assert.Equal(expectedVersion, actualVersion);
        _assemblyProviderMock.Verify(ap => ap.GetAssemblyByName(upperCaseAssemblyName), Times.Once);
        _assemblyAttributeProviderMock.Verify(ap => ap.GetCustomAttribute<AssemblyFileVersionAttribute>(mockAssembly.Object), Times.Once);
    }

    [Fact]
    public void GetVersionFromAssembly_AssemblyHasNoFileVersion_ReturnsZeroVersion()
    {
        // Arrange
        const string assemblyName = "AssemblyWithoutFileVersion";
        var mockAssembly = new Mock<Assembly>();

        _assemblyAttributeProviderMock.Setup(ap => ap.GetCustomAttribute<AssemblyFileVersionAttribute>(mockAssembly.Object))
                                     .Returns((AssemblyFileVersionAttribute?)null);

        _assemblyProviderMock.Setup(ap => ap.GetAssemblyByName(assemblyName))
                              .Returns(mockAssembly.Object);

        var expectedVersion = new Version(0, 0, 0, 0);

        // Act
        var actualVersion = _versionProvider.GetVersionFromAssembly(assemblyName);

        // Assert
        Assert.Equal(expectedVersion, actualVersion);
        _assemblyProviderMock.Verify(ap => ap.GetAssemblyByName(assemblyName), Times.Once);
        _assemblyAttributeProviderMock.Verify(ap => ap.GetCustomAttribute<AssemblyFileVersionAttribute>(mockAssembly.Object), Times.Once);
    }

    [Fact]
    public void GetVersionFromAssembly_AssemblyHasInvalidFileVersion_ReturnsZeroVersion()
    {
        // Arrange
        const string assemblyName = "AssemblyWithInvalidFileVersion";
        const string invalidFileVersion = "invalid_version_string";
        var mockAssembly = new Mock<Assembly>();
        var mockAttribute = new AssemblyFileVersionAttribute(invalidFileVersion);

        _assemblyAttributeProviderMock.Setup(ap => ap.GetCustomAttribute<AssemblyFileVersionAttribute>(mockAssembly.Object))
                                     .Returns(mockAttribute);

        _assemblyProviderMock.Setup(ap => ap.GetAssemblyByName(assemblyName))
                              .Returns(mockAssembly.Object);

        var expectedVersion = new Version(0, 0, 0, 0);

        // Act
        var actualVersion = _versionProvider.GetVersionFromAssembly(assemblyName);

        // Assert
        Assert.Equal(expectedVersion, actualVersion);
        _assemblyProviderMock.Verify(ap => ap.GetAssemblyByName(assemblyName), Times.Once);
        _assemblyAttributeProviderMock.Verify(ap => ap.GetCustomAttribute<AssemblyFileVersionAttribute>(mockAssembly.Object), Times.Once);
    }

    #endregion

    #region GetVersionFromExecutable Tests

    [Fact]
    public void GetVersionFromExecutable_ValidExecutable_ReturnsCorrectVersion()
    {
        // Arrange
        const string executablePath = "C:\\Program Files\\TestApp\\TestApp.exe";
        const string fileVersion = "5.6.7.8";

        _fileVersionInfoProviderMock.Setup(fv => fv.FileExists(executablePath))
                                    .Returns(true);
        _fileVersionInfoProviderMock.Setup(fv => fv.GetFileVersion(executablePath))
                                    .Returns(fileVersion);

        var expectedVersion = new Version(5, 6, 7, 8);

        // Act
        var actualVersion = _versionProvider.GetVersionFromExecutable(executablePath);

        // Assert
        Assert.Equal(expectedVersion, actualVersion);
        _fileVersionInfoProviderMock.Verify(fv => fv.FileExists(executablePath), Times.Once);
        _fileVersionInfoProviderMock.Verify(fv => fv.GetFileVersion(executablePath), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetVersionFromExecutable_InvalidPath_ThrowsArgumentException(string executablePath)
    {
        // Arrange, Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _versionProvider.GetVersionFromExecutable(executablePath));
        Assert.Contains("cannot be null or empty", exception.Message);
        Assert.Equal("executablePath", exception.ParamName);
        _fileVersionInfoProviderMock.Verify(fv => fv.FileExists(It.IsAny<string>()), Times.Never);
        _fileVersionInfoProviderMock.Verify(fv => fv.GetFileVersion(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void GetVersionFromExecutable_FileDoesNotExist_ThrowsFileNotFoundException()
    {
        // Arrange
        const string nonExistentPath = "C:\\NonExistent\\nonexistent.exe";

        _fileVersionInfoProviderMock.Setup(fv => fv.FileExists(nonExistentPath))
                                    .Returns(false);

        // Act & Assert
        var exception = Assert.Throws<FileNotFoundException>(() => _versionProvider.GetVersionFromExecutable(nonExistentPath));
        Assert.Contains("Executable file not found", exception.Message);
        Assert.Equal(nonExistentPath, exception.FileName);
        _fileVersionInfoProviderMock.Verify(fv => fv.FileExists(nonExistentPath), Times.Once);
        _fileVersionInfoProviderMock.Verify(fv => fv.GetFileVersion(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void GetVersionFromExecutable_FileHasNoFileVersion_ReturnsZeroVersion()
    {
        // Arrange
        const string executablePath = "C:\\Program Files\\TestApp\\Empty.exe";
        var fileVersion = string.Empty; // Simulate no file version

        _fileVersionInfoProviderMock.Setup(fv => fv.FileExists(executablePath))
                                    .Returns(true);
        _fileVersionInfoProviderMock.Setup(fv => fv.GetFileVersion(executablePath))
                                    .Returns(fileVersion);

        var expectedVersion = new Version(0, 0, 0, 0);

        // Act
        var actualVersion = _versionProvider.GetVersionFromExecutable(executablePath);

        // Assert
        Assert.Equal(expectedVersion, actualVersion);
        _fileVersionInfoProviderMock.Verify(fv => fv.FileExists(executablePath), Times.Once);
        _fileVersionInfoProviderMock.Verify(fv => fv.GetFileVersion(executablePath), Times.Once);
    }

    [Fact]
    public void GetVersionFromExecutable_FileHasInvalidFileVersion_ReturnsZeroVersion()
    {
        // Arrange
        const string executablePath = "C:\\Program Files\\TestApp\\InvalidVersion.exe";
        const string fileVersion = "invalid_version_string"; // Simulate invalid file version

        _fileVersionInfoProviderMock.Setup(fv => fv.FileExists(executablePath))
                                    .Returns(true);
        _fileVersionInfoProviderMock.Setup(fv => fv.GetFileVersion(executablePath))
                                    .Returns(fileVersion);

        var expectedVersion = new Version(0, 0, 0, 0);

        // Act
        var actualVersion = _versionProvider.GetVersionFromExecutable(executablePath);

        // Assert
        Assert.Equal(expectedVersion, actualVersion);
        _fileVersionInfoProviderMock.Verify(fv => fv.FileExists(executablePath), Times.Once);
        _fileVersionInfoProviderMock.Verify(fv => fv.GetFileVersion(executablePath), Times.Once);
    }

    #endregion
}
