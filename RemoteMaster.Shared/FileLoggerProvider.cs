using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace RemoteMaster.Shared;

public class FileLoggerProvider : ILoggerProvider, IDisposable
{
    private readonly string _applicationName;
    private readonly ConcurrentDictionary<string, FileLogger> _loggers = new();

    public FileLoggerProvider(string applicationName)
    {
        _applicationName = applicationName;
    }

    public ILogger CreateLogger(string categoryName) => _loggers.GetOrAdd(categoryName, name => new FileLogger(_applicationName, name));

    public void Dispose()
    {
        _loggers.Clear();
        GC.SuppressFinalize(this);
    }
}
