// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.Logging;

namespace RemoteMaster.Server.Tests;

public class LogEntry
{
    public LogLevel LogLevel { get; set; }
    
    public EventId EventId { get; set; }

    public IReadOnlyList<KeyValuePair<string, object?>> State { get; set; } = [];

    public Exception? Exception { get; set; }
    
    public string Message { get; set; } = string.Empty;
}
