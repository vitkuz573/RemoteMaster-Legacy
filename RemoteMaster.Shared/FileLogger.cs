// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using RemoteMaster.Shared.Extensions;

namespace RemoteMaster.Shared;

public class FileLogger : ILogger
{
    private static readonly ConcurrentQueue<string> _logQueue = new();
    private static readonly ConcurrentStack<string> _scopeStack = new();
    private static readonly SemaphoreSlim _writeLock = new(1, 1);
    private static string? _logDir;
    private readonly string _applicationName;
    private readonly string _categoryName;
    private readonly System.Timers.Timer _sinkTimer = new(5000) { AutoReset = false };

    public FileLogger(string applicationName, string categoryName)
    {
        _applicationName = applicationName?.SanitizeFileName() ?? string.Empty;
        _categoryName = categoryName;
        _sinkTimer.Elapsed += SinkTimer_Elapsed;
    }

    private static string LogDirectory
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(_logDir))
            {
                return _logDir;
            }

            _logDir = OperatingSystem.IsWindows()
                ? Directory.CreateDirectory(@"C:\sc\Logs").FullName
                : Directory.CreateDirectory("/var/log/rcontrol").FullName;

            return _logDir;
        }
    }

    private string LogPath => Path.Combine(LogDirectory, $"LogFile_{_applicationName}_{DateTime.Now:yyyy-MM-dd}.log");

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        _scopeStack.Push($"{state}");

        return new NoopDisposable();
    }

    public void DeleteLogs()
    {
        try
        {
            _writeLock.Wait();

            if (File.Exists(LogPath))
            {
                File.Delete(LogPath);
            }
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        var isDebug = false;

#if DEBUG
        isDebug = true;
#endif

        return logLevel switch
        {
            LogLevel.Trace or LogLevel.Debug => isDebug,
            LogLevel.Information or LogLevel.Warning or LogLevel.Error or LogLevel.Critical => true,
            _ => false,
        };
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception, string> formatter)
    {
        try
        {
            var scopeStack = _scopeStack.Any() ? new string[] { _scopeStack.First(), _scopeStack.Last() } : Array.Empty<string>();

            var message = FormatLogEntry(logLevel, _categoryName, $"{state}", exception, scopeStack);
            _logQueue.Enqueue(message);
            _sinkTimer.Start();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error queueing log entry: {ex.Message}");
        }
    }

    public async Task<byte[]> ReadAllBytes()
    {
        try
        {
            await _writeLock.WaitAsync();
            CheckLogFileExists();

            return await File.ReadAllBytesAsync(LogPath);
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error while reading all bytes from logs.");

            return Array.Empty<byte>();
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private void CheckLogFileExists()
    {
        _ = Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);

        if (File.Exists(LogPath))
        {
            var fi = new FileInfo(LogPath);

            while (fi.Length > 1_000_000)
            {
                var content = File.ReadAllLines(LogPath);
                File.WriteAllLines(LogPath, content.Skip(10));
                fi = new FileInfo(LogPath);
            }
        }
    }

    private static string FormatLogEntry(LogLevel logLevel, string categoryName, string state, Exception? exception, string[] scopeStack)
    {
        var exMessage = new StringBuilder(exception?.Message ?? "");

        var ex = exception;
        while (ex?.InnerException is not null)
        {
            exMessage.Append($" | {ex.InnerException.Message}");
            ex = ex.InnerException;
        }

        var entry = new StringBuilder()
            .Append($"[{logLevel}]\t")
            .Append($"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff}\t");

        if (scopeStack.Any())
        {
            entry.Append($"[{string.Join(" - ", scopeStack)} - {categoryName}]\t");
        }
        else
        {
            entry.Append($"[{categoryName}]\t");
        }

        entry.Append($"Message: {state}\t");

        if (!string.IsNullOrWhiteSpace(exMessage.ToString()))
        {
            entry.Append(exMessage);
        }

        if (exception is not null)
        {
            entry.Append($"{Environment.NewLine}{exception.StackTrace}");
        }

        entry.Append(Environment.NewLine);

        return entry.ToString();
    }

    private async void SinkTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        try
        {
            await _writeLock.WaitAsync();
            CheckLogFileExists();

            var message = new StringBuilder();

            while (_logQueue.TryDequeue(out var entry))
            {
                message.Append(entry);
            }

            File.AppendAllText(LogPath, message.ToString());
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error writing log entry: {ex.Message}");
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private class NoopDisposable : IDisposable
    {
        public void Dispose()
        {
            _scopeStack.TryPop(out _);
        }
    }
}
