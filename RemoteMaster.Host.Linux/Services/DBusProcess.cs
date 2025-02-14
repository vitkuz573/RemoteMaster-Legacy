// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Linux.Abstractions;
using RemoteMaster.Host.Linux.Extensions;
using Tmds.DBus;

namespace RemoteMaster.Host.Linux.Services;

public class DBusProcess(INativeProcessOptions processOptions, IProcessService processService, ICommandLineProvider commandLineProvider) : IProcess
{
    private Connection? _connection;
    private IProcess? _attachedProcess;

    public int Id { get; private set; }

    public int ExitCode => _attachedProcess?.ExitCode ?? 0;

    public int SessionId => _attachedProcess?.SessionId ?? 0;

    public StreamWriter StandardInput => _attachedProcess?.StandardInput ?? throw new InvalidOperationException("The process standard input is not available.");

    public StreamReader StandardOutput => _attachedProcess?.StandardOutput ?? throw new InvalidOperationException("The process standard output is not available.");

    public StreamReader StandardError => _attachedProcess?.StandardError ?? throw new InvalidOperationException("The process standard error is not available.");

    public ProcessModule? MainModule => _attachedProcess?.MainModule;

    public string ProcessName => _attachedProcess?.ProcessName ?? string.Empty;

    public long WorkingSet64 => _attachedProcess?.WorkingSet64 ?? 0;

    public bool HasExited
    {
        get
        {
            try
            {
                var proc = processService.GetProcessById(Id);

                return proc.HasExited;
            }
            catch
            {
                return true;
            }
        }
    }

    public void Start(ProcessStartInfo startInfo)
    {
        ArgumentNullException.ThrowIfNull(startInfo);

        StartAsync(startInfo).GetAwaiter().GetResult();
    }

    private async Task StartAsync(ProcessStartInfo startInfo)
    {
        var serviceName = $"dbusprocess-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.service";

        var execCommand = startInfo.FileName;

        startInfo.ArgumentList.Insert(0, execCommand);
        var execArgs = startInfo.ArgumentList.ToArray();

        _connection = new Connection(Address.System);

        await _connection.ConnectAsync();

        var manager = _connection.CreateProxy<ISystemdManager>("org.freedesktop.systemd1", "/org/freedesktop/systemd1");

        var properties = new (string, object)[]
        {
            ("Description", "Transient unit launched via DBusProcess"),
            ("ExecStart", new (string, string[], bool)[] { (execCommand, execArgs, false) }),
            ("Restart", "no")
        };

        var aux = Array.Empty<(string, (string, object)[])>();

        await manager.StartTransientUnitAsync(serviceName, "replace", properties, aux);

        var servicePathName = serviceName.ToLower()
            .Replace("-", "_2d")
            .Replace(".", "_2e");

        await Task.Delay(5000);

        var serviceProxy = _connection.CreateProxy<ISystemdService>("org.freedesktop.systemd1", $"/org/freedesktop/systemd1/unit/{servicePathName}");

        Id = (int)await serviceProxy.GetMainPIDAsync();

        try
        {
            _attachedProcess = processService.GetProcessById(Id);
        }
        catch
        {
            _attachedProcess = null;
        }
    }

    public void Kill()
    {
        try
        {
            if (_attachedProcess is { HasExited: false })
            {
                _attachedProcess.Kill();
            }
            else
            {
                var proc = processService.GetProcessById(Id);
                proc.Kill();
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to kill the process.", ex);
        }
    }

    public string[] GetCommandLine()
    {
        return commandLineProvider.GetCommandLine(this);
    }

    public bool WaitForExit(uint millisecondsTimeout = uint.MaxValue)
    {
        try
        {
            var proc = processService.GetProcessById(Id);

            return proc.WaitForExit(millisecondsTimeout);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed while waiting for process exit.", ex);
        }
    }

    public void Dispose()
    {
        _connection?.Dispose();
        _attachedProcess?.Dispose();
    }
}
