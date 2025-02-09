// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Linux.Abstractions;
using Tmds.DBus;

namespace RemoteMaster.Host.Linux.Services;

public class DBusProcess(INativeProcessOptions processOptions, IProcessService processService, ICommandLineProvider commandLineProvider) : IProcess
{
    private Connection? _connection;
    private ObjectPath _unitJobPath;
    private ObjectPath _unitPath;
    private IProcess? _attachedProcess;

    public int Id { get; private set; }

    public int ExitCode { get; private set; }

    public int SessionId { get; private set; }

    public StreamWriter StandardInput => throw new NotImplementedException();

    public StreamReader StandardOutput => throw new NotImplementedException();

    public StreamReader StandardError => throw new NotImplementedException();

    public ProcessModule? MainModule { get; private set; }

    public string ProcessName { get; private set; }

    public long WorkingSet64 { get; private set; }

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
        var unitName = $"dbusprocess-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.service";

        var execCommand = startInfo.FileName;

        startInfo.ArgumentList.Insert(0, execCommand);
        var execArgs = startInfo.ArgumentList.ToArray();

        _connection = new Connection(Address.System);

        await _connection.ConnectAsync();

        var manager = _connection.CreateProxy<ISystemdManager>("org.freedesktop.systemd1", new ObjectPath("/org/freedesktop/systemd1"));

        var properties = new (string, object)[]
        {
            ("Description", "Transient unit launched via DBusProcess"),
            ("ExecStart", new (string, string[], bool)[] { (execCommand, execArgs, false) }),
            ("Restart", "no")
        };

        var aux = Array.Empty<(string, (string, object)[])>();

        var job = await manager.StartTransientUnitAsync(unitName, "replace", properties, aux);

        _unitJobPath = job;

        var unitBasePath = "/org/freedesktop/systemd1/unit";
        var unitPathName = unitName.ToLower()
            .Replace("-", "_2d")
            .Replace(".", "_2e");

        _unitPath = new ObjectPath($"{unitBasePath}/{unitPathName}");

        await Task.Delay(5000);

        var unitProxy = _connection.CreateProxy<IUnit>("org.freedesktop.systemd1", _unitPath);

        var unitProps = await unitProxy.GetAllAsync();
        
        Id = (int)unitProps.MainPID;

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
