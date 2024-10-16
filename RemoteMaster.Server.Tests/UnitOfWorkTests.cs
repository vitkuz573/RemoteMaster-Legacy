// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using RemoteMaster.Server.UnitOfWork;

namespace RemoteMaster.Server.Tests;

public class UnitOfWorkTests
{
    private readonly Mock<DbContext> _dbContextMock;
    private readonly Mock<ILogger<UnitOfWork<DbContext>>> _loggerMock;
    private readonly UnitOfWork<DbContext> _unitOfWork;

    public UnitOfWorkTests()
    {
        _dbContextMock = new Mock<DbContext>();
        Mock<IDbContextTransaction> dbContextTransactionMock = new();
        _loggerMock = new Mock<ILogger<UnitOfWork<DbContext>>>();

        var databaseMock = new Mock<DatabaseFacade>(_dbContextMock.Object);
        databaseMock.Setup(db => db.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(dbContextTransactionMock.Object);

        _dbContextMock.Setup(db => db.Database).Returns(databaseMock.Object);

        _unitOfWork = new UnitOfWork<DbContext>(_dbContextMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task BeginTransactionAsync_Should_StartTransactionSuccessfully()
    {
        // Act
        await _unitOfWork.BeginTransactionAsync();

        // Assert
        _dbContextMock.Verify(db => db.Database.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);

        _loggerMock.Verify(
            log => log.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Beginning transaction...")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task CommitTransactionAsync_Should_CommitTransactionSuccessfully()
    {
        // Arrange
        Mock<IDbContextTransaction> dbContextTransactionMock = new();
        _dbContextMock.Setup(db => db.Database.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(dbContextTransactionMock.Object);

        await _unitOfWork.BeginTransactionAsync();

        // Act
        await _unitOfWork.CommitTransactionAsync();

        // Assert
        dbContextTransactionMock.Verify(tx => tx.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);

        _loggerMock.Verify(
            log => log.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Committing transaction")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task RollbackTransactionAsync_Should_RollbackTransactionSuccessfully()
    {
        // Arrange
        Mock<IDbContextTransaction> dbContextTransactionMock = new();
        _dbContextMock.Setup(db => db.Database.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(dbContextTransactionMock.Object);

        await _unitOfWork.BeginTransactionAsync();

        // Act
        await _unitOfWork.RollbackTransactionAsync();

        // Assert
        dbContextTransactionMock.Verify(tx => tx.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);

        _loggerMock.Verify(
            log => log.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Rolling back transaction")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_Should_DisposeResourcesSuccessfully()
    {
        // Act
        await _unitOfWork.DisposeAsync();

        // Assert
        _dbContextMock.Verify(db => db.DisposeAsync(), Times.Once);

        _loggerMock.Verify(
            log => log.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Context and transaction disposed asynchronously.")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public void Dispose_Should_DisposeResourcesSuccessfully()
    {
        // Act
        _unitOfWork.Dispose();

        // Assert
        _dbContextMock.Verify(db => db.Dispose(), Times.Once);

        _loggerMock.Verify(
            log => log.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Context and transaction disposed.")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }
}
