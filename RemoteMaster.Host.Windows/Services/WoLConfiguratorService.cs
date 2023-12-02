// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using System.Management;
using Microsoft.Win32;
using RemoteMaster.Host.Core.Abstractions;
using Serilog;

namespace RemoteMaster.Host.Windows.Services;

public class WoLConfiguratorService : IWoLConfiguratorService
{
    private const int AllowToTurnOff = 0x18;

    public void Configure()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(@"SELECT * FROM Win32_NetworkAdapter WHERE NetEnabled = TRUE");

            foreach (var networkAdapter in searcher.Get().Cast<ManagementObject>())
            {
                var connectionId = (string)networkAdapter["NetConnectionID"];

                if (connectionId != null)
                {
                    Log.Information("Enabling WoL for {ConnectionId}...", connectionId);
                    EnableWoLForAdapter(connectionId);
                }
            }

            DisablePowerManagementForAllAdapters();
        }
        catch (Exception ex)
        {
            Log.Error("Failed to enable WoL: {Message}", ex.Message);
        }
    }

    private static void EnableWoLForAdapter(string connectionId)
    {
        try
        {
            var command = $"powercfg /deviceenablewake \"{connectionId}\"";
            ExecuteCommand(command);
        }
        catch (Exception ex)
        {
            Log.Error("Failed to enable WoL for {ConnectionId}: {Message}", connectionId, ex.Message);
        }
    }

    private static void ExecuteCommand(string command)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {command}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();

        process.WaitForExit();

        if (!string.IsNullOrEmpty(output))
        {
            Log.Information("{Output}", output);
        }

        if (!string.IsNullOrEmpty(error))
        {
            Log.Error("Error: {Error}", error);
        }
    }

    private static void DisablePowerManagementForAllAdapters()
    {
        try
        {
            var registryPath = @"SYSTEM\CurrentControlSet\Control\Class\{4D36E972-E325-11CE-BFC1-08002bE10318}";
            using var key = Registry.LocalMachine.OpenSubKey(registryPath, true);

            if (key == null)
            {
                Log.Error("Failed to open registry path: {Path}", registryPath);
                return;
            }

            foreach (var subKeyName in key.GetSubKeyNames())
            {
                try
                {
                    using var subKey = key.OpenSubKey(subKeyName, true);

                    if (subKey == null)
                    {
                        continue;
                    }

                    if (subKey.GetValue("PnPCapabilities") != null)
                    {
                        var currentValue = (int?)subKey.GetValue("PnPCapabilities");
                        Log.Information("Current PnPCapabilities for adapter {Adapter}: {Value}", subKeyName, currentValue);

                        subKey.SetValue("PnPCapabilities", AllowToTurnOff);
                        Log.Information("Set PnPCapabilities for adapter {Adapter} to {Value}", subKeyName, AllowToTurnOff);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Failed to disable power management for adapter {Adapter}: {Message}", subKeyName, ex.Message);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error("Failed to disable power management for network adapters: {Message}", ex.Message);
        }
    }
}
