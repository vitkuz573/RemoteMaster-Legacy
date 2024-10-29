// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.Logging;

namespace RemoteMaster.Server.Tests;

public class TestLogger<T> : ILogger<T>
{
    public List<LogEntry> LogEntries { get; } = [];

    public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        if (state is IReadOnlyList<KeyValuePair<string, object?>> stateProperties)
        {
            LogEntries.Add(new LogEntry
            {
                LogLevel = logLevel,
                EventId = eventId,
                State = stateProperties,
                Exception = exception,
                Message = formatter(state, exception),
            });
        }
        else
        {
            LogEntries.Add(new LogEntry
            {
                LogLevel = logLevel,
                EventId = eventId,
                State = [],
                Exception = exception,
                Message = formatter(state, exception),
            });
        }
    }
}
