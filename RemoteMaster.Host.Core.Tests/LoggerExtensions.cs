// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.Logging;
using Moq;

namespace RemoteMaster.Host.Core.Tests;

public static class LoggerExtensions
{
    public static void VerifyLog<T>(this Mock<ILogger<T>> loggerMock, LogLevel logLevel, string expectedMessage, Times times)
    {
        ArgumentNullException.ThrowIfNull(loggerMock);

        loggerMock.Verify(
            x => x.Log(
                logLevel,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessage)),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            times);
    }
}
